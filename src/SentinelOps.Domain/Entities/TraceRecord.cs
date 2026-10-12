using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class TraceRecord : Entity
{
    public string TraceId { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public string RootSpanName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int SpanCount { get; set; }
    public bool HasErrors { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
}
