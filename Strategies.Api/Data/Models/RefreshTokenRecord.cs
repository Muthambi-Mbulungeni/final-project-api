using System;

namespace Strategies.Api.Data.Models
{
    public class RefreshTokenRecord
    {
        public Guid Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public User? User { get; set; }
    }
}
