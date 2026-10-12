namespace SentinelOps.Contracts.Requests;

public record UpdateApplicationRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}
