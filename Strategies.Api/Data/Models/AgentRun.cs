using System;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Data.Models
{
    public class AgentRun
    {
        public Guid Id { get; set; }
        public Guid? CaseId { get; set; }
        public Guid? LeadId { get; set; }
        public AgentType AgentType { get; set; }
        public string Output { get; set; } = string.Empty;
        public RunStatus Status { get; set; }
        public DateTime RanAt { get; set; } = DateTime.UtcNow;
    }
}