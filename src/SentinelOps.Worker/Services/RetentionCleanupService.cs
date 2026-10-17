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

        var deletedLogs = await DeleteBatchAsync(context.LogEntries.Where(l => l.Timestamp < logCutoff), context.LogEntries, context, cancellationToken);
        var deletedTraces = await DeleteBatchAsync(context.TraceRecords.Where(t => t.StartedAtUtc < traceCutoff), context.TraceRecords, context, cancellationToken);
        var deletedMetrics = await DeleteBatchAsync(context.MetricRecords.Where(m => m.Timestamp < metricCutoff), context.MetricRecords, context, cancellationToken);

        _logger.LogInformation(
            "Telemetry retention cleanup completed. Deleted {Logs} logs older than {LogCutoff}, {Traces} traces older than {TraceCutoff}, {Metrics} metrics older than {MetricCutoff}",
            deletedLogs, logCutoff, deletedTraces, traceCutoff, deletedMetrics, metricCutoff);
    }

    private static async Task<long> DeleteBatchAsync<T>(IQueryable<T> query, DbSet<T> dbSet, IApplicationDbContext context, CancellationToken ct) where T : class
    {
        const int BatchSize = 500;
        long totalDeleted = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var batchDeleted = await query.Take(BatchSize).ExecuteDeleteAsync(ct);
                totalDeleted += batchDeleted;
                if (batchDeleted < BatchSize) break;
            }
            return totalDeleted;
        }
        catch (Exception)
        {
            while (!ct.IsCancellationRequested)
            {
                var batch = await query.Take(BatchSize).ToListAsync(ct);
                if (batch.Count == 0) break;

                dbSet.RemoveRange(batch);
                await context.SaveChangesAsync(ct);
                totalDeleted += batch.Count;

                if (batch.Count < BatchSize) break;
            }
            return totalDeleted;
        }
    }
}
