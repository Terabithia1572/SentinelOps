namespace SentinelOps.Contracts.DTOs;

public record MetricDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string MetricType { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Tags { get; init; } = "{}";
    public DateTime Timestamp { get; init; }
}
