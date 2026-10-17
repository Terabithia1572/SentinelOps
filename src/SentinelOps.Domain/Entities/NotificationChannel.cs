using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class NotificationChannel : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NotificationChannelType Type { get; set; } = NotificationChannelType.Email;
    public string Destination { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public string? CustomHeadersJson { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Guid? ApplicationId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string? CreatedBy { get; set; }

    public ICollection<AlertRuleNotificationChannel> AlertRules { get; set; } = new List<AlertRuleNotificationChannel>();
    public ICollection<NotificationOutbox> OutboxMessages { get; set; } = new List<NotificationOutbox>();
}
