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

    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
    }

    public async Task JoinApplication(Guid applicationId)
    {
        var app = await _context.Applications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app == null || !app.IsActive)
        {
            throw new HubException("Application not found or inactive.");
        }

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId) && Context.User?.IsInRole(SentinelOps.Domain.Constants.Roles.Admin) != true)
        {
            var isMember = await _context.OrganizationMembers.AsNoTracking()
                .AnyAsync(m => m.OrganizationId == app.OrganizationId && m.UserId == userId);
            if (!isMember)
            {
                throw new HubException("Access denied: You are not a member of the organization owning this application.");
            }
        }

        var groupName = $"app_{applicationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} joined real-time telemetry group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task JoinApplicationGroup(string applicationIdStr)
    {
        if (Guid.TryParse(applicationIdStr, out var appId))
        {
            await JoinApplication(appId);
        }
    }

    public async Task LeaveApplication(Guid applicationId)
    {
        var groupName = $"app_{applicationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left real-time telemetry group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveApplicationGroup(string applicationIdStr)
    {
        if (Guid.TryParse(applicationIdStr, out var appId))
        {
            await LeaveApplication(appId);
        }
    }

    public async Task JoinOrganization(Guid organizationId)
    {
        var org = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId);
        if (org == null || !org.IsActive)
        {
            throw new HubException("Organization not found or inactive.");
        }

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId) && Context.User?.IsInRole(SentinelOps.Domain.Constants.Roles.Admin) != true)
        {
            var isMember = await _context.OrganizationMembers.AsNoTracking()
                .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
            if (!isMember)
            {
                throw new HubException("Access denied: You are not a member of this organization.");
            }
        }

        var groupName = $"org_{organizationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} joined real-time telemetry group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task JoinOrganizationGroup(string organizationIdStr)
    {
        if (Guid.TryParse(organizationIdStr, out var orgId))
        {
            await JoinOrganization(orgId);
        }
    }

    public async Task LeaveOrganization(Guid organizationId)
    {
        var groupName = $"org_{organizationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left real-time telemetry group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveOrganizationGroup(string organizationIdStr)
    {
        if (Guid.TryParse(organizationIdStr, out var orgId))
        {
            await LeaveOrganization(orgId);
        }
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
