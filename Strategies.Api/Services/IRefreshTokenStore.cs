using System;
using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }

    public interface IRefreshTokenStore
    {
        Task SaveTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetTokenAsync(string token);
        Task RevokeTokenAsync(string token, string? replacedByToken = null);
        Task RevokeAllUserTokensAsync(Guid userId);
    }
}
