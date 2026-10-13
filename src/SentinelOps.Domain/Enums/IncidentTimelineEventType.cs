namespace SentinelOps.Domain.Enums;

public enum IncidentTimelineEventType
{
    Created,
    Acknowledged,
    Assigned,
    SeverityChanged,
    StatusChanged,
    CommentAdded,
    AlertLinked,
    AlertResolved,
    Mitigated,
    Resolved,
    Closed,
    Reopened
}
