using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DbRefreshTokenStoreTests
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

        [Fact]
        public async Task SaveTokenAsync_StoresHashedToken_NeverStoresRawToken()
        {
            using var ctx = CreateContext(nameof(SaveTokenAsync_StoresHashedToken_NeverStoresRawToken));
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);

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
            Assert.False(savedRecord.IsRevoked);
            Assert.Null(savedRecord.RevokedAt);
        }

        [Fact]
        public async Task GetTokenAsync_WithValidRawToken_FindsAndReturnsToken()
        {
            using var ctx = CreateContext(nameof(GetTokenAsync_WithValidRawToken_FindsAndReturnsToken));
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);

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
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);

            var fetched = await store.GetTokenAsync("non-existent-token");
            Assert.Null(fetched);
        }

        [Fact]
        public async Task RevokeTokenAsync_MarksRecordRevokedAndHashesReplacement()
        {
            using var ctx = CreateContext(nameof(RevokeTokenAsync_MarksRecordRevokedAndHashesReplacement));
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);

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
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);

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
        public async Task AuthService_Rotation_WithDbRefreshTokenStore_RotatesAndSavesToDb()
        {
            using var ctx = CreateContext(nameof(AuthService_Rotation_WithDbRefreshTokenStore_RotatesAndSavesToDb));
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);
            var tokenService = CreateTokenService();
            var authService = new AuthService(ctx, new PasswordHasherService(), tokenService, store, NullLogger<AuthService>.Instance);

            // Login
            var login = await authService.LoginAsync(new LoginRequest { Email = "admin@nexus.com", Password = "AdminPassword123!" });
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
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);
            var tokenService = CreateTokenService();
            var authService = new AuthService(ctx, new PasswordHasherService(), tokenService, store, NullLogger<AuthService>.Instance);

            var login = await authService.LoginAsync(new LoginRequest { Email = "client@nexus.com", Password = "ClientPassword123!" });
            Assert.NotNull(login);
            var token1 = login!.RefreshToken;

            // Legitimate refresh
            var refresh = await authService.RefreshTokenAsync(token1);
            Assert.NotNull(refresh);
            var token2 = refresh!.RefreshToken;

            // Attacker tries to reuse token1
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
        public async Task AuthService_Logout_RevokesAllUserTokens()
        {
            using var ctx = CreateContext(nameof(AuthService_Logout_RevokesAllUserTokens));
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);
            var tokenService = CreateTokenService();
            var authService = new AuthService(ctx, new PasswordHasherService(), tokenService, store, NullLogger<AuthService>.Instance);

            var login = await authService.LoginAsync(new LoginRequest { Email = "admin@nexus.com", Password = "AdminPassword123!" });
            Assert.NotNull(login);

            var revoked = await authService.RevokeTokenAsync(login!.RefreshToken);
            Assert.True(revoked);

            var tokenInStore = await store.GetTokenAsync(login.RefreshToken);
            Assert.NotNull(tokenInStore);
            Assert.True(tokenInStore!.IsRevoked);

            var tryRefresh = await authService.RefreshTokenAsync(login.RefreshToken);
            Assert.Null(tryRefresh);
        }

        [Fact]
        public async Task AuthService_PasswordReset_RevokesAllUserTokens()
        {
            using var ctx = CreateContext(nameof(AuthService_PasswordReset_RevokesAllUserTokens));
            var store = new DbRefreshTokenStore(ctx, NullLogger<DbRefreshTokenStore>.Instance);
            var tokenService = CreateTokenService();
            var authService = new AuthService(ctx, new PasswordHasherService(), tokenService, store, NullLogger<AuthService>.Instance);

            var login = await authService.LoginAsync(new LoginRequest { Email = "admin@nexus.com", Password = "AdminPassword123!" });
            Assert.NotNull(login);

            // Password reset
            var resetResult = await authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "admin@nexus.com",
                Token = "dummy-valid-token",
                NewPassword = "BrandNewAdminPassword123!"
            });
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
