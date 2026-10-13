using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Audit.Queries.GetAuditEventDetail;
using SentinelOps.Application.Audit.Queries.GetAuditEvents;
using SentinelOps.Application.Audit.Queries.GetAuditSummary;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class AuditController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditEventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditEvents([FromQuery] GetAuditEventsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAuditEventsQuery(
            request.Search,
            request.Actor,
            request.Action,
            request.TargetType,
            request.Outcome,
            request.ApplicationScope,
            request.FromUtc,
            request.ToUtc,
            request.Page,
            request.PageSize);

        var result = await Mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<AuditEventDto>>.SuccessResponse(result));
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<AuditSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditSummary(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAuditSummaryQuery(), cancellationToken);
        return Ok(ApiResponse<AuditSummaryDto>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuditEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditEventById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAuditEventDetailQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<AuditEventDto>.FailureResponse("Audit event not found."));
        }

        return Ok(ApiResponse<AuditEventDto>.SuccessResponse(result));
    }
}
