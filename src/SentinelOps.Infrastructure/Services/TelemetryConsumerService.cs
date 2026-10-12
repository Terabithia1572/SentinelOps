using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services;

public class TelemetryConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelemetryBuffer _buffer;
    private readonly ILogger<TelemetryConsumerService> _logger;

    public TelemetryConsumerService(
        IServiceScopeFactory scopeFactory,
        ITelemetryBuffer buffer,
        ILogger<TelemetryConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _buffer = buffer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting TelemetryConsumerService stream processing");

        var logsTask = ProcessLogsLoopAsync(stoppingToken);
        var tracesTask = ProcessTracesLoopAsync(stoppingToken);
        var metricsTask = ProcessMetricsLoopAsync(stoppingToken);

        await Task.WhenAll(logsTask, tracesTask, metricsTask);
    }

    private async Task ProcessLogsLoopAsync(CancellationToken stoppingToken)
    {
        var batch = new List<LogIngestionMessage>();
        var sync = new SemaphoreSlim(1, 1);

        using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var flushTimerTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            try
            {
                while (await timer.WaitForNextTickAsync(flushCts.Token))
                {
                    List<LogIngestionMessage>? toFlush = null;
                    await sync.WaitAsync(flushCts.Token);
                    try
                    {
                        if (batch.Count > 0)
                        {
                            toFlush = new List<LogIngestionMessage>(batch);
                            batch.Clear();
                        }
                    }
                    finally
                    {
                        sync.Release();
                    }

                    if (toFlush is { Count: > 0 })
                    {
                        await PersistLogsBatchAsync(toFlush, flushCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal timer shutdown
            }
        }, flushCts.Token);

        try
        {
            await foreach (var msg in _buffer.ReadLogsAsync(stoppingToken))
            {
                List<LogIngestionMessage>? toFlush = null;
                await sync.WaitAsync(stoppingToken);
                try
                {
                    batch.Add(msg);
                    if (batch.Count >= 100)
                    {
                        toFlush = new List<LogIngestionMessage>(batch);
                        batch.Clear();
                    }
                }
                finally
                {
                    sync.Release();
                }

                if (toFlush is { Count: > 0 })
                {
                    await PersistLogsBatchAsync(toFlush, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in logs processing stream");
        }
        finally
        {
            await flushCts.CancelAsync();
            try { await flushTimerTask; } catch { /* ignore */ }

            List<LogIngestionMessage>? remaining = null;
            await sync.WaitAsync(CancellationToken.None);
            try
            {
                if (batch.Count > 0)
                {
                    remaining = new List<LogIngestionMessage>(batch);
                    batch.Clear();
                }
            }
            finally
            {
                sync.Release();
            }

            if (remaining is { Count: > 0 })
            {
                await PersistLogsBatchAsync(remaining, CancellationToken.None);
            }
        }
    }

    private async Task PersistLogsBatchAsync(List<LogIngestionMessage> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var entities = new List<LogEntry>(batch.Count);
            foreach (var msg in batch)
            {
                var severity = Enum.TryParse<Severity>(msg.Severity, true, out var parsedSev)
                    ? parsedSev
                    : Severity.Information;

                entities.Add(new LogEntry
                {
                    ApplicationId = msg.ApplicationId,
                    ServiceName = msg.ServiceName,
                    EnvironmentName = msg.EnvironmentName,
                    Severity = severity,
                    Message = msg.Message,
                    MessageTemplate = msg.MessageTemplate,
                    Properties = msg.Properties,
                    ExceptionDetail = msg.ExceptionDetail,
                    TraceId = msg.TraceId,
                    SpanId = msg.SpanId,
                    CorrelationId = msg.CorrelationId,
                    Timestamp = msg.Timestamp,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.LogEntries.AddRangeAsync(entities, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Persisted batch of {Count} log entries to database", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist log entries batch of size {Count}", batch.Count);
        }
    }

    private async Task ProcessTracesLoopAsync(CancellationToken stoppingToken)
    {
        var batch = new List<TraceIngestionMessage>();
        var sync = new SemaphoreSlim(1, 1);

        using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var flushTimerTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            try
            {
                while (await timer.WaitForNextTickAsync(flushCts.Token))
                {
                    List<TraceIngestionMessage>? toFlush = null;
                    await sync.WaitAsync(flushCts.Token);
                    try
                    {
                        if (batch.Count > 0)
                        {
                            toFlush = new List<TraceIngestionMessage>(batch);
                            batch.Clear();
                        }
                    }
                    finally
                    {
                        sync.Release();
                    }

                    if (toFlush is { Count: > 0 })
                    {
                        await PersistTracesBatchAsync(toFlush, flushCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal timer shutdown
            }
        }, flushCts.Token);

        try
        {
            await foreach (var msg in _buffer.ReadTracesAsync(stoppingToken))
            {
                List<TraceIngestionMessage>? toFlush = null;
                await sync.WaitAsync(stoppingToken);
                try
                {
                    batch.Add(msg);
                    if (batch.Count >= 50)
                    {
                        toFlush = new List<TraceIngestionMessage>(batch);
                        batch.Clear();
                    }
                }
                finally
                {
                    sync.Release();
                }

                if (toFlush is { Count: > 0 })
                {
                    await PersistTracesBatchAsync(toFlush, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in traces processing stream");
        }
        finally
        {
            await flushCts.CancelAsync();
            try { await flushTimerTask; } catch { /* ignore */ }

            List<TraceIngestionMessage>? remaining = null;
            await sync.WaitAsync(CancellationToken.None);
            try
            {
                if (batch.Count > 0)
                {
                    remaining = new List<TraceIngestionMessage>(batch);
                    batch.Clear();
                }
            }
            finally
            {
                sync.Release();
            }

            if (remaining is { Count: > 0 })
            {
                await PersistTracesBatchAsync(remaining, CancellationToken.None);
            }
        }
    }

    private async Task PersistTracesBatchAsync(List<TraceIngestionMessage> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var traceGroups = batch.GroupBy(t => t.TraceId).ToList();
            var traceIds = traceGroups.Select(g => g.Key).ToList();

            var existingTraces = await dbContext.TraceRecords
                .Where(t => traceIds.Contains(t.TraceId))
                .ToDictionaryAsync(t => t.TraceId, cancellationToken);

            var spanIds = traceGroups.SelectMany(g => g.SelectMany(t => t.Spans.Select(s => s.SpanId))).Distinct().ToList();
            var existingSpanIds = await dbContext.SpanRecords
                .Where(s => traceIds.Contains(s.TraceId) && spanIds.Contains(s.SpanId))
                .Select(s => s.SpanId)
                .ToListAsync(cancellationToken);
            var existingSpanIdSet = new HashSet<string>(existingSpanIds);

            var spansToInsert = new List<SpanRecord>();
            var seenInBatch = new HashSet<string>();

            foreach (var group in traceGroups)
            {
                var traceId = group.Key;
                var groupTraces = group.ToList();

                var combinedSpans = groupTraces.SelectMany(t => t.Spans).ToList();
                var newSpans = combinedSpans
                    .Where(s => !existingSpanIdSet.Contains(s.SpanId) && seenInBatch.Add(s.SpanId))
                    .ToList();

                var totalDurationMs = groupTraces.Max(t => t.DurationMs);
                var hasErrors = groupTraces.Any(t => t.HasErrors);
                var firstMsg = groupTraces[0];

                if (existingTraces.TryGetValue(traceId, out var existing))
                {
                    existing.Duration = TimeSpan.FromMilliseconds(Math.Max(existing.Duration.TotalMilliseconds, totalDurationMs));
                    existing.SpanCount += newSpans.Count;
                    existing.HasErrors = existing.HasErrors || hasErrors;
                    existing.UpdatedAtUtc = DateTime.UtcNow;
                }
                else
                {
                    var newTrace = new TraceRecord
                    {
                        TraceId = traceId,
                        ApplicationId = firstMsg.ApplicationId,
                        ServiceName = firstMsg.ServiceName,
                        EnvironmentName = firstMsg.EnvironmentName,
                        RootSpanName = firstMsg.RootSpanName,
                        Duration = TimeSpan.FromMilliseconds(totalDurationMs),
                        SpanCount = newSpans.Count,
                        HasErrors = hasErrors,
                        StartedAtUtc = firstMsg.StartedAtUtc,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await dbContext.TraceRecords.AddAsync(newTrace, cancellationToken);
                    existingTraces[traceId] = newTrace;
                }

                foreach (var s in newSpans)
                {
                    spansToInsert.Add(new SpanRecord
                    {
                        SpanId = s.SpanId,
                        TraceId = s.TraceId,
                        ParentSpanId = s.ParentSpanId,
                        OperationName = s.OperationName,
                        ServiceName = s.ServiceName,
                        Duration = TimeSpan.FromMilliseconds(s.DurationMs),
                        Status = s.Status,
                        StartedAtUtc = s.StartedAtUtc,
                        Attributes = s.Attributes,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            if (spansToInsert.Count > 0)
            {
                await dbContext.SpanRecords.AddRangeAsync(spansToInsert, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Persisted {TraceCount} traces and {SpanCount} spans", traceGroups.Count, spansToInsert.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist trace records batch of size {Count}", batch.Count);
        }
    }

    private async Task ProcessMetricsLoopAsync(CancellationToken stoppingToken)
    {
        var batch = new List<MetricIngestionMessage>();
        var sync = new SemaphoreSlim(1, 1);

        using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var flushTimerTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            try
            {
                while (await timer.WaitForNextTickAsync(flushCts.Token))
                {
                    List<MetricIngestionMessage>? toFlush = null;
                    await sync.WaitAsync(flushCts.Token);
                    try
                    {
                        if (batch.Count > 0)
                        {
                            toFlush = new List<MetricIngestionMessage>(batch);
                            batch.Clear();
                        }
                    }
                    finally
                    {
                        sync.Release();
                    }

                    if (toFlush is { Count: > 0 })
                    {
                        await PersistMetricsBatchAsync(toFlush, flushCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal timer shutdown
            }
        }, flushCts.Token);

        try
        {
            await foreach (var msg in _buffer.ReadMetricsAsync(stoppingToken))
            {
                List<MetricIngestionMessage>? toFlush = null;
                await sync.WaitAsync(stoppingToken);
                try
                {
                    batch.Add(msg);
                    if (batch.Count >= 100)
                    {
                        toFlush = new List<MetricIngestionMessage>(batch);
                        batch.Clear();
                    }
                }
                finally
                {
                    sync.Release();
                }

                if (toFlush is { Count: > 0 })
                {
                    await PersistMetricsBatchAsync(toFlush, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in metrics processing stream");
        }
        finally
        {
            await flushCts.CancelAsync();
            try { await flushTimerTask; } catch { /* ignore */ }

            List<MetricIngestionMessage>? remaining = null;
            await sync.WaitAsync(CancellationToken.None);
            try
            {
                if (batch.Count > 0)
                {
                    remaining = new List<MetricIngestionMessage>(batch);
                    batch.Clear();
                }
            }
            finally
            {
                sync.Release();
            }

            if (remaining is { Count: > 0 })
            {
                await PersistMetricsBatchAsync(remaining, CancellationToken.None);
            }
        }
    }

    private async Task PersistMetricsBatchAsync(List<MetricIngestionMessage> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var entities = new List<MetricRecord>(batch.Count);
            foreach (var msg in batch)
            {
                var metricType = Enum.TryParse<MetricType>(msg.MetricType, true, out var parsedType)
                    ? parsedType
                    : MetricType.Counter;

                entities.Add(new MetricRecord
                {
                    ApplicationId = msg.ApplicationId,
                    ServiceName = msg.ServiceName,
                    EnvironmentName = msg.EnvironmentName,
                    MetricName = msg.MetricName,
                    MetricType = metricType,
                    Value = msg.Value,
                    Tags = msg.Tags,
                    Timestamp = msg.Timestamp,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.MetricRecords.AddRangeAsync(entities, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Persisted batch of {Count} metric records to database", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist metric records batch of size {Count}", batch.Count);
        }
    }
}
