using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// Integration tests for /api/auth over HTTP, run against the real pipeline in a TestServer with an
    /// in-memory database and a recording email service: routing (including the alias routes), model
    /// binding, [Authorize], status codes and response bodies. AuthServiceTests and LoginLockoutTests
    /// cover the service rules; these check what a client of the API actually receives.
    /// </summary>
    public class AuthControllerTests : IDisposable
    {
        // Same stand-in for the connection address as LeadsRateLimitTests. Every request gets a new
        // address, so the 5-per-minute "auth-strict" limit never decides a test here.
        private const string RemoteIpHeader = "X-Test-Remote-Ip";
        private static int _nextIp;

        private const string ClientEmail = "lerato@example.com";
        private const string ClientPassword = "a long pass phrase 42";
        private const string ConfirmLinkPrefix = "http://localhost:5173/#/confirm-email?token=";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerTests()
        {
            _factory = new ApiFactory();
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private sealed class RecordingEmailService : IEmailService
        {
            public ConcurrentQueue<(string To, string Link)> Confirmations { get; } = new();
            public ConcurrentQueue<(string To, string Link)> Resets { get; } = new();

            public Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
            {
                Resets.Enqueue((toEmail, resetLink));
                return Task.CompletedTask;
            }

            public Task SendAccountConfirmationEmailAsync(string toEmail, string confirmationLink)
            {
                Confirmations.Enqueue((toEmail, confirmationLink));
                return Task.CompletedTask;
            }
        }

        private sealed class ApiFactory : WebApplicationFactory<Program>
        {
            public RecordingEmailService Email { get; } = new();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");
                foreach (var (key, value) in StartupConfigurationTests.RequiredTestSettings)
                    builder.UseSetting(key, value);

                builder.ConfigureTestServices(services =>
                {
                    services.Insert(0, ServiceDescriptor.Singleton<IStartupFilter, TestRemoteIpStartupFilter>());

                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                    var dbName = Guid.NewGuid().ToString();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));

                    services.RemoveAll<IEmailService>();
                    services.AddSingleton<IEmailService>(Email);
                });
            }
        }

        private sealed class TestRemoteIpStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (IPAddress.TryParse(context.Request.Headers[RemoteIpHeader], out var address))
                        context.Connection.RemoteIpAddress = address;
                    await nextMiddleware();
                });
                next(app);
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private Task<HttpResponseMessage> Post(string path, object body) =>
            Send(HttpMethod.Post, path, JsonSerializer.Serialize(body));

        private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? json = null, string? bearer = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (json != null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            var n = Interlocked.Increment(ref _nextIp);
            request.Headers.Add(RemoteIpHeader, $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}");
            return await _client.SendAsync(request);
        }

        private async Task<User> AddUserAsync(string email = ClientEmail, bool emailConfirmed = true, bool isActive = true)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Lerato Mahlangu",
                Email = email,
                PasswordHash = new PasswordHasherService().HashPassword(ClientPassword),
                Role = UserRole.Client,
                IsActive = isActive,
                EmailConfirmed = emailConfirmed,
                CreatedAt = DateTime.UtcNow
            };
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        private async Task<User?> FindUserAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        private async Task<AuthResponse> SignInAsync()
        {
            var response = await Post("/api/auth/login", new { email = ClientEmail, password = ClientPassword });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        }

        private static async Task<string?> MessageOf(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Message;

        private static object NewRegistration(string email = "thabo@example.com", string password = ClientPassword) =>
            new { email, password, firstName = "Thabo", lastName = "Nkosi" };

        // ── Login ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_WithValidCredentials_Returns200WithTokensRoleAndName()
        {
            var user = await AddUserAsync();

            var auth = await SignInAsync();

            Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
            Assert.True(auth.TokenExpiry > DateTime.UtcNow);
            Assert.Equal("Client", auth.Role);
            Assert.Equal("Lerato Mahlangu", auth.DisplayName);
            Assert.Equal(user.Id, auth.UserId);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401WithTheGenericMessage()
        {
            await AddUserAsync();

            var response = await Post("/api/auth/login", new { email = ClientEmail, password = "not the password at all" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Invalid email or password.", await MessageOf(response));
        }

        [Fact]
        public async Task Login_UnconfirmedEmail_Returns401_SameAsAWrongPassword()
        {
            await AddUserAsync(emailConfirmed: false);

            var response = await Post("/api/auth/login", new { email = ClientEmail, password = ClientPassword });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Invalid email or password.", await MessageOf(response));
        }

        [Fact]
        public async Task Login_SuspendedAccount_Returns401()
        {
            await AddUserAsync(isActive: false);

            var response = await Post("/api/auth/login", new { email = ClientEmail, password = ClientPassword });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_EmptyBody_Returns400WithApiErrorResponse()
        {
            var response = await Send(HttpMethod.Post, "/api/auth/login", "{}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(await MessageOf(response)));
        }

        // ── Refresh ────────────────────────────────────────────────────────

        [Theory]
        [InlineData("/api/auth/refresh")]
        [InlineData("/api/auth/refresh-token")]
        public async Task Refresh_RotatesTheToken_AndTheOldOneIsRefused(string path)
        {
            await AddUserAsync();
            var first = await SignInAsync();

            var refreshed = await Post(path, new { refreshToken = first.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
            var second = (await refreshed.Content.ReadFromJsonAsync<AuthResponse>())!;
            Assert.NotEqual(first.RefreshToken, second.RefreshToken);
            Assert.False(string.IsNullOrWhiteSpace(second.AccessToken));

            var reused = await Post(path, new { refreshToken = first.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
            Assert.Equal("Refresh token is invalid or has expired. Please sign in again.", await MessageOf(reused));
        }

        [Fact]
        public async Task Refresh_OldTokenReusedAfterTheGraceWindow_Returns401_AndSignsOutEverySession()
        {
            await AddUserAsync();
            var phone = await SignInAsync();
            var first = await SignInAsync();
            var second = (await (await Post("/api/auth/refresh", new { refreshToken = first.RefreshToken }))
                .Content.ReadFromJsonAsync<AuthResponse>())!;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hash = DbRefreshTokenStore.HashToken(first.RefreshToken);
                var rotated = await db.RefreshTokens.SingleAsync(r => r.TokenHash == hash);
                rotated.RevokedAt = DateTime.UtcNow - AuthService.RotationReuseGrace - TimeSpan.FromSeconds(1);
                await db.SaveChangesAsync();
            }

            var reused = await Post("/api/auth/refresh", new { refreshToken = first.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/refresh", new { refreshToken = second.RefreshToken })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/refresh", new { refreshToken = phone.RefreshToken })).StatusCode);
        }

        [Fact]
        public async Task Refresh_OldTokenSentAgainAtOnce_Returns401_ButTheNewTokenStillWorks()
        {
            await AddUserAsync();
            var first = await SignInAsync();
            var second = (await (await Post("/api/auth/refresh", new { refreshToken = first.RefreshToken }))
                .Content.ReadFromJsonAsync<AuthResponse>())!;

            var duplicate = await Post("/api/auth/refresh", new { refreshToken = first.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, duplicate.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Post("/api/auth/refresh", new { refreshToken = second.RefreshToken })).StatusCode);
        }

        [Fact]
        public async Task Logout_RevokesEveryTokenOfTheUser_InTheDatabase()
        {
            var user = await AddUserAsync();
            var browser = await SignInAsync();
            await SignInAsync();

            Assert.Equal(HttpStatusCode.OK, (await Post("/api/auth/logout", new { refreshToken = browser.RefreshToken })).StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rows = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == user.Id).ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.True(r.IsRevoked));
            Assert.All(rows, r => Assert.NotEqual(browser.RefreshToken, r.TokenHash));
        }

        [Fact]
        public async Task Logout_WithOnlyTheAccessToken_RevokesEveryRefreshToken()
        {
            await AddUserAsync();
            var browser = await SignInAsync();
            var phone = await SignInAsync();

            var response = await Send(HttpMethod.Post, "/api/auth/logout", bearer: browser.AccessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/refresh", new { refreshToken = browser.RefreshToken })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/refresh", new { refreshToken = phone.RefreshToken })).StatusCode);
        }

        [Fact]
        public async Task Logout_WithNoTokenAtAll_Returns400()
        {
            var response = await Send(HttpMethod.Post, "/api/auth/logout");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_UnknownToken_Returns401()
        {
            var response = await Post("/api/auth/refresh", new { refreshToken = "made-up-token" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_EmptyBody_Returns400()
        {
            var response = await Send(HttpMethod.Post, "/api/auth/refresh", "{}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── Logout (revoke) ────────────────────────────────────────────────

        [Theory]
        [InlineData("/api/auth/revoke")]
        [InlineData("/api/auth/revoke-token")]
        public async Task Revoke_Returns200_AndTheRefreshTokenStopsWorking(string path)
        {
            await AddUserAsync();
            var auth = await SignInAsync();

            var signedOut = await Post(path, new { refreshToken = auth.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, signedOut.StatusCode);
            var refreshed = await Post("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, refreshed.StatusCode);
        }

        [Fact]
        public async Task Revoke_UnknownToken_StillReturns200_SoTokensCannotBeProbed()
        {
            var response = await Post("/api/auth/revoke", new { refreshToken = "made-up-token" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Revoke_EmptyBody_Returns400()
        {
            var response = await Send(HttpMethod.Post, "/api/auth/revoke", "{}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── Me ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Me_WithoutAToken_Returns401()
        {
            var response = await Send(HttpMethod.Get, "/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_WithTheAccessToken_ReturnsTheSignedInUser()
        {
            var user = await AddUserAsync();
            var auth = await SignInAsync();

            var response = await Send(HttpMethod.Get, "/api/auth/me", bearer: auth.AccessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(user.Id, body.RootElement.GetProperty("userId").GetGuid());
            Assert.Equal(ClientEmail, body.RootElement.GetProperty("email").GetString());
            Assert.Equal("Client", body.RootElement.GetProperty("role").GetString());
            Assert.True(body.RootElement.GetProperty("emailConfirmed").GetBoolean());
        }

        // ── Register and email confirmation ────────────────────────────────

        [Fact]
        public async Task Register_Returns201_CreatesAnUnconfirmedClient_AndEmailsAConfirmationLink()
        {
            var response = await Post("/api/auth/register", NewRegistration());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var user = await FindUserAsync("thabo@example.com");
            Assert.NotNull(user);
            Assert.False(user!.EmailConfirmed);
            Assert.Equal(UserRole.Client, user.Role);
            var (to, link) = Assert.Single(_factory.Email.Confirmations);
            Assert.Equal("thabo@example.com", to);
            Assert.StartsWith(ConfirmLinkPrefix, link);
        }

        [Fact]
        public async Task Register_ThenConfirm_ThenSignIn_WorksEndToEnd()
        {
            Assert.Equal(HttpStatusCode.Created, (await Post("/api/auth/register", NewRegistration())).StatusCode);
            var beforeConfirming = await Post("/api/auth/login", new { email = "thabo@example.com", password = ClientPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, beforeConfirming.StatusCode);

            var (_, link) = Assert.Single(_factory.Email.Confirmations);
            var token = Uri.UnescapeDataString(link[ConfirmLinkPrefix.Length..]);
            var confirmed = await Post("/api/auth/confirm-email", new { token });

            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            var signedIn = await Post("/api/auth/login", new { email = "thabo@example.com", password = ClientPassword });
            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);

            var reused = await Post("/api/auth/confirm-email", new { token });
            Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
            Assert.Equal("Invalid or expired confirmation token.", await MessageOf(reused));
        }

        [Fact]
        public async Task Register_ExistingEmail_Returns400_AndSendsNoEmail()
        {
            await AddUserAsync(email: "thabo@example.com");

            var response = await Post("/api/auth/register", NewRegistration());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Email.Confirmations);
        }

        [Fact]
        public async Task Register_WeakPassword_Returns400WithPolicyErrors_AndCreatesNoUser()
        {
            var response = await Post("/api/auth/register", NewRegistration(password: "short"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
            Assert.Equal("Password does not meet the requirements.", body.Message);
            Assert.NotEmpty(body.Errors);
            Assert.Null(await FindUserAsync("thabo@example.com"));
        }

        [Fact]
        public async Task Register_InvalidEmail_Returns400()
        {
            var response = await Post("/api/auth/register", NewRegistration(email: "not-an-email"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Email.Confirmations);
        }

        [Fact]
        public async Task ConfirmEmail_MadeUpToken_Returns400()
        {
            var response = await Post("/api/auth/confirm-email", new { token = "x" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Invalid or expired confirmation token.", await MessageOf(response));
        }

        [Fact]
        public async Task ResendConfirmation_UnconfirmedAndUnknownEmails_GetTheSameAnswer()
        {
            await AddUserAsync(emailConfirmed: false);

            var known = await Post("/api/auth/resend-confirmation", new { email = ClientEmail });
            var unknown = await Post("/api/auth/resend-confirmation", new { email = "nobody@example.com" });

            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
            Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
            Assert.Equal(await unknown.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());
        }

        // ── Forgot password ────────────────────────────────────────────────

        [Fact]
        public async Task ForgotPassword_KnownAndUnknownEmails_GetTheSameAnswer_OnlyTheKnownOneIsEmailed()
        {
            await AddUserAsync();

            var known = await Post("/api/auth/forgot-password", new { email = ClientEmail });
            var unknown = await Post("/api/auth/forgot-password", new { email = "nobody@example.com" });

            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
            Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
            Assert.Equal(await unknown.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());
            var (to, link) = Assert.Single(_factory.Email.Resets);
            Assert.Equal(ClientEmail, to);
            Assert.StartsWith("http://localhost:5173/#/reset-password?token=", link);
        }
    }
}
