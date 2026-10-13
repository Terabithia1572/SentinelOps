using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class AlertRuleNotificationChannel : Entity
{
    public Guid AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }

    public Guid NotificationChannelId { get; set; }
    public NotificationChannel? NotificationChannel { get; set; }
}
