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

        private readonly List<User> _seedUsers;

        public AuthService(
            ApplicationDbContext dbContext,
            IPasswordHasherService passwordHasher,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,
            ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _logger = logger;

            _seedUsers = new List<User>
            {
                new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    FullName = "System Admin",
                    Email = "admin@nexus.com",
                    PasswordHash = _passwordHasher.HashPassword("AdminPassword123!"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    FullName = "Riaad",
                    Email = "riaad@nexus.com",
                    PasswordHash = _passwordHasher.HashPassword("ConsultantPassword123!"),
                    Role = UserRole.Consultant,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                    FullName = "Consultant User",
                    Email = "consultant@nexus.com",
                    PasswordHash = _passwordHasher.HashPassword("ConsultantPassword123!"),
                    Role = UserRole.Consultant,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    FullName = "John Doe",
                    Email = "client@nexus.com",
                    PasswordHash = _passwordHasher.HashPassword("ClientPassword123!"),
                    Role = UserRole.Client,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        private static bool _dbConnectionChecked = false;
        private static bool _dbAvailable = false;

        private async Task<bool> IsDbAvailableAsync()
        {
            if (!_dbConnectionChecked)
            {
                try
                {
                    _dbAvailable = await _dbContext.Database.CanConnectAsync();
                }
                catch
                {
                    _dbAvailable = false;
                }
                _dbConnectionChecked = true;
            }
            return _dbAvailable;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (await IsDbAvailableAsync())
            {
                try
                {
                    var dbUser = await _dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

                    if (dbUser != null)
                    {
                        return dbUser;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not query database for user, falling back to seed store.");
                }
            }

            return _seedUsers.FirstOrDefault(u => u.Email.ToLowerInvariant() == normalizedEmail);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            if (await IsDbAvailableAsync())
            {
                try
                {
                    var dbUser = await _dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (dbUser != null)
                    {
                        return dbUser;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not query database for user by id, falling back to seed store.");
                }
            }

            return _seedUsers.FirstOrDefault(u => u.Id == userId);
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return null;
            }

            var (accessToken, expiration) = _tokenService.GenerateAccessToken(user);
            var refreshTokenString = _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            await _refreshTokenStore.SaveTokenAsync(refreshToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                TokenExpiry = expiration,
                RefreshToken = refreshTokenString,
                Role = user.Role.ToString(),
                DisplayName = user.FullName,
                UserId = user.Id
            };
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshTokenString)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenString))
            {
                return null;
            }

            var existingToken = await _refreshTokenStore.GetTokenAsync(refreshTokenString);
            if (existingToken == null || !existingToken.IsActive)
            {
                return null;
            }

            var user = await GetUserByIdAsync(existingToken.UserId);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            var (newAccessToken, expiration) = _tokenService.GenerateAccessToken(user);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            await _refreshTokenStore.RevokeTokenAsync(refreshTokenString, replacedByToken: newRefreshTokenString);
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

        public async Task<bool> RevokeTokenAsync(string refreshTokenString)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenString))
            {
                return false;
            }

            var existing = await _refreshTokenStore.GetTokenAsync(refreshTokenString);
            if (existing == null || existing.IsRevoked)
            {
                return false;
            }

            await _refreshTokenStore.RevokeTokenAsync(refreshTokenString);
            return true;
        }
    }
}
