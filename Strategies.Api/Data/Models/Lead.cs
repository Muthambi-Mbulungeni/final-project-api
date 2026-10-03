using System;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Data.Models
{
    public class Lead
    {
        public string? TriageReason { get; set; }
        public Guid Id { get; set; }
        public Guid? ReviewedById { get; set; }
        public Guid? VisaCategoryId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ImmigrationStatus ImmigrationStatus { get; set; }
        public bool NeedsAssistance { get; set; }
        public DateTime DesiredStartDate { get; set; }
        public TriageStatus TriageStatus { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}