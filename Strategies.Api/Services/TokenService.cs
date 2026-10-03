using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public TokenService(IConfiguration config)
        {
            _config = config;
            var secretKey = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("Jwt:Key is not configured. Set it via .NET User Secrets or environment variables.");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            _issuer = _config["Jwt:Issuer"] ?? "StrategiesApi";
            _audience = _config["Jwt:Audience"] ?? "StrategiesClients";

            if (!int.TryParse(_config["Jwt:AccessTokenExpirationMinutes"], out _accessTokenExpirationMinutes))
            {
                _accessTokenExpirationMinutes = 60;
            }

            if (!int.TryParse(_config["Jwt:RefreshTokenExpirationDays"], out _refreshTokenExpirationDays))
            {
                _refreshTokenExpirationDays = 7;
            }
        }

        public int AccessTokenExpirationMinutes => _accessTokenExpirationMinutes;
        public int RefreshTokenExpirationDays => _refreshTokenExpirationDays;

        public (string Token, DateTime Expiration) GenerateAccessToken(User user)
        {
            var expiration = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
            var roleString = user.Role.ToString();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, roleString),
                new Claim("role", roleString),
                new Claim("displayName", user.FullName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiration,
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return (tokenHandler.WriteToken(token), expiration);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
