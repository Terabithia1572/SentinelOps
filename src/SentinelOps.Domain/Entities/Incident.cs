using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class Incident : AggregateRoot
{
    public string IncidentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Error;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public Guid ApplicationId { get; set; }
    public string? EnvironmentName { get; set; }
    public string? ServiceName { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? MitigatedAtUtc { get; set; }
    public string? MitigatedBy { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }
    public string? RootCauseSummary { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? RelatedTraceId { get; set; }
    public Guid? RelatedLogId { get; set; }

    public ICollection<IncidentTimelineEvent> TimelineEvents { get; set; } = new List<IncidentTimelineEvent>();
    public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
    public ICollection<IncidentAlertRelation> AlertRelations { get; set; } = new List<IncidentAlertRelation>();

    public void Acknowledge(string userId, string userName)
    {
        if (AcknowledgedAtUtc.HasValue) return;

        var now = DateTime.UtcNow;
        AcknowledgedAtUtc = now;
        AcknowledgedBy = userName;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.Acknowledged,
            TimestampUtc = now,
            ActorId = userId,
            ActorName = userName,
            Title = "Incident Acknowledged",
            Description = $"{userName} acknowledged the incident."
        });
    }

    public void Investigate(string userId, string userName)
    {
        if (Status == IncidentStatus.Closed || Status == IncidentStatus.Resolved)
        {
            throw new InvalidOperationException($"Cannot transition to Investigating from status {Status}.");
        }

        var now = DateTime.UtcNow;
        Status = IncidentStatus.Investigating;
        if (!AcknowledgedAtUtc.HasValue)
        {
            AcknowledgedAtUtc = now;
            AcknowledgedBy = userName;
        }

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.StatusChanged,
            TimestampUtc = now,
            ActorId = userId,
            ActorName = userName,
            Title = "Status Changed to Investigating",
            Description = $"{userName} started investigation."
        });
    }

    public void Mitigate(string userId, string userName, string? notes)
    {
        if (Status == IncidentStatus.Closed || Status == IncidentStatus.Resolved)
        {
            throw new InvalidOperationException($"Cannot mitigate an incident that is already {Status}.");
        }

        var now = DateTime.UtcNow;
        Status = IncidentStatus.Mitigated;
        MitigatedAtUtc = now;
        MitigatedBy = userName;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.Mitigated,
            TimestampUtc = now,
            ActorId = userId,
            ActorName = userName,
            Title = "Incident Mitigated",
            Description = string.IsNullOrWhiteSpace(notes) ? $"{userName} marked the incident as mitigated." : notes
        });
    }

    public void Resolve(string userId, string userName, string rootCause, string resolutionNotes)
    {
        if (Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Cannot resolve an incident that is closed.");
        }

        var now = DateTime.UtcNow;
        Status = IncidentStatus.Resolved;
        ResolvedAtUtc = now;
        ResolvedBy = userName;
        RootCauseSummary = rootCause;
        ResolutionNotes = resolutionNotes;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.Resolved,
            TimestampUtc = now,
            ActorId = userId,
            ActorName = userName,
            Title = "Incident Resolved",
            Description = $"Root cause: {rootCause}. Notes: {resolutionNotes}"
        });
    }

    public void Close(string userId, string userName, string? closingNotes)
    {
        var now = DateTime.UtcNow;
        Status = IncidentStatus.Closed;
        ClosedAtUtc = now;
        ClosedBy = userName;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.Closed,
            TimestampUtc = now,
            ActorId = userId,
            ActorName = userName,
            Title = "Incident Closed",
            Description = string.IsNullOrWhiteSpace(closingNotes) ? $"{userName} closed the incident." : closingNotes
        });
    }

    public void Reopen(string userId, string userName, string reason)
    {
        if (Status != IncidentStatus.Resolved && Status != IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Only resolved or closed incidents can be reopened.");
        }

        var now = DateTime.UtcNow;
        Status = IncidentStatus.Open;
        ResolvedAtUtc = null;
        ResolvedBy = null;
        ClosedAtUtc = null;
        ClosedBy = null;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.Reopened,
            TimestampUtc = now,
            ActorId = userId,
            ActorName = userName,
            Title = "Incident Reopened",
            Description = $"Reopened by {userName}. Reason: {reason}"
        });
    }

    public void Assign(string userId, string userName, string actorId, string actorName)
    {
        AssignedToUserId = userId;
        AssignedToUserName = userName;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.Assigned,
            TimestampUtc = DateTime.UtcNow,
            ActorId = actorId,
            ActorName = actorName,
            Title = "Incident Assigned",
            Description = $"Assigned to {userName} by {actorName}."
        });
    }

    public void ChangeSeverity(Severity newSeverity, string actorId, string actorName)
    {
        if (Severity == newSeverity) return;

        var old = Severity;
        Severity = newSeverity;

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.SeverityChanged,
            TimestampUtc = DateTime.UtcNow,
            ActorId = actorId,
            ActorName = actorName,
            Title = "Severity Changed",
            Description = $"Severity changed from {old} to {newSeverity} by {actorName}."
        });
    }

    public void AddComment(string userId, string userName, string commentText)
    {
        var comment = new IncidentComment
        {
            IncidentId = Id,
            UserId = userId,
            UserName = userName,
            CommentText = commentText,
            CreatedAtUtc = DateTime.UtcNow
        };
        Comments.Add(comment);

        TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = Id,
            EventType = IncidentTimelineEventType.CommentAdded,
            TimestampUtc = DateTime.UtcNow,
            ActorId = userId,
            ActorName = userName,
            Title = "Comment Added",
            Description = $"{userName}: {commentText}"
        });
    }

    public void LinkAlert(Guid alertRuleId)
    {
        if (!AlertRelations.Any(r => r.AlertRuleId == alertRuleId))
        {
            AlertRelations.Add(new IncidentAlertRelation
            {
                IncidentId = Id,
                AlertRuleId = alertRuleId,
                LinkedAtUtc = DateTime.UtcNow
            });
        }
    }
}
