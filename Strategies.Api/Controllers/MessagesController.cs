using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;

namespace Strategies.Api.Controllers
{
    /// <summary>
    /// Case message thread (FR-3.5). Admin: any case, read and post, never mark read.
    /// Consultant: assigned cases only. Client: own cases only.
    /// </summary>
    [ApiController]
    [Route("api/cases/{caseId:guid}/messages")]
    [Authorize(Roles = "Admin,Consultant,Client")]
    public class MessagesController : ControllerBase
    {
        public const int MaxBodyLength = 2000;
        public const string BodyRequiredError = "Message body is required.";
        public const string BodyTooLongError = "Message body must be 2000 characters or fewer.";

        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/cases/{caseId}/messages?page=1&amp;pageSize=50
        /// The thread, oldest first. pageSize is clamped to 1..100.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMessages(
            Guid caseId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (_, _, denied) = await LoadCaseForParticipantAsync(caseId);
            if (denied != null) return denied;

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = ThreadQuery().Where(m => m.CaseId == caseId);
            var totalRecords = await query.CountAsync();
            var messages = await query
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResponse<MessageDto>
            {
                Data = messages,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page
            });
        }

        /// <summary>
        /// POST /api/cases/{caseId}/messages
        /// Body { body }: trimmed, 1 to 2000 characters counted as Unicode code points (the web's rule).
        /// 400 with errors[] for a problem with the text; 400 without errors when the case is archived.
        /// </summary>
        [HttpPost]
        [MessageBodyBindingFilter]
        public async Task<IActionResult> SendMessage(Guid caseId, [FromBody] SendMessageRequest request)
        {
            var (caseRecord, currentUserId, denied) = await LoadCaseForParticipantAsync(caseId);
            if (denied != null) return denied;

            if (caseRecord!.IsArchived)
                return BadRequest(new ApiErrorResponse { Message = "Messages cannot be sent on an archived case." });

            var body = request.Body?.Trim() ?? string.Empty;
            if (body.Length == 0)
                return ValidationFailed(BodyRequiredError);
            if (body.EnumerateRunes().Count() > MaxBodyLength)
                return ValidationFailed(BodyTooLongError);

            var message = new Message
            {
                Id = Guid.NewGuid(),
                CaseId = caseId,
                SenderId = currentUserId,
                Body = body,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "Sent message",
                EntityType = "Message",
                EntityId = message.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var created = await ThreadQuery().FirstAsync(m => m.Id == message.Id);
            return CreatedAtAction(nameof(GetMessages), new { caseId }, created);
        }

        /// <summary>
        /// PATCH /api/cases/{caseId}/messages/read
        /// Marks every message the caller did not send as read. The case's client or assigned
        /// consultant only: admins are read-only on threads, so opening a case cannot clear
        /// the client's unread state.
        /// </summary>
        [HttpPatch("read")]
        public async Task<IActionResult> MarkThreadRead(Guid caseId)
        {
            var (caseRecord, currentUserId, denied) = await LoadCaseForParticipantAsync(caseId);
            if (denied != null) return denied;

            if (User.IsInRole("Admin"))
                return Forbid();

            var unread = await _context.Messages
                .Where(m => m.CaseId == caseRecord!.Id && m.SenderId != currentUserId && !m.IsRead)
                .ToListAsync();
            foreach (var message in unread)
                message.IsRead = true;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Messages marked as read.", Updated = unread.Count });
        }

        // Existence first (404 for every role), then access (403): Admin any case, Consultant
        // when assigned, Client when it is their case.
        private async Task<(Case? Case, Guid UserId, IActionResult? Denied)> LoadCaseForParticipantAsync(Guid caseId)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return (null, Guid.Empty, Unauthorized(new ApiErrorResponse { Message = "Invalid token." }));

            var caseRecord = await _context.Cases.FindAsync(caseId);
            if (caseRecord == null)
                return (null, userId, NotFound(new ApiErrorResponse { Message = "Case not found." }));

            var allowed =
                User.IsInRole("Admin") ||
                (User.IsInRole("Consultant") && caseRecord.ConsultantId == userId) ||
                (User.IsInRole("Client") && caseRecord.ClientId == userId);

            return allowed ? (caseRecord, userId, null) : (caseRecord, userId, Forbid());
        }

        private IQueryable<MessageDto> ThreadQuery() =>
            from m in _context.Messages
            join u in _context.Users on m.SenderId equals u.Id into senderGroup
            from sender in senderGroup.DefaultIfEmpty()
            select new MessageDto
            {
                Id = m.Id,
                CaseId = m.CaseId,
                SenderId = m.SenderId,
                SenderName = sender != null ? sender.FullName : "Unknown sender",
                SenderRole = sender != null ? sender.Role.ToString() : null,
                Body = m.Body,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            };

        private BadRequestObjectResult ValidationFailed(string error) =>
            BadRequest(new ApiErrorResponse { Message = "Validation failed.", Errors = new List<string> { error } });
    }

    /// <summary>
    /// Runs before [ApiController]'s automatic 400 (order -2000) and answers a missing, malformed or
    /// wrongly typed body with ApiErrorResponse and errors[], not ProblemDetails. The web thread reads a
    /// 400 without an errors array as "case closed" and would switch the send box off.
    /// </summary>
    public sealed class MessageBodyBindingFilterAttribute : ActionFilterAttribute
    {
        public MessageBodyBindingFilterAttribute()
        {
            Order = -2001;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
                context.Result = new BadRequestObjectResult(new ApiErrorResponse
                {
                    Message = "Validation failed.",
                    Errors = new List<string> { MessagesController.BodyRequiredError }
                });
        }
    }
}
