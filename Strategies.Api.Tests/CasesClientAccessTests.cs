using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    /// GET /api/cases/client/{clientId} over HTTP (same pattern as AuthControllerTests): who may list a
    /// client's cases. A consultant needs a case for that client assigned to them and sees only those.
    /// </summary>
    public class CasesClientAccessTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _http;

        private readonly Guid _client = Guid.NewGuid();
        private readonly Guid _consultant = Guid.NewGuid();
        private readonly Guid _otherConsultant = Guid.NewGuid();
        private readonly Guid _ownCase = Guid.NewGuid();
        private readonly Guid _otherCase = Guid.NewGuid();

        public CasesClientAccessTests()
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

        private async Task AddCaseAsync(Guid id, Guid? consultantId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Cases.Add(new Case
            {
                Id = id,
                ClientId = _client,
                ConsultantId = consultantId,
                VisaCategoryId = Guid.NewGuid(),
                Stage = CaseStage.DocumentGathering,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        private async Task<HttpResponseMessage> List(string? bearer)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/cases/client/{_client}");
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _http.SendAsync(request);
        }

        private static async Task<Guid[]> CaseIds(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = (await response.Content.ReadFromJsonAsync<PagedResponse<CaseDto>>())!;
            return page.Data.Select(c => c.Id).OrderBy(id => id).ToArray();
        }

        // ── Tests ──────────────────────────────────────────────────────────

        [Fact]
        public async Task WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await List(bearer: null)).StatusCode);
        }

        [Fact]
        public async Task TheClient_SeesAllTheirCases()
        {
            await AddCaseAsync(_ownCase, _consultant);
            await AddCaseAsync(_otherCase, _otherConsultant);

            var ids = await CaseIds(await List(TokenFor(_client, UserRole.Client)));

            Assert.Equal(new[] { _ownCase, _otherCase }.OrderBy(id => id), ids);
        }

        [Fact]
        public async Task AnotherClient_Returns403()
        {
            await AddCaseAsync(_ownCase, _consultant);

            Assert.Equal(HttpStatusCode.Forbidden, (await List(TokenFor(Guid.NewGuid(), UserRole.Client))).StatusCode);
        }

        [Fact]
        public async Task AnAdmin_SeesEveryCaseOfTheClient()
        {
            await AddCaseAsync(_ownCase, _consultant);
            await AddCaseAsync(_otherCase, _otherConsultant);

            var ids = await CaseIds(await List(TokenFor(Guid.NewGuid(), UserRole.Admin)));

            Assert.Equal(2, ids.Length);
        }

        [Fact]
        public async Task TheAssignedConsultant_SeesOnlyTheCasesAssignedToThem()
        {
            await AddCaseAsync(_ownCase, _consultant);
            await AddCaseAsync(_otherCase, _otherConsultant);

            var response = await List(TokenFor(_consultant, UserRole.Consultant));

            Assert.Equal(new[] { _ownCase }, await CaseIds(response));
        }

        [Fact]
        public async Task AConsultantNotAssigned_Returns403()
        {
            await AddCaseAsync(_otherCase, _otherConsultant);

            Assert.Equal(HttpStatusCode.Forbidden, (await List(TokenFor(_consultant, UserRole.Consultant))).StatusCode);
        }

        [Fact]
        public async Task AConsultant_ForACaseWithNoConsultant_Returns403()
        {
            await AddCaseAsync(_ownCase, consultantId: null);

            Assert.Equal(HttpStatusCode.Forbidden, (await List(TokenFor(_consultant, UserRole.Consultant))).StatusCode);
        }
    }
}
