using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
using Strategies.Api.DTOs;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// /api/cases create, detail and CSV export over HTTP (same pattern as AuthControllerTests).
    /// The list, assign and stage endpoints are covered by CaseListTests, CaseAssignTests and
    /// CasesControllerStageTests; the client case list by CasesClientAccessTests.
    /// </summary>
    public class CasesControllerTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _http;

        private readonly Guid _admin = Guid.NewGuid();
        private readonly Guid _consultant = Guid.NewGuid();
        private readonly Guid _otherConsultant = Guid.NewGuid();
        private readonly Guid _client = Guid.NewGuid();
        private readonly Guid _otherClient = Guid.NewGuid();
        private readonly Guid _inactiveClient = Guid.NewGuid();
        private readonly Guid _visa = Guid.NewGuid();
        private readonly Guid _closedVisa = Guid.NewGuid();

        public CasesControllerTests()
        {
            _factory = new ApiFactory();
            _http = _factory.CreateClient();

            Seed(db =>
            {
                db.Users.AddRange(
                    NewUser(_admin, "System Admin", UserRole.Admin),
                    NewUser(_consultant, "Nomvula Zulu", UserRole.Consultant),
                    NewUser(_otherConsultant, "Pieter van Wyk", UserRole.Consultant),
                    NewUser(_client, "Lerato Mahlangu", UserRole.Client),
                    NewUser(_otherClient, "Mokoena, Thabo", UserRole.Client),
                    NewUser(_inactiveClient, "Sipho Dlamini", UserRole.Client, isActive: false));
                db.VisaCategories.AddRange(
                    new VisaCategory { Id = _visa, Name = "Critical Skills Work Visa", IsActive = true },
                    new VisaCategory { Id = _closedVisa, Name = "Retired Visa", IsActive = false });
            });
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

        private static User NewUser(Guid id, string name, UserRole role, bool isActive = true) => new()
        {
            Id = id,
            FullName = name,
            Email = $"{id:N}@example.com",
            PasswordHash = "not-used",
            Role = role,
            IsActive = isActive,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        private Case NewCase(Guid clientId, Guid? consultantId, int daysAgo = 0) => new()
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ConsultantId = consultantId,
            VisaCategoryId = _visa,
            Stage = CaseStage.DocumentGathering,
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo),
            UpdatedAt = DateTime.UtcNow
        };

        private void Seed(Action<ApplicationDbContext> seed)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seed(db);
            db.SaveChanges();
        }

        private T Query<T>(Func<ApplicationDbContext, T> query)
        {
            using var scope = _factory.Services.CreateScope();
            return query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        }

        private string TokenFor(Guid userId, UserRole role)
        {
            var user = new User { Id = userId, FullName = "Test User", Email = "test@example.com", Role = role };
            return _factory.Services.GetRequiredService<ITokenService>().GenerateAccessToken(user).Token;
        }

        private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? bearer, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _http.SendAsync(request);
        }

        private Task<HttpResponseMessage> Create(string? bearer, Guid clientId, Guid visaId) =>
            Send(HttpMethod.Post, "/api/cases", bearer, new { clientId, visaCategoryId = visaId });

        // ── POST /api/cases ────────────────────────────────────────────────

        [Fact]
        public async Task Create_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Create(null, _client, _visa)).StatusCode);
        }

        [Fact]
        public async Task Create_ByAClient_Returns403_AndCreatesNothing()
        {
            var response = await Create(TokenFor(_client, UserRole.Client), _client, _visa);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, Query(db => db.Cases.Count()));
        }

        [Fact]
        public async Task Create_ByAConsultant_Returns201_AssignedToThem_AtInitialIntake_WithAnAuditRow()
        {
            var response = await Create(TokenFor(_consultant, UserRole.Consultant), _client, _visa);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var caseId = body.RootElement.GetProperty("caseId").GetGuid();
            Assert.EndsWith($"/api/Cases/{caseId}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

            var created = Query(db => db.Cases.Single(c => c.Id == caseId));
            Assert.Equal(_client, created.ClientId);
            Assert.Equal(_consultant, created.ConsultantId);
            Assert.Equal(CaseStage.InitialIntake, created.Stage);
            Assert.False(created.IsArchived);
            var audit = Query(db => db.AuditLogs.Single(a => a.EntityId == caseId));
            Assert.Equal(_consultant, audit.UserId);
            Assert.Equal("Case", audit.EntityType);
        }

        [Fact]
        public async Task Create_ByAnAdmin_LeavesTheCaseUnassigned()
        {
            var response = await Create(TokenFor(_admin, UserRole.Admin), _client, _visa);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Null(Query(db => db.Cases.Single().ConsultantId));
        }

        [Fact]
        public async Task Create_ThenGet_ReturnsTheNewCase()
        {
            var token = TokenFor(_consultant, UserRole.Consultant);
            var created = await Create(token, _client, _visa);

            var detail = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, created.Headers.Location)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
            });

            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            Assert.Equal("InitialIntake", (await detail.Content.ReadFromJsonAsync<CaseDto>())!.Stage);
        }

        [Theory]
        [InlineData("unknown client")]
        [InlineData("inactive client")]
        public async Task Create_WithoutAValidActiveClient_Returns400_AndCreatesNothing(string which)
        {
            var clientId = which == "unknown client" ? Guid.NewGuid() : _inactiveClient;

            var response = await Create(TokenFor(_admin, UserRole.Admin), clientId, _visa);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Valid, active client is required.", (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Message);
            Assert.Equal(0, Query(db => db.Cases.Count()));
            Assert.Equal(0, Query(db => db.AuditLogs.Count()));
        }

        [Theory]
        [InlineData("unknown visa")]
        [InlineData("inactive visa")]
        public async Task Create_WithoutAValidActiveVisaCategory_Returns400(string which)
        {
            var visaId = which == "unknown visa" ? Guid.NewGuid() : _closedVisa;

            var response = await Create(TokenFor(_admin, UserRole.Admin), _client, visaId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Valid, active visa category is required.", (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Message);
            Assert.Equal(0, Query(db => db.Cases.Count()));
        }

        // ── GET /api/cases/{id} ────────────────────────────────────────────

        [Fact]
        public async Task Detail_WithoutAToken_Returns401()
        {
            var c = NewCase(_client, _consultant);
            Seed(db => db.Cases.Add(c));

            Assert.Equal(HttpStatusCode.Unauthorized, (await Send(HttpMethod.Get, $"/api/cases/{c.Id}", null)).StatusCode);
        }

        [Fact]
        public async Task Detail_ForAnAdmin_ReturnsTheCaseWithNames()
        {
            var c = NewCase(_client, _consultant);
            Seed(db => db.Cases.Add(c));

            var response = await Send(HttpMethod.Get, $"/api/cases/{c.Id}", TokenFor(_admin, UserRole.Admin));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = (await response.Content.ReadFromJsonAsync<CaseDto>())!;
            Assert.Equal(c.Id, dto.Id);
            Assert.Equal("Lerato Mahlangu", dto.ClientName);
            Assert.Equal("Nomvula Zulu", dto.ConsultantName);
            Assert.Equal("Critical Skills Work Visa", dto.VisaCategoryName);
            Assert.Equal("DocumentGathering", dto.Stage);
        }

        [Fact]
        public async Task Detail_UnassignedCase_SaysUnassigned()
        {
            var c = NewCase(_client, consultantId: null);
            Seed(db => db.Cases.Add(c));

            var dto = await (await Send(HttpMethod.Get, $"/api/cases/{c.Id}", TokenFor(_admin, UserRole.Admin)))
                .Content.ReadFromJsonAsync<CaseDto>();

            Assert.Equal("Unassigned", dto!.ConsultantName);
        }

        [Theory]
        [InlineData(UserRole.Consultant, true)]
        [InlineData(UserRole.Consultant, false)]
        [InlineData(UserRole.Client, true)]
        [InlineData(UserRole.Client, false)]
        public async Task Detail_ConsultantsAndClients_OpenOnlyTheirOwnCases(UserRole role, bool own)
        {
            var c = NewCase(_client, _consultant);
            Seed(db => db.Cases.Add(c));
            var userId = role == UserRole.Consultant
                ? (own ? _consultant : _otherConsultant)
                : (own ? _client : _otherClient);

            var response = await Send(HttpMethod.Get, $"/api/cases/{c.Id}", TokenFor(userId, role));

            Assert.Equal(own ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Detail_UnknownCase_ForAnAdmin_Returns404()
        {
            var response = await Send(HttpMethod.Get, $"/api/cases/{Guid.NewGuid()}", TokenFor(_admin, UserRole.Admin));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── GET /api/cases/export ──────────────────────────────────────────

        [Fact]
        public async Task Export_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send(HttpMethod.Get, "/api/cases/export", null)).StatusCode);
        }

        [Fact]
        public async Task Export_ByAClient_Returns403()
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/api/cases/export", TokenFor(_client, UserRole.Client))).StatusCode);
        }

        [Fact]
        public async Task Export_ForAnAdmin_IsACsvFileOfEveryCase_NewestFirst()
        {
            var older = NewCase(_client, _consultant, daysAgo: 5);
            var newer = NewCase(_otherClient, _otherConsultant, daysAgo: 1);
            Seed(db => db.Cases.AddRange(older, newer));

            var response = await Send(HttpMethod.Get, "/api/cases/export", TokenFor(_admin, UserRole.Admin));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal("cases_export.csv", response.Content.Headers.ContentDisposition!.FileName);
            var lines = (await response.Content.ReadAsStringAsync()).Split('\n');
            Assert.Equal("Id,Client,VisaCategory,Stage,IsArchived,Created", lines[0]);
            Assert.Equal(3, lines.Length);
            Assert.StartsWith(newer.Id.ToString(), lines[1]);
            Assert.StartsWith(older.Id.ToString(), lines[2]);
        }

        [Fact]
        public async Task Export_QuotesANameThatContainsAComma()
        {
            Seed(db => db.Cases.Add(NewCase(_otherClient, _otherConsultant)));

            var csv = await (await Send(HttpMethod.Get, "/api/cases/export", TokenFor(_admin, UserRole.Admin))).Content.ReadAsStringAsync();

            Assert.Contains(",\"Mokoena, Thabo\",Critical Skills Work Visa,DocumentGathering,False,", csv);
        }

        // The client's cell in the one data row: between the case id and the visa category.
        private async Task<string> ExportedClientCellAsync(string clientName)
        {
            var clientId = Guid.NewGuid();
            Seed(db =>
            {
                db.Users.Add(NewUser(clientId, clientName, UserRole.Client));
                db.Cases.Add(NewCase(clientId, _consultant));
            });

            var csv = await (await Send(HttpMethod.Get, "/api/cases/export", TokenFor(_admin, UserRole.Admin))).Content.ReadAsStringAsync();

            var row = csv[(csv.IndexOf('\n') + 1)..];
            var start = row.IndexOf(',') + 1;
            return row[start..row.LastIndexOf(",Critical Skills Work Visa,", StringComparison.Ordinal)];
        }

        [Theory]
        [InlineData("=HYPERLINK(\"http://example.com\")", "\"'=HYPERLINK(\"\"http://example.com\"\")\"")]
        [InlineData("+27 82 555 0101", "'+27 82 555 0101")]
        [InlineData("-1+1", "'-1+1")]
        [InlineData("@SUM(A1)", "'@SUM(A1)")]
        [InlineData("\tTabbed", "'\tTabbed")]
        [InlineData("\rReturned", "\"'\rReturned\"")]
        [InlineData("=1,2", "\"'=1,2\"")]
        public async Task Export_NameThatStartsLikeAFormula_IsWrittenAsText(string name, string expectedCell)
        {
            Assert.Equal(expectedCell, await ExportedClientCellAsync(name));
        }

        [Theory]
        [InlineData("Lerato Mahlangu", "Lerato Mahlangu")]
        [InlineData("Anne-Marie du Toit", "Anne-Marie du Toit")]
        [InlineData("O'Neil = Smith", "O'Neil = Smith")]
        public async Task Export_OrdinaryName_IsUnchanged(string name, string expectedCell)
        {
            Assert.Equal(expectedCell, await ExportedClientCellAsync(name));
        }

        [Fact]
        public async Task Export_ForAConsultant_HasOnlyTheirCases()
        {
            var own = NewCase(_client, _consultant);
            var other = NewCase(_otherClient, _otherConsultant);
            Seed(db => db.Cases.AddRange(own, other));

            var csv = await (await Send(HttpMethod.Get, "/api/cases/export", TokenFor(_consultant, UserRole.Consultant))).Content.ReadAsStringAsync();

            Assert.Contains(own.Id.ToString(), csv);
            Assert.DoesNotContain(other.Id.ToString(), csv);
        }
    }
}
