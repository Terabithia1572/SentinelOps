using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services;

public class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifyAlertFiredAsync(Guid applicationId, Guid? organizationId, AlertEventDto alertEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyAlertResolvedAsync(Guid applicationId, Guid? organizationId, AlertEventDto alertEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyIncidentCreatedAsync(Guid applicationId, Guid? organizationId, IncidentDto incident, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyIncidentUpdatedAsync(Guid applicationId, Guid? organizationId, IncidentDto incident, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyNotificationReceivedAsync(Guid? applicationId, Guid? organizationId, InAppNotificationDto notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
