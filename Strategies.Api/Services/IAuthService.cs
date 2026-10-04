using System;
using System.Threading.Tasks;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs.Auth;

namespace Strategies.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
