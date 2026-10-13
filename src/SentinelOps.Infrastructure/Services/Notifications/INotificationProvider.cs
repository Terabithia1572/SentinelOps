using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Notifications;

public interface INotificationProvider
{
    NotificationChannelType SupportedType { get; }
    Task<NotificationDeliveryResult> SendAsync(NotificationOutbox message, NotificationChannel channel, CancellationToken cancellationToken);
}

public record NotificationDeliveryResult(bool Success, string? ErrorMessage = null, bool IsTransient = false);
