namespace SentinelOps.Domain.Enums;

public enum NotificationEventType
{
    AlertTriggered,
    AlertResolved,
    IncidentCreated,
    IncidentAssigned,
    IncidentSeverityChanged,
    IncidentResolved,
    SystemCritical
}
