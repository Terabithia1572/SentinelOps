using SentinelOps.Contracts.Ingestion;

namespace SentinelOps.Application.Common.Interfaces;

public interface ITelemetryBuffer
{
    Task PushLogAsync(LogIngestionMessage message, CancellationToken cancellationToken = default);
    Task PushLogsAsync(IEnumerable<LogIngestionMessage> messages, CancellationToken cancellationToken = default);

    Task PushTraceAsync(TraceIngestionMessage message, CancellationToken cancellationToken = default);
    Task PushTracesAsync(IEnumerable<TraceIngestionMessage> messages, CancellationToken cancellationToken = default);

    Task PushMetricAsync(MetricIngestionMessage message, CancellationToken cancellationToken = default);
    Task PushMetricsAsync(IEnumerable<MetricIngestionMessage> messages, CancellationToken cancellationToken = default);

    IAsyncEnumerable<LogIngestionMessage> ReadLogsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<TraceIngestionMessage> ReadTracesAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<MetricIngestionMessage> ReadMetricsAsync(CancellationToken cancellationToken = default);
}
