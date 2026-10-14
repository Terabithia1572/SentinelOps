using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class TelemetryAnomaly : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string SignalType { get; set; } = "ErrorRate"; // ErrorRate, Latency, RequestRate, Cpu, Memory, LogVolume
    public DateTime DetectionTimestamp { get; set; } = DateTime.UtcNow;
    public double BaselineValue { get; set; }
    public double ObservedValue { get; set; }
    public double DeviationScore { get; set; } // Z-Score or MAD multiplier
    public double ConfidenceScore { get; set; } // 0.0 - 1.0
    public Severity Severity { get; set; } = Severity.Warning;
    public string DetectionMethod { get; set; } = "RollingZScore";
    public string Explanation { get; set; } = string.Empty;
    public string? RelatedTelemetryReferences { get; set; } // JSON references to logs or traces
    public string Status { get; set; } = "Active"; // Active, Suppressed, Resolved, FalsePositive
    public DateTime? SuppressedUntilUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedBy { get; set; }

    public TelemetryAnomaly()
    {
    }

    public TelemetryAnomaly(
        Guid organizationId,
        Guid applicationId,
        string serviceName,
        string signalType,
        double baselineValue,
        double observedValue,
        double deviationScore,
        double confidenceScore,
        Severity severity,
        string detectionMethod,
        string explanation,
        string? relatedTelemetryReferences = null)
    {
        OrganizationId = organizationId;
        ApplicationId = applicationId;
        ServiceName = serviceName;
        SignalType = signalType;
        BaselineValue = baselineValue;
        ObservedValue = observedValue;
        DeviationScore = deviationScore;
        ConfidenceScore = confidenceScore;
        Severity = severity;
        DetectionMethod = detectionMethod;
        Explanation = explanation;
        RelatedTelemetryReferences = relatedTelemetryReferences;
        DetectionTimestamp = DateTime.UtcNow;
        Status = "Active";
    }

    public void Resolve(string? resolvedBy)
    {
        Status = "Resolved";
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
    }

    public void Suppress(TimeSpan duration)
    {
        Status = "Suppressed";
        SuppressedUntilUtc = DateTime.UtcNow.Add(duration);
    }
}
