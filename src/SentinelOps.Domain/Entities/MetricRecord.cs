using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class MetricRecord : Entity
{
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public MetricType MetricType { get; set; } = MetricType.Counter;
    public double Value { get; set; }
    public string Tags { get; set; } = "{}";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
