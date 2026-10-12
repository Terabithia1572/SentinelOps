using FluentAssertions;
using SentinelOps.Application.Services;
using SentinelOps.Contracts.Otlp;
using Xunit;

namespace SentinelOps.Application.Tests.Services;

public class OtlpPayloadMapperTests
{
    private readonly Guid _appId = Guid.NewGuid();

    [Fact]
    public void MapLogs_ParsesSeverityAndResourceCorrectly()
    {
        // Arrange
        var payload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes =
                        [
                            new() { Key = "service.name", Value = new() { StringValue = "OrderService" } },
                            new() { Key = "deployment.environment", Value = new() { StringValue = "Staging" } }
                        ]
                    },
                    ScopeLogs =
                    [
                        new()
                        {
                            LogRecords =
                            [
                                new()
                                {
                                    SeverityNumber = 9,
                                    SeverityText = "Information",
                                    Body = new OtlpAnyValue { StringValue = "Payment processed successfully" },
                                    TraceId = "4bf92f3577b34da6a3ce929d0e0e4736",
                                    SpanId = "00f067aa0ba902b7",
                                    Attributes =
                                    [
                                        new() { Key = "order.id", Value = new() { StringValue = "ORD-99" } }
                                    ]
                                },
                                new()
                                {
                                    SeverityNumber = 17,
                                    SeverityText = "Error",
                                    Body = new OtlpAnyValue { StringValue = "Database connection failed" },
                                    Attributes =
                                    [
                                        new() { Key = "exception.type", Value = new() { StringValue = "Npgsql.NpgsqlException" } },
                                        new() { Key = "exception.message", Value = new() { StringValue = "Connection refused" } },
                                        new() { Key = "exception.stacktrace", Value = new() { StringValue = "at Npgsql.Connector.Connect()" } }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var messages = OtlpPayloadMapper.MapLogs(payload, _appId);

        // Assert
        messages.Should().HaveCount(2);

        var first = messages[0];
        first.ApplicationId.Should().Be(_appId);
        first.ServiceName.Should().Be("OrderService");
        first.EnvironmentName.Should().Be("Staging");
        first.Severity.Should().Be("Information");
        first.Message.Should().Be("Payment processed successfully");
        first.TraceId.Should().Be("4bf92f3577b34da6a3ce929d0e0e4736");
        first.SpanId.Should().Be("00f067aa0ba902b7");
        first.Properties.Should().Contain("ORD-99");
        first.ExceptionDetail.Should().BeNull();

        var second = messages[1];
        second.Severity.Should().Be("Error");
        second.ExceptionDetail.Should().Contain("Npgsql.NpgsqlException: Connection refused");
    }

    [Fact]
    public void MapTraces_CalculatesTraceDurationAndErrorStatusCorrectly()
    {
        // Arrange
        var traceId = "5b832f20d0130f698b3337a6ceeef647";
        var payload = new OtlpTracesPayload
        {
            ResourceSpans =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes =
                        [
                            new() { Key = "service.name", Value = new() { StringValue = "CartService" } }
                        ]
                    },
                    ScopeSpans =
                    [
                        new()
                        {
                            Spans =
                            [
                                new()
                                {
                                    TraceId = traceId,
                                    SpanId = "0000000000000001",
                                    Name = "GET /cart",
                                    StartTimeUnixNano = "1700000000000000000",
                                    EndTimeUnixNano = "1700000000500000000", // +500ms
                                    Status = new OtlpStatus { Code = 1 }
                                },
                                new()
                                {
                                    TraceId = traceId,
                                    SpanId = "0000000000000002",
                                    ParentSpanId = "0000000000000001",
                                    Name = "redis:get",
                                    StartTimeUnixNano = "1700000000050000000",
                                    EndTimeUnixNano = "1700000000200000000", // +150ms
                                    Status = new OtlpStatus { Code = 2 } // Error
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var traces = OtlpPayloadMapper.MapTraces(payload, _appId);

        // Assert
        traces.Should().HaveCount(1);
        var trace = traces[0];
        trace.TraceId.Should().Be(traceId);
        trace.ServiceName.Should().Be("CartService");
        trace.RootSpanName.Should().Be("GET /cart");
        trace.SpanCount.Should().Be(2);
        trace.HasErrors.Should().BeTrue();
        trace.DurationMs.Should().Be(500);
        trace.Spans.Should().HaveCount(2);
    }

    [Fact]
    public void MapMetrics_ParsesGaugeSumAndHistogramCorrectly()
    {
        // Arrange
        var payload = new OtlpMetricsPayload
        {
            ResourceMetrics =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "AnalyticsService" } }]
                    },
                    ScopeMetrics =
                    [
                        new()
                        {
                            Metrics =
                            [
                                new()
                                {
                                    Name = "memory_bytes",
                                    Gauge = new OtlpGauge
                                    {
                                        DataPoints = [new() { AsDouble = 1048576, TimeUnixNano = "1700000000000000000" }]
                                    }
                                },
                                new()
                                {
                                    Name = "requests_total",
                                    Sum = new OtlpSum
                                    {
                                        DataPoints = [new() { AsInt = 42, TimeUnixNano = "1700000000000000000" }]
                                    }
                                },
                                new()
                                {
                                    Name = "latency_histogram",
                                    Histogram = new OtlpHistogram
                                    {
                                        DataPoints = [new() { Sum = 125.4, Count = 5, TimeUnixNano = "1700000000000000000" }]
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var metrics = OtlpPayloadMapper.MapMetrics(payload, _appId);

        // Assert
        metrics.Should().HaveCount(3);
        metrics[0].MetricName.Should().Be("memory_bytes");
        metrics[0].MetricType.Should().Be("Gauge");
        metrics[0].Value.Should().Be(1048576);

        metrics[1].MetricName.Should().Be("requests_total");
        metrics[1].MetricType.Should().Be("Counter");
        metrics[1].Value.Should().Be(42);

        metrics[2].MetricName.Should().Be("latency_histogram");
        metrics[2].MetricType.Should().Be("Histogram");
        metrics[2].Value.Should().Be(125.4);
    }
}
