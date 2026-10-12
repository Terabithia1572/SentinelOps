namespace SentinelOps.Contracts.Requests;

public record CreateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
