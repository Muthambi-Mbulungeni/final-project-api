using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
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

        // ---- Email confirmation: register, confirm-email, resend-confirmation ----

        private const string JwtKey = "TestOnlyKey_MustBeAtLeast32CharactersLong!";
        private const string BaseUrl = "http://localhost:5173";
        private const string ConfirmLinkPrefix = BaseUrl + "/#/confirm-email?token=";
        private const string NewUserEmail = "thandi@example.com";
        private const string NewUserPassword = "a long pass phrase 42";

        // Records every email instead of sending it. FirstSent completes on the first confirmation email.
        private sealed class FakeEmailService : IEmailService
        {
            public ConcurrentQueue<(string To, string Link)> Confirmations { get; } = new();
            public TaskCompletionSource FirstSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task SendPasswordResetEmailAsync(string toEmail, string resetLink) => Task.CompletedTask;

            public Task SendAccountConfirmationEmailAsync(string toEmail, string confirmationLink)
            {
                Confirmations.Enqueue((toEmail, confirmationLink));
                FirstSent.TrySetResult();
                return Task.CompletedTask;
            }
        }

        private sealed class TestEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Testing";
            public string ApplicationName { get; set; } = "Strategies.Api";
            public string WebRootPath { get; set; } = string.Empty;
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private static IConfiguration CreateAppConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Key",     JwtKey),
                    new KeyValuePair<string, string?>("App:BaseUrl", BaseUrl),
                })
                .Build();

        private static EmailConfirmationTokenService CreateConfirmationTokenService(ApplicationDbContext ctx) =>
            new(CreateAppConfig(), ctx, NullLogger<EmailConfirmationTokenService>.Instance);

        private static AuthController CreateAuthController(ApplicationDbContext ctx, FakeEmailService email)
        {
            var config = CreateAppConfig();
            var scopeFactory = new ServiceCollection()
                .AddSingleton<IEmailService>(email)
                .BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            return new AuthController(
                CreateAuthService(ctx),
                email,
                new PasswordResetTokenService(config, ctx, NullLogger<PasswordResetTokenService>.Instance),
                new PasswordPolicy(),
                CreateConfirmationTokenService(ctx),
                scopeFactory,
                config,
                new TestEnvironment(),
                NullLogger<AuthController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        private static async Task<User> AddUserAsync(ApplicationDbContext ctx, string email, bool emailConfirmed)
        {
            var user = new User
            {
                Id             = Guid.NewGuid(),
                FullName       = "Thandi Nkosi",
                Email          = email,
                PasswordHash   = new PasswordHasherService().HashPassword(NewUserPassword),
                Role           = UserRole.Client,
                IsActive       = true,
                EmailConfirmed = emailConfirmed,
                CreatedAt      = DateTime.UtcNow
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
            return user;
        }

        private static async Task<bool> IsConfirmedAsync(ApplicationDbContext ctx, Guid userId)
        {
            ctx.ChangeTracker.Clear();
            return (await ctx.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).EmailConfirmed;
        }

        // A confirmation token with the right key, audience and purpose, but already expired.
        private static string CreateExpiredConfirmationToken(User user)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(EmailConfirmationTokenService.PurposeClaim, EmailConfirmationTokenService.Purpose)
                }),
                Audience           = EmailConfirmationTokenService.Audience,
                NotBefore          = DateTime.UtcNow.AddHours(-25),
                IssuedAt           = DateTime.UtcNow.AddHours(-25),
                Expires            = DateTime.UtcNow.AddHours(-1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private static void AssertInvalidTokenResponse(IActionResult result)
        {
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("Invalid or expired confirmation token.", body.Message);
            Assert.Empty(body.Errors);
            Assert.Null(body.Code);
        }

        [Fact]
        public async Task Register_SendsConfirmationEmail_WithAValidToken()
        {
            using var ctx = CreateContext(nameof(Register_SendsConfirmationEmail_WithAValidToken));
            var email = new FakeEmailService();

            var result = await CreateAuthController(ctx, email).Register(new RegisterRequest
            {
                Email     = NewUserEmail,
                Password  = NewUserPassword,
                FirstName = "Thandi",
                LastName  = "Nkosi"
            });

            Assert.Equal(201, Assert.IsType<ObjectResult>(result).StatusCode);
            var sent = Assert.Single(email.Confirmations);
            Assert.Equal(NewUserEmail, sent.To);
            Assert.StartsWith(ConfirmLinkPrefix, sent.Link);

            var token = Uri.UnescapeDataString(sent.Link[ConfirmLinkPrefix.Length..]);
            ctx.ChangeTracker.Clear();
            var user = await CreateConfirmationTokenService(ctx).ValidateTokenAsync(token);
            Assert.NotNull(user);
            Assert.Equal(NewUserEmail, user!.Email);
            Assert.False(user.EmailConfirmed);
        }

        [Fact]
        public async Task ConfirmEmail_WithValidToken_Returns200_AndConfirmsTheAccount()
        {
            using var ctx = CreateContext(nameof(ConfirmEmail_WithValidToken_Returns200_AndConfirmsTheAccount));
            var user = await AddUserAsync(ctx, NewUserEmail, emailConfirmed: false);
            var token = CreateConfirmationTokenService(ctx).CreateToken(user);

            var result = await CreateAuthController(ctx, new FakeEmailService())
                .ConfirmEmail(new ConfirmEmailRequest { Token = token });

            Assert.IsType<OkObjectResult>(result);
            Assert.True(await IsConfirmedAsync(ctx, user.Id));
        }

        [Fact]
        public async Task ConfirmEmail_ThenSignIn_Succeeds()
        {
            using var ctx = CreateContext(nameof(ConfirmEmail_ThenSignIn_Succeeds));
            var user = await AddUserAsync(ctx, NewUserEmail, emailConfirmed: false);
            var login = new LoginRequest { Email = NewUserEmail, Password = NewUserPassword };
            Assert.Null(await CreateAuthService(ctx).LoginAsync(login));
            ctx.ChangeTracker.Clear();

            await CreateAuthController(ctx, new FakeEmailService())
                .ConfirmEmail(new ConfirmEmailRequest { Token = CreateConfirmationTokenService(ctx).CreateToken(user) });
            ctx.ChangeTracker.Clear();

            Assert.NotNull(await CreateAuthService(ctx).LoginAsync(login));
        }

        [Fact]
        public async Task ConfirmEmail_SameTokenTwice_SecondIs400()
        {
            using var ctx = CreateContext(nameof(ConfirmEmail_SameTokenTwice_SecondIs400));
            var user = await AddUserAsync(ctx, NewUserEmail, emailConfirmed: false);
            var token = CreateConfirmationTokenService(ctx).CreateToken(user);
            var controller = CreateAuthController(ctx, new FakeEmailService());

            Assert.IsType<OkObjectResult>(await controller.ConfirmEmail(new ConfirmEmailRequest { Token = token }));
            ctx.ChangeTracker.Clear();

            AssertInvalidTokenResponse(await controller.ConfirmEmail(new ConfirmEmailRequest { Token = token }));
        }

        [Theory]
        [InlineData("garbage")]
        [InlineData("empty")]
        [InlineData("missing")]
        [InlineData("expired")]
        [InlineData("reset-token")]
        [InlineData("access-token")]
        public async Task ConfirmEmail_WithInvalidOrExpiredToken_Returns400_AndLeavesEmailUnconfirmed(string kind)
        {
            using var ctx = CreateContext(nameof(ConfirmEmail_WithInvalidOrExpiredToken_Returns400_AndLeavesEmailUnconfirmed) + kind);
            var user = await AddUserAsync(ctx, NewUserEmail, emailConfirmed: false);
            var token = kind switch
            {
                "garbage"      => "this-is-not-a-real-token",
                "empty"        => "",
                "missing"      => null,
                "expired"      => CreateExpiredConfirmationToken(user),
                // Signed with the same key, but must not double as a confirmation token.
                "reset-token"  => new PasswordResetTokenService(CreateAppConfig(), ctx, NullLogger<PasswordResetTokenService>.Instance).CreateToken(user),
                "access-token" => CreateTokenService().GenerateAccessToken(user).Item1,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

            var result = await CreateAuthController(ctx, new FakeEmailService())
                .ConfirmEmail(new ConfirmEmailRequest { Token = token });

            AssertInvalidTokenResponse(result);
            Assert.False(await IsConfirmedAsync(ctx, user.Id));
        }

        [Fact]
        public async Task ResendConfirmation_Returns200WithTheSameBody_ForUnconfirmedConfirmedAndUnknownEmails()
        {
            using var ctx = CreateContext(nameof(ResendConfirmation_Returns200WithTheSameBody_ForUnconfirmedConfirmedAndUnknownEmails));
            await AddUserAsync(ctx, "unconfirmed@example.com", emailConfirmed: false);
            await AddUserAsync(ctx, "confirmed@example.com", emailConfirmed: true);
            var email = new FakeEmailService();
            var controller = CreateAuthController(ctx, email);

            var bodies = new List<string>();
            foreach (var address in new[] { "unconfirmed@example.com", "confirmed@example.com", "nobody@example.com" })
            {
                var ok = Assert.IsType<OkObjectResult>(
                    await controller.ResendConfirmation(new ResendConfirmationRequest { Email = address }));
                bodies.Add(JsonSerializer.Serialize(ok.Value));
            }

            Assert.Single(new HashSet<string>(bodies));

            // Only the unconfirmed account gets an email, sent after the response.
            await email.FirstSent.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(200);
            var sent = Assert.Single(email.Confirmations);
            Assert.Equal("unconfirmed@example.com", sent.To);
            Assert.StartsWith(ConfirmLinkPrefix, sent.Link);
        }
    }
}
