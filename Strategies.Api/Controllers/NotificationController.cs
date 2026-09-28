using Microsoft.AspNetCore.Mvc;
using System;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private static readonly Guid StaticNotifId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        [HttpGet]
        public IActionResult GetNotifications()
        {
            return Ok(new[] { new { Id = StaticNotifId, Message = "Your passport scan has been approved.", IsRead = false, CreatedAt = DateTime.UtcNow } });
        }

        [HttpPatch("{id}/read")]
        public IActionResult MarkAsRead(Guid id) => Ok(new { Message = "Notification marked as read." });
    }
}