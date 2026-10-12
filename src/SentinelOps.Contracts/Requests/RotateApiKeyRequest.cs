namespace SentinelOps.Contracts.Requests;

public record RotateApiKeyRequest
{
    public int? ExpiresInDays { get; init; }
}
