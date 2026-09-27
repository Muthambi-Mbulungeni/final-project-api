using System;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Data.Models
{
    public class Case
    {
        public Guid Id { get; set; }
        public Guid? LeadId { get; set; }
        public Guid ClientId { get; set; }
        public Guid? ConsultantId { get; set; }
        public Guid VisaCategoryId { get; set; }
        public CaseStage Stage { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}