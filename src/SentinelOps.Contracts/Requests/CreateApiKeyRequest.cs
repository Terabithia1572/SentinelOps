namespace SentinelOps.Contracts.Requests;

public record CreateApiKeyRequest
{
    public string Name { get; init; } = string.Empty;
    public int? ExpiresInDays { get; init; }
}
