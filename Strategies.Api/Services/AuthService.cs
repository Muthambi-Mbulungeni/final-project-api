
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

        // Used when the email is unknown or the account cannot sign in.
        // This ensures password verification takes approximately the same time.
        private static readonly Lazy<string> DummyHash =
            new(() => new PasswordHasherService().HashPassword("not-a-real-password"));

        // How long after a rotation the old refresh token is refused quietly instead of being treated
        // as stolen (which revokes every session of the user). The store stamps RevokedAt with
        // DateTime.UtcNow, so the check uses the same clock.
        public static readonly TimeSpan RotationReuseGrace = TimeSpan.FromSeconds(30);

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
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _logger = logger;
            _attempts = attempts;
            _lockout = lockout.Value;
            _time = time;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Database only. If unavailable, the exception surfaces.
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

        // Sign-in with lockout.
        public async Task<LoginOutcome> TryLoginAsync(LoginRequest request)
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = email.Length == 0
                ? null
                : await _dbContext.Users.FirstOrDefaultAsync(
                    u => u.Email.ToLower() == email);

            var canSignIn = user != null &&
                            user.IsActive &&
                            user.EmailConfirmed &&
                            user.DeletedAt == null;

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
                request.Password,
                canSignIn ? user!.PasswordHash : DummyHash.Value);

            if (!canSignIn || !passwordOk)
            {
                var lockedFor = canSignIn
                    ? await RecordFailureAsync(user!, now)
                    : RecordFailure(AttemptsFor(email), now);

                return new LoginOutcome(null, lockedFor);
            }

            if (user!.FailedLoginCount != 0 ||
                user.FailedLoginWindowStart != null)
            {
                user.FailedLoginCount = 0;
                user.FailedLoginWindowStart = null;
                await _dbContext.SaveChangesAsync();
            }

            var (accessToken, expiration) =
                _tokenService.GenerateAccessToken(user);

            var refreshTokenString =
                _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    _tokenService.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            await _refreshTokenStore.SaveTokenAsync(refreshToken);

            return new LoginOutcome(new AuthResponse
            {
                AccessToken = accessToken,
                TokenExpiry = expiration,
                RefreshToken = refreshTokenString,
                Role = user.Role.ToString(),
                DisplayName = user.FullName,
                UserId = user.Id
            });
        }

        // Counts one failed sign-in for a real account.
        private async Task<TimeSpan?> RecordFailureAsync(
            User user,
            DateTime now)
        {
            if (user.FailedLoginWindowStart == null ||
                now - user.FailedLoginWindowStart.Value > _lockout.Window)
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

                _logger.LogWarning(
                    "Account {UserId} locked until {Until} after {Count} failed sign-ins.",
                    user.Id,
                    user.LockoutEndsAt,
                    user.FailedLoginCount);
            }

            await _dbContext.SaveChangesAsync();

            return lockedFor;
        }

        // Clears an expired lockout when the next login attempt occurs.
        private bool ClearExpiredLock(User user, DateTime now)
        {
            if (user.LockoutEndsAt == null ||
                user.LockoutEndsAt > now)
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

        private void AddAudit(
            Guid? actorId,
            string action,
            Guid userId,
            DateTime now)
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = action,
                EntityType = "User",
                EntityId = userId,
                CreatedAt = now
            });
        }

        private sealed class Attempts
        {
            public int Count;
            public DateTime WindowStart;
            public DateTime? LockedUntil;
        }

        // Uses a SHA256 hash as the cache key rather than storing email addresses.
        private Attempts AttemptsFor(string email)
        {
            var key = "login-attempts:" +
                Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(email)));

            return _attempts.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow =
                    (_lockout.Window > _lockout.Duration
                        ? _lockout.Window
                        : _lockout.Duration) * 2;

                return new Attempts();
            })!;
        }

        private static TimeSpan? HeldFor(
            Attempts attempts,
            DateTime now)
        {
            lock (attempts)
            {
                return attempts.LockedUntil is { } until && until > now
                    ? until - now
                    : null;
            }
        }

        private TimeSpan? RecordFailure(
            Attempts attempts,
            DateTime now)
        {
            lock (attempts)
            {
                if (attempts.LockedUntil != null &&
                    attempts.LockedUntil <= now)
                {
                    attempts.LockedUntil = null;
                }

                if (attempts.Count == 0 ||
                    now - attempts.WindowStart > _lockout.Window)
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

        public async Task<AuthResponse?> RefreshTokenAsync(
            string refreshTokenString)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenString))
                return null;

            var existingToken =
                await _refreshTokenStore.GetTokenAsync(refreshTokenString);

            if (existingToken == null || !existingToken.IsActive)
            {
                // A token rotated moments ago is refused without the theft response: a duplicated
                // browser tab, or a client retrying after a lost response, sends it again in good faith.
                if (existingToken is { IsRevoked: true, ReplacedByToken: not null, RevokedAt: { } rotatedAt } &&
                    DateTime.UtcNow - rotatedAt < RotationReuseGrace)
                {
                    _logger.LogInformation(
                        "Refresh token for user {UserId} reused within the grace window; refused without revoking other sessions.",
                        existingToken.UserId);
                    return null;
                }

                if (existingToken != null && existingToken.IsRevoked)
                {
                    _logger.LogWarning(
                        "Refresh token reuse detected for user {UserId}. Revoking all tokens.",
                        existingToken.UserId);

                    await _refreshTokenStore.RevokeAllUserTokensAsync(
                        existingToken.UserId);
                }

                return null;
            }

            var user = await GetUserByIdAsync(existingToken.UserId);

            if (user == null || !user.IsActive)
                return null;

            var (newAccessToken, expiration) =
                _tokenService.GenerateAccessToken(user);

            var newRefreshTokenString =
                _tokenService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    _tokenService.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            await _refreshTokenStore.RevokeTokenAsync(
                refreshTokenString,
                replacedByToken: newRefreshTokenString);

            await _refreshTokenStore.SaveTokenAsync(newRefreshToken);

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                TokenExpiry = expiration,
                RefreshToken = newRefreshTokenString,
                Role = user.Role.ToString(),
                DisplayName = user.FullName,
                UserId = user.Id
            };
        }

        public async Task<bool> RevokeTokenAsync(
            string refreshTokenString)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenString))
                return false;

            var existing =
                await _refreshTokenStore.GetTokenAsync(refreshTokenString);

            if (existing == null || existing.IsRevoked)
                return false;

            await _refreshTokenStore.RevokeTokenAsync(refreshTokenString);

            await _refreshTokenStore.RevokeAllUserTokensAsync(
                existing.UserId);

            return true;
        }

        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            await _refreshTokenStore.RevokeAllUserTokensAsync(userId);
        }

        public async Task<User?> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await GetUserByEmailAsync(request.Email);

            if (existingUser != null)
                return null;

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
        /// Confirms the email of a user identified by a validated
        /// confirmation token.
        /// </summary>
        public async Task<bool> ConfirmEmailAsync(User user)
        {
            user.EmailConfirmed = true;

            _dbContext.Users.Update(user);

            await _dbContext.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Resets the password of a user identified by a validated
        /// password-reset token and revokes all refresh tokens.
        /// </summary>
        public async Task<bool> ResetPasswordAsync(
            User user,
            string newPassword)
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(newPassword);

            // A completed reset also lifts any existing lockout.
            var wasLocked = user.LockoutEndsAt != null;

            ClearLockout(user);

            if (wasLocked)
            {
                AddAudit(
                    user.Id,
                    "Account unlocked",
                    user.Id,
                    _time.GetUtcNow().UtcDateTime);
            }

            _dbContext.Users.Update(user);

            await _dbContext.SaveChangesAsync();

            await _refreshTokenStore.RevokeAllUserTokensAsync(user.Id);

            return true;
        }
    }
}
