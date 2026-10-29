using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public class DbRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DbRefreshTokenStore> _logger;
        private readonly InMemoryRefreshTokenStore _fallbackStore = new();
        private bool _dbChecked = false;
        private bool _dbAvailable = false;

        public DbRefreshTokenStore(ApplicationDbContext dbContext, ILogger<DbRefreshTokenStore> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public static string HashToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private async Task<bool> IsDbAvailableAsync()
        {
            if (!_dbChecked)
            {
                try
                {
                    _dbAvailable = await _dbContext.Database.CanConnectAsync();
                }
                catch
                {
                    _dbAvailable = false;
                }
                _dbChecked = true;
            }
            return _dbAvailable;
        }

        public async Task SaveTokenAsync(RefreshToken token)
        {
            if (await IsDbAvailableAsync())
            {
                try
                {
                    var record = new RefreshTokenRecord
                    {
                        Id = Guid.NewGuid(),
                        UserId = token.UserId,
                        TokenHash = HashToken(token.Token),
                        CreatedAt = token.CreatedAt,
                        ExpiresAt = token.ExpiresAt,
                        IsRevoked = token.IsRevoked,
                        RevokedAt = token.RevokedAt,
                        ReplacedByTokenHash = !string.IsNullOrWhiteSpace(token.ReplacedByToken)
                            ? HashToken(token.ReplacedByToken)
                            : null
                    };

                    _dbContext.RefreshTokens.Add(record);
                    await _dbContext.SaveChangesAsync();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save refresh token to database, saving to fallback store.");
                }
            }

            await _fallbackStore.SaveTokenAsync(token);
        }

        public async Task<RefreshToken?> GetTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var tokenHash = HashToken(token);

            if (await IsDbAvailableAsync())
            {
                try
                {
                    var record = await _dbContext.RefreshTokens
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

                    if (record != null)
                    {
                        return new RefreshToken
                        {
                            Token = token,
                            UserId = record.UserId,
                            CreatedAt = record.CreatedAt,
                            ExpiresAt = record.ExpiresAt,
                            IsRevoked = record.IsRevoked,
                            RevokedAt = record.RevokedAt,
                            ReplacedByToken = record.ReplacedByTokenHash
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to query refresh token from database, checking fallback store.");
                }
            }

            return await _fallbackStore.GetTokenAsync(token);
        }

        public async Task RevokeTokenAsync(string token, string? replacedByToken = null)
        {
            if (string.IsNullOrWhiteSpace(token)) return;
            var tokenHash = HashToken(token);

            if (await IsDbAvailableAsync())
            {
                try
                {
                    var record = await _dbContext.RefreshTokens
                        .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

                    if (record != null)
                    {
                        record.IsRevoked = true;
                        record.RevokedAt = DateTime.UtcNow;
                        if (!string.IsNullOrWhiteSpace(replacedByToken))
                        {
                            record.ReplacedByTokenHash = HashToken(replacedByToken);
                        }

                        await _dbContext.SaveChangesAsync();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to revoke refresh token in database, revoking in fallback store.");
                }
            }

            await _fallbackStore.RevokeTokenAsync(token, replacedByToken);
        }

        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            if (await IsDbAvailableAsync())
            {
                try
                {
                    var activeTokens = await _dbContext.RefreshTokens
                        .Where(r => r.UserId == userId && !r.IsRevoked)
                        .ToListAsync();

                    if (activeTokens.Count > 0)
                    {
                        var now = DateTime.UtcNow;
                        foreach (var t in activeTokens)
                        {
                            t.IsRevoked = true;
                            t.RevokedAt = now;
                        }

                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to revoke all user tokens in database.");
                }
            }

            await _fallbackStore.RevokeAllUserTokensAsync(userId);
        }
    }
}
