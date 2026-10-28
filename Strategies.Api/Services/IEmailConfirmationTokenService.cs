using System.Threading.Tasks;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public interface IEmailConfirmationTokenService
    {
        /// <summary>Creates a signed email confirmation token for the user, valid for 24 hours.</summary>
        string CreateToken(User user);

        /// <summary>
        /// Returns the user the token was issued to, or null when the token is invalid, expired,
        /// not a confirmation token, or already used (the email is confirmed or the account deleted).
        /// </summary>
        Task<User?> ValidateTokenAsync(string token);
    }
}
