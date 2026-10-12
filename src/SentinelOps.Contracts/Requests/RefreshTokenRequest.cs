namespace SentinelOps.Contracts.Requests;

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
