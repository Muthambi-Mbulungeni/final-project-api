using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class TokenServiceTests
    {
        private static TokenService CreateService(int expiryMinutes = 60, int refreshDays = 7)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Key",      "TestOnlyKey_MustBeAtLeast32CharactersLong!"),
                    new KeyValuePair<string, string?>("Jwt:Issuer",   "TestIssuer"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                    new KeyValuePair<string, string?>("Jwt:AccessTokenExpirationMinutes", expiryMinutes.ToString()),
                    new KeyValuePair<string, string?>("Jwt:RefreshTokenExpirationDays",   refreshDays.ToString()),
                })
                .Build();
            return new TokenService(config);
        }

        private static User MakeUser(UserRole role = UserRole.Consultant) => new User
        {
            Id       = Guid.NewGuid(),
            FullName = "Test User",
            Email    = "test@nexus.com",
            Role     = role,
            IsActive = true
        };

        [Fact]
        public void GenerateAccessToken_ReturnsNonEmptyToken()
        {
            var svc   = CreateService();
            var (token, _) = svc.GenerateAccessToken(MakeUser());

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GenerateAccessToken_ExpiryIsInFuture()
        {
            var svc = CreateService(expiryMinutes: 60);
            var (_, expiry) = svc.GenerateAccessToken(MakeUser());

            Assert.True(expiry > DateTime.UtcNow);
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.Consultant)]
        [InlineData(UserRole.Client)]
        public void GenerateAccessToken_ContainsCorrectRoleClaim(UserRole role)
        {
            var svc  = CreateService();
            var user = MakeUser(role);
            var (token, _) = svc.GenerateAccessToken(user);

            var handler = new JwtSecurityTokenHandler();
            var jwt     = handler.ReadJwtToken(token);

            var roleClaim = jwt.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Role || c.Type == "role");

            Assert.NotNull(roleClaim);
            Assert.Equal(role.ToString(), roleClaim.Value);
        }

        [Fact]
        public void GenerateAccessToken_ContainsCorrectSubject()
        {
            var svc  = CreateService();
            var user = MakeUser();
            var (token, _) = svc.GenerateAccessToken(user);

            var handler = new JwtSecurityTokenHandler();
            var jwt     = handler.ReadJwtToken(token);

            var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            Assert.Equal(user.Id.ToString(), sub);
        }

        [Fact]
        public void GenerateRefreshToken_IsUrlSafe()
        {
            var svc   = CreateService();
            var token = svc.GenerateRefreshToken();

            Assert.False(token.Contains('+'), "Refresh token must not contain '+'");
            Assert.False(token.Contains('/'), "Refresh token must not contain '/'");
            Assert.False(token.Contains('='), "Refresh token must not contain padding '='");
        }

        [Fact]
        public void GenerateRefreshToken_HasSufficientEntropy()
        {
            var svc   = CreateService();
            var token = svc.GenerateRefreshToken();

            // 64 random bytes -> base64url without padding -> at least 80 chars
            Assert.True(token.Length >= 80, $"Refresh token length {token.Length} is too short.");
        }

        [Fact]
        public void GenerateRefreshToken_EachCallIsUnique()
        {
            var svc    = CreateService();
            var token1 = svc.GenerateRefreshToken();
            var token2 = svc.GenerateRefreshToken();

            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void Constructor_ThrowsWhenJwtKeyMissing()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Issuer",   "TestIssuer"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                })
                .Build();

            Assert.Throws<InvalidOperationException>(() => new TokenService(config));
        }
    }
}
