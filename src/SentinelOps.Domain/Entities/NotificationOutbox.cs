using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class NotificationOutbox : Entity
{
    public Guid ChannelId { get; set; }
    public NotificationChannel? Channel { get; set; }
    public NotificationEventType EventType { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? LastAttemptedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TraceId { get; set; }
}
