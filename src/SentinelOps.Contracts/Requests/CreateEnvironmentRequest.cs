namespace SentinelOps.Contracts.Requests;

public record CreateEnvironmentRequest
{
    public string Name { get; init; } = string.Empty;
    public string EnvironmentType { get; init; } = "Production";
}
