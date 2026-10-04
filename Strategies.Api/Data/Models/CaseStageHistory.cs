using System;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Data.Models
{
    public class CaseStageHistory
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public Guid? ChangedById { get; set; }
        public CaseStage FromStage { get; set; }
        public CaseStage ToStage { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}