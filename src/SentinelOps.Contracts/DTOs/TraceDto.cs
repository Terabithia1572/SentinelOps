namespace SentinelOps.Contracts.DTOs;

public record TraceDto
{
    public Guid Id { get; init; }
    public string TraceId { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string RootSpanName { get; init; } = string.Empty;
    public double DurationMs { get; init; }
    public int SpanCount { get; init; }
    public bool HasErrors { get; init; }
    public DateTime StartedAtUtc { get; init; }
}
