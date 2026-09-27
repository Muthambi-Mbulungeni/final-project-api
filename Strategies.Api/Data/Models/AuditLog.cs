using System;

namespace Strategies.Api.Data.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}