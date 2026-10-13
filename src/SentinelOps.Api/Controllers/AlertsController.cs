using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/alerts")]
public class AlertsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AlertsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AlertEventDto>>>> GetAlerts(
        [FromQuery] Guid? applicationId,
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AlertEvents
            .Include(e => e.AlertRule)
            .AsNoTracking()
            .AsQueryable();

        if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            query = query.Where(e => e.AlertRule != null && e.AlertRule.ApplicationId == applicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AlertStatus>(status, true, out var alertStatus))
        {
            query = query.Where(e => e.NewStatus == alertStatus);
        }

        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<Severity>(severity, true, out var alertSeverity))
        {
            query = query.Where(e => e.AlertRule != null && e.AlertRule.Severity == alertSeverity);
        }

        int totalCount = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.TriggeredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = events.Select(e => e.ToDto()).ToList();
        var paged = PagedResponse<AlertEventDto>.Create(dtos, totalCount, page, pageSize);

        return Ok(ApiResponse<PagedResponse<AlertEventDto>>.SuccessResponse(paged));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<AlertSummaryDto>>> GetAlertSummary(
        [FromQuery] Guid? applicationId)
    {
        var rulesQuery = _context.AlertRules.AsNoTracking().AsQueryable();
        if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            rulesQuery = rulesQuery.Where(r => r.ApplicationId == applicationId.Value);
        }

        int totalRules = await rulesQuery.CountAsync();
        int enabledRules = await rulesQuery.CountAsync(r => r.IsEnabled);
        int firing = await rulesQuery.CountAsync(r => r.Status == AlertStatus.Firing);
        int acknowledged = await rulesQuery.CountAsync(r => r.Status == AlertStatus.Acknowledged);
        int pending = await rulesQuery.CountAsync(r => r.Status == AlertStatus.Pending);
        int critical = await rulesQuery.CountAsync(r => r.Status == AlertStatus.Firing && r.Severity == Severity.Critical);
        int warning = await rulesQuery.CountAsync(r => r.Status == AlertStatus.Firing && r.Severity == Severity.Warning);

        var summary = new AlertSummaryDto
        {
            TotalRules = totalRules,
            EnabledRules = enabledRules,
            FiringAlerts = firing,
            AcknowledgedAlerts = acknowledged,
            PendingAlerts = pending,
            CriticalAlerts = critical,
            WarningAlerts = warning
        };

        return Ok(ApiResponse<AlertSummaryDto>.SuccessResponse(summary));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> GetAlert(Guid id)
    {
        var rule = await _context.AlertRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert not found."));
        }

        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto()));
    }

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> AcknowledgeAlert(
        Guid id,
        [FromBody] AcknowledgeAlertRequest? request)
    {
        var rule = await _context.AlertRules.FindAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert not found."));
        }

        rule.Acknowledge(_currentUserService.UserName ?? "operator", DateTime.UtcNow);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto(), "Alert acknowledged successfully."));
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> ResolveAlert(
        Guid id,
        [FromBody] ResolveAlertRequest request)
    {
        var rule = await _context.AlertRules.FindAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert not found."));
        }

        rule.Resolve(_currentUserService.UserName ?? "operator", DateTime.UtcNow, request?.Reason ?? "Manually resolved");
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto(), "Alert resolved successfully."));
    }
}
