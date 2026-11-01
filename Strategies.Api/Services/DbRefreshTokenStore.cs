using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    /// <summary>
    /// Keeps refresh tokens in the RefreshToken table, so they survive an API restart or redeploy and
    /// are shared by every API instance. Only a SHA-256 hash of each token is stored, never the token.
    ///
    /// There is no fallback store: a database error is thrown, so a missing table or a lost connection
    /// shows up as a failed request and in the logs instead of signing users out without a trace.
    /// </summary>
    public class DbRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ApplicationDbContext _dbContext;

        public DbRefreshTokenStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Lowercase hex SHA-256 of the token (64 characters, the TokenHash column length).
        public static string HashToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        public async Task SaveTokenAsync(RefreshToken token)
        {
            _dbContext.RefreshTokens.Add(new RefreshTokenRecord
            {
                Id = Guid.NewGuid(),
                UserId = token.UserId,
                TokenHash = HashToken(token.Token),
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                IsRevoked = token.IsRevoked,
                RevokedAt = token.RevokedAt,
                ReplacedByTokenHash = string.IsNullOrWhiteSpace(token.ReplacedByToken) ? null : HashToken(token.ReplacedByToken)
            });
            await _dbContext.SaveChangesAsync();
        }

        // ReplacedByToken comes back as the replacement's hash: only whether it is set matters to callers.
        public async Task<RefreshToken?> GetTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var tokenHash = HashToken(token);
            var record = await _dbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
            if (record == null) return null;

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

        public async Task RevokeTokenAsync(string token, string? replacedByToken = null)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            var tokenHash = HashToken(token);
            var record = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
            if (record == null) return;

            record.IsRevoked = true;
            record.RevokedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(replacedByToken))
                record.ReplacedByTokenHash = HashToken(replacedByToken);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            var activeTokens = await _dbContext.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync();
            if (activeTokens.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var record in activeTokens)
            {
                record.IsRevoked = true;
                record.RevokedAt = now;
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}
