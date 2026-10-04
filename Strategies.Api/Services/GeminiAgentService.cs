using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Services
{
    public class GeminiAgentService : IAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiAgentService> _logger;

        public GeminiAgentService(HttpClient httpClient, IConfiguration config, ILogger<GeminiAgentService> logger)
        {
            _httpClient = httpClient;
            _config     = config;
            _logger     = logger;
        }

        public async Task<AgentResponse> ProcessLeadAsync(string leadData)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var url    = _config["Gemini:Url"];

            // Fail fast with a clear fallback rather than making a pointless HTTP call
            // to a URL that will just return 400 from Google due to a missing key.
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Gemini:ApiKey or Gemini:Url is not configured. Returning fallback response.");
                return new AgentResponse
                {
                    Status        = RunStatus.Fallback.ToString(),
                    Message       = "AI assistance unavailable. Please complete manual intake.",
                    ExtractedData = null!
                };
            }

            try
            {
                var requestUrl = $"{url}?key={apiKey}";
                var payload    = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"Evaluate this immigration lead: {leadData}. Respond STRICTLY in JSON format without markdown blocks: {{ \"status\": \"Qualified\" or \"Unqualified\", \"reason\": \"One sentence explaining why.\" }}" }
                            }
                        }
                    }
                };

                var content  = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                
                string extractedText = string.Empty;
                try
                {
                    using var jsonDoc = JsonDocument.Parse(responseString);
                    extractedText = jsonDoc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? string.Empty;
                        
                    // Strip markdown blocks if Gemini returns them
                    extractedText = extractedText.Trim();
                    if (extractedText.StartsWith("```json"))
                        extractedText = extractedText.Substring(7);
                    else if (extractedText.StartsWith("```"))
                        extractedText = extractedText.Substring(3);
                        
                    if (extractedText.EndsWith("```"))
                        extractedText = extractedText.Substring(0, extractedText.Length - 3);
                        
                    extractedText = extractedText.Trim();
                }
                catch (Exception parseEx)
                {
                    _logger.LogWarning(parseEx, "Failed to parse Gemini response: {Response}", responseString);
                    // Fall back to just returning the raw string if parsing fails
                    extractedText = responseString;
                }

                return new AgentResponse
                {
                    Status        = RunStatus.Success.ToString(),
                    Message       = "AI processed successfully.",
                    ExtractedData = extractedText
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API call failed. Triggering graceful fallback.");
                return new AgentResponse
                {
                    Status        = RunStatus.Fallback.ToString(),
                    Message       = "AI assistance unavailable. Please complete manual intake.",
                    ExtractedData = null!
                };
            }
        }
    }
}