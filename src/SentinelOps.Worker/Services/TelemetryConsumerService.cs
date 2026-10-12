using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Worker.Services;

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
        var lastFlush = DateTime.UtcNow;

        try
        {
            await foreach (var msg in _buffer.ReadLogsAsync(stoppingToken))
            {
                batch.Add(msg);

                if (batch.Count >= 100 || (DateTime.UtcNow - lastFlush).TotalMilliseconds >= 500)
                {
                    await PersistLogsBatchAsync(batch, stoppingToken);
                    batch.Clear();
                    lastFlush = DateTime.UtcNow;
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
            if (batch.Count > 0)
            {
                await PersistLogsBatchAsync(batch, CancellationToken.None);
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
            _logger.LogDebug("Persisted batch of {Count} log entries to database", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist log entries batch of size {Count}", batch.Count);
        }
    }

    private async Task ProcessTracesLoopAsync(CancellationToken stoppingToken)
    {
        var batch = new List<TraceIngestionMessage>();
        var lastFlush = DateTime.UtcNow;

        try
        {
            await foreach (var msg in _buffer.ReadTracesAsync(stoppingToken))
            {
                batch.Add(msg);

                if (batch.Count >= 50 || (DateTime.UtcNow - lastFlush).TotalMilliseconds >= 500)
                {
                    await PersistTracesBatchAsync(batch, stoppingToken);
                    batch.Clear();
                    lastFlush = DateTime.UtcNow;
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
            if (batch.Count > 0)
            {
                await PersistTracesBatchAsync(batch, CancellationToken.None);
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

            var spansToInsert = new List<SpanRecord>();

            foreach (var group in traceGroups)
            {
                var traceId = group.Key;
                var groupTraces = group.ToList();

                var combinedSpans = groupTraces.SelectMany(t => t.Spans).ToList();
                var totalDurationMs = groupTraces.Max(t => t.DurationMs);
                var hasErrors = groupTraces.Any(t => t.HasErrors);
                var firstMsg = groupTraces[0];

                if (existingTraces.TryGetValue(traceId, out var existing))
                {
                    existing.Duration = TimeSpan.FromMilliseconds(Math.Max(existing.Duration.TotalMilliseconds, totalDurationMs));
                    existing.SpanCount += combinedSpans.Count;
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
                        SpanCount = combinedSpans.Count,
                        HasErrors = hasErrors,
                        StartedAtUtc = firstMsg.StartedAtUtc,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await dbContext.TraceRecords.AddAsync(newTrace, cancellationToken);
                    existingTraces[traceId] = newTrace;
                }

                foreach (var s in combinedSpans)
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
            _logger.LogDebug("Persisted {TraceCount} traces and {SpanCount} spans", traceGroups.Count, spansToInsert.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist trace records batch of size {Count}", batch.Count);
        }
    }

    private async Task ProcessMetricsLoopAsync(CancellationToken stoppingToken)
    {
        var batch = new List<MetricIngestionMessage>();
        var lastFlush = DateTime.UtcNow;

        try
        {
            await foreach (var msg in _buffer.ReadMetricsAsync(stoppingToken))
            {
                batch.Add(msg);

                if (batch.Count >= 100 || (DateTime.UtcNow - lastFlush).TotalMilliseconds >= 500)
                {
                    await PersistMetricsBatchAsync(batch, stoppingToken);
                    batch.Clear();
                    lastFlush = DateTime.UtcNow;
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
            if (batch.Count > 0)
            {
                await PersistMetricsBatchAsync(batch, CancellationToken.None);
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
            _logger.LogDebug("Persisted batch of {Count} metric records to database", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist metric records batch of size {Count}", batch.Count);
        }
    }
}
