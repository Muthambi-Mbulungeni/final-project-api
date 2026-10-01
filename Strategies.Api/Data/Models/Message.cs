using System;

namespace Strategies.Api.Data.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public Guid SenderId { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}