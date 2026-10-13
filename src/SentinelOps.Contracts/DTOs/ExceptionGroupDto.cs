namespace SentinelOps.Contracts.DTOs;

public record ExceptionGroupDto
{
    public string Fingerprint { get; init; } = string.Empty;
    public string ExceptionType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public int Count { get; init; }
    public DateTime FirstSeenAtUtc { get; init; }
    public DateTime LastSeenAtUtc { get; init; }
    public string? SampleTraceId { get; init; }
    public Guid? SampleLogId { get; init; }
}
