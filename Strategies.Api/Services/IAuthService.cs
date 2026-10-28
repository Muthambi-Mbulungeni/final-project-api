using System;
using System.Threading.Tasks;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs.Auth;

namespace Strategies.Api.Services
{
    // The result of a sign-in attempt: tokens on success, nothing on a refusal, and LockedFor (the time
    // left) when the account is locked, whether or not the password was right.
    public record LoginOutcome(AuthResponse? Response, TimeSpan? LockedFor = null);

    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<LoginOutcome> TryLoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        // The new, unconfirmed user, or null when the email is already registered.
        Task<User?> RegisterAsync(RegisterRequest request);
        Task<bool> ResetPasswordAsync(User user, string newPassword);
        Task<bool> ConfirmEmailAsync(User user);
    }
}
