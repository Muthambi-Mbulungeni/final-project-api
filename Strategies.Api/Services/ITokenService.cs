using System;
using Strategies.Api.Data.Models;

namespace Strategies.Api.Services
{
    public interface ITokenService
    {
        (string Token, DateTime Expiration) GenerateAccessToken(User user);
        string GenerateRefreshToken();
        int AccessTokenExpirationMinutes { get; }
        int RefreshTokenExpirationDays { get; }
    }
}
