using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        public AuthService(
            ApplicationDbContext dbContext,
            IPasswordHasherService passwordHasher,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,
            ILogger<AuthService> logger)
        {
            _dbContext          = dbContext;
            _passwordHasher     = passwordHasher;
            _tokenService       = tokenService;
            _refreshTokenStore  = refreshTokenStore;
            _logger             = logger;
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

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null || !user.IsActive || !user.EmailConfirmed)
                return null;

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return null;

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

            return new AuthResponse
            {
                AccessToken  = accessToken,
                TokenExpiry  = expiration,
                RefreshToken = refreshTokenString,
                Role         = user.Role.ToString(),
                DisplayName  = user.FullName,
                UserId       = user.Id
            };
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

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null) return false;

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

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            await _refreshTokenStore.RevokeAllUserTokensAsync(user.Id);
            return true;
        }
    }
}
