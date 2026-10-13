using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class IncidentAlertRelation : Entity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public Guid AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }

    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
}
