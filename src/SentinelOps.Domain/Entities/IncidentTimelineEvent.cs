using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class IncidentTimelineEvent : Entity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public IncidentTimelineEventType EventType { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
