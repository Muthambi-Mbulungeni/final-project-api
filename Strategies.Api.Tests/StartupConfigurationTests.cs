using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Strategies.Api.Configuration;
using Strategies.Api.Data;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// Production-readiness settings, checked against the real startup in a TestServer:
    /// fail-fast on missing settings, CORS origins from configuration, Swagger switch, /health.
    /// </summary>
    public class StartupConfigurationTests
    {
        // Every setting the API requires outside Development, with test-only values.
        public static readonly IReadOnlyDictionary<string, string?> RequiredTestSettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1",
            ["Jwt:Key"] = "TestOnlyKey_MustBeAtLeast32CharactersLong!",
            ["App:BaseUrl"] = "http://localhost:5173",
            ["Cors:AllowedOrigins:0"] = "https://portal.example.test"
        };

        private sealed class ApiFactory : WebApplicationFactory<Program>
        {
            private readonly Dictionary<string, string?> _settings;
            private readonly string _environment;
            private readonly bool _inMemoryDatabase;

            public ApiFactory(Dictionary<string, string?>? settings = null, string environment = "Production",
                bool withRequiredSettings = true, bool inMemoryDatabase = false)
            {
                _settings = withRequiredSettings
                    ? new Dictionary<string, string?>(RequiredTestSettings)
                    : new Dictionary<string, string?>();
                foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
                    _settings[key] = value;
                _environment = environment;
                _inMemoryDatabase = inMemoryDatabase;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment(_environment);
                foreach (var (key, value) in _settings)
                    builder.UseSetting(key, value);

                if (_inMemoryDatabase)
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                        services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                        var dbName = Guid.NewGuid().ToString();
                        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));
                    });
            }
        }

        // ── Fail fast ──────────────────────────────────────────────────────

        [Fact]
        public void Production_WithoutRequiredSettings_FailsNamingEveryMissingSetting()
        {
            // Blank values, so a Jwt__Key environment variable (as in CI) cannot fill the gap.
            using var factory = new ApiFactory(new()
            {
                ["ConnectionStrings:DefaultConnection"] = "",
                ["Jwt:Key"] = "",
                ["App:BaseUrl"] = ""
            }, withRequiredSettings: false);

            var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("'Production'", error.Message);
            foreach (var key in RequiredSettings.Keys.Append(CorsSettings.SectionName))
                Assert.Contains(key, error.Message);
            Assert.Contains("Cors__AllowedOrigins__0", error.Message);
            Assert.Contains("ConnectionStrings__DefaultConnection", error.Message);
        }

        [Theory]
        [InlineData("ConnectionStrings:DefaultConnection")]
        [InlineData("Jwt:Key")]
        [InlineData("App:BaseUrl")]
        public void Production_MissingOneSetting_FailsNamingIt(string key)
        {
            using var factory = new ApiFactory(new() { [key] = "" });

            var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains(key, error.Message);
            Assert.DoesNotContain(CorsSettings.SectionName, error.Message);
        }

        [Fact]
        public void Production_NoCorsOrigins_Fails()
        {
            using var factory = new ApiFactory(new() { ["Cors:AllowedOrigins:0"] = "" });

            var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains(CorsSettings.SectionName, error.Message);
        }

        [Fact]
        public async Task Production_WithRequiredSettings_Starts()
        {
            using var factory = new ApiFactory(inMemoryDatabase: true);
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }

        [Fact]
        public async Task Development_DoesNotRequireTheSettings()
        {
            // appsettings.Development.json supplies them; the check itself is skipped in Development.
            using var factory = new ApiFactory(environment: "Development", withRequiredSettings: false, inMemoryDatabase: true);
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }

        // ── CORS ───────────────────────────────────────────────────────────

        [Theory]
        [InlineData("https://emgprs.github.io/insy7315-2026-insy7315-2026-final-project-herculesriaadst10258492/")]
        [InlineData("https://emgprs.github.io/")]
        [InlineData("emgprs.github.io")]
        [InlineData("ftp://emgprs.github.io")]
        [InlineData("https://user@emgprs.github.io")]
        public void CorsOrigin_WithPathOrWrongForm_StopsStartup(string origin)
        {
            using var factory = new ApiFactory(new() { ["Cors:AllowedOrigins:0"] = origin });

            var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains(CorsSettings.SectionName, error.Message);
            Assert.Contains(origin, error.Message);
        }

        [Theory]
        [InlineData("https://emgprs.github.io", true)]
        [InlineData("http://localhost:5173", true)]
        [InlineData("https://strategies-api.azurewebsites.net", true)]
        [InlineData("https://emgprs.github.io/", false)]
        [InlineData("https://emgprs.github.io/app", false)]
        [InlineData("https://emgprs.github.io?x=1", false)]
        public void IsValidOrigin_AcceptsSchemeAndHostOnly(string origin, bool expected)
        {
            Assert.Equal(expected, CorsSettings.IsValidOrigin(origin));
        }

        [Fact]
        public async Task Cors_AllowsConfiguredOriginsOnly()
        {
            using var factory = new ApiFactory(new()
            {
                ["Cors:AllowedOrigins:0"] = "https://emgprs.github.io",
                ["Cors:AllowedOrigins:1"] = "https://staging.example.test"
            }, inMemoryDatabase: true);
            using var client = factory.CreateClient();

            async Task<string?> AllowedOriginFor(string origin)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/health");
                request.Headers.Add("Origin", origin);
                var response = await client.SendAsync(request);
                return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
            }

            Assert.Equal("https://emgprs.github.io", await AllowedOriginFor("https://emgprs.github.io"));
            Assert.Equal("https://staging.example.test", await AllowedOriginFor("https://staging.example.test"));
            Assert.Null(await AllowedOriginFor("http://localhost:5173"));
            Assert.Null(await AllowedOriginFor("https://evil.example.test"));
        }

        [Fact]
        public async Task Cors_Development_AllowsLocalhostDefaults()
        {
            using var factory = new ApiFactory(environment: "Development", withRequiredSettings: false, inMemoryDatabase: true);
            using var client = factory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", "http://localhost:5173");
            var response = await client.SendAsync(request);

            Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
            Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        }

        // ── Swagger ────────────────────────────────────────────────────────

        [Theory]
        [InlineData("true", HttpStatusCode.OK)]
        [InlineData("false", HttpStatusCode.NotFound)]
        public async Task Swagger_FollowsTheSetting(string enabled, HttpStatusCode expected)
        {
            using var factory = new ApiFactory(new() { ["Swagger:Enabled"] = enabled }, inMemoryDatabase: true);
            using var client = factory.CreateClient();

            Assert.Equal(expected, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        }

        [Fact]
        public async Task Swagger_OffInProductionByDefault()
        {
            using var factory = new ApiFactory(inMemoryDatabase: true);
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        }

        [Fact]
        public async Task Swagger_OnInDevelopmentByDefault()
        {
            using var factory = new ApiFactory(environment: "Development", withRequiredSettings: false, inMemoryDatabase: true);
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        }

        // ── Health ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Health_DatabaseReachable_Returns200Healthy()
        {
            using var factory = new ApiFactory(inMemoryDatabase: true);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Health_DatabaseUnreachable_Returns503WithoutDetails()
        {
            // RequiredTestSettings points at 127.0.0.1:1, where nothing listens.
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
        }
    }
}
