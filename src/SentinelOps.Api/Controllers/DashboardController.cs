using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Dashboard.Queries.GetDashboardSummary;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

public class DashboardController : BaseApiController
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result));
    }
}
