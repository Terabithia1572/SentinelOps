using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelOps.Infrastructure.Services.Notifications;

namespace SentinelOps.Worker.Services;

public class NotificationDispatcherBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDispatcherBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public NotificationDispatcherBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationDispatcherBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationDispatcherBackgroundService started with {IntervalSeconds}s interval.", _checkInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcherService>();

                int processed = await dispatcher.DispatchPendingOutboxMessagesAsync(stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("Dispatched {Count} outbox notifications successfully.", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during notification dispatch cycle.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("NotificationDispatcherBackgroundService stopped.");
    }
}
