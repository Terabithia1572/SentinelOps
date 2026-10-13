using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class AlertEvent : Entity
{
    public Guid AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }
    public AlertStatus OldStatus { get; set; }
    public AlertStatus NewStatus { get; set; }
    public double MeasuredValue { get; set; }
    public double Threshold { get; set; }
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}
