using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public class InMemoryRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

        public Task SaveTokenAsync(RefreshToken token)
        {
            _tokens[token.Token] = token;
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
                existing.IsRevoked = true;
                existing.RevokedAt = DateTime.UtcNow;
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
    }
}
