using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Api.Hubs;

public interface IAlertsHubClient
{
    Task AlertFired(AlertEventDto alertEvent);
    Task AlertResolved(AlertEventDto alertEvent);
    Task AlertUpdated(AlertRuleDto alertRule);
    Task IncidentCreated(IncidentDto incident);
    Task IncidentUpdated(IncidentDto incident);
    Task NotificationReceived(InAppNotificationDto notification);
    Task SloUpdated(SloDto slo);
}

[Authorize]
public class AlertsHub : Hub<IAlertsHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AlertsHub> _logger;

    public AlertsHub(
        IApplicationDbContext context,
        ILogger<AlertsHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task JoinApplication(Guid applicationId)
    {
        // Server-side verification of application existence and active state
        var app = await _context.Applications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app == null || !app.IsActive)
        {
            throw new HubException("Application not found or inactive.");
        }

        var groupName = $"app_{applicationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} joined real-time telemetry group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveApplication(Guid applicationId)
    {
        var groupName = $"app_{applicationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left real-time telemetry group {GroupName}", Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        _logger.LogDebug("Real-time telemetry WebSocket connected for user {UserName} (Connection: {ConnectionId})", user, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Real-time telemetry WebSocket disconnected (Connection: {ConnectionId})", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
