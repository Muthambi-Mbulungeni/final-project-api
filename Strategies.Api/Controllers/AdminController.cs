using Microsoft.AspNetCore.Mvc;
using Strategies.Api.DTOs;
using System;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private static readonly Guid StaticUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(new PagedResponse<object>
            {
                Data = new[] { new { Id = StaticUserId, FullName = "Riaad", Email = "riaad@nexus.com", Role = "Consultant", IsActive = true } },
                TotalRecords = 8,
                TotalPages = 1,
                CurrentPage = page
            });
        }

        [HttpPatch("users/{id}/suspend")]
        public IActionResult SuspendUser(Guid id) => Ok(new { Message = "User account has been suspended." });

        [HttpGet("audit-logs")]
        public IActionResult GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(new PagedResponse<object>
            {
                Data = new[] { new { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), UserId = StaticUserId, Action = "User Suspended", EntityType = "User", EntityId = StaticUserId, CreatedAt = DateTime.UtcNow } },
                TotalRecords = 250,
                TotalPages = 13,
                CurrentPage = page
            });
        }
    }
}