using Microsoft.AspNetCore.Mvc;
using Strategies.Api.DTOs;
using System;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] int page = 1) => Ok(new PagedResponse<object> { Data = new[] { new { Id = Guid.NewGuid(), Name = "Riaad" } }, CurrentPage = page });

        [HttpPatch("users/{id}/suspend")]
        public IActionResult SuspendUser(Guid id) => Ok(new { Message = "User suspended." });

        [HttpGet("audit-logs")]
        public IActionResult GetLogs([FromQuery] int page = 1) => Ok(new PagedResponse<object> { Data = new[] { new { Action = "User Suspended" } }, CurrentPage = page });
    }
}