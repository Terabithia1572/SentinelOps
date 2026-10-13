namespace SentinelOps.Domain.Enums;

public enum AlertConditionType
{
    ErrorRateAboveThreshold,
    P95LatencyAboveThreshold,
    P99LatencyAboveThreshold,
    ThroughputBelowThreshold,
    ThroughputAboveThreshold,
    ExceptionCountAboveThreshold,
    LogSeverityCountAboveThreshold,
    NoTelemetryReceived
}
