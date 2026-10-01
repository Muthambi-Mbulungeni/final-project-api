using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.DTOs;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/notifications
        /// Returns paginated notifications for the current user, scoped to cases
        /// they are involved in. Unread count is returned for the bell badge.
        ///
        /// Scoping rules:
        ///   Admin     — all messages from any case
        ///   Consultant— messages from cases they are assigned to (ConsultantId match)
        ///   Client    — messages from cases where they are the client (ClientId match)
        ///
        /// In all cases, messages the user sent themselves are excluded.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(new ApiErrorResponse { Message = "Invalid token." });

            IQueryable<Strategies.Api.Data.Models.Message> query;

            if (User.IsInRole("Admin"))
            {
                // Admin sees all messages not sent by themselves
                query = _context.Messages.Where(m => m.SenderId != userId);
            }
            else if (User.IsInRole("Consultant"))
            {
                // Consultant sees messages from their assigned cases, not sent by themselves
                var assignedCaseIds = _context.Cases
                    .Where(c => c.ConsultantId == userId)
                    .Select(c => c.Id);

                query = _context.Messages
                    .Where(m => assignedCaseIds.Contains(m.CaseId) && m.SenderId != userId);
            }
            else
            {
                // Client sees messages from their own cases, not sent by themselves
                var clientCaseIds = _context.Cases
                    .Where(c => c.ClientId == userId)
                    .Select(c => c.Id);

                query = _context.Messages
                    .Where(m => clientCaseIds.Contains(m.CaseId) && m.SenderId != userId);
            }

            var unreadCount  = await query.CountAsync(m => !m.IsRead);
            var totalRecords = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.CaseId,
                    m.Body,
                    m.IsRead,
                    m.SentAt
                })
                .ToListAsync();

            return Ok(new
            {
                UnreadCount  = unreadCount,
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage  = page,
                Data         = messages
            });
        }

        /// <summary>
        /// PATCH /api/notifications/{id}/read
        /// Marks a notification as read. Only valid for messages in cases
        /// the current user is involved in. Idempotent — returns 200 if already read.
        /// </summary>
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(new ApiErrorResponse { Message = "Invalid token." });

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound(new ApiErrorResponse { Message = "Notification not found." });

            // Users cannot mark messages they sent as "read"
            if (message.SenderId == userId)
                return Forbid();

            // Verify the message belongs to a case this user is involved in
            var isInvolved = await _context.Cases.AnyAsync(c =>
                c.Id == message.CaseId &&
                (User.IsInRole("Admin") || c.ClientId == userId || c.ConsultantId == userId));

            if (!isInvolved)
                return Forbid();

            if (message.IsRead)
                return Ok(new { Message = "Already marked as read." });

            message.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Notification marked as read." });
        }
    }
}
