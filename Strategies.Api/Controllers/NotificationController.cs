using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;
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
        /// Returns unread messages (notifications) for the currently authenticated user.
        /// Paginated. Unread count is included for the bell badge.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(new ApiErrorResponse { Message = "Invalid token." });

            // Notifications are messages where the current user is NOT the sender
            // (i.e. messages sent to cases they are involved in by someone else)
            var query = _context.Messages
                .Where(m => m.SenderId != userId);

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
        /// Marks a single message as read.
        /// Users may only mark their own notifications — not other users' messages.
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

            // A user cannot mark someone else's outbound message as read
            if (message.SenderId == userId)
                return Forbid();

            if (message.IsRead)
                return Ok(new { Message = "Already marked as read." });

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notification marked as read." });
        }
    }
}
