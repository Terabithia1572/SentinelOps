using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Services.Queries.GetServiceHealth;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ServicesController : BaseApiController
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceHealthSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceHealth([FromQuery] Guid? applicationId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetServiceHealthQuery(applicationId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceHealthSummaryDto>>.Ok(result));
    }
}
