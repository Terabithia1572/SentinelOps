namespace SentinelOps.Contracts.Requests;

public record RevokeTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
