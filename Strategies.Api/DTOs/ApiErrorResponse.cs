using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Strategies.Api.DTOs
{
    public class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        // Optional machine-readable reason for clients that must tell errors apart without
        // matching message text. Left out of the JSON when null.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; set; }

        public const string DuplicateEmailCode = "duplicate_email";
        public const string ConsentRequiredCode = "consent_required";

        public const string InvalidBodyMessage = "The request body is missing or is not valid JSON.";

        // Builds the response for automatic model validation. When the body itself could not be read
        // (keys "" or "$..."), one plain sentence replaces the JSON parser's internal details and the
        // follow-on "field is required" error for the whole request object.
        public static ApiErrorResponse FromModelState(ModelStateDictionary modelState)
        {
            var bodyUnreadable = modelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .Any(entry => entry.Key.Length == 0 || entry.Key.StartsWith("$"));

            var errors = bodyUnreadable
                ? new List<string> { InvalidBodyMessage }
                : modelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? InvalidBodyMessage : e.ErrorMessage)
                    .Distinct()
                    .ToList();
            if (errors.Count == 0)
                errors.Add(InvalidBodyMessage);

            return new ApiErrorResponse { Message = "Validation failed.", Errors = errors };
        }
    }
}
