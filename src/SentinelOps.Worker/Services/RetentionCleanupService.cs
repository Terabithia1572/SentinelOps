using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Worker.Services;

public class RetentionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionCleanupService> _logger;
    private readonly IConfiguration _configuration;

    public RetentionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RetentionCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retention Cleanup Service started.");

        var intervalHours = _configuration.GetValue("Retention:CleanupIntervalHours", 24);
        var interval = TimeSpan.FromHours(intervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during telemetry retention cleanup");
            }

            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("Retention Cleanup Service stopping.");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting telemetry retention cleanup run...");

        var logRetentionDays = _configuration.GetValue("Retention:LogRetentionDays", 30);
        var traceRetentionDays = _configuration.GetValue("Retention:TraceRetentionDays", 14);
        var metricRetentionDays = _configuration.GetValue("Retention:MetricRetentionDays", 90);

        var logCutoff = DateTime.UtcNow.AddDays(-logRetentionDays);
        var traceCutoff = DateTime.UtcNow.AddDays(-traceRetentionDays);
        var metricCutoff = DateTime.UtcNow.AddDays(-metricRetentionDays);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var deletedLogs = await context.LogEntries
            .Where(l => l.Timestamp < logCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedTraces = await context.TraceRecords
            .Where(t => t.StartedAtUtc < traceCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedMetrics = await context.MetricRecords
            .Where(m => m.Timestamp < metricCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "Telemetry retention cleanup completed. Deleted {Logs} logs older than {LogCutoff}, {Traces} traces older than {TraceCutoff}, {Metrics} metrics older than {MetricCutoff}",
            deletedLogs, logCutoff, deletedTraces, traceCutoff, deletedMetrics, metricCutoff);
    }
}
