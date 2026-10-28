using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Strategies.Api.Configuration;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs.Auth;

namespace Strategies.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ILogger<AuthService> _logger;
        private readonly IMemoryCache _attempts;
        private readonly LockoutSettings _lockout;
        private readonly TimeProvider _time;

        // Checked against when the email is unknown or the account cannot sign in, so those attempts cost
        // the same time as a wrong password. Computed once: hashing is deliberately slow.
        private static readonly Lazy<string> DummyHash =
            new(() => new PasswordHasherService().HashPassword("not-a-real-password"));

        public AuthService(
            ApplicationDbContext dbContext,
            IPasswordHasherService passwordHasher,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,
            ILogger<AuthService> logger,
            IMemoryCache attempts,
            IOptions<LockoutSettings> lockout,
            TimeProvider time)
        {
            _dbContext          = dbContext;
            _passwordHasher     = passwordHasher;
            _tokenService       = tokenService;
            _refreshTokenStore  = refreshTokenStore;
            _logger             = logger;
            _attempts           = attempts;
            _lockout            = lockout.Value;
            _time               = time;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Database only. If it is unreachable the exception surfaces; nobody is authenticated.
            return await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request) =>
            (await TryLoginAsync(request)).Response;

        // Sign-in with lockout. Accounts that can sign in keep their failure count on the user row.
        // Everything else (unknown email, suspended, deleted, unconfirmed) is counted per submitted
        // email in memory and checked against a dummy hash, so a guess at a real account and a guess at
        // a made-up one get the same answers, in the same order, at the same cost. That includes the
        // lockout itself: an address that does not exist locks after the same number of attempts.
        public async Task<LoginOutcome> TryLoginAsync(LoginRequest request)
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = email.Length == 0
                ? null
                : await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            var canSignIn = user != null && user.IsActive && user.EmailConfirmed && user.DeletedAt == null;

            if (canSignIn)
            {
                if (ClearExpiredLock(user!, now))
                    await _dbContext.SaveChangesAsync();
                if (user!.LockoutEndsAt is { } until && until > now)
                    return new LoginOutcome(null, until - now);
            }
            else
            {
                var held = HeldFor(AttemptsFor(email), now);
                if (held != null)
                    return new LoginOutcome(null, held);
            }

            var passwordOk = _passwordHasher.VerifyPassword(
                request.Password, canSignIn ? user!.PasswordHash : DummyHash.Value);

            if (!canSignIn || !passwordOk)
            {
                var lockedFor = canSignIn
                    ? await RecordFailureAsync(user!, now)
                    : RecordFailure(AttemptsFor(email), now);
                return new LoginOutcome(null, lockedFor);
            }

            if (user!.FailedLoginCount != 0 || user.FailedLoginWindowStart != null)
            {
                user.FailedLoginCount = 0;
                user.FailedLoginWindowStart = null;
                await _dbContext.SaveChangesAsync();
            }

            var (accessToken, expiration) = _tokenService.GenerateAccessToken(user);
            var refreshTokenString        = _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token       = refreshTokenString,
                UserId      = user.Id,
                CreatedAt   = DateTime.UtcNow,
                ExpiresAt   = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
                IsRevoked   = false
            };

            await _refreshTokenStore.SaveTokenAsync(refreshToken);

            return new LoginOutcome(new AuthResponse
            {
                AccessToken  = accessToken,
                TokenExpiry  = expiration,
                RefreshToken = refreshTokenString,
                Role         = user.Role.ToString(),
                DisplayName  = user.FullName,
                UserId       = user.Id
            });
        }

        // Counts one failed sign-in for a real account. Returns the lock time when this one locks it.
        private async Task<TimeSpan?> RecordFailureAsync(User user, DateTime now)
        {
            if (user.FailedLoginWindowStart == null || now - user.FailedLoginWindowStart.Value > _lockout.Window)
            {
                user.FailedLoginWindowStart = now;
                user.FailedLoginCount = 0;
            }
            user.FailedLoginCount++;

            TimeSpan? lockedFor = null;
            if (user.FailedLoginCount >= _lockout.MaxAttempts)
            {
                user.LockoutEndsAt = now + _lockout.Duration;
                lockedFor = _lockout.Duration;
                AddAudit(null, "Account locked", user.Id, now);
                _logger.LogWarning("Account {UserId} locked until {Until} after {Count} failed sign-ins.",
                    user.Id, user.LockoutEndsAt, user.FailedLoginCount);
            }

            await _dbContext.SaveChangesAsync();
            return lockedFor;
        }

        // The lock ran out: clear it and start the count again (the next attempt, not a timer, notices).
        private bool ClearExpiredLock(User user, DateTime now)
        {
            if (user.LockoutEndsAt == null || user.LockoutEndsAt > now)
                return false;

            ClearLockout(user);
            AddAudit(null, "Account unlocked", user.Id, now);
            return true;
        }

        private static void ClearLockout(User user)
        {
            user.FailedLoginCount = 0;
            user.FailedLoginWindowStart = null;
            user.LockoutEndsAt = null;
        }

        private void AddAudit(Guid? actorId, string action, Guid userId, DateTime now) =>
            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = action,
                EntityType = "User",
                EntityId = userId,
                CreatedAt = now
            });

        private sealed class Attempts
        {
            public int Count;
            public DateTime WindowStart;
            public DateTime? LockedUntil;
        }

        // In-memory counter for one submitted email. The key is a hash, so the cache never holds addresses
        // and a very long value cannot make a very long key.
        private Attempts AttemptsFor(string email)
        {
            var key = "login-attempts:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email)));
            return _attempts.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = (_lockout.Window > _lockout.Duration ? _lockout.Window : _lockout.Duration) * 2;
                return new Attempts();
            })!;
        }

        private static TimeSpan? HeldFor(Attempts attempts, DateTime now)
        {
            lock (attempts)
                return attempts.LockedUntil is { } until && until > now ? until - now : null;
        }

        private TimeSpan? RecordFailure(Attempts attempts, DateTime now)
        {
            lock (attempts)
            {
                if (attempts.LockedUntil != null && attempts.LockedUntil <= now)
                    attempts.LockedUntil = null;
                if (attempts.Count == 0 || now - attempts.WindowStart > _lockout.Window)
                {
                    attempts.WindowStart = now;
                    attempts.Count = 0;
                }
                attempts.Count++;
                if (attempts.Count < _lockout.MaxAttempts)
                    return null;

                attempts.LockedUntil = now + _lockout.Duration;
                attempts.Count = 0;
                return _lockout.Duration;
            }
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshTokenString)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenString))
                return null;

            var existingToken = await _refreshTokenStore.GetTokenAsync(refreshTokenString);
            if (existingToken == null || !existingToken.IsActive)
                return null;

            var user = await GetUserByIdAsync(existingToken.UserId);
            if (user == null || !user.IsActive)
                return null;

            var (newAccessToken, expiration) = _tokenService.GenerateAccessToken(user);
            var newRefreshTokenString        = _tokenService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                Token     = newRefreshTokenString,
                UserId    = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            await _refreshTokenStore.RevokeTokenAsync(refreshTokenString, replacedByToken: newRefreshTokenString);
            await _refreshTokenStore.SaveTokenAsync(newRefreshToken);

            return new AuthResponse
            {
                AccessToken  = newAccessToken,
                TokenExpiry  = expiration,
                RefreshToken = newRefreshTokenString,
                Role         = user.Role.ToString(),
                DisplayName  = user.FullName,
                UserId       = user.Id
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshTokenString)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenString))
                return false;

            var existing = await _refreshTokenStore.GetTokenAsync(refreshTokenString);
            if (existing == null || existing.IsRevoked)
                return false;

            await _refreshTokenStore.RevokeTokenAsync(refreshTokenString);
            return true;
        }

        public async Task<User?> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null) return null;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim().ToLowerInvariant(),
                FullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = UserRole.Client,
                IsActive = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Marks the email of a user already identified by a validated confirmation token
        /// (IEmailConfirmationTokenService) as confirmed, which also makes that token unusable.
        /// </summary>
        public async Task<bool> ConfirmEmailAsync(User user)
        {
            user.EmailConfirmed = true;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Sets a new password for a user already identified by a validated reset token
        /// (IPasswordResetTokenService) and revokes all of their refresh tokens.
        /// The caller checks the password against IPasswordPolicy first.
        /// </summary>
        public async Task<bool> ResetPasswordAsync(User user, string newPassword)
        {
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);

            // A completed reset proves control of the email address, so it also lifts any lockout.
            var wasLocked = user.LockoutEndsAt != null;
            ClearLockout(user);
            if (wasLocked)
                AddAudit(user.Id, "Account unlocked", user.Id, _time.GetUtcNow().UtcDateTime);

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            await _refreshTokenStore.RevokeAllUserTokensAsync(user.Id);
            return true;
        }
    }
}
