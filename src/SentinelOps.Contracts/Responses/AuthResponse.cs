namespace SentinelOps.Contracts.Responses;

public record AuthResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime Expiration { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
}
