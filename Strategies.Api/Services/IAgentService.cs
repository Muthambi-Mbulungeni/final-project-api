using System;
using System.Threading;
using System.Threading.Tasks;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Services
{
    public interface IAgentService
    {
        Task<AgentResponse> TriageLeadAsync(LeadTriageInput input, CancellationToken cancellationToken = default);
    }

    // Only the fields the model needs for triage. Name and email are deliberately absent (POPIA data minimisation).
    public record LeadTriageInput(
        ImmigrationStatus ImmigrationStatus,
        bool NeedsAssistance,
        DateTime DesiredStartDate,
        string? VisaCategoryName);

    public class AgentResponse
    {
        public RunStatus Status { get; set; }
        public TriageStatus TriageStatus { get; set; } = TriageStatus.NeedsReview;
        public string Message { get; set; } = string.Empty;
        public string? Reason { get; set; }
        // The model's raw text, or the failure reason. Stored on the AgentRun row.
        public string Output { get; set; } = string.Empty;
    }
}
