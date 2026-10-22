using System.Threading.Tasks;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public interface IPasswordResetTokenService
    {
        /// <summary>Creates a signed, single-use reset token for the user, valid for 30 minutes.</summary>
        string CreateToken(User user);

        /// <summary>
        /// Returns the user the token was issued to, or null when the token is invalid, expired,
        /// not a reset token, or already used (the password has changed since it was issued).
        /// </summary>
        Task<User?> ValidateTokenAsync(string token);
    }
}
