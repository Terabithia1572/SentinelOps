namespace SentinelOps.Application.Common.Interfaces;

public interface ITokenService
{
    Task<(string Token, DateTime Expiration)> GenerateAccessTokenAsync(
        string userId,
        string userName,
        string email,
        IEnumerable<string> roles);

    (string RawRefreshToken, string RefreshTokenHash, DateTime Expiration) GenerateRefreshToken();

    string HashToken(string rawToken);

    bool VerifyToken(string rawToken, string tokenHash);
}
