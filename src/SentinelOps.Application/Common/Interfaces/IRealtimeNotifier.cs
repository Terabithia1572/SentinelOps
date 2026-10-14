using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyAlertFiredAsync(Guid applicationId, Guid? organizationId, AlertEventDto alertEvent, CancellationToken cancellationToken = default);
    Task NotifyAlertResolvedAsync(Guid applicationId, Guid? organizationId, AlertEventDto alertEvent, CancellationToken cancellationToken = default);
    Task NotifyIncidentCreatedAsync(Guid applicationId, Guid? organizationId, IncidentDto incident, CancellationToken cancellationToken = default);
    Task NotifyIncidentUpdatedAsync(Guid applicationId, Guid? organizationId, IncidentDto incident, CancellationToken cancellationToken = default);
    Task NotifyNotificationReceivedAsync(Guid? applicationId, Guid? organizationId, InAppNotificationDto notification, CancellationToken cancellationToken = default);
}
