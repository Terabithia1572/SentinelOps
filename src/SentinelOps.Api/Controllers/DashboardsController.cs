using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DashboardSummaryItemDto>>>> GetDashboards(CancellationToken cancellationToken)
    {
        var dashboards = await _dashboardService.GetDashboardsAsync(cancellationToken);
        return Ok(ApiResponse<List<DashboardSummaryItemDto>>.SuccessResponse(dashboards));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> GetDashboard(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetDashboardByIdAsync(id, cancellationToken);
        if (dashboard == null)
        {
            return NotFound(ApiResponse<DashboardDetailDto>.FailureResponse("Dashboard not found or access denied."));
        }
        return Ok(ApiResponse<DashboardDetailDto>.SuccessResponse(dashboard));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> CreateDashboard(
        [FromBody] CreateDashboardRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<DashboardDetailDto>.FailureResponse("Title is required."));
        }

        var created = await _dashboardService.CreateDashboardAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDashboard), new { id = created.Id }, ApiResponse<DashboardDetailDto>.SuccessResponse(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> UpdateDashboard(
        Guid id,
        [FromBody] UpdateDashboardRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<DashboardDetailDto>.FailureResponse("Title is required."));
        }

        var updated = await _dashboardService.UpdateDashboardAsync(id, request, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<DashboardDetailDto>.FailureResponse("Dashboard not found or access denied."));
        }
        return Ok(ApiResponse<DashboardDetailDto>.SuccessResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDashboard(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _dashboardService.DeleteDashboardAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Dashboard not found or access denied."));
        }
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Dashboard deleted successfully."));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> DuplicateDashboard(Guid id, CancellationToken cancellationToken)
    {
        var duplicated = await _dashboardService.DuplicateDashboardAsync(id, cancellationToken);
        if (duplicated == null)
        {
            return NotFound(ApiResponse<DashboardDetailDto>.FailureResponse("Dashboard not found or access denied."));
        }
        return Ok(ApiResponse<DashboardDetailDto>.SuccessResponse(duplicated, "Dashboard duplicated successfully."));
    }

    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> ImportDashboard(
        [FromBody] ImportDashboardRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<DashboardDetailDto>.FailureResponse("Title is required."));
        }

        var imported = await _dashboardService.ImportDashboardAsync(request, cancellationToken);
        return Ok(ApiResponse<DashboardDetailDto>.SuccessResponse(imported, "Dashboard imported successfully."));
    }

    [HttpGet("{id:guid}/export")]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> ExportDashboard(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetDashboardByIdAsync(id, cancellationToken);
        if (dashboard == null)
        {
            return NotFound(ApiResponse<DashboardDetailDto>.FailureResponse("Dashboard not found or access denied."));
        }
        return Ok(ApiResponse<DashboardDetailDto>.SuccessResponse(dashboard));
    }

    [HttpPost("templates/{templateKey}/instantiate")]
    public async Task<ActionResult<ApiResponse<DashboardDetailDto>>> InstantiateTemplate(
        string templateKey,
        CancellationToken cancellationToken)
    {
        var instantiated = await _dashboardService.InstantiateTemplateAsync(templateKey, cancellationToken);
        if (instantiated == null)
        {
            return BadRequest(ApiResponse<DashboardDetailDto>.FailureResponse($"Unknown template '{templateKey}'."));
        }
        return Ok(ApiResponse<DashboardDetailDto>.SuccessResponse(instantiated, "Dashboard created from template."));
    }
}
