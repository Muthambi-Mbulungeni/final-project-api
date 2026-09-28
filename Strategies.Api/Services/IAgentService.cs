using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public interface IAgentService
    {
        Task<AgentResponse> ProcessLeadAsync(string leadData);
    }

    public class AgentResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string ExtractedData { get; set; }
    }
}