namespace SentinelOps.Contracts.DTOs;

public record HealthCheckDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double ResponseTimeMs { get; init; }
    public DateTime CheckedAtUtc { get; init; }
}
