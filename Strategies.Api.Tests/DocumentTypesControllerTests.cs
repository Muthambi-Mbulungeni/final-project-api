using System;
using System.Linq;
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
    /// Integration tests for GET /api/DocumentTypes over HTTP, run against the real pipeline in a
    /// TestServer with an in-memory database (same pattern as AuthControllerTests).
    /// </summary>
    public class DocumentTypesControllerTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public DocumentTypesControllerTests()
        {
            _factory = new ApiFactory();
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
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

        private string TokenFor(UserRole role)
        {
            var user = new User { Id = Guid.NewGuid(), FullName = "Test User", Email = "test@example.com", Role = role };
            return _factory.Services.GetRequiredService<ITokenService>().GenerateAccessToken(user).Token;
        }

        private async Task<HttpResponseMessage> Get(string path, string? bearer)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _client.SendAsync(request);
        }

        private async Task AddTypesAsync(params (string Name, string? Description)[] types)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var (name, description) in types)
                db.DocumentTypes.Add(new DocumentType { Id = Guid.NewGuid(), Name = name, Description = description });
            await db.SaveChangesAsync();
        }

        // ── Tests ──────────────────────────────────────────────────────────

        [Fact]
        public async Task WithoutAToken_Returns401()
        {
            var response = await Get("/api/DocumentTypes", bearer: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData(UserRole.Client)]
        [InlineData(UserRole.Consultant)]
        [InlineData(UserRole.Admin)]
        public async Task EverySignedInRole_GetsTheList(UserRole role)
        {
            await AddTypesAsync(("Passport", "Certified copy"));

            var response = await Get("/api/DocumentTypes", TokenFor(role));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task List_IsSortedByName_WithIdNameAndDescription()
        {
            await AddTypesAsync(("Police clearance", null), ("Bank statement", "Last 3 months"), ("Medical report", "From a registered doctor"));

            var response = await Get("/api/DocumentTypes", TokenFor(UserRole.Client));

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var items = body.RootElement.EnumerateArray().ToList();
            Assert.Equal(new[] { "Bank statement", "Medical report", "Police clearance" },
                items.Select(i => i.GetProperty("name").GetString()));
            Assert.Equal("Last 3 months", items[0].GetProperty("description").GetString());
            Assert.Equal(JsonValueKind.Null, items[2].GetProperty("description").ValueKind);
            Assert.All(items, i => Assert.NotEqual(Guid.Empty, i.GetProperty("id").GetGuid()));
            Assert.All(items, i => Assert.Equal(3, i.EnumerateObject().Count()));
        }

        [Fact]
        public async Task NoTypes_ReturnsAnEmptyList()
        {
            var response = await Get("/api/DocumentTypes", TokenFor(UserRole.Client));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Route_IsCaseInsensitive_SoTheLowercasePathAlsoWorks()
        {
            await AddTypesAsync(("Passport", null));

            var response = await Get("/api/documenttypes", TokenFor(UserRole.Client));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
