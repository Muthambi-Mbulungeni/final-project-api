using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// GET /api/leads and PATCH /api/leads/{id}/status over HTTP (same pattern as AuthControllerTests):
    /// roles, paging, search, the verdict filter, and the review through the real pipeline (route, JSON
    /// enum names, [Authorize]). LeadsStatusTests covers the review rules on the controller itself.
    /// </summary>
    public class LeadsControllerTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _http;
        private readonly Guid _consultant = Guid.NewGuid();

        public LeadsControllerTests()
        {
            _factory = new ApiFactory();
            _http = _factory.CreateClient();
        }

        public void Dispose()
        {
            _http.Dispose();
            _factory.Dispose();
        }

        private sealed class ApiFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");
                foreach (var (key, value) in StartupConfigurationTests.RequiredTestSettings)
                    builder.UseSetting(key, value);

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                    var dbName = Guid.NewGuid().ToString();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));
                });
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private string TokenFor(Guid userId, UserRole role)
        {
            var user = new User { Id = userId, FullName = "Test User", Email = "test@example.com", Role = role };
            return _factory.Services.GetRequiredService<ITokenService>().GenerateAccessToken(user).Token;
        }

        private async Task<Guid> AddLeadAsync(string name, string email, TriageStatus status, int hoursAgo = 0)
        {
            var lead = new Lead
            {
                Id = Guid.NewGuid(),
                FullName = name,
                Email = email,
                ImmigrationStatus = ImmigrationStatus.Foreigner,
                TriageStatus = status,
                DesiredStartDate = DateTime.UtcNow.AddMonths(3),
                SubmittedAt = DateTime.UtcNow.AddHours(-hoursAgo)
            };
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Leads.Add(lead);
            await db.SaveChangesAsync();
            return lead.Id;
        }

        private T Query<T>(Func<ApplicationDbContext, T> query)
        {
            using var scope = _factory.Services.CreateScope();
            return query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        }

        private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? bearer, string? json = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (json != null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _http.SendAsync(request);
        }

        private async Task<JsonElement> ListAsync(string query = "")
        {
            var response = await Send(HttpMethod.Get, "/api/leads" + query, TokenFor(_consultant, UserRole.Consultant));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        }

        private static string[] Names(JsonElement page) =>
            page.GetProperty("data").EnumerateArray().Select(l => l.GetProperty("fullName").GetString()!).ToArray();

        private Task<HttpResponseMessage> Review(Guid leadId, string json, string? bearer) =>
            Send(HttpMethod.Patch, $"/api/leads/{leadId}/status", bearer, json);

        // ── GET /api/leads ─────────────────────────────────────────────────

        [Fact]
        public async Task List_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send(HttpMethod.Get, "/api/leads", null)).StatusCode);
        }

        [Fact]
        public async Task List_ForAClient_Returns403()
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/api/leads", TokenFor(Guid.NewGuid(), UserRole.Client))).StatusCode);
        }

        [Fact]
        public async Task List_ForAnAdmin_Returns200()
        {
            Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/leads", TokenFor(Guid.NewGuid(), UserRole.Admin))).StatusCode);
        }

        [Fact]
        public async Task List_IsNewestFirst_AndPaged()
        {
            for (var i = 1; i <= 5; i++)
                await AddLeadAsync($"Lead {i}", $"lead{i}@example.com", TriageStatus.Pending, hoursAgo: 10 - i);

            var page2 = await ListAsync("?page=2&pageSize=2");

            Assert.Equal(new[] { "Lead 3", "Lead 2" }, Names(page2));
            Assert.Equal(5, page2.GetProperty("totalRecords").GetInt32());
            Assert.Equal(3, page2.GetProperty("totalPages").GetInt32());
            Assert.Equal(2, page2.GetProperty("currentPage").GetInt32());
        }

        [Fact]
        public async Task List_DefaultPageSize_Is10()
        {
            for (var i = 0; i < 11; i++)
                await AddLeadAsync($"Lead {i}", $"lead{i}@example.com", TriageStatus.Pending);

            var page = await ListAsync();

            Assert.Equal(10, page.GetProperty("data").GetArrayLength());
            Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
        }

        [Theory]
        [InlineData("Naledi", "Naledi Khumalo")]
        [InlineData("sarah.p@", "Sarah Pillay")]
        public async Task List_Search_MatchesNameOrEmail(string search, string expected)
        {
            await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.Qualified);
            await AddLeadAsync("Sarah Pillay", "sarah.p@example.com", TriageStatus.Unqualified);

            var page = await ListAsync($"?search={Uri.EscapeDataString(search)}");

            Assert.Equal(new[] { expected }, Names(page));
        }

        [Theory]
        [InlineData("Qualified")]
        [InlineData("qualified")]
        public async Task List_StatusFilter_IgnoresCase_AndReturnsTheVerdictAsAName(string status)
        {
            await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.Qualified);
            await AddLeadAsync("Sarah Pillay", "sarah.p@example.com", TriageStatus.Unqualified);

            var page = await ListAsync($"?status={status}");

            Assert.Equal(new[] { "Naledi Khumalo" }, Names(page));
            Assert.Equal("Qualified", page.GetProperty("data")[0].GetProperty("triageStatus").GetString());
        }

        [Fact]
        public async Task List_UnknownStatus_IsIgnored()
        {
            await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.Qualified);
            await AddLeadAsync("Sarah Pillay", "sarah.p@example.com", TriageStatus.Unqualified);

            var page = await ListAsync("?status=Maybe");

            Assert.Equal(2, page.GetProperty("totalRecords").GetInt32());
        }

        // ── PATCH /api/leads/{id}/status ───────────────────────────────────

        [Fact]
        public async Task Review_WithoutAToken_Returns401()
        {
            var id = await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.NeedsReview);

            Assert.Equal(HttpStatusCode.Unauthorized, (await Review(id, "{\"status\":\"Qualified\"}", null)).StatusCode);
        }

        [Fact]
        public async Task Review_ByAClient_Returns403_AndChangesNothing()
        {
            var id = await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.NeedsReview);

            var response = await Review(id, "{\"status\":\"Qualified\"}", TokenFor(Guid.NewGuid(), UserRole.Client));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(TriageStatus.NeedsReview, Query(db => db.Leads.Single().TriageStatus));
        }

        [Fact]
        public async Task Review_ByAConsultant_WithTheVerdictName_SetsTheVerdictAndTheReviewer()
        {
            var id = await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.NeedsReview);

            var response = await Review(id, "{\"status\":\"Qualified\"}", TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var lead = Query(db => db.Leads.Single());
            Assert.Equal(TriageStatus.Qualified, lead.TriageStatus);
            Assert.Equal(_consultant, lead.ReviewedById);
            var audit = Query(db => db.AuditLogs.Single(a => a.EntityId == id));
            Assert.Equal("Changed lead verdict from NeedsReview to Qualified", audit.Action);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"status\":\"Maybe\"}")]
        public async Task Review_MissingOrUnknownVerdict_Returns400_AndChangesNothing(string json)
        {
            var id = await AddLeadAsync("Naledi Khumalo", "naledi@example.com", TriageStatus.NeedsReview);

            var response = await Review(id, json, TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(TriageStatus.NeedsReview, Query(db => db.Leads.Single().TriageStatus));
            Assert.Equal(0, Query(db => db.AuditLogs.Count()));
        }

        [Fact]
        public async Task Review_UnknownLead_Returns404()
        {
            var response = await Review(Guid.NewGuid(), "{\"status\":\"Qualified\"}", TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
