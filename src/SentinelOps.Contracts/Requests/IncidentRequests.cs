namespace SentinelOps.Contracts.Requests;

public record CreateIncidentRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = "Error";
    public Guid ApplicationId { get; init; }
    public string? EnvironmentName { get; init; }
    public string? ServiceName { get; init; }
    public string? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public string? RelatedTraceId { get; init; }
    public Guid? RelatedAlertRuleId { get; init; }
}

public record UpdateIncidentRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Severity { get; init; }
}

public record AssignIncidentRequest
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

public record TransitionIncidentRequest
{
    public string TargetStatus { get; init; } = string.Empty;
    public string? RootCause { get; init; }
    public string? ResolutionNotes { get; init; }
    public string? ClosingNotes { get; init; }
    public string? Reason { get; init; }
}

public record AddIncidentCommentRequest
{
    public string CommentText { get; init; } = string.Empty;
}
