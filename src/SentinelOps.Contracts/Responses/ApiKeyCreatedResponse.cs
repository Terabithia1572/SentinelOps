namespace SentinelOps.Contracts.Responses;

public record ApiKeyCreatedResponse
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public string RawApiKey { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}
