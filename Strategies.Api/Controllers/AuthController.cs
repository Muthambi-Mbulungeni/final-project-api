using Microsoft.AspNetCore.Mvc;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] object email) => Ok(new { Message = "Reset link sent." });

        [HttpPost("confirm-email")]
        public IActionResult ConfirmEmail([FromQuery] string token) => Ok(new { Message = "Email confirmed." });
    }
}