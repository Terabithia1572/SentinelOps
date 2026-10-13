using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Worker.Services;

public class SloEvaluationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SloEvaluationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

    public SloEvaluationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SloEvaluationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SloEvaluationBackgroundService started with {IntervalSeconds}s interval.", _checkInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sloService = scope.ServiceProvider.GetRequiredService<ISloEvaluationService>();

                int evaluated = await sloService.EvaluateAllPendingSlosAsync(stoppingToken);
                if (evaluated > 0)
                {
                    _logger.LogInformation("Evaluated {Count} SLO objectives successfully.", evaluated);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during SLO evaluation cycle.");
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

        _logger.LogInformation("SloEvaluationBackgroundService stopped.");
    }
}
