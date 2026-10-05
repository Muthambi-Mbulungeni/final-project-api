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
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public AuthController(IAuthService authService, IEmailService emailService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _authService = authService;
            _emailService = emailService;
            _configuration = configuration;
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
        /// POST /api/auth/refresh (or /api/auth/refresh-token)
        /// Rotates the refresh token. Old token is revoked, new pair is returned.
        /// Accepts the refresh token from the request body (for mobile compatibility).
        /// </summary>
        [HttpPost("refresh")]
        [HttpPost("refresh-token")]
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
        /// POST /api/auth/revoke (or /api/auth/revoke-token)
        /// Signs the user out by revoking their refresh token.
        /// </summary>
        [HttpPost("revoke")]
        [HttpPost("revoke-token")]
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
        /// Sends a password reset email if the user exists.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "A valid email address is required." });

            var user = await _authService.GetUserByEmailAsync(request.Email);
            if (user != null)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("purpose", "password_reset")
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };
                var resetToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

                var baseUrl = _configuration["App:BaseUrl"] ?? "https://strategies.com";
                var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password?token={resetToken}";

                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            }

            return Ok(new { Message = "If an account with that email exists, a reset link has been sent." });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Validation failed." });

            var result = await _authService.RegisterAsync(request);
            if (!result)
                return BadRequest(new ApiErrorResponse { Message = "Registration failed or email already exists." });

            return StatusCode(201, new { Message = "Registration successful. Please check your email to confirm your account." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Validation failed." });

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);

                tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
                var purposeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "purpose")?.Value;

                if (purposeClaim != "password_reset" || !string.Equals(emailClaim, request.Email, StringComparison.InvariantCultureIgnoreCase))
                    return BadRequest(new ApiErrorResponse { Message = "Invalid token claims." });
            }
            catch
            {
                return BadRequest(new ApiErrorResponse { Message = "Invalid or expired reset token." });
            }

            var result = await _authService.ResetPasswordAsync(request);
            if (!result)
                return BadRequest(new ApiErrorResponse { Message = "Invalid request or token." });

            return Ok(new { Message = "Password reset successfully." });
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
