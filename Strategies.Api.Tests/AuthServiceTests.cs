using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// AuthService tests use the InMemory EF provider. AuthService reads users from the
    /// database only, so each test inserts the users it needs.
    /// </summary>
    public class AuthServiceTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static TokenService CreateTokenService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Key",      "TestOnlyKey_MustBeAtLeast32CharactersLong!"),
                    new KeyValuePair<string, string?>("Jwt:Issuer",   "TestIssuer"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                    new KeyValuePair<string, string?>("Jwt:AccessTokenExpirationMinutes", "60"),
                    new KeyValuePair<string, string?>("Jwt:RefreshTokenExpirationDays",   "7"),
                })
                .Build();
            return new TokenService(config);
        }

        private static AuthService CreateAuthService(ApplicationDbContext ctx)
        {
            var hasher       = new PasswordHasherService();
            var tokenService = CreateTokenService();
            var tokenStore   = new InMemoryRefreshTokenStore();
            var logger       = NullLogger<AuthService>.Instance;
            return new AuthService(ctx, hasher, tokenService, tokenStore, logger,
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                Microsoft.Extensions.Options.Options.Create(new Strategies.Api.Configuration.LockoutSettings()), TimeProvider.System);
        }

        // Same accounts as database/seed/1-reference-and-users.sql
        private const string AdminEmail    = "admin@nexus.com";
        private const string AdminPassword = "AdminPassword123!";
        private const string ClientEmail   = "client@nexus.com";
        private const string ClientPassword = "ClientPassword123!";

        private static async Task SeedUsersAsync(ApplicationDbContext ctx)
        {
            var hasher = new PasswordHasherService();
            ctx.Users.AddRange(
                new User
                {
                    Id             = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    FullName       = "System Admin",
                    Email          = AdminEmail,
                    PasswordHash   = hasher.HashPassword(AdminPassword),
                    Role           = UserRole.Admin,
                    IsActive       = true,
                    EmailConfirmed = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new User
                {
                    Id             = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    FullName       = "John Doe",
                    Email          = ClientEmail,
                    PasswordHash   = hasher.HashPassword(ClientPassword),
                    Role           = UserRole.Client,
                    IsActive       = true,
                    EmailConfirmed = true,
                    CreatedAt      = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task Login_WithValidDbCredentials_ReturnsAuthResponse()
        {
            using var ctx = CreateContext(nameof(Login_WithValidDbCredentials_ReturnsAuthResponse));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail,
                Password = AdminPassword
            });

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
            Assert.Equal("Admin", result.Role);
            Assert.Equal("System Admin", result.DisplayName);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(Login_WithWrongPassword_ReturnsNull));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail,
                Password = "WrongPassword!"
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(Login_WithUnknownEmail_ReturnsNull));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Email    = "nobody@nexus.com",
                Password = "Anything123!"
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_AdminNotInDatabase_ReturnsNull()
        {
            // No users inserted: the removed built-in admin must not be able to sign in.
            using var ctx = CreateContext(nameof(Login_AdminNotInDatabase_ReturnsNull));
            var svc = CreateAuthService(ctx);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail,
                Password = AdminPassword
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_WithInactiveDbUser_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(Login_WithInactiveDbUser_ReturnsNull));

            var hasher = new PasswordHasherService();
            var inactive = new User
            {
                Id           = Guid.NewGuid(),
                FullName     = "Suspended User",
                Email        = "suspended@nexus.com",
                PasswordHash = hasher.HashPassword("Password123!"),
                Role         = UserRole.Consultant,
                IsActive     = false,
                CreatedAt    = DateTime.UtcNow
            };
            ctx.Users.Add(inactive);
            await ctx.SaveChangesAsync();

            var svc = CreateAuthService(ctx);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Email    = "suspended@nexus.com",
                Password = "Password123!"
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_IsCaseInsensitiveOnEmail()
        {
            using var ctx = CreateContext(nameof(Login_IsCaseInsensitiveOnEmail));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail.ToUpper(),
                Password = AdminPassword
            });

            Assert.NotNull(result);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsNewAuthResponse()
        {
            using var ctx = CreateContext(nameof(RefreshToken_WithValidToken_ReturnsNewAuthResponse));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var loginResult = await svc.LoginAsync(new LoginRequest
            {
                Email    = ClientEmail,
                Password = ClientPassword
            });
            Assert.NotNull(loginResult);

            var refreshResult = await svc.RefreshTokenAsync(loginResult!.RefreshToken);

            Assert.NotNull(refreshResult);
            Assert.False(string.IsNullOrWhiteSpace(refreshResult!.AccessToken));
            // Rotated — new refresh token must differ from the original
            Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);
        }

        [Fact]
        public async Task RefreshToken_WithRevokedToken_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(RefreshToken_WithRevokedToken_ReturnsNull));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var loginResult = await svc.LoginAsync(new LoginRequest
            {
                Email    = ClientEmail,
                Password = ClientPassword
            });
            Assert.NotNull(loginResult);

            // Revoke it
            await svc.RevokeTokenAsync(loginResult!.RefreshToken);

            // Try to refresh with the now-revoked token
            var refreshResult = await svc.RefreshTokenAsync(loginResult.RefreshToken);

            Assert.Null(refreshResult);
        }

        [Fact]
        public async Task RefreshToken_WithGarbageToken_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(RefreshToken_WithGarbageToken_ReturnsNull));
            var svc = CreateAuthService(ctx);

            var result = await svc.RefreshTokenAsync("this-is-not-a-real-token");

            Assert.Null(result);
        }

        [Fact]
        public async Task RevokeToken_ReturnsTrueForActiveToken()
        {
            using var ctx = CreateContext(nameof(RevokeToken_ReturnsTrueForActiveToken));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var loginResult = await svc.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail,
                Password = AdminPassword
            });
            Assert.NotNull(loginResult);

            var revoked = await svc.RevokeTokenAsync(loginResult!.RefreshToken);

            Assert.True(revoked);
        }

        [Fact]
        public async Task RevokeToken_ReturnsFalseForAlreadyRevokedToken()
        {
            using var ctx = CreateContext(nameof(RevokeToken_ReturnsFalseForAlreadyRevokedToken));
            await SeedUsersAsync(ctx);
            var svc = CreateAuthService(ctx);

            var loginResult = await svc.LoginAsync(new LoginRequest
            {
                Email    = AdminEmail,
                Password = AdminPassword
            });
            Assert.NotNull(loginResult);

            await svc.RevokeTokenAsync(loginResult!.RefreshToken);
            var secondRevoke = await svc.RevokeTokenAsync(loginResult.RefreshToken);

            Assert.False(secondRevoke);
        }
    }
}
