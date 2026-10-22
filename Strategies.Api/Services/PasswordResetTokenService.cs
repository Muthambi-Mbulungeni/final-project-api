using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Strategies.Api.Data;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public class PasswordResetTokenService : IPasswordResetTokenService
    {
        // Access tokens use Jwt:Audience, so neither kind of token is accepted in place of the other.
        public const string Audience        = "password-reset";
        public const string Purpose         = "password_reset";
        public const string PurposeClaim    = "purpose";
        public const string PasswordStampClaim = "pwd_stamp";
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PasswordResetTokenService> _logger;
        private readonly SymmetricSecurityKey _key;

        public PasswordResetTokenService(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ILogger<PasswordResetTokenService> logger)
        {
            var secretKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("Jwt:Key is not configured. Set it via .NET User Secrets or environment variables.");

            _key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            _dbContext = dbContext;
            _logger    = logger;
        }

        public string CreateToken(User user)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(PurposeClaim, Purpose),
                    new Claim(PasswordStampClaim, PasswordStamp(user.PasswordHash))
                }),
                Audience           = Audience,
                Expires            = DateTime.UtcNow.Add(Lifetime),
                SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        public async Task<User?> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            ClaimsPrincipal principal;
            try
            {
                principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = _key,
                    ValidAlgorithms          = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateIssuer           = false,
                    ValidateAudience         = true,
                    ValidAudience            = Audience,
                    ValidateLifetime         = true,
                    RequireExpirationTime    = true,
                    ClockSkew                = TimeSpan.Zero
                }, out _);
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                _logger.LogInformation("Password reset token refused: {Reason}", ex.GetType().Name);
                return null;
            }

            // Read claims from the validated principal: the handler maps the short "email"
            // claim in the token to ClaimTypes.Email here (reading jwtToken.Claims was ISS-01).
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var purpose = principal.FindFirst(PurposeClaim)?.Value;
            var stamp = principal.FindFirst(PasswordStampClaim)?.Value;

            if (purpose != Purpose || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(stamp))
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
                _logger.LogWarning(ex, "Could not look up the user for a password reset token.");
                return null;
            }

            if (user == null)
                return null;

            // The stamp changes when the password does, so a used token stops working.
            var currentStamp = PasswordStamp(user.PasswordHash);
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(stamp), Encoding.ASCII.GetBytes(currentStamp)))
                return null;

            return user;
        }

        /// <summary>First 16 hex characters of SHA-256 of the stored password hash.</summary>
        public static string PasswordStamp(string passwordHash)
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash ?? string.Empty));
            return Convert.ToHexString(digest).Substring(0, 16).ToLowerInvariant();
        }
    }
}
