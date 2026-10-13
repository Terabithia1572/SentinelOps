namespace SentinelOps.Contracts.DTOs;

public record IncidentDto
{
    public Guid Id { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string? EnvironmentName { get; init; }
    public string? ServiceName { get; init; }
    public string? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? AcknowledgedAtUtc { get; init; }
    public string? AcknowledgedBy { get; init; }
    public DateTime? MitigatedAtUtc { get; init; }
    public string? MitigatedBy { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public string? ClosedBy { get; init; }
    public string? RootCauseSummary { get; init; }
    public string? ResolutionNotes { get; init; }
    public string? RelatedTraceId { get; init; }
    public int TimelineEventsCount { get; init; }
    public int CommentsCount { get; init; }
}

public record IncidentDetailDto : IncidentDto
{
    public List<IncidentTimelineEventDto> TimelineEvents { get; init; } = new();
    public List<IncidentCommentDto> Comments { get; init; } = new();
    public List<AlertRuleDto> LinkedAlerts { get; init; } = new();
}

public record IncidentTimelineEventDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; }
    public string ActorId { get; init; } = string.Empty;
    public string ActorName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record IncidentCommentDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string CommentText { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
