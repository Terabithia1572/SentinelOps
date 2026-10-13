using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InAppNotificationDto>>>> GetNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 100);

        var query = _context.InAppNotifications.AsNoTracking().AsQueryable();

        var userId = _currentUserService.UserId;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(n => n.UserId == userId || n.UserId == null);
        }

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(limit)
            .ToListAsync();

        var dtos = notifications.Select(n => n.ToDto()).ToList();
        return Ok(ApiResponse<List<InAppNotificationDto>>.SuccessResponse(dtos));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
    {
        var notification = await _context.InAppNotifications.FindAsync(id);
        if (notification == null)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Notification not found."));
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Notification marked as read."));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead()
    {
        var userId = _currentUserService.UserId;
        var unread = await _context.InAppNotifications
            .Where(n => !n.IsRead && (n.UserId == userId || n.UserId == null))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAtUtc = now;
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<int>.SuccessResponse(unread.Count, "All notifications marked as read."));
    }
}
