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

namespace SentinelOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notification-channels")]
public class NotificationChannelsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _tenantService;

    public NotificationChannelsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService tenantService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
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
                query = query.Where(c => c.ApplicationId == applicationId.Value);
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                query = query.Where(c => c.ApplicationId == null || tenantAppIds.Contains(c.ApplicationId.Value));
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

        if (channel.ApplicationId.HasValue && _tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == channel.ApplicationId.Value && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<NotificationChannelDto>.FailureResponse("Notification channel not found."));
            }
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

        // SSRF protection for Webhook destinations
        if (channelType == NotificationChannelType.Webhook)
        {
            var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(request.Destination.Trim());
            if (!isAllowed)
            {
                return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse($"Invalid webhook URL: {reason}"));
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

        if (channel.ApplicationId.HasValue && _tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == channel.ApplicationId.Value && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<NotificationChannelDto>.FailureResponse("Notification channel not found."));
            }
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Channel name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse("Channel destination is required."));
        }

        if (channel.Type == NotificationChannelType.Webhook)
        {
            var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(request.Destination.Trim());
            if (!isAllowed)
            {
                return BadRequest(ApiResponse<NotificationChannelDto>.FailureResponse($"Invalid webhook URL: {reason}"));
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

        if (channel.ApplicationId.HasValue && _tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == channel.ApplicationId.Value && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<bool>.FailureResponse("Notification channel not found."));
            }
        }

        _context.NotificationChannels.Remove(channel);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Notification channel deleted."));
    }
}
