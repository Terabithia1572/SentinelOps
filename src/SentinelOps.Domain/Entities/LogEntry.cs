using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class LogEntry : Entity
{
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Information;
    public string Message { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string Properties { get; set; } = "{}";
    public string? ExceptionDetail { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
