namespace SentinelOps.Contracts.Requests;

public record CreateApplicationRequest
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
