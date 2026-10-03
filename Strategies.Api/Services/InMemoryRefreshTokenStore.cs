using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public class InMemoryRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

        // Evict tokens that have been revoked or expired for more than this threshold.
        // Keeps memory bounded without a background thread.
        private static readonly TimeSpan EvictionGracePeriod = TimeSpan.FromHours(24);

        public Task SaveTokenAsync(RefreshToken token)
        {
            _tokens[token.Token] = token;
            EvictStaleTokens();
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetTokenAsync(string token)
        {
            _tokens.TryGetValue(token, out var refreshToken);
            return Task.FromResult(refreshToken);
        }

        public Task RevokeTokenAsync(string token, string? replacedByToken = null)
        {
            if (_tokens.TryGetValue(token, out var existing))
            {
                existing.IsRevoked       = true;
                existing.RevokedAt       = DateTime.UtcNow;
                existing.ReplacedByToken = replacedByToken;
            }
            return Task.CompletedTask;
        }

        public Task RevokeAllUserTokensAsync(Guid userId)
        {
            foreach (var item in _tokens.Values.Where(t => t.UserId == userId && t.IsActive))
            {
                item.IsRevoked = true;
                item.RevokedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes tokens that are inactive (revoked or expired) and have been so
        /// for longer than the eviction grace period. Called on every save so no
        /// background thread is required. On a typical system this runs infrequently
        /// (only on token issuance, not on every request) and keeps the dictionary bounded.
        /// </summary>
        private void EvictStaleTokens()
        {
            var cutoff = DateTime.UtcNow - EvictionGracePeriod;
            var staleKeys = _tokens
                .Where(kvp => !kvp.Value.IsActive &&
                              (kvp.Value.RevokedAt ?? kvp.Value.ExpiresAt) < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in staleKeys)
                _tokens.TryRemove(key, out _);
        }
    }
}
