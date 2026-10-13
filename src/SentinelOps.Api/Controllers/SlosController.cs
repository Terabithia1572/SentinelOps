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
[Route("api/slos")]
public class SlosController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ISloEvaluationService _sloEvaluationService;
    private readonly ICurrentUserService _currentUserService;

    public SlosController(
        IApplicationDbContext context,
        ISloEvaluationService sloEvaluationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _sloEvaluationService = sloEvaluationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SloDto>>>> GetSlos(
        [FromQuery] Guid? applicationId,
        [FromQuery] string? serviceName)
    {
        var query = _context.ServiceLevelObjectives.AsNoTracking().AsQueryable();

        if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            query = query.Where(s => s.ApplicationId == applicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            query = query.Where(s => s.ServiceName == serviceName);
        }

        var slos = await query
            .OrderBy(s => s.ServiceName)
            .ThenBy(s => s.Name)
            .ToListAsync();

        var dtos = slos.Select(s => s.ToDto()).ToList();
        return Ok(ApiResponse<List<SloDto>>.SuccessResponse(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SloDto>>> GetSlo(Guid id)
    {
        var slo = await _context.ServiceLevelObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (slo == null)
        {
            return NotFound(ApiResponse<SloDto>.FailureResponse("SLO not found."));
        }

        return Ok(ApiResponse<SloDto>.SuccessResponse(slo.ToDto()));
    }

    [HttpPost]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<SloDto>>> CreateSlo([FromBody] CreateSloRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<SloDto>.FailureResponse("SLO name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return BadRequest(ApiResponse<SloDto>.FailureResponse("Service name is required."));
        }

        var app = await _context.Applications.FindAsync(request.ApplicationId);
        if (app == null)
        {
            return BadRequest(ApiResponse<SloDto>.FailureResponse("Application not found."));
        }

        if (!Enum.TryParse<SloIndicatorType>(request.IndicatorType, true, out var indicatorType))
        {
            indicatorType = SloIndicatorType.Availability;
        }

        if (!Enum.TryParse<SloEvaluationWindow>(request.EvaluationWindow, true, out var window))
        {
            window = SloEvaluationWindow.Rolling7Days;
        }

        var slo = new ServiceLevelObjective
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ApplicationId = request.ApplicationId,
            ServiceName = request.ServiceName.Trim(),
            EnvironmentName = request.EnvironmentName?.Trim(),
            IndicatorType = indicatorType,
            TargetPercentage = Math.Clamp(request.TargetPercentage, 50.0, 99.999),
            EvaluationWindow = window,
            LatencyThresholdMs = request.LatencyThresholdMs.HasValue ? Math.Max(1.0, request.LatencyThresholdMs.Value) : 500.0,
            IsEnabled = true,
            CreatedBy = _currentUserService.UserName
        };

        _context.ServiceLevelObjectives.Add(slo);
        await _context.SaveChangesAsync();

        // Perform initial evaluation immediately
        await _sloEvaluationService.EvaluateSloAsync(slo.Id);

        var updated = await _context.ServiceLevelObjectives.FindAsync(slo.Id);
        return CreatedAtAction(nameof(GetSlo), new { id = slo.Id }, ApiResponse<SloDto>.SuccessResponse(updated!.ToDto(), "SLO created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<SloDto>>> UpdateSlo(Guid id, [FromBody] UpdateSloRequest request)
    {
        var slo = await _context.ServiceLevelObjectives.FindAsync(id);
        if (slo == null)
        {
            return NotFound(ApiResponse<SloDto>.FailureResponse("SLO not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<SloDto>.FailureResponse("SLO name is required."));
        }

        if (Enum.TryParse<SloEvaluationWindow>(request.EvaluationWindow, true, out var window))
        {
            slo.EvaluationWindow = window;
        }

        slo.Name = request.Name.Trim();
        slo.Description = request.Description?.Trim() ?? string.Empty;
        slo.TargetPercentage = Math.Clamp(request.TargetPercentage, 50.0, 99.999);
        slo.LatencyThresholdMs = request.LatencyThresholdMs.HasValue ? Math.Max(1.0, request.LatencyThresholdMs.Value) : slo.LatencyThresholdMs;
        slo.IsEnabled = request.IsEnabled;

        await _context.SaveChangesAsync();
        await _sloEvaluationService.EvaluateSloAsync(slo.Id);

        var updated = await _context.ServiceLevelObjectives.FindAsync(id);
        return Ok(ApiResponse<SloDto>.SuccessResponse(updated!.ToDto(), "SLO updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSlo(Guid id)
    {
        var slo = await _context.ServiceLevelObjectives.FindAsync(id);
        if (slo == null)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("SLO not found."));
        }

        _context.ServiceLevelObjectives.Remove(slo);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<bool>.SuccessResponse(true, "SLO deleted successfully."));
    }

    [HttpGet("{id:guid}/compliance")]
    public async Task<ActionResult<ApiResponse<SloComplianceHistoryDto>>> GetSloCompliance(Guid id)
    {
        var slo = await _context.ServiceLevelObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (slo == null)
        {
            return NotFound(ApiResponse<SloComplianceHistoryDto>.FailureResponse("SLO not found."));
        }

        var history = await _context.SloEvaluations
            .Where(e => e.ServiceLevelObjectiveId == id)
            .OrderByDescending(e => e.EvaluatedAtUtc)
            .Take(50)
            .ToListAsync();

        var historyDtos = history.Select(e => new SloEvaluationPointDto
        {
            Timestamp = e.EvaluatedAtUtc,
            CompliancePercentage = e.CompliancePercentage,
            ErrorBudgetRemaining = e.ErrorBudgetRemaining,
            BurnRate = e.BurnRate,
            TotalRequests = e.TotalRequests,
            GoodRequests = e.GoodRequests,
            IsBreached = e.IsBreached
        }).OrderBy(e => e.Timestamp).ToList();

        var result = new SloComplianceHistoryDto
        {
            ServiceLevelObjectiveId = slo.Id,
            Name = slo.Name,
            TargetPercentage = slo.TargetPercentage,
            CurrentCompliance = slo.CurrentCompliancePercentage ?? 100.0,
            ErrorBudgetRemaining = slo.CurrentErrorBudgetRemaining ?? 100.0,
            BurnRate = slo.BurnRate ?? 0.0,
            IsBreached = slo.IsBreached,
            History = historyDtos
        };

        return Ok(ApiResponse<SloComplianceHistoryDto>.SuccessResponse(result));
    }

    [HttpPost("{id:guid}/evaluate")]
    public async Task<ActionResult<ApiResponse<SloEvaluationResult>>> EvaluateSlo(Guid id)
    {
        var result = await _sloEvaluationService.EvaluateSloAsync(id);
        if (!result.Evaluated)
        {
            return NotFound(ApiResponse<SloEvaluationResult>.FailureResponse(result.Message));
        }

        return Ok(ApiResponse<SloEvaluationResult>.SuccessResponse(result, "SLO evaluated."));
    }
}
