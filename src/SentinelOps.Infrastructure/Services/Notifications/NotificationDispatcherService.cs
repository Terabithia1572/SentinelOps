using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Notifications;

public class NotificationDispatcherService : INotificationDispatcherService
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly ILogger<NotificationDispatcherService> _logger;

    public NotificationDispatcherService(
        IApplicationDbContext context,
        IEnumerable<INotificationProvider> providers,
        ILogger<NotificationDispatcherService> logger)
    {
        _context = context;
        _providers = providers;
        _logger = logger;
    }

    public async Task<int> DispatchPendingOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var pendingMessages = await _context.NotificationOutboxes
            .Include(o => o.Channel)
            .Where(o => o.Status == NotificationStatus.Pending ||
                        (o.Status == NotificationStatus.Failed &&
                         o.RetryCount < o.MaxRetries &&
                         (o.NextRetryAtUtc == null || o.NextRetryAtUtc <= utcNow)))
            .OrderBy(o => o.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0) return 0;

        int processed = 0;
        foreach (var message in pendingMessages)
        {
            if (message.Channel == null || !message.Channel.IsEnabled)
            {
                message.Status = NotificationStatus.DeadLetter;
                message.ErrorMessage = "Notification channel is disabled or deleted.";
                continue;
            }

            var provider = _providers.FirstOrDefault(p => p.SupportedType == message.Channel.Type);
            if (provider == null)
            {
                message.Status = NotificationStatus.DeadLetter;
                message.ErrorMessage = $"No notification provider registered for type '{message.Channel.Type}'.";
                continue;
            }

            message.LastAttemptedAtUtc = utcNow;

            try
            {
                var result = await provider.SendAsync(message, message.Channel, cancellationToken);
                if (result.Success)
                {
                    message.Status = NotificationStatus.Delivered;
                    message.DeliveredAtUtc = utcNow;
                    message.ErrorMessage = null;
                    processed++;
                }
                else
                {
                    message.RetryCount++;
                    message.ErrorMessage = result.ErrorMessage;

                    if (result.IsTransient && message.RetryCount < message.MaxRetries)
                    {
                        message.Status = NotificationStatus.Failed;
                        message.NextRetryAtUtc = utcNow.AddSeconds(Math.Pow(2, message.RetryCount) * 10);
                        _logger.LogWarning(
                            "Transient failure delivering notification {MessageId} to {ChannelName}. Retry {RetryCount}/{MaxRetries} at {NextRetry}",
                            message.Id, message.Channel.Name, message.RetryCount, message.MaxRetries, message.NextRetryAtUtc);
                    }
                    else
                    {
                        message.Status = NotificationStatus.DeadLetter;
                        _logger.LogError(
                            "Permanent failure or retries exhausted for notification {MessageId} to {ChannelName}: {Error}",
                            message.Id, message.Channel.Name, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                message.Status = message.RetryCount >= message.MaxRetries ? NotificationStatus.DeadLetter : NotificationStatus.Failed;
                message.NextRetryAtUtc = utcNow.AddSeconds(Math.Pow(2, message.RetryCount) * 10);
                _logger.LogError(ex, "Unexpected exception delivering notification {MessageId}", message.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return processed;
    }
}
