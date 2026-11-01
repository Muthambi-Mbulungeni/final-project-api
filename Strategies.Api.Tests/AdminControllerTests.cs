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
    /// GET /api/admin/users and GET /api/admin/audit-logs over HTTP (same pattern as AuthControllerTests):
    /// admin-only access, paging, search, ordering and the names shown. AdminUserTests covers the user
    /// actions (update, suspend, reinstate, delete) on the controller itself.
    /// </summary>
    public class AdminControllerTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _http;
        private readonly Guid _admin = Guid.NewGuid();

        public AdminControllerTests()
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

        private void Seed(Action<ApplicationDbContext> seed)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seed(db);
            db.SaveChanges();
        }

        private static User NewUser(string name, string email, UserRole role = UserRole.Client, bool isActive = true,
            DateTime? deletedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Email = email,
            PasswordHash = "not-used",
            Role = role,
            IsActive = isActive,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = deletedAt
        };

        private static AuditLog NewAudit(Guid? userId, string action, int minutesAgo) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = "Case",
            EntityId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
        };

        private async Task<HttpResponseMessage> Get(string path, string? bearer)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _http.SendAsync(request);
        }

        private async Task<JsonElement> GetOkAsync(string path)
        {
            var response = await Get(path, TokenFor(_admin, UserRole.Admin));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        }

        private static string[] Values(JsonElement page, string property) =>
            page.GetProperty("data").EnumerateArray().Select(e => e.GetProperty(property).GetString()!).ToArray();

        // ── Access ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData("/api/admin/users")]
        [InlineData("/api/admin/audit-logs")]
        public async Task WithoutAToken_Returns401(string path)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(path, null)).StatusCode);
        }

        [Theory]
        [InlineData("/api/admin/users", UserRole.Consultant)]
        [InlineData("/api/admin/users", UserRole.Client)]
        [InlineData("/api/admin/audit-logs", UserRole.Consultant)]
        [InlineData("/api/admin/audit-logs", UserRole.Client)]
        public async Task OtherRoles_Get403(string path, UserRole role)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await Get(path, TokenFor(Guid.NewGuid(), role))).StatusCode);
        }

        // ── GET /api/admin/users ───────────────────────────────────────────

        [Fact]
        public async Task Users_AreSortedByName_WithoutDeletedAccounts_ButWithSuspendedOnes()
        {
            Seed(db => db.Users.AddRange(
                NewUser("Thandi Nkosi", "thandi@example.com"),
                NewUser("Ayesha Khan", "ayesha@example.com", UserRole.Consultant),
                NewUser("Sipho Dlamini", "sipho@example.com", isActive: false),
                NewUser("Gone User", "gone@example.com", isActive: false, deletedAt: DateTime.UtcNow)));

            var page = await GetOkAsync("/api/admin/users");

            Assert.Equal(new[] { "Ayesha Khan", "Sipho Dlamini", "Thandi Nkosi" }, Values(page, "fullName"));
            Assert.Equal(3, page.GetProperty("totalRecords").GetInt32());
            var sipho = page.GetProperty("data")[1];
            Assert.False(sipho.GetProperty("isActive").GetBoolean());
            Assert.Equal("Consultant", page.GetProperty("data")[0].GetProperty("role").GetString());
        }

        [Fact]
        public async Task Users_DoNotExposeThePasswordHash()
        {
            Seed(db => db.Users.Add(NewUser("Thandi Nkosi", "thandi@example.com")));

            var page = await GetOkAsync("/api/admin/users");

            var fields = page.GetProperty("data")[0].EnumerateObject().Select(p => p.Name).ToArray();
            Assert.DoesNotContain(fields, f => f.Contains("password", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("createdAt", fields);
        }

        [Theory]
        [InlineData("Thandi", "Thandi Nkosi")]
        [InlineData("ayesha@", "Ayesha Khan")]
        public async Task Users_Search_MatchesNameOrEmail(string search, string expected)
        {
            Seed(db => db.Users.AddRange(
                NewUser("Thandi Nkosi", "thandi@example.com"),
                NewUser("Ayesha Khan", "ayesha@example.com")));

            var page = await GetOkAsync($"/api/admin/users?search={Uri.EscapeDataString(search)}");

            Assert.Equal(new[] { expected }, Values(page, "fullName"));
        }

        [Fact]
        public async Task Users_ArePaged()
        {
            Seed(db =>
            {
                for (var i = 1; i <= 5; i++)
                    db.Users.Add(NewUser($"User {i}", $"user{i}@example.com"));
            });

            var page2 = await GetOkAsync("/api/admin/users?page=2&pageSize=2");

            Assert.Equal(new[] { "User 3", "User 4" }, Values(page2, "fullName"));
            Assert.Equal(3, page2.GetProperty("totalPages").GetInt32());
            Assert.Equal(2, page2.GetProperty("currentPage").GetInt32());
        }

        // ── GET /api/admin/audit-logs ──────────────────────────────────────

        [Fact]
        public async Task AuditLogs_AreNewestFirst_WithTheActorsName()
        {
            var consultant = NewUser("Nomvula Zulu", "nomvula@example.com", UserRole.Consultant);
            Seed(db =>
            {
                db.Users.Add(consultant);
                db.AuditLogs.AddRange(
                    NewAudit(consultant.Id, "Created case", minutesAgo: 30),
                    NewAudit(consultant.Id, "Approved document", minutesAgo: 5),
                    NewAudit(consultant.Id, "Rejected document", minutesAgo: 15));
            });

            var page = await GetOkAsync("/api/admin/audit-logs");

            Assert.Equal(new[] { "Approved document", "Rejected document", "Created case" }, Values(page, "action"));
            Assert.All(Values(page, "userName"), name => Assert.Equal("Nomvula Zulu", name));
            Assert.Equal(3, page.GetProperty("totalRecords").GetInt32());
        }

        [Fact]
        public async Task AuditLogs_WithNoOrAnUnknownActor_SaySystemUnknown()
        {
            Seed(db => db.AuditLogs.AddRange(
                NewAudit(null, "Seeded", minutesAgo: 2),
                NewAudit(Guid.NewGuid(), "Removed user's action", minutesAgo: 1)));

            var page = await GetOkAsync("/api/admin/audit-logs");

            Assert.All(Values(page, "userName"), name => Assert.Equal("System/Unknown", name));
        }

        [Fact]
        public async Task AuditLogs_FilterByUser()
        {
            var a = NewUser("Nomvula Zulu", "nomvula@example.com", UserRole.Consultant);
            var b = NewUser("Pieter van Wyk", "pieter@example.com", UserRole.Consultant);
            Seed(db =>
            {
                db.Users.AddRange(a, b);
                db.AuditLogs.AddRange(NewAudit(a.Id, "By Nomvula", 2), NewAudit(b.Id, "By Pieter", 1));
            });

            var page = await GetOkAsync($"/api/admin/audit-logs?userId={a.Id}");

            Assert.Equal(new[] { "By Nomvula" }, Values(page, "action"));
        }

        [Fact]
        public async Task AuditLogs_DefaultPageSize_Is20_AndArePaged()
        {
            Seed(db =>
            {
                for (var i = 0; i < 25; i++)
                    db.AuditLogs.Add(NewAudit(null, $"Action {i}", minutesAgo: i));
            });

            var first = await GetOkAsync("/api/admin/audit-logs");
            var second = await GetOkAsync("/api/admin/audit-logs?page=2");

            Assert.Equal(20, first.GetProperty("data").GetArrayLength());
            Assert.Equal(2, first.GetProperty("totalPages").GetInt32());
            Assert.Equal(5, second.GetProperty("data").GetArrayLength());
            Assert.Equal("Action 20", Values(second, "action")[0]);
        }
    }
}
