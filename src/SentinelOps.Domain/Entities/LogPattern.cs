using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class LogPattern : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string PatternFingerprint { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Information;
    public long TotalOccurrences { get; set; } = 1;
    public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public string SampleMessage { get; set; } = string.Empty;
    public string? SampleTraceId { get; set; }

    public LogPattern()
    {
    }

    public LogPattern(
        Guid organizationId,
        Guid applicationId,
        string serviceName,
        string patternFingerprint,
        string template,
        Severity severity,
        string sampleMessage,
        string? sampleTraceId = null)
    {
        OrganizationId = organizationId;
        ApplicationId = applicationId;
        ServiceName = serviceName;
        PatternFingerprint = patternFingerprint;
        Template = template;
        Severity = severity;
        SampleMessage = sampleMessage;
        SampleTraceId = sampleTraceId;
        TotalOccurrences = 1;
        FirstSeenAtUtc = DateTime.UtcNow;
        LastSeenAtUtc = DateTime.UtcNow;
    }

    public void RecordOccurrence(DateTime timestamp, string? latestTraceId = null)
    {
        TotalOccurrences++;
        if (timestamp > LastSeenAtUtc)
        {
            LastSeenAtUtc = timestamp;
        }
        if (!string.IsNullOrEmpty(latestTraceId))
        {
            SampleTraceId = latestTraceId;
        }
    }
}
