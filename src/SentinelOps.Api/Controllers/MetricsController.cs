using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Metrics.Queries.GetMetricsTimeSeries;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MetricsController : BaseApiController
{
    [HttpGet("timeseries")]
    [ProducesResponseType(typeof(ApiResponse<MetricSeriesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSeries([FromQuery] GetMetricsRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMetricsTimeSeriesQuery(request), cancellationToken);
        return Ok(ApiResponse<MetricSeriesResponse>.Ok(result));
    }
}
