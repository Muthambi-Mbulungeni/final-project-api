using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Strategies.Api.DTOs;
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Exceptions;
using Strategies.Api.Services;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string InvalidResetTokenMessage = "Invalid or expired reset token.";
        private const string PasswordPolicyMessage = "Password does not meet the requirements.";

        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetTokenService _resetTokenService;
        private readonly IPasswordPolicy _passwordPolicy;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IEmailService emailService,
            IPasswordResetTokenService resetTokenService,
            IPasswordPolicy passwordPolicy,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _emailService = emailService;
            _resetTokenService = resetTokenService;
            _passwordPolicy = passwordPolicy;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
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

            try
            {
                var result = await _authService.LoginAsync(request);

                if (result == null)
                    return Unauthorized(new ApiErrorResponse { Message = "Invalid email or password." });

                return Ok(result);
            }
            catch (AccountLockedException ex)
            {
                return StatusCode(StatusCodes.Status423Locked, new ApiErrorResponse { Message = ex.Message });
            }
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
                var baseUrl = _configuration["App:BaseUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _logger.LogError("App:BaseUrl is not configured, so no password reset email was sent.");
                }
                else
                {
                    var resetToken = _resetTokenService.CreateToken(user);

                    // The web portal uses HashRouter, so the route goes after /#.
                    var resetLink = $"{baseUrl.TrimEnd('/')}/#/reset-password?token={Uri.EscapeDataString(resetToken)}";

                    // Lets developers test without SMTP. Never logged outside Development.
                    if (_environment.IsDevelopment())
                        _logger.LogInformation("Development only. Password reset link for {Email}: {ResetLink}", user.Email, resetLink);

                    await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
                }
            }

            // Same response whether or not the account exists, to prevent account enumeration.
            return Ok(new { Message = "If an account with that email exists, a reset link has been sent." });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Validation failed." });

            var passwordErrors = _passwordPolicy.Validate(request.Password, request.Email, $"{request.FirstName} {request.LastName}");
            if (passwordErrors.Count > 0)
                return BadRequest(new ApiErrorResponse { Message = PasswordPolicyMessage, Errors = passwordErrors.ToList() });

            var result = await _authService.RegisterAsync(request);
            if (!result)
                return BadRequest(new ApiErrorResponse { Message = "Registration failed or email already exists." });

            return StatusCode(201, new { Message = "Registration successful. Please check your email to confirm your account." });
        }

        /// <summary>
        /// POST /api/auth/reset-password
        /// Body { token, newPassword }. The email comes from the validated token.
        /// 200 { message } on success; 400 { message, errors: [] } for an invalid, expired or used token;
        /// 400 { message, errors: [...] } when the new password fails the password policy.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _resetTokenService.ValidateTokenAsync(request.Token ?? string.Empty);
            if (user == null)
                return BadRequest(new ApiErrorResponse { Message = InvalidResetTokenMessage });

            var passwordErrors = _passwordPolicy.Validate(request.NewPassword, user.Email, user.FullName);
            if (passwordErrors.Count > 0)
                return BadRequest(new ApiErrorResponse { Message = PasswordPolicyMessage, Errors = passwordErrors.ToList() });

            // Only fails when the database cannot be written; reported as an unusable token
            // to stay within the documented responses.
            var result = await _authService.ResetPasswordAsync(user, request.NewPassword!);
            if (!result)
                return BadRequest(new ApiErrorResponse { Message = InvalidResetTokenMessage });

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
