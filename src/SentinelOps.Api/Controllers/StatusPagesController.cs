using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/status-pages")]
[Authorize]
public class StatusPagesController : ControllerBase
{
    private readonly IStatusPageService _statusPageService;
    private readonly ICurrentUserService _currentUserService;

    public StatusPagesController(
        IStatusPageService statusPageService,
        ICurrentUserService currentUserService)
    {
        _statusPageService = statusPageService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StatusPageDto>>>> GetStatusPages(CancellationToken ct)
    {
        var pages = await _statusPageService.GetStatusPagesAsync(ct);
        return Ok(ApiResponse<List<StatusPageDto>>.SuccessResponse(pages));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StatusPageDto>>> GetStatusPage(Guid id, CancellationToken ct)
    {
        var page = await _statusPageService.GetStatusPageByIdAsync(id, ct);
        if (page == null)
        {
            return NotFound(ApiResponse<StatusPageDto>.FailureResponse("Status page not found."));
        }
        return Ok(ApiResponse<StatusPageDto>.SuccessResponse(page));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StatusPageDto>>> CreateStatusPage(
        [FromBody] CreateStatusPageRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
        {
            return BadRequest(ApiResponse<StatusPageDto>.FailureResponse("Name and slug are required."));
        }

        var page = await _statusPageService.CreateStatusPageAsync(request, ct);
        return CreatedAtAction(nameof(GetStatusPage), new { id = page.Id }, ApiResponse<StatusPageDto>.SuccessResponse(page));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StatusPageDto>>> UpdateStatusPage(
        Guid id,
        [FromBody] UpdateStatusPageRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<StatusPageDto>.FailureResponse("Name is required."));
        }

        var updated = await _statusPageService.UpdateStatusPageAsync(id, request, ct);
        if (updated == null)
        {
            return NotFound(ApiResponse<StatusPageDto>.FailureResponse("Status page not found."));
        }

        return Ok(ApiResponse<StatusPageDto>.SuccessResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteStatusPage(Guid id, CancellationToken ct)
    {
        var deleted = await _statusPageService.DeleteStatusPageAsync(id, ct);
        if (!deleted)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Status page not found."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    [HttpPost("{id:guid}/incidents")]
    public async Task<ActionResult<ApiResponse<StatusPageIncidentDto>>> PostIncident(
        Guid id,
        [FromBody] CreateStatusPageIncidentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            return BadRequest(ApiResponse<StatusPageIncidentDto>.FailureResponse("Title and initial message are required."));
        }

        var postedBy = _currentUserService.UserName ?? "Operator";
        var incident = await _statusPageService.PostIncidentAsync(id, request, postedBy, ct);
        if (incident == null)
        {
            return NotFound(ApiResponse<StatusPageIncidentDto>.FailureResponse("Status page not found."));
        }

        return Ok(ApiResponse<StatusPageIncidentDto>.SuccessResponse(incident));
    }

    [HttpPost("{id:guid}/incidents/{incidentId:guid}/updates")]
    public async Task<ActionResult<ApiResponse<StatusPageIncidentDto>>> AddIncidentUpdate(
        Guid id,
        Guid incidentId,
        [FromBody] AddStatusPageIncidentUpdateRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiResponse<StatusPageIncidentDto>.FailureResponse("Update message is required."));
        }

        var postedBy = _currentUserService.UserName ?? "Operator";
        var updated = await _statusPageService.AddIncidentUpdateAsync(id, incidentId, request, postedBy, ct);
        if (updated == null)
        {
            return NotFound(ApiResponse<StatusPageIncidentDto>.FailureResponse("Status page incident not found."));
        }

        return Ok(ApiResponse<StatusPageIncidentDto>.SuccessResponse(updated));
    }
}
