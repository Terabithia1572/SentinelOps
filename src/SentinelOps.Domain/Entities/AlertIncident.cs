using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class AlertIncident : Entity
{
    public Guid AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Firing;
    public DateTime FiredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string Details { get; set; } = string.Empty;
}
