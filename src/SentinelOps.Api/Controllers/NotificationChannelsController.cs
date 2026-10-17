using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Security;
using SentinelOps.Infrastructure.Services.Notifications;

namespace SentinelOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notification-channels")]
public class NotificationChannelsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _tenantService;
    private readonly IEnumerable<INotificationProvider> _providers;

    public NotificationChannelsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService tenantService,
        IEnumerable<INotificationProvider> providers)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _providers = providers;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationChannelDto>>>> GetChannels(
        [FromQuery] Guid? applicationId)
    {
        var query = _context.NotificationChannels.AsNoTracking().AsQueryable();

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (applicationId.HasValue && applicationId.Value != Guid.Empty)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == applicationId.Value && a.OrganizationId == tenantOrgId);
                if (!isAppInTenant)
                {
                    return Ok(ApiResponse<List<NotificationChannelDto>>.SuccessResponse([]));
                }
                query = query.Where(c => c.ApplicationId == applicationId.Value && (c.OrganizationId == null || c.OrganizationId == tenantOrgId));
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                query = query.Where(c => c.OrganizationId == tenantOrgId || (c.ApplicationId != null && tenantAppIds.Contains(c.ApplicationId.Value)));
            }
        }
        else if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            query = query.Where(c => c.ApplicationId == applicationId.Value || c.ApplicationId == null);
        }

        var channels = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        var dtos = channels.Select(c => c.ToDto()).ToList();
        return Ok(ApiResponse<List<NotificationChannelDto>>.SuccessResponse(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationChannelDto>>> GetChannel(Guid id)
    {
        var channel = await _context.NotificationChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (channel == null)
        {
            return NotFound(ApiResponse<NotificationChannelDto>.FailureResponse("Notification channel not found."));
        }

        if (!await IsChannelAuthorizedAsync(channel))
        {
            return NotFound(ApiResponse<NotificationChannelDto>.FailureResponse("Notification channel not found."));
        }

        return Ok(ApiResponse<NotificationChannelDto>.SuccessResponse(channel.ToDto()));
    }

    [HttpPost]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<NotificationChannelDto>>> CreateChannel(
        [FromBody] CreateNotificationChannelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Channel name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Channel destination is required."));
        }

        if (!Enum.TryParse<NotificationChannelType>(request.Type, true, out var channelType))
        {
            channelType = NotificationChannelType.Email;
        }

        // SSRF protection for Webhook, Slack, and Teams destinations
        if (channelType == NotificationChannelType.Webhook || 
            channelType == NotificationChannelType.Slack || 
            channelType == NotificationChannelType.Teams)
        {
            var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(request.Destination.Trim());
            if (!isAllowed)
            {
                return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse($"Invalid webhook destination URL: {reason}"));
            }
        }

        if (request.ApplicationId.HasValue && _tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == request.ApplicationId.Value && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Application not found or access denied."));
            }
        }

        var channel = new NotificationChannel
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Type = channelType,
            Destination = request.Destination.Trim(),
            WebhookSecret = request.WebhookSecret?.Trim(),
            CustomHeadersJson = request.CustomHeadersJson?.Trim(),
            ApplicationId = request.ApplicationId,
            OrganizationId = _tenantService.OrganizationId,
            IsEnabled = true,
            CreatedBy = _currentUserService.UserName
        };

        _context.NotificationChannels.Add(channel);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChannel), new { id = channel.Id }, ApiResponse<NotificationChannelDto>.SuccessResponse(channel.ToDto(), "Notification channel created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<NotificationChannelDto>>> UpdateChannel(
        Guid id,
        [FromBody] UpdateNotificationChannelRequest request)
    {
        var channel = await _context.NotificationChannels.FindAsync(id);
        if (channel == null)
        {
            return NotFound(ApiResponse<NotificationChannelDto>.FailureResponse("Notification channel not found."));
        }

        if (!await IsChannelAuthorizedAsync(channel))
        {
            return NotFound(ApiResponse<NotificationChannelDto>.FailureResponse("Notification channel not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Channel name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Channel destination is required."));
        }

        if (channel.Type == NotificationChannelType.Webhook || 
            channel.Type == NotificationChannelType.Slack || 
            channel.Type == NotificationChannelType.Teams)
        {
            var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(request.Destination.Trim());
            if (!isAllowed)
            {
                return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse($"Invalid webhook destination URL: {reason}"));
            }
        }

        channel.Name = request.Name.Trim();
        channel.Description = request.Description?.Trim() ?? string.Empty;
        channel.Destination = request.Destination.Trim();
        channel.WebhookSecret = request.WebhookSecret?.Trim();
        channel.CustomHeadersJson = request.CustomHeadersJson?.Trim();
        channel.IsEnabled = request.IsEnabled;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<NotificationChannelDto>.SuccessResponse(channel.ToDto(), "Notification channel updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteChannel(Guid id)
    {
        var channel = await _context.NotificationChannels.FindAsync(id);
        if (channel == null)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Notification channel not found."));
        }

        if (!await IsChannelAuthorizedAsync(channel))
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Notification channel not found."));
        }

        _context.NotificationChannels.Remove(channel);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Notification channel deleted."));
    }

    [HttpPost("{id:guid}/test")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<NotificationTestResultDto>>> TestChannel(Guid id)
    {
        var channel = await _context.NotificationChannels.FindAsync(id);
        if (channel == null)
        {
            return NotFound(ApiResponse<NotificationTestResultDto>.FailureResponse("Notification channel not found."));
        }

        if (!await IsChannelAuthorizedAsync(channel))
        {
            return NotFound(ApiResponse<NotificationTestResultDto>.FailureResponse("Notification channel not found."));
        }

        var provider = _providers.FirstOrDefault(p => p.SupportedType == channel.Type);
        if (provider == null)
        {
            return BadRequest(ApiResponse<NotificationTestResultDto>.FailureResponse($"No notification provider found for type '{channel.Type}'."));
        }

        var testMessage = new NotificationOutbox
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            Channel = channel,
            EventType = NotificationEventType.AlertTriggered,
            Status = NotificationStatus.Sending,
            Subject = "[TEST] SentinelOps Channel Verification",
            Message = $"This is an automated test message dispatched by SentinelOps to verify channel '{channel.Name}'. Delivery initiated at {DateTime.UtcNow:u}.",
            PayloadJson = $"{{\"event\":\"channel_test\",\"channel\":\"{channel.Name}\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}",
            CreatedAtUtc = DateTime.UtcNow
        };

        var deliveryResult = await provider.SendAsync(testMessage, channel, HttpContext.RequestAborted);

        var response = new NotificationTestResultDto
        {
            Success = deliveryResult.Success,
            Message = deliveryResult.Success ? "Test notification dispatched successfully." : "Test notification dispatch failed.",
            ErrorMessage = deliveryResult.ErrorMessage,
            TestedAtUtc = DateTime.UtcNow
        };

        return Ok(ApiResponse<NotificationTestResultDto>.SuccessResponse(response, response.Message));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<List<NotificationHistoryDto>>>> GetChannelHistory(Guid id)
    {
        var channel = await _context.NotificationChannels.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (channel == null)
        {
            return NotFound(ApiResponse<List<NotificationHistoryDto>>.FailureResponse("Notification channel not found."));
        }

        if (!await IsChannelAuthorizedAsync(channel))
        {
            return NotFound(ApiResponse<List<NotificationHistoryDto>>.FailureResponse("Notification channel not found."));
        }

        var history = await _context.NotificationOutboxes
            .AsNoTracking()
            .Where(o => o.ChannelId == id)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(50)
            .ToListAsync();

        var dtos = history.Select(h => h.ToHistoryDto()).ToList();
        return Ok(ApiResponse<List<NotificationHistoryDto>>.SuccessResponse(dtos));
    }

    private async Task<bool> IsChannelAuthorizedAsync(NotificationChannel channel)
    {
        if (!_tenantService.OrganizationId.HasValue) return true;

        var tenantOrgId = _tenantService.OrganizationId.Value;
        if (channel.OrganizationId.HasValue && channel.OrganizationId.Value == tenantOrgId)
        {
            return true;
        }

        if (channel.ApplicationId.HasValue)
        {
            return await _context.Applications
                .AnyAsync(a => a.Id == channel.ApplicationId.Value && a.OrganizationId == tenantOrgId);
        }

        return false;
    }
}

