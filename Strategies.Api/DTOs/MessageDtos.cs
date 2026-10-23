using System;

namespace Strategies.Api.DTOs
{
    // No DataAnnotations: the body is validated in MessagesController so every 400 uses
    // ApiErrorResponse with errors[], which the web thread relies on.
    public class SendMessageRequest
    {
        public string? Body { get; set; }
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderRole { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
    }
}
