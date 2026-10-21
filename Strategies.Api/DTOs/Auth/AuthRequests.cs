using System.ComponentModel.DataAnnotations;

namespace Strategies.Api.DTOs.Auth
{
    public class RevokeTokenRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;
    }

    // No [Required] and nullable on purpose: under [ApiController] a missing field would get an
    // automatic ProblemDetails 400. AuthController.ResetPassword returns the documented shapes instead.
    // The email comes from the validated token, not the request.
    public class ResetPasswordRequest
    {
        public string? Token { get; set; }

        public string? NewPassword { get; set; }
    }
}
