using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class SpanRecord : Entity
{
    public string SpanId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Status { get; set; } = "Ok";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public string Attributes { get; set; } = "{}";
}
