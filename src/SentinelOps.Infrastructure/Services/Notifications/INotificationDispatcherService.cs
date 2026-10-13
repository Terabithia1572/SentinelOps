namespace SentinelOps.Infrastructure.Services.Notifications;

public interface INotificationDispatcherService
{
    Task<int> DispatchPendingOutboxMessagesAsync(CancellationToken cancellationToken = default);
}
