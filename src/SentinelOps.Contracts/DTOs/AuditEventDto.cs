namespace SentinelOps.Contracts.DTOs;

public class AuditEventDto
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string? ApplicationScope { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public class AuditSummaryDto
{
    public int TotalEvents24h { get; set; }
    public int FailureCount24h { get; set; }
    public List<TopFacetCountDto> TopActions { get; set; } = [];
    public List<TopFacetCountDto> TopActors { get; set; } = [];
}

public class TopFacetCountDto
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}
