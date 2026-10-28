using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public class EmailConfirmationTokenService : IEmailConfirmationTokenService
    {
        // Its own audience, so neither a reset token nor an access token is accepted here, nor this one there.
        public const string Audience     = "email-confirmation";
        public const string Purpose      = "email_confirmation";
        public const string PurposeClaim = PasswordResetTokenService.PurposeClaim;
        // Longer than a reset: a new user may not open the email for hours.
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EmailConfirmationTokenService> _logger;
        private readonly SymmetricSecurityKey _key;

        public EmailConfirmationTokenService(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ILogger<EmailConfirmationTokenService> logger)
        {
            _key       = PurposeTokenHelper.ReadKey(configuration);
            _dbContext = dbContext;
            _logger    = logger;
        }

        public string CreateToken(User user)
        {
            return PurposeTokenHelper.Create(_key, Audience, new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(PurposeClaim, Purpose)
            }, Lifetime);
        }

        public async Task<User?> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var principal = PurposeTokenHelper.Validate(_key, Audience, token, out var refusedBecause);
            if (principal == null)
            {
                _logger.LogInformation("Email confirmation token refused: {Reason}", refusedBecause);
                return null;
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var purpose = principal.FindFirst(PurposeClaim)?.Value;

            if (purpose != Purpose || string.IsNullOrWhiteSpace(email))
                return null;

            User? user;
            try
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                user = await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not look up the user for an email confirmation token.");
                return null;
            }

            // Confirming flips EmailConfirmed, so every token for the account stops working after the first use.
            if (user == null || user.EmailConfirmed || user.DeletedAt != null)
                return null;

            return user;
        }
    }
}
