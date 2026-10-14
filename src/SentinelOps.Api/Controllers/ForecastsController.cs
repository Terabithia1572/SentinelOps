using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ForecastsController : ControllerBase
{
    private readonly IPredictiveAlertingService _forecastService;

    public ForecastsController(IPredictiveAlertingService forecastService)
    {
        _forecastService = forecastService;
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<CapacityForecastResult>>> Evaluate(
        [FromBody] CapacityForecastRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _forecastService.ForecastMetricAsync(request, cancellationToken);
        return Ok(ApiResponse<CapacityForecastResult>.SuccessResponse(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PredictiveForecastDto>>>> GetActive(
        [FromQuery] Guid applicationId,
        CancellationToken cancellationToken)
    {
        var forecasts = await _forecastService.GetActiveForecastsAsync(applicationId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PredictiveForecastDto>>.SuccessResponse(forecasts));
    }
}
