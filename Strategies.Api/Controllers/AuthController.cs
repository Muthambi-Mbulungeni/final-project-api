using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Strategies.Api.DTOs;
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Services;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// POST /api/auth/login
        /// Rate-limited to 5 requests/min per IP.
        /// Returns an AuthResponse with access token, refresh token, role, and display name.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new ApiErrorResponse { Message = "Validation failed.", Errors = errors });
            }

            var result = await _authService.LoginAsync(request);

            if (result == null)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid email or password." });

            return Ok(result);
        }

        /// <summary>
        /// POST /api/auth/refresh
        /// Rotates the refresh token. Old token is revoked, new pair is returned.
        /// Accepts the refresh token from the request body (for mobile compatibility).
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RevokeTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new ApiErrorResponse { Message = "Refresh token is required." });

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (result == null)
                return Unauthorized(new ApiErrorResponse { Message = "Refresh token is invalid or has expired. Please sign in again." });

            return Ok(result);
        }

        /// <summary>
        /// POST /api/auth/revoke
        /// Signs the user out by revoking their refresh token.
        /// </summary>
        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new ApiErrorResponse { Message = "Refresh token is required." });

            await _authService.RevokeTokenAsync(request.RefreshToken);

            // Always 200 — don't reveal whether the token existed or not.
            return Ok(new { Message = "Signed out successfully." });
        }

        /// <summary>
        /// POST /api/auth/forgot-password
        /// Always returns the same response to prevent account enumeration.
        /// Real email delivery is pending email service integration.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "A valid email address is required." });

            // Fire and forget — never reveal whether the account exists.
            _ = _authService.GetUserByEmailAsync(request.Email);

            await Task.CompletedTask;
            return Ok(new { Message = "If an account with that email exists, a reset link has been sent." });
        }

        /// <summary>
        /// GET /api/auth/me
        /// Returns the current user's profile from their JWT claims.
        /// Requires a valid Bearer token.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(new ApiErrorResponse { Message = "Invalid token." });

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
                return Unauthorized(new ApiErrorResponse { Message = "User account not found or is suspended." });

            return Ok(new
            {
                UserId = user.Id,
                DisplayName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                EmailConfirmed = user.EmailConfirmed
            });
        }
    }
}
