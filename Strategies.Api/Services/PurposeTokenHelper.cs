using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Strategies.Api.Services
{
    /// <summary>
    /// Signed, short-lived single-purpose tokens (password reset, email confirmation): an HMAC-SHA256 JWT
    /// on Jwt:Key with its own audience, so no kind of token is accepted in place of another.
    /// Each service adds its own claims and decides what makes a token used.
    /// </summary>
    public static class PurposeTokenHelper
    {
        public static SymmetricSecurityKey ReadKey(IConfiguration configuration)
        {
            var secretKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("Jwt:Key is not configured. Set it via .NET User Secrets or environment variables.");

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        }

        public static string Create(SymmetricSecurityKey key, string audience, IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Audience           = audience,
                Expires            = DateTime.UtcNow.Add(lifetime),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        /// <summary>
        /// Returns the validated principal, or null with the reason (the exception type name) when the
        /// signature, audience, algorithm or lifetime is wrong.
        /// </summary>
        public static ClaimsPrincipal? Validate(SymmetricSecurityKey key, string audience, string token, out string? refusedBecause)
        {
            refusedBecause = null;
            try
            {
                return new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidAlgorithms          = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateIssuer           = false,
                    ValidateAudience         = true,
                    ValidAudience            = audience,
                    ValidateLifetime         = true,
                    RequireExpirationTime    = true,
                    ClockSkew                = TimeSpan.Zero
                }, out _);
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                refusedBecause = ex.GetType().Name;
                return null;
            }
        }
    }
}
