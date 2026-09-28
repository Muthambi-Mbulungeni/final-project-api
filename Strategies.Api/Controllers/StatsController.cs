using Microsoft.AspNetCore.Mvc;
namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        [HttpGet("admin")]
        public IActionResult GetAdmin() => Ok(new { TotalUsers = 45, SystemErrors = 2, ActiveCases = 112 });

        [HttpGet("consultant")]
        public IActionResult GetConsultant() => Ok(new { ActiveCases = 12, PendingLeads = 5 });

        [HttpGet("client/{clientId}")]
        public IActionResult GetClient(System.Guid clientId) => Ok(new { ActiveApplications = 1, UnreadMessages = 3 });
    }
}