using Microsoft.AspNetCore.SignalR;
using SentinelOps.Api.Hubs;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Api.Services;

public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<AlertsHub, IAlertsHubClient> _hubContext;

    public RealtimeNotifier(IHubContext<AlertsHub, IAlertsHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAlertFiredAsync(Guid applicationId, Guid? organizationId, AlertEventDto alertEvent, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group($"app_{applicationId}").AlertFired(alertEvent)
        };
        if (organizationId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"org_{organizationId.Value}").AlertFired(alertEvent));
        }
        await Task.WhenAll(tasks);
    }

    public async Task NotifyAlertResolvedAsync(Guid applicationId, Guid? organizationId, AlertEventDto alertEvent, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group($"app_{applicationId}").AlertResolved(alertEvent)
        };
        if (organizationId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"org_{organizationId.Value}").AlertResolved(alertEvent));
        }
        await Task.WhenAll(tasks);
    }

    public async Task NotifyIncidentCreatedAsync(Guid applicationId, Guid? organizationId, IncidentDto incident, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group($"app_{applicationId}").IncidentCreated(incident)
        };
        if (organizationId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"org_{organizationId.Value}").IncidentCreated(incident));
        }
        await Task.WhenAll(tasks);
    }

    public async Task NotifyIncidentUpdatedAsync(Guid applicationId, Guid? organizationId, IncidentDto incident, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group($"app_{applicationId}").IncidentUpdated(incident)
        };
        if (organizationId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"org_{organizationId.Value}").IncidentUpdated(incident));
        }
        await Task.WhenAll(tasks);
    }

    public async Task NotifyNotificationReceivedAsync(Guid? applicationId, Guid? organizationId, InAppNotificationDto notification, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        if (applicationId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"app_{applicationId.Value}").NotificationReceived(notification));
        }
        if (organizationId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"org_{organizationId.Value}").NotificationReceived(notification));
        }
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
}
