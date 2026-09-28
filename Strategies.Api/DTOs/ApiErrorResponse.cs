using System.Collections.Generic;
namespace Strategies.Api.DTOs
{
    public class ApiErrorResponse
    {
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}