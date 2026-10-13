using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Worker.Services;

public class AlertEvaluationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertEvaluationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

    public AlertEvaluationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AlertEvaluationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertEvaluationBackgroundService started with {IntervalSeconds}s interval.", _checkInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertEvaluationService>();

                int evaluated = await alertService.EvaluateAllPendingRulesAsync(stoppingToken);
                if (evaluated > 0)
                {
                    _logger.LogInformation("AlertEvaluationBackgroundService evaluated {Count} pending alert rules.", evaluated);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during alert rule evaluation cycle.");
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

        _logger.LogInformation("AlertEvaluationBackgroundService stopped.");
    }
}
