using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Infrastructure.Services;
using Xunit;

namespace SentinelOps.Application.Tests.Services;

public class RedisTelemetryBufferTests
{
    private readonly RedisTelemetryBuffer _buffer;

    public RedisTelemetryBufferTests()
    {
        // When redis connection multiplexer is null, buffer operates in resilient in-memory mode
        _buffer = new RedisTelemetryBuffer(NullLogger<RedisTelemetryBuffer>.Instance, redis: null);
    }

    [Fact]
    public async Task PushAndReadLogs_InMemoryFallback_QueuesAndYieldsMessages()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var log = new LogIngestionMessage
        {
            ApplicationId = appId,
            ServiceName = "TestService",
            Severity = "Information",
            Message = "Test log message"
        };

        // Act
        await _buffer.PushLogAsync(log);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<LogIngestionMessage>();

        await foreach (var item in _buffer.ReadLogsAsync(cts.Token))
        {
            received.Add(item);
            break; // Read the one item and finish
        }

        // Assert
        received.Should().HaveCount(1);
        received[0].Message.Should().Be("Test log message");
        received[0].ServiceName.Should().Be("TestService");
    }

    [Fact]
    public async Task PushAndReadTraces_InMemoryFallback_QueuesAndYieldsMessages()
    {
        // Arrange
        var trace = new TraceIngestionMessage
        {
            TraceId = "trace-12345",
            ApplicationId = Guid.NewGuid(),
            ServiceName = "OrderService",
            RootSpanName = "POST /orders",
            DurationMs = 120,
            SpanCount = 2,
            HasErrors = false
        };

        // Act
        await _buffer.PushTraceAsync(trace);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<TraceIngestionMessage>();

        await foreach (var item in _buffer.ReadTracesAsync(cts.Token))
        {
            received.Add(item);
            break;
        }

        // Assert
        received.Should().HaveCount(1);
        received[0].TraceId.Should().Be("trace-12345");
        received[0].DurationMs.Should().Be(120);
    }

    [Fact]
    public async Task PushAndReadMetrics_InMemoryFallback_QueuesAndYieldsMessages()
    {
        // Arrange
        var metric = new MetricIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            ServiceName = "InventoryService",
            MetricName = "stock_count",
            MetricType = "Gauge",
            Value = 450
        };

        // Act
        await _buffer.PushMetricAsync(metric);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<MetricIngestionMessage>();

        await foreach (var item in _buffer.ReadMetricsAsync(cts.Token))
        {
            received.Add(item);
            break;
        }

        // Assert
        received.Should().HaveCount(1);
        received[0].MetricName.Should().Be("stock_count");
        received[0].Value.Should().Be(450);
    }
}
