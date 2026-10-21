using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class PasswordResetTokenServiceTests
    {
        private const string JwtKey = "TestOnlyKey_MustBeAtLeast32CharactersLong!";

        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IConfiguration CreateConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Key",      JwtKey),
                    new KeyValuePair<string, string?>("Jwt:Issuer",   "TestIssuer"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                })
                .Build();
        }

        private static PasswordResetTokenService CreateService(ApplicationDbContext ctx)
        {
            return new PasswordResetTokenService(CreateConfig(), ctx, NullLogger<PasswordResetTokenService>.Instance);
        }

        private static async Task<User> AddUserAsync(ApplicationDbContext ctx, string password = "original pass phrase")
        {
            var user = new User
            {
                Id             = Guid.NewGuid(),
                FullName       = "Reset Tester",
                Email          = "reset.tester@example.com",
                PasswordHash   = new PasswordHasherService().HashPassword(password),
                Role           = UserRole.Client,
                IsActive       = true,
                EmailConfirmed = true,
                CreatedAt      = DateTime.UtcNow
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
            return user;
        }

        // Builds a token by hand with the reset key and audience, so a single claim or the lifetime can differ.
        private static string CreateTokenByHand(User user, string purpose, DateTime notBefore, DateTime expires)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(PasswordResetTokenService.PurposeClaim, purpose),
                    new Claim(PasswordResetTokenService.PasswordStampClaim, PasswordResetTokenService.PasswordStamp(user.PasswordHash))
                }),
                Audience           = PasswordResetTokenService.Audience,
                NotBefore          = notBefore,
                IssuedAt           = notBefore,
                Expires            = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        [Fact]
        public async Task ValidateToken_GenuineToken_ReturnsUser()
        {
            using var ctx = CreateContext(nameof(ValidateToken_GenuineToken_ReturnsUser));
            var user = await AddUserAsync(ctx);
            var svc = CreateService(ctx);

            var token = svc.CreateToken(user);
            var result = await svc.ValidateTokenAsync(token);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }

        [Fact]
        public async Task ValidateToken_AfterPasswordChanged_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(ValidateToken_AfterPasswordChanged_ReturnsNull));
            var user = await AddUserAsync(ctx);
            var svc = CreateService(ctx);
            var token = svc.CreateToken(user);

            // Simulates the token having been used: the stored hash is now different.
            var stored = await ctx.Users.SingleAsync(u => u.Id == user.Id);
            stored.PasswordHash = new PasswordHasherService().HashPassword("a brand new pass phrase");
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var result = await svc.ValidateTokenAsync(token);

            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateToken_WrongPurpose_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(ValidateToken_WrongPurpose_ReturnsNull));
            var user = await AddUserAsync(ctx);
            var svc = CreateService(ctx);
            var token = CreateTokenByHand(user, "email_confirmation", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30));

            var result = await svc.ValidateTokenAsync(token);

            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateToken_Expired_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(ValidateToken_Expired_ReturnsNull));
            var user = await AddUserAsync(ctx);
            var svc = CreateService(ctx);
            var token = CreateTokenByHand(user, PasswordResetTokenService.Purpose, DateTime.UtcNow.AddMinutes(-40), DateTime.UtcNow.AddMinutes(-10));

            var result = await svc.ValidateTokenAsync(token);

            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateToken_AccessToken_ReturnsNull()
        {
            using var ctx = CreateContext(nameof(ValidateToken_AccessToken_ReturnsNull));
            var user = await AddUserAsync(ctx);
            var svc = CreateService(ctx);
            var (accessToken, _) = new TokenService(CreateConfig()).GenerateAccessToken(user);

            var result = await svc.ValidateTokenAsync(accessToken);

            Assert.Null(result);
        }
    }
}
