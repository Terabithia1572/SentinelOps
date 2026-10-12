namespace SentinelOps.Contracts.DTOs;

public record LogEntryDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string MessageTemplate { get; init; } = string.Empty;
    public string Properties { get; init; } = "{}";
    public string? ExceptionDetail { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; }
}
