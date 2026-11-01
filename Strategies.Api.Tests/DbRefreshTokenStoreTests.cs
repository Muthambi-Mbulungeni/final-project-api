using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Strategies.Api.Configuration;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class DbRefreshTokenStoreTests
    {
        private const string AdminEmail = "admin@nexus.com";
        private const string AdminPassword = "AdminPassword123!";
        private const string ClientEmail = "client@nexus.com";
        private const string ClientPassword = "ClientPassword123!";

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

        private static AuthService CreateAuthService(ApplicationDbContext ctx, DbRefreshTokenStore store) =>
            new(ctx, new PasswordHasherService(), CreateTokenService(), store, NullLogger<AuthService>.Instance,
                new MemoryCache(new MemoryCacheOptions()), Options.Create(new LockoutSettings()), TimeProvider.System);

        // Same accounts as database/seed/1-reference-and-users.sql
        private static async Task SeedUsersAsync(ApplicationDbContext ctx)
        {
            var hasher = new PasswordHasherService();
            ctx.Users.AddRange(
                new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "System Admin",
                    Email = AdminEmail,
                    PasswordHash = hasher.HashPassword(AdminPassword),
                    Role = UserRole.Admin,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = ClientEmail,
                    PasswordHash = hasher.HashPassword(ClientPassword),
                    Role = UserRole.Client,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();
        }

        // Moves a rotation into the past, as if the old token had been revoked that long ago.
        private static async Task BackdateRevocationAsync(ApplicationDbContext ctx, string rawToken, TimeSpan ago)
        {
            var hash = DbRefreshTokenStore.HashToken(rawToken);
            var record = await ctx.RefreshTokens.SingleAsync(r => r.TokenHash == hash);
            record.RevokedAt = DateTime.UtcNow - ago;
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
        }

        [Fact]
        public async Task SaveTokenAsync_StoresHashedToken_NeverStoresRawToken()
        {
            using var ctx = CreateContext(nameof(SaveTokenAsync_StoresHashedToken_NeverStoresRawToken));
            var store = new DbRefreshTokenStore(ctx);

            var userId = Guid.NewGuid();
            var rawToken = "super-secret-random-refresh-token-12345";
            var token = new RefreshToken
            {
                Token = rawToken,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await store.SaveTokenAsync(token);

            var savedRecord = await ctx.RefreshTokens.FirstOrDefaultAsync(r => r.UserId == userId);
            Assert.NotNull(savedRecord);
            Assert.NotEqual(rawToken, savedRecord!.TokenHash);
            Assert.Equal(DbRefreshTokenStore.HashToken(rawToken), savedRecord.TokenHash);
            Assert.Equal(64, savedRecord.TokenHash.Length);
            Assert.False(savedRecord.IsRevoked);
            Assert.Null(savedRecord.RevokedAt);
        }

        [Fact]
        public async Task GetTokenAsync_WithValidRawToken_FindsAndReturnsToken()
        {
            using var ctx = CreateContext(nameof(GetTokenAsync_WithValidRawToken_FindsAndReturnsToken));
            var store = new DbRefreshTokenStore(ctx);

            var userId = Guid.NewGuid();
            var rawToken = "raw-refresh-token-abc";
            var token = new RefreshToken
            {
                Token = rawToken,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await store.SaveTokenAsync(token);

            var fetched = await store.GetTokenAsync(rawToken);
            Assert.NotNull(fetched);
            Assert.Equal(rawToken, fetched!.Token);
            Assert.Equal(userId, fetched.UserId);
            Assert.True(fetched.IsActive);
        }

        [Fact]
        public async Task GetTokenAsync_WithInvalidToken_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(GetTokenAsync_WithInvalidToken_ReturnsNull));
            var store = new DbRefreshTokenStore(ctx);

            var fetched = await store.GetTokenAsync("non-existent-token");
            Assert.Null(fetched);
        }

        [Fact]
        public async Task RevokeTokenAsync_MarksRecordRevokedAndHashesReplacement()
        {
            using var ctx = CreateContext(nameof(RevokeTokenAsync_MarksRecordRevokedAndHashesReplacement));
            var store = new DbRefreshTokenStore(ctx);

            var rawToken = "initial-token-to-revoke";
            var replacementToken = "replacement-token-new";
            var userId = Guid.NewGuid();

            await store.SaveTokenAsync(new RefreshToken
            {
                Token = rawToken,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await store.RevokeTokenAsync(rawToken, replacedByToken: replacementToken);

            var record = await ctx.RefreshTokens.FirstOrDefaultAsync(r => r.UserId == userId);
            Assert.NotNull(record);
            Assert.True(record!.IsRevoked);
            Assert.NotNull(record.RevokedAt);
            Assert.Equal(DbRefreshTokenStore.HashToken(replacementToken), record.ReplacedByTokenHash);

            var retrieved = await store.GetTokenAsync(rawToken);
            Assert.NotNull(retrieved);
            Assert.True(retrieved!.IsRevoked);
            Assert.False(retrieved.IsActive);
        }

        [Fact]
        public async Task RevokeAllUserTokensAsync_RevokesAllTokensForSpecifiedUserOnly()
        {
            using var ctx = CreateContext(nameof(RevokeAllUserTokensAsync_RevokesAllTokensForSpecifiedUserOnly));
            var store = new DbRefreshTokenStore(ctx);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            await store.SaveTokenAsync(new RefreshToken { Token = "u1-token1", UserId = user1, ExpiresAt = DateTime.UtcNow.AddDays(7) });
            await store.SaveTokenAsync(new RefreshToken { Token = "u1-token2", UserId = user1, ExpiresAt = DateTime.UtcNow.AddDays(7) });
            await store.SaveTokenAsync(new RefreshToken { Token = "u2-token1", UserId = user2, ExpiresAt = DateTime.UtcNow.AddDays(7) });

            await store.RevokeAllUserTokensAsync(user1);

            var u1Tokens = await ctx.RefreshTokens.Where(r => r.UserId == user1).ToListAsync();
            Assert.Equal(2, u1Tokens.Count);
            Assert.All(u1Tokens, t => Assert.True(t.IsRevoked));

            var u2Tokens = await ctx.RefreshTokens.Where(r => r.UserId == user2).ToListAsync();
            Assert.Single(u2Tokens);
            Assert.False(u2Tokens[0].IsRevoked);
        }

        [Fact]
        public async Task DatabaseError_IsThrown_NotHiddenByAFallbackStore()
        {
            var ctx = CreateContext(nameof(DatabaseError_IsThrown_NotHiddenByAFallbackStore));
            var store = new DbRefreshTokenStore(ctx);
            await ctx.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.SaveTokenAsync(
                new RefreshToken { Token = "lost-token", UserId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(7) }));
        }

        [Fact]
        public async Task AuthService_Rotation_WithDbRefreshTokenStore_RotatesAndSavesToDb()
        {
            using var ctx = CreateContext(nameof(AuthService_Rotation_WithDbRefreshTokenStore_RotatesAndSavesToDb));
            await SeedUsersAsync(ctx);
            var authService = CreateAuthService(ctx, new DbRefreshTokenStore(ctx));

            // Login
            var login = await authService.LoginAsync(new LoginRequest { Email = AdminEmail, Password = AdminPassword });
            Assert.NotNull(login);
            var firstRefreshToken = login!.RefreshToken;

            // Rotate
            var refresh = await authService.RefreshTokenAsync(firstRefreshToken);
            Assert.NotNull(refresh);
            var secondRefreshToken = refresh!.RefreshToken;
            Assert.NotEqual(firstRefreshToken, secondRefreshToken);

            // DB Verification: First token is revoked, second token is active
            var firstHash = DbRefreshTokenStore.HashToken(firstRefreshToken);
            var secondHash = DbRefreshTokenStore.HashToken(secondRefreshToken);

            var firstRec = await ctx.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == firstHash);
            var secondRec = await ctx.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == secondHash);

            Assert.NotNull(firstRec);
            Assert.True(firstRec!.IsRevoked);
            Assert.Equal(secondHash, firstRec.ReplacedByTokenHash);

            Assert.NotNull(secondRec);
            Assert.False(secondRec!.IsRevoked);
        }

        [Fact]
        public async Task AuthService_ReuseOfRevokedToken_RejectedAndRevokesUserTokens()
        {
            using var ctx = CreateContext(nameof(AuthService_ReuseOfRevokedToken_RejectedAndRevokesUserTokens));
            await SeedUsersAsync(ctx);
            var store = new DbRefreshTokenStore(ctx);
            var authService = CreateAuthService(ctx, store);

            var login = await authService.LoginAsync(new LoginRequest { Email = ClientEmail, Password = ClientPassword });
            Assert.NotNull(login);
            var token1 = login!.RefreshToken;

            // Legitimate refresh
            var refresh = await authService.RefreshTokenAsync(token1);
            Assert.NotNull(refresh);
            var token2 = refresh!.RefreshToken;

            // Attacker tries to reuse token1, after the grace window
            await BackdateRevocationAsync(ctx, token1, AuthService.RotationReuseGrace + TimeSpan.FromSeconds(1));
            var reuseAttempt = await authService.RefreshTokenAsync(token1);
            Assert.Null(reuseAttempt); // Rejected!

            // Reuse detection must have revoked all tokens for the user
            var token2Status = await store.GetTokenAsync(token2);
            Assert.NotNull(token2Status);
            Assert.True(token2Status!.IsRevoked); // token2 is now also revoked!

            // Subsequent refresh with token2 also fails
            var subsequentAttempt = await authService.RefreshTokenAsync(token2);
            Assert.Null(subsequentAttempt);
        }

        [Fact]
        public async Task AuthService_ReuseWithinTheGraceWindow_IsRefused_ButOtherSessionsSurvive()
        {
            using var ctx = CreateContext(nameof(AuthService_ReuseWithinTheGraceWindow_IsRefused_ButOtherSessionsSurvive));
            await SeedUsersAsync(ctx);
            var store = new DbRefreshTokenStore(ctx);
            var authService = CreateAuthService(ctx, store);

            var phone = await authService.LoginAsync(new LoginRequest { Email = ClientEmail, Password = ClientPassword });
            var browser = await authService.LoginAsync(new LoginRequest { Email = ClientEmail, Password = ClientPassword });
            var token1 = browser!.RefreshToken;
            var token2 = (await authService.RefreshTokenAsync(token1))!.RefreshToken;

            // A duplicated tab sends the old token again a moment later.
            var duplicate = await authService.RefreshTokenAsync(token1);

            Assert.Null(duplicate);
            Assert.True((await store.GetTokenAsync(token2))!.IsActive);
            Assert.True((await store.GetTokenAsync(phone!.RefreshToken))!.IsActive);
            Assert.NotNull(await authService.RefreshTokenAsync(token2));
        }

        [Fact]
        public async Task AuthService_ReuseAfterLogout_GetsNoGrace()
        {
            using var ctx = CreateContext(nameof(AuthService_ReuseAfterLogout_GetsNoGrace));
            await SeedUsersAsync(ctx);
            var store = new DbRefreshTokenStore(ctx);
            var authService = CreateAuthService(ctx, store);

            var login = await authService.LoginAsync(new LoginRequest { Email = ClientEmail, Password = ClientPassword });
            await authService.RevokeTokenAsync(login!.RefreshToken);
            var signedInAgain = await authService.LoginAsync(new LoginRequest { Email = ClientEmail, Password = ClientPassword });

            // The logged-out token was not rotated, so its reuse is treated as theft straight away.
            Assert.Null(await authService.RefreshTokenAsync(login.RefreshToken));
            Assert.True((await store.GetTokenAsync(signedInAgain!.RefreshToken))!.IsRevoked);
        }

        [Fact]
        public async Task AuthService_Logout_RevokesAllUserTokens()
        {
            using var ctx = CreateContext(nameof(AuthService_Logout_RevokesAllUserTokens));
            await SeedUsersAsync(ctx);
            var store = new DbRefreshTokenStore(ctx);
            var authService = CreateAuthService(ctx, store);

            var login = await authService.LoginAsync(new LoginRequest { Email = AdminEmail, Password = AdminPassword });
            Assert.NotNull(login);
            var otherDevice = await authService.LoginAsync(new LoginRequest { Email = AdminEmail, Password = AdminPassword });

            var revoked = await authService.RevokeTokenAsync(login!.RefreshToken);
            Assert.True(revoked);

            var tokenInStore = await store.GetTokenAsync(login.RefreshToken);
            Assert.NotNull(tokenInStore);
            Assert.True(tokenInStore!.IsRevoked);
            Assert.True((await store.GetTokenAsync(otherDevice!.RefreshToken))!.IsRevoked);

            var tryRefresh = await authService.RefreshTokenAsync(login.RefreshToken);
            Assert.Null(tryRefresh);
        }

        [Fact]
        public async Task AuthService_PasswordReset_RevokesAllUserTokens()
        {
            using var ctx = CreateContext(nameof(AuthService_PasswordReset_RevokesAllUserTokens));
            await SeedUsersAsync(ctx);
            var store = new DbRefreshTokenStore(ctx);
            var authService = CreateAuthService(ctx, store);

            var login = await authService.LoginAsync(new LoginRequest { Email = AdminEmail, Password = AdminPassword });
            Assert.NotNull(login);

            // Password reset (the controller passes the user from a validated reset token)
            var user = await ctx.Users.SingleAsync(u => u.Email == AdminEmail);
            var resetResult = await authService.ResetPasswordAsync(user, "BrandNewAdminPassword123!");
            Assert.True(resetResult);

            // Refresh token should be revoked now
            var tokenInStore = await store.GetTokenAsync(login!.RefreshToken);
            Assert.NotNull(tokenInStore);
            Assert.True(tokenInStore!.IsRevoked);

            var tryRefresh = await authService.RefreshTokenAsync(login.RefreshToken);
            Assert.Null(tryRefresh);
        }
    }
}
