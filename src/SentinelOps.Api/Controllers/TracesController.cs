using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Traces.Queries.GetTraceById;
using SentinelOps.Application.Traces.Queries.GetTraces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TracesController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<TraceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTraces([FromQuery] GetTracesRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTracesQuery(request), cancellationToken);
        return Ok(ApiResponse<PagedResponse<TraceDto>>.Ok(result));
    }

    [HttpGet("{traceId}")]
    [ProducesResponseType(typeof(ApiResponse<TraceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTraceById(string traceId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTraceByIdQuery(traceId), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<TraceDetailDto>.Fail($"Trace '{traceId}' was not found."));
        }

        return Ok(ApiResponse<TraceDetailDto>.Ok(result));
    }
}
