namespace SentinelOps.Application.Common.Interfaces;

public interface IApiKeyService
{
    (string RawApiKey, string KeyPrefix, string KeyHash) GenerateApiKey(string environment = "live");

    string HashApiKey(string rawApiKey);

    bool ValidateApiKey(string rawApiKey, string storedHash);
}
