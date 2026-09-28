using Microsoft.AspNetCore.Mvc;
using System;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new[] { new { Id = Guid.NewGuid(), Message = "Approved", IsRead = false } });

        [HttpPatch("{id}/read")]
        public IActionResult MarkRead(Guid id) => Ok();
    }
}