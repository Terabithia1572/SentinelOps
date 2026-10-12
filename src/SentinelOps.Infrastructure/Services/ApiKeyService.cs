using System.Security.Cryptography;
using System.Text;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private const string DefaultPrefix = "sent";

    public (string RawApiKey, string KeyPrefix, string KeyHash) GenerateApiKey(string environment = "live")
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var hexSecret = Convert.ToHexStringLower(randomBytes);
        var prefix = $"{DefaultPrefix}_{environment}_{hexSecret[..8]}";
        var rawApiKey = $"{DefaultPrefix}_{environment}_{hexSecret}";
        var keyHash = HashApiKey(rawApiKey);

        return (rawApiKey, prefix, keyHash);
    }

    public string HashApiKey(string rawApiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(rawApiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public bool ValidateApiKey(string rawApiKey, string storedHash)
    {
        var computed = HashApiKey(rawApiKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
