using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<(string Token, DateTime Expiration)> GenerateAccessTokenAsync(
        string userId,
        string userName,
        string email,
        IEnumerable<string> roles)
    {
        var jwtKey = _configuration["JwtSettings:Key"] ?? "PLACEHOLDER_DEV_SECRET_KEY_AT_LEAST_32_CHARS_LONG";
        var jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "SentinelOps";
        var jwtAudience = _configuration["JwtSettings:Audience"] ?? "SentinelOpsUsers";
        var expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out var exp) ? exp : 60;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Task.FromResult((tokenString, expiresAt));
    }

    public (string RawRefreshToken, string RefreshTokenHash, DateTime Expiration) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var rawToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = HashToken(rawToken);
        var expiration = DateTime.UtcNow.AddDays(7); // 7 days refresh token lifetime

        return (rawToken, tokenHash, expiration);
    }

    public string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public bool VerifyToken(string rawToken, string tokenHash)
    {
        var computed = HashToken(rawToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(tokenHash));
    }
}
