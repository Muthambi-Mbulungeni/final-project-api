using System;

namespace Strategies.Api.Data.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}