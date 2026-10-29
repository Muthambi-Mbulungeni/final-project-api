using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Services;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string InvalidResetTokenMessage = "Invalid or expired reset token.";
        private const string PasswordPolicyMessage = "Password does not meet the requirements.";
        private const string InvalidConfirmationTokenMessage = "Invalid or expired confirmation token.";

        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetTokenService _resetTokenService;
        private readonly IPasswordPolicy _passwordPolicy;
        private readonly IEmailConfirmationTokenService _confirmationTokenService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IEmailService emailService,
            IPasswordResetTokenService resetTokenService,
            IPasswordPolicy passwordPolicy,
            IEmailConfirmationTokenService confirmationTokenService,
            IServiceScopeFactory scopeFactory,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _emailService = emailService;
            _resetTokenService = resetTokenService;
            _passwordPolicy = passwordPolicy;
            _confirmationTokenService = confirmationTokenService;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/auth/login
        /// Rate-limited to 5 requests/min per IP.
        /// Also locks an account after repeated failed sign-ins: 429 with code "account_locked" and Retry-After.
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

            var outcome = await _authService.TryLoginAsync(request);

            if (outcome.LockedFor is { } wait)
            {
                Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                return StatusCode(StatusCodes.Status429TooManyRequests, new ApiErrorResponse
                {
                    Message = "Too many failed sign-in attempts. Try again later, or reset your password.",
                    Code = ApiErrorResponse.AccountLockedCode
                });
            }

            if (outcome.Response == null)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid email or password." });

            return Ok(outcome.Response);
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
        /// POST /api/auth/revoke (or /api/auth/revoke-token, or /api/auth/logout)
        /// Signs the user out by revoking their refresh tokens.
        /// </summary>
        [HttpPost("revoke")]
        [HttpPost("revoke-token")]
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest? request)
        {
            if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                await _authService.RevokeTokenAsync(request.RefreshToken);
                return Ok(new { Message = "Signed out successfully." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var userId))
            {
                await _authService.RevokeAllUserTokensAsync(userId);
                return Ok(new { Message = "Signed out successfully." });
            }

            return BadRequest(new ApiErrorResponse { Message = "Refresh token is required." });
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

            var user = await _authService.RegisterAsync(request);
            if (user == null)
                return BadRequest(new ApiErrorResponse { Message = "Registration failed or email already exists." });

            var confirmationLink = BuildConfirmationLink(user);
            if (confirmationLink != null)
                await _emailService.SendAccountConfirmationEmailAsync(user.Email, confirmationLink);

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
        /// POST /api/auth/confirm-email
        /// Body { token }. 200 { message } and the account can sign in; 400 { message, errors: [] } for an
        /// invalid, expired or already used token.
        /// </summary>
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var user = await _confirmationTokenService.ValidateTokenAsync(request.Token ?? string.Empty);
            if (user == null)
                return BadRequest(new ApiErrorResponse { Message = InvalidConfirmationTokenMessage });

            await _authService.ConfirmEmailAsync(user);
            return Ok(new { Message = "Email confirmed. You can now sign in." });
        }

        /// <summary>
        /// POST /api/auth/resend-confirmation
        /// Body { email }. Always 200 with the same body, to prevent account enumeration. Sends a new
        /// confirmation email only to an existing, unconfirmed account. The email goes out after the
        /// response, so how long the request takes does not depend on whether one was sent.
        /// </summary>
        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            var user = await _authService.GetUserByEmailAsync(request.Email ?? string.Empty);
            if (user != null && !user.EmailConfirmed && user.DeletedAt == null)
            {
                var confirmationLink = BuildConfirmationLink(user);
                if (confirmationLink != null)
                    SendConfirmationInBackground(user.Email, confirmationLink);
            }

            return Ok(new { Message = "If an account with that email needs confirming, a new confirmation link has been sent." });
        }

        // Null (and logged) when App:BaseUrl is not configured.
        private string? BuildConfirmationLink(User user)
        {
            var baseUrl = _configuration["App:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("App:BaseUrl is not configured, so no confirmation email was sent.");
                return null;
            }

            var confirmationToken = _confirmationTokenService.CreateToken(user);

            // The web portal uses HashRouter, so the route goes after /#.
            var confirmationLink = $"{baseUrl.TrimEnd('/')}/#/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";

            // Lets developers test without SMTP. Never logged outside Development.
            if (_environment.IsDevelopment())
                _logger.LogInformation("Development only. Email confirmation link for {Email}: {ConfirmationLink}", user.Email, confirmationLink);

            return confirmationLink;
        }

        // Runs after the request has ended, so it gets its own scope instead of the request's services.
        private void SendConfirmationInBackground(string email, string confirmationLink)
        {
            var scopeFactory = _scopeFactory;
            var logger = _logger;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IEmailService>()
                        .SendAccountConfirmationEmailAsync(email, confirmationLink);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send a confirmation email.");
                }
            });
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
