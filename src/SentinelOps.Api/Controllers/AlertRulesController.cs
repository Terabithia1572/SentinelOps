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

namespace SentinelOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/alert-rules")]
public class AlertRulesController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly IAlertEvaluationService _evaluationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _tenantService;

    public AlertRulesController(
        IApplicationDbContext context,
        IAlertEvaluationService evaluationService,
        ICurrentUserService currentUserService,
        ICurrentTenantService tenantService)
    {
        _context = context;
        _evaluationService = evaluationService;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AlertRuleDto>>>> GetAlertRules(
        [FromQuery] Guid? applicationId,
        [FromQuery] string? status,
        [FromQuery] bool? isEnabled,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AlertRules.AsNoTracking().AsQueryable();

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (applicationId.HasValue && applicationId.Value != Guid.Empty)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == applicationId.Value && a.OrganizationId == tenantOrgId);
                if (!isAppInTenant)
                {
                    return Ok(ApiResponse<PagedResponse<AlertRuleDto>>.SuccessResponse(
                        PagedResponse<AlertRuleDto>.Create([], 0, page, pageSize)));
                }
                query = query.Where(r => r.ApplicationId == applicationId.Value);
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                query = query.Where(r => tenantAppIds.Contains(r.ApplicationId));
            }
        }
        else if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            query = query.Where(r => r.ApplicationId == applicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AlertStatus>(status, true, out var alertStatus))
        {
            query = query.Where(r => r.Status == alertStatus);
        }

        if (isEnabled.HasValue)
        {
            query = query.Where(r => r.IsEnabled == isEnabled.Value);
        }

        int totalCount = await query.CountAsync();

        var rules = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = rules.Select(r => r.ToDto()).ToList();
        var paged = PagedResponse<AlertRuleDto>.Create(dtos, totalCount, page, pageSize);

        return Ok(ApiResponse<PagedResponse<AlertRuleDto>>.SuccessResponse(paged));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> GetAlertRule(Guid id)
    {
        var rule = await _context.AlertRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == rule.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
            }
        }

        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto()));
    }

    [HttpPost]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> CreateAlertRule([FromBody] CreateAlertRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<AlertRuleDto>.FailureResponse("Rule name is required."));
        }

        var app = await _context.Applications.FindAsync(request.ApplicationId);
        if (app == null || (_tenantService.OrganizationId.HasValue && app.OrganizationId != _tenantService.OrganizationId.Value))
        {
            return BadRequest(ApiResponse<AlertRuleDto>.FailureResponse("Application not found."));
        }

        if (!Enum.TryParse<AlertConditionType>(request.ConditionType, true, out var conditionType))
        {
            conditionType = AlertConditionType.ErrorRateAboveThreshold;
        }

        if (!Enum.TryParse<ComparisonOperator>(request.Operator, true, out var op))
        {
            op = ComparisonOperator.GreaterThan;
        }

        if (!Enum.TryParse<Severity>(request.Severity, true, out var severity))
        {
            severity = Severity.Warning;
        }

        var rule = new AlertRule
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ApplicationId = request.ApplicationId,
            EnvironmentName = request.EnvironmentName?.Trim(),
            ServiceName = request.ServiceName?.Trim(),
            ConditionType = conditionType,
            Operator = op,
            MetricName = string.IsNullOrWhiteSpace(request.MetricName) ? "telemetry_metric" : request.MetricName.Trim(),
            Threshold = request.Threshold,
            EvaluationWindowMinutes = Math.Max(1, request.EvaluationWindowMinutes),
            EvaluationFrequencyMinutes = Math.Max(1, request.EvaluationFrequencyMinutes),
            ConsecutiveCountRequired = Math.Max(1, request.ConsecutiveCountRequired),
            CooldownMinutes = Math.Max(1, request.CooldownMinutes),
            Severity = severity,
            IsEnabled = true,
            Status = AlertStatus.Normal,
            CreatedBy = _currentUserService.UserName
        };

        if (request.NotificationChannelIds != null && request.NotificationChannelIds.Count > 0)
        {
            foreach (var channelId in request.NotificationChannelIds)
            {
                rule.NotificationChannels.Add(new AlertRuleNotificationChannel
                {
                    AlertRuleId = rule.Id,
                    NotificationChannelId = channelId
                });
            }
        }

        _context.AlertRules.Add(rule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAlertRule), new { id = rule.Id }, ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto(), "Alert rule created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> UpdateAlertRule(Guid id, [FromBody] UpdateAlertRuleRequest request)
    {
        var rule = await _context.AlertRules
            .Include(r => r.NotificationChannels)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == rule.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
            }
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<AlertRuleDto>.FailureResponse("Rule name is required."));
        }

        if (Enum.TryParse<AlertConditionType>(request.ConditionType, true, out var conditionType))
        {
            rule.ConditionType = conditionType;
        }

        if (Enum.TryParse<ComparisonOperator>(request.Operator, true, out var op))
        {
            rule.Operator = op;
        }

        if (Enum.TryParse<Severity>(request.Severity, true, out var severity))
        {
            rule.Severity = severity;
        }

        rule.Name = request.Name.Trim();
        rule.Description = request.Description?.Trim() ?? string.Empty;
        rule.EnvironmentName = request.EnvironmentName?.Trim();
        rule.ServiceName = request.ServiceName?.Trim();
        rule.MetricName = string.IsNullOrWhiteSpace(request.MetricName) ? rule.MetricName : request.MetricName.Trim();
        rule.Threshold = request.Threshold;
        rule.EvaluationWindowMinutes = Math.Max(1, request.EvaluationWindowMinutes);
        rule.EvaluationFrequencyMinutes = Math.Max(1, request.EvaluationFrequencyMinutes);
        rule.ConsecutiveCountRequired = Math.Max(1, request.ConsecutiveCountRequired);
        rule.CooldownMinutes = Math.Max(1, request.CooldownMinutes);
        rule.UpdatedBy = _currentUserService.UserName;

        if (request.NotificationChannelIds != null)
        {
            rule.NotificationChannels.Clear();
            foreach (var channelId in request.NotificationChannelIds)
            {
                rule.NotificationChannels.Add(new AlertRuleNotificationChannel
                {
                    AlertRuleId = rule.Id,
                    NotificationChannelId = channelId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto(), "Alert rule updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAlertRule(Guid id)
    {
        var rule = await _context.AlertRules.FindAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Alert rule not found."));
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == rule.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<bool>.FailureResponse("Alert rule not found."));
            }
        }

        _context.AlertRules.Remove(rule);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Alert rule deleted successfully."));
    }

    [HttpPost("{id:guid}/enable")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> EnableAlertRule(Guid id)
    {
        var rule = await _context.AlertRules.FindAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == rule.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
            }
        }

        rule.Enable();
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto(), "Alert rule enabled."));
    }

    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> DisableAlertRule(Guid id)
    {
        var rule = await _context.AlertRules.FindAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == rule.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value);
            if (!isOwned)
            {
                return NotFound(ApiResponse<AlertRuleDto>.FailureResponse("Alert rule not found."));
            }
        }

        rule.Disable();
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<AlertRuleDto>.SuccessResponse(rule.ToDto(), "Alert rule disabled."));
    }

    [HttpPost("{id:guid}/evaluate")]
    public async Task<ActionResult<ApiResponse<AlertEvaluationResult>>> EvaluateAlertRule(Guid id)
    {
        var result = await _evaluationService.EvaluateRuleAsync(id);
        if (!result.Evaluated)
        {
            return NotFound(ApiResponse<AlertEvaluationResult>.FailureResponse(result.Message));
        }

        return Ok(ApiResponse<AlertEvaluationResult>.SuccessResponse(result, "Alert rule evaluated."));
    }
}
