using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public interface IAgentService
    {
        Task<AgentResponse> ProcessLeadAsync(string leadData);
    }

    public class AgentResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ExtractedData { get; set; }
    }
}