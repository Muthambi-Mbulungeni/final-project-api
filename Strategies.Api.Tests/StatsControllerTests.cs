using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
    /// Integration tests for /api/stats over HTTP, run against the real pipeline in a TestServer with an
    /// in-memory database (same pattern as AuthControllerTests): role access and what each count includes.
    /// </summary>
    public class StatsControllerTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _http;

        private readonly Guid _admin = Guid.NewGuid();
        private readonly Guid _consultant = Guid.NewGuid();
        private readonly Guid _otherConsultant = Guid.NewGuid();
        private readonly Guid _client = Guid.NewGuid();
        private readonly Guid _otherClient = Guid.NewGuid();

        public StatsControllerTests()
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

        private async Task SeedAsync(Action<ApplicationDbContext> seed)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seed(db);
            await db.SaveChangesAsync();
        }

        private static User NewUser(bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            FullName = "Seeded User",
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "not-used",
            Role = UserRole.Client,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        private static Lead NewLead(TriageStatus status) => new()
        {
            Id = Guid.NewGuid(),
            FullName = "Seeded Lead",
            Email = "lead@example.com",
            TriageStatus = status,
            DesiredStartDate = DateTime.UtcNow.AddMonths(3),
            SubmittedAt = DateTime.UtcNow
        };

        private static Case NewCase(Guid clientId, Guid? consultantId, CaseStage stage = CaseStage.DocumentGathering,
            bool isArchived = false, int daysAgo = 0) => new()
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ConsultantId = consultantId,
            VisaCategoryId = Guid.NewGuid(),
            Stage = stage,
            IsArchived = isArchived,
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo),
            UpdatedAt = DateTime.UtcNow
        };

        private static Document NewDocument(Guid caseId) => new()
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            DocumentTypeId = Guid.NewGuid(),
            FileName = "passport.pdf",
            FilePath = "uploads/passport.pdf",
            Status = DocumentStatus.Pending,
            UploadedAt = DateTime.UtcNow
        };

        private async Task<HttpResponseMessage> Get(string path, string? bearer)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _http.SendAsync(request);
        }

        private async Task<JsonElement> GetOkAsync(string path, string bearer)
        {
            var response = await Get(path, bearer);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        }

        // ── Access ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData("/api/stats/admin")]
        [InlineData("/api/stats/consultant")]
        public async Task WithoutAToken_Returns401(string path)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(path, bearer: null)).StatusCode);
        }

        [Fact]
        public async Task ClientStats_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Get($"/api/stats/client/{_client}", bearer: null)).StatusCode);
        }

        [Theory]
        [InlineData(UserRole.Consultant)]
        [InlineData(UserRole.Client)]
        public async Task AdminStats_ForOtherRoles_Returns403(UserRole role)
        {
            var response = await Get("/api/stats/admin", TokenFor(Guid.NewGuid(), role));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ConsultantStats_ForAClient_Returns403()
        {
            var response = await Get("/api/stats/consultant", TokenFor(_client, UserRole.Client));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ClientStats_ForAnotherClient_Returns403()
        {
            var response = await Get($"/api/stats/client/{_otherClient}", TokenFor(_client, UserRole.Client));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── GET /api/stats/admin ───────────────────────────────────────────

        [Fact]
        public async Task AdminStats_CountActiveUsers_UnarchivedCases_AndLeadsByVerdict()
        {
            await SeedAsync(db =>
            {
                db.Users.AddRange(NewUser(), NewUser(), NewUser(isActive: false));
                db.Leads.AddRange(
                    NewLead(TriageStatus.Qualified), NewLead(TriageStatus.Qualified),
                    NewLead(TriageStatus.Unqualified),
                    NewLead(TriageStatus.NeedsReview),
                    NewLead(TriageStatus.Pending));
                db.Cases.AddRange(
                    NewCase(_client, _consultant),
                    NewCase(_client, _consultant, CaseStage.FinalDecision),
                    NewCase(_otherClient, _otherConsultant, isArchived: true));
            });

            var stats = await GetOkAsync("/api/stats/admin", TokenFor(_admin, UserRole.Admin));

            Assert.Equal(2, stats.GetProperty("totalUsers").GetInt32());
            Assert.Equal(5, stats.GetProperty("totalLeads").GetInt32());
            Assert.Equal(2, stats.GetProperty("totalCases").GetInt32());
            Assert.Equal(1, stats.GetProperty("activeCases").GetInt32());
            Assert.Equal(2, stats.GetProperty("qualifiedLeads").GetInt32());
            Assert.Equal(1, stats.GetProperty("unqualifiedLeads").GetInt32());
            Assert.Equal(1, stats.GetProperty("needsReviewLeads").GetInt32());
        }

        // ── GET /api/stats/consultant ──────────────────────────────────────

        [Fact]
        public async Task ConsultantStats_CountOnlyTheirOwnUnarchivedCases_AndTheWholeLeadQueue()
        {
            await SeedAsync(db =>
            {
                db.Cases.AddRange(
                    NewCase(_client, _consultant),
                    NewCase(_client, _consultant, CaseStage.FinalDecision),
                    NewCase(_client, _consultant, isArchived: true),
                    NewCase(_otherClient, _otherConsultant));
                db.Leads.AddRange(
                    NewLead(TriageStatus.Pending), NewLead(TriageStatus.NeedsReview),
                    NewLead(TriageStatus.Qualified), NewLead(TriageStatus.Unqualified));
            });

            var stats = await GetOkAsync("/api/stats/consultant", TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(2, stats.GetProperty("myCases").GetInt32());
            Assert.Equal(1, stats.GetProperty("activeCases").GetInt32());
            Assert.Equal(2, stats.GetProperty("pendingLeads").GetInt32());
        }

        // ── GET /api/stats/client/{clientId} ───────────────────────────────

        [Fact]
        public async Task ClientStats_OwnStats_ShowTheNewestUnarchivedCase_AndItsDocuments()
        {
            var older = NewCase(_client, _consultant, CaseStage.DocumentGathering, daysAgo: 30);
            var newer = NewCase(_client, _consultant, CaseStage.UnderReview, daysAgo: 1);
            var archived = NewCase(_client, _consultant, CaseStage.SubmittedToAuthority, isArchived: true);
            await SeedAsync(db =>
            {
                db.Cases.AddRange(older, newer, archived);
                db.Documents.AddRange(NewDocument(newer.Id), NewDocument(newer.Id), NewDocument(older.Id), NewDocument(archived.Id));
            });

            var stats = await GetOkAsync($"/api/stats/client/{_client}", TokenFor(_client, UserRole.Client));

            Assert.True(stats.GetProperty("hasActiveCase").GetBoolean());
            Assert.Equal("UnderReview", stats.GetProperty("currentStage").GetString());
            Assert.Equal(2, stats.GetProperty("totalDocuments").GetInt32());
        }

        [Fact]
        public async Task ClientStats_WithNoCase_SayNone()
        {
            var stats = await GetOkAsync($"/api/stats/client/{_client}", TokenFor(_client, UserRole.Client));

            Assert.False(stats.GetProperty("hasActiveCase").GetBoolean());
            Assert.Equal("None", stats.GetProperty("currentStage").GetString());
            Assert.Equal(0, stats.GetProperty("totalDocuments").GetInt32());
        }

        [Fact]
        public async Task ClientStats_TheAssignedConsultant_CanRead()
        {
            await SeedAsync(db => db.Cases.Add(NewCase(_client, _consultant, CaseStage.UnderReview)));

            var stats = await GetOkAsync($"/api/stats/client/{_client}", TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal("UnderReview", stats.GetProperty("currentStage").GetString());
        }

        [Fact]
        public async Task ClientStats_AConsultantNotAssigned_Returns403()
        {
            await SeedAsync(db => db.Cases.Add(NewCase(_client, _otherConsultant)));

            var response = await Get($"/api/stats/client/{_client}", TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ClientStats_AConsultant_ForAClientWithNoCase_Returns403()
        {
            var response = await Get($"/api/stats/client/{_client}", TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ClientStats_AnAdmin_CanReadAnyClient()
        {
            await SeedAsync(db => db.Cases.Add(NewCase(_client, _consultant, CaseStage.InitialIntake)));

            var stats = await GetOkAsync($"/api/stats/client/{_client}", TokenFor(_admin, UserRole.Admin));

            Assert.Equal("InitialIntake", stats.GetProperty("currentStage").GetString());
        }
    }
}
