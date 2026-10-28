using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Strategies.Api.Configuration;
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
    /// Per-account sign-in lockout: 5 failed sign-ins within 15 minutes lock the account for 15 minutes,
    /// the same goes for an email that does not exist, and a reset or an admin unlock lifts it.
    /// </summary>
    public class LoginLockoutTests
    {
        private const string Email = "lerato@example.com";
        private const string Password = "Correct-Password-1";
        private const string Wrong = "Wrong-Password-1";

        private sealed class FakeClock : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by) => _now += by;
        }

        private sealed class Setup : IDisposable
        {
            public ApplicationDbContext Context { get; }
            public FakeClock Clock { get; } = new();
            public AuthService Service { get; }
            public User User { get; }

            public Setup(string dbName, LockoutSettings? settings = null, bool isActive = true)
            {
                Context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: dbName)
                    .Options);
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string?>("Jwt:Key", "TestOnlyKey_MustBeAtLeast32CharactersLong!"),
                        new KeyValuePair<string, string?>("Jwt:Issuer", "TestIssuer"),
                        new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                        new KeyValuePair<string, string?>("Jwt:AccessTokenExpirationMinutes", "60"),
                        new KeyValuePair<string, string?>("Jwt:RefreshTokenExpirationDays", "7"),
                    })
                    .Build();
                Service = new AuthService(Context, new PasswordHasherService(), new TokenService(config),
                    new InMemoryRefreshTokenStore(), NullLogger<AuthService>.Instance,
                    new MemoryCache(new MemoryCacheOptions()), Options.Create(settings ?? new LockoutSettings()), Clock);

                User = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Lerato Mahlangu",
                    Email = Email,
                    PasswordHash = new PasswordHasherService().HashPassword(Password),
                    Role = UserRole.Client,
                    IsActive = isActive,
                    EmailConfirmed = true
                };
                Context.Users.Add(User);
                Context.SaveChanges();
            }

            public Task<LoginOutcome> Try(string email, string password) =>
                Service.TryLoginAsync(new LoginRequest { Email = email, Password = password });

            public async Task Fail(int times, string email = Email)
            {
                for (var i = 0; i < times; i++)
                    await Try(email, Wrong);
            }

            public void Dispose() => Context.Dispose();
        }

        private static readonly TimeSpan Fifteen = TimeSpan.FromMinutes(15);

        [Fact]
        public async Task FifthFailedSignIn_LocksTheAccount_AndIsWrittenToTheAudit()
        {
            using var s = new Setup(nameof(FifthFailedSignIn_LocksTheAccount_AndIsWrittenToTheAudit));

            for (var i = 1; i <= 4; i++)
            {
                var outcome = await s.Try(Email, Wrong);
                Assert.Null(outcome.Response);
                Assert.Null(outcome.LockedFor);
            }
            var fifth = await s.Try(Email, Wrong);

            Assert.Null(fifth.Response);
            Assert.Equal(Fifteen, fifth.LockedFor);
            var user = await s.Context.Users.FindAsync(s.User.Id);
            Assert.Equal(s.Clock.GetUtcNow().UtcDateTime + Fifteen, user!.LockoutEndsAt);
            var audit = Assert.Single(s.Context.AuditLogs.Where(a => a.Action == "Account locked"));
            Assert.Equal(s.User.Id, audit.EntityId);
            Assert.Null(audit.UserId);
        }

        [Fact]
        public async Task CorrectPassword_IsRefusedWhileLocked_WithTheTimeLeft()
        {
            using var s = new Setup(nameof(CorrectPassword_IsRefusedWhileLocked_WithTheTimeLeft));
            await s.Fail(5);
            s.Clock.Advance(TimeSpan.FromMinutes(5));

            var outcome = await s.Try(Email, Password);

            Assert.Null(outcome.Response);
            Assert.Equal(TimeSpan.FromMinutes(10), outcome.LockedFor);
        }

        [Fact]
        public async Task AttemptsWhileLocked_DoNotExtendTheLock()
        {
            using var s = new Setup(nameof(AttemptsWhileLocked_DoNotExtendTheLock));
            await s.Fail(5);
            s.Clock.Advance(TimeSpan.FromMinutes(10));
            await s.Fail(3);

            Assert.Equal(TimeSpan.FromMinutes(5), (await s.Try(Email, Wrong)).LockedFor);
        }

        [Fact]
        public async Task AfterTheDuration_TheAccountSignsInAgain_AndTheUnlockIsAudited()
        {
            using var s = new Setup(nameof(AfterTheDuration_TheAccountSignsInAgain_AndTheUnlockIsAudited));
            await s.Fail(5);
            s.Clock.Advance(Fifteen + TimeSpan.FromSeconds(1));

            var outcome = await s.Try(Email, Password);

            Assert.NotNull(outcome.Response);
            var user = await s.Context.Users.FindAsync(s.User.Id);
            Assert.Null(user!.LockoutEndsAt);
            Assert.Equal(0, user.FailedLoginCount);
            Assert.Single(s.Context.AuditLogs.Where(a => a.Action == "Account unlocked" && a.EntityId == s.User.Id));
        }

        [Fact]
        public async Task OneMoreSecond_BeforeTheDurationEnds_StillRefuses()
        {
            using var s = new Setup(nameof(OneMoreSecond_BeforeTheDurationEnds_StillRefuses));
            await s.Fail(5);
            s.Clock.Advance(Fifteen - TimeSpan.FromSeconds(1));

            var outcome = await s.Try(Email, Password);

            Assert.Null(outcome.Response);
            Assert.Equal(TimeSpan.FromSeconds(1), outcome.LockedFor);
        }

        [Fact]
        public async Task AfterAnUnlock_TheCountStartsAgainFromZero()
        {
            using var s = new Setup(nameof(AfterAnUnlock_TheCountStartsAgainFromZero));
            await s.Fail(5);
            s.Clock.Advance(Fifteen + TimeSpan.FromSeconds(1));

            await s.Fail(4);
            Assert.Null((await s.Try(Email, Wrong)).Response);
            Assert.Equal(Fifteen, (await s.Try(Email, Password)).LockedFor);
        }

        [Fact]
        public async Task SuccessfulSignIn_ResetsTheCount()
        {
            using var s = new Setup(nameof(SuccessfulSignIn_ResetsTheCount));
            await s.Fail(4);
            Assert.NotNull((await s.Try(Email, Password)).Response);
            Assert.Equal(0, (await s.Context.Users.FindAsync(s.User.Id))!.FailedLoginCount);

            await s.Fail(4);

            Assert.NotNull((await s.Try(Email, Password)).Response);
        }

        [Fact]
        public async Task FailuresMoreThanTheWindowApart_DoNotAddUp()
        {
            using var s = new Setup(nameof(FailuresMoreThanTheWindowApart_DoNotAddUp));
            await s.Fail(4);
            s.Clock.Advance(TimeSpan.FromMinutes(16));
            await s.Fail(4);

            Assert.NotNull((await s.Try(Email, Password)).Response);
        }

        [Fact]
        public async Task Limits_ComeFromConfiguration()
        {
            using var s = new Setup(nameof(Limits_ComeFromConfiguration),
                new LockoutSettings { MaxAttempts = 2, WindowMinutes = 1, DurationMinutes = 3 });
            await s.Fail(1);

            Assert.Equal(TimeSpan.FromMinutes(3), (await s.Try(Email, Wrong)).LockedFor);
        }

        [Fact]
        public async Task CompletedPasswordReset_LiftsTheLock()
        {
            using var s = new Setup(nameof(CompletedPasswordReset_LiftsTheLock));
            await s.Fail(5);
            var user = (await s.Context.Users.FindAsync(s.User.Id))!;

            await s.Service.ResetPasswordAsync(user, "A-Brand-New-Password-2");

            Assert.NotNull((await s.Try(Email, "A-Brand-New-Password-2")).Response);
            var audit = Assert.Single(s.Context.AuditLogs.Where(a => a.Action == "Account unlocked"));
            Assert.Equal(s.User.Id, audit.UserId);
        }

        [Fact]
        public async Task PasswordReset_OfAnUnlockedAccount_WritesNoUnlockRow()
        {
            using var s = new Setup(nameof(PasswordReset_OfAnUnlockedAccount_WritesNoUnlockRow));
            await s.Fail(2);
            var user = (await s.Context.Users.FindAsync(s.User.Id))!;

            await s.Service.ResetPasswordAsync(user, "A-Brand-New-Password-2");

            Assert.Equal(0, user.FailedLoginCount);
            Assert.Empty(s.Context.AuditLogs.Where(a => a.Action == "Account unlocked"));
        }

        [Fact]
        public async Task AdminReinstate_LiftsTheLock_OfAnActiveAccount_AndNamesTheAdmin()
        {
            using var s = new Setup(nameof(AdminReinstate_LiftsTheLock_OfAnActiveAccount_AndNamesTheAdmin));
            await s.Fail(5);
            var adminId = Guid.NewGuid();

            var result = await AdminController(s.Context, adminId).ReinstateUser(s.User.Id);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull((await s.Try(Email, Password)).Response);
            var audit = Assert.Single(s.Context.AuditLogs.Where(a => a.Action == "Account unlocked"));
            Assert.Equal(adminId, audit.UserId);
            Assert.Empty(s.Context.AuditLogs.Where(a => a.Action == "Reinstated user"));
        }

        [Fact]
        public async Task AdminReinstate_OfASuspendedAndLockedAccount_ClearsBoth()
        {
            using var s = new Setup(nameof(AdminReinstate_OfASuspendedAndLockedAccount_ClearsBoth));
            await s.Fail(5);
            var user = (await s.Context.Users.FindAsync(s.User.Id))!;
            user.IsActive = false;
            await s.Context.SaveChangesAsync();

            await AdminController(s.Context, Guid.NewGuid()).ReinstateUser(s.User.Id);

            Assert.NotNull((await s.Try(Email, Password)).Response);
            Assert.Single(s.Context.AuditLogs.Where(a => a.Action == "Reinstated user"));
            Assert.Single(s.Context.AuditLogs.Where(a => a.Action == "Account unlocked"));
        }

        [Fact]
        public async Task AdminReinstate_OfAnActiveUnlockedAccount_StillConflicts()
        {
            using var s = new Setup(nameof(AdminReinstate_OfAnActiveUnlockedAccount_StillConflicts));

            var result = await AdminController(s.Context, Guid.NewGuid()).ReinstateUser(s.User.Id);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UnknownEmail_GetsTheSameAnswersAsAKnownAccount_AttemptForAttempt()
        {
            using var s = new Setup(nameof(UnknownEmail_GetsTheSameAnswersAsAKnownAccount_AttemptForAttempt));

            var known = new List<(bool signedIn, TimeSpan? locked)>();
            var unknown = new List<(bool signedIn, TimeSpan? locked)>();
            for (var i = 0; i < 7; i++)
            {
                var a = await s.Try(Email, Wrong);
                known.Add((a.Response != null, a.LockedFor));
                var b = await s.Try("nobody@example.com", Wrong);
                unknown.Add((b.Response != null, b.LockedFor));
            }

            Assert.Equal(known, unknown);
            Assert.Equal(Fifteen, unknown[4].locked);
            Assert.All(unknown.Take(4), a => Assert.Null(a.locked));
        }

        [Fact]
        public async Task UnknownEmail_IsRefusedWhileLocked_AndUnlocksAfterTheDuration_LikeARealOne()
        {
            using var s = new Setup(nameof(UnknownEmail_IsRefusedWhileLocked_AndUnlocksAfterTheDuration_LikeARealOne));
            await s.Fail(5, "nobody@example.com");
            s.Clock.Advance(TimeSpan.FromMinutes(5));

            Assert.Equal(TimeSpan.FromMinutes(10), (await s.Try("nobody@example.com", Wrong)).LockedFor);

            s.Clock.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
            var after = await s.Try("nobody@example.com", Wrong);
            Assert.Null(after.Response);
            Assert.Null(after.LockedFor);
        }

        [Fact]
        public async Task UnknownEmail_IsCountedWhateverTheCaseOrSpaces_AndTouchesNoRows()
        {
            using var s = new Setup(nameof(UnknownEmail_IsCountedWhateverTheCaseOrSpaces_AndTouchesNoRows));
            var variants = new[] { "Nobody@Example.com", " nobody@example.com", "nobody@example.com ", "NOBODY@EXAMPLE.COM", "nobody@example.com" };

            TimeSpan? last = null;
            foreach (var variant in variants)
                last = (await s.Try(variant, Wrong)).LockedFor;

            Assert.Equal(Fifteen, last);
            Assert.Single(s.Context.Users);
            Assert.Empty(s.Context.AuditLogs);
        }

        [Fact]
        public async Task SuspendedAccount_IsCountedAndLockedLikeAnUnknownEmail_NeverSigningIn()
        {
            using var s = new Setup(nameof(SuspendedAccount_IsCountedAndLockedLikeAnUnknownEmail_NeverSigningIn), isActive: false);

            for (var i = 0; i < 4; i++)
                Assert.Null((await s.Try(Email, Password)).LockedFor);

            var fifth = await s.Try(Email, Password);
            Assert.Null(fifth.Response);
            Assert.Equal(Fifteen, fifth.LockedFor);
            Assert.Equal(0, (await s.Context.Users.FindAsync(s.User.Id))!.FailedLoginCount);
        }

        [Fact]
        public async Task OneAccountLocked_DoesNotLockAnother()
        {
            using var s = new Setup(nameof(OneAccountLocked_DoesNotLockAnother));
            s.Context.Users.Add(new User
            {
                Id = Guid.NewGuid(), FullName = "Other", Email = "other@example.com", Role = UserRole.Client,
                PasswordHash = new PasswordHasherService().HashPassword(Password), IsActive = true, EmailConfirmed = true
            });
            await s.Context.SaveChangesAsync();
            await s.Fail(5);

            Assert.NotNull((await s.Try("other@example.com", Password)).Response);
        }

        [Fact]
        public async Task Controller_Answers429WithCodeAndRetryAfter_WhenLocked()
        {
            using var s = new Setup(nameof(Controller_Answers429WithCodeAndRetryAfter_WhenLocked));
            await s.Fail(5);
            s.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(500));
            var controller = AuthController(s.Service);

            var result = await controller.Login(new LoginRequest { Email = Email, Password = Password });

            var locked = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, locked.StatusCode);
            var body = Assert.IsType<ApiErrorResponse>(locked.Value);
            Assert.Equal("account_locked", body.Code);
            Assert.Equal("Too many failed sign-in attempts. Try again later, or reset your password.", body.Message);
            Assert.Equal("600", controller.Response.Headers.RetryAfter.ToString());
        }

        [Fact]
        public async Task Controller_WrongPassword_IsTheSame401_ForKnownAndUnknownEmails()
        {
            using var s = new Setup(nameof(Controller_WrongPassword_IsTheSame401_ForKnownAndUnknownEmails));
            var controller = AuthController(s.Service);

            var known = Assert.IsType<UnauthorizedObjectResult>(
                await controller.Login(new LoginRequest { Email = Email, Password = Wrong }));
            var unknown = Assert.IsType<UnauthorizedObjectResult>(
                await controller.Login(new LoginRequest { Email = "nobody@example.com", Password = Wrong }));

            Assert.Equal(known.StatusCode, unknown.StatusCode);
            var a = Assert.IsType<ApiErrorResponse>(known.Value);
            var b = Assert.IsType<ApiErrorResponse>(unknown.Value);
            Assert.Equal(a.Message, b.Message);
            Assert.Equal(a.Code, b.Code);
            Assert.False(controller.Response.Headers.ContainsKey("Retry-After"));
        }

        [Fact]
        public async Task Controller_SignsInNormally_WhenNothingIsLocked()
        {
            using var s = new Setup(nameof(Controller_SignsInNormally_WhenNothingIsLocked));

            var result = await AuthController(s.Service).Login(new LoginRequest { Email = Email, Password = Password });

            Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(result).Value);
        }

        private static AuthController AuthController(IAuthService service) =>
            new(service, null!, null!, null!, null!, null!, null!, null!, NullLogger<AuthController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        private static AdminController AdminController(ApplicationDbContext ctx, Guid adminId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test");

            return new AdminController(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }
    }
}
