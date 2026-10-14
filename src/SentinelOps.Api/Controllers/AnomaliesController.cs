using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnomaliesController : ControllerBase
{
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly ICurrentUserService _currentUserService;

    public AnomaliesController(
        IAnomalyDetectionService anomalyService,
        ICurrentUserService currentUserService)
    {
        _anomalyService = anomalyService;
        _currentUserService = currentUserService;
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<AnomalyDetectionResult>>> Evaluate(
        [FromBody] AnomalyEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _anomalyService.EvaluateSignalAsync(request, cancellationToken);
        return Ok(ApiResponse<AnomalyDetectionResult>.SuccessResponse(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TelemetryAnomalyDto>>>> GetActive(
        [FromQuery] Guid applicationId,
        CancellationToken cancellationToken)
    {
        var anomalies = await _anomalyService.GetActiveAnomaliesAsync(applicationId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TelemetryAnomalyDto>>.SuccessResponse(anomalies));
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<bool>>> Resolve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resolved = await _anomalyService.ResolveAnomalyAsync(id, _currentUserService.UserName, cancellationToken);
        if (!resolved)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Anomaly not found or already resolved."));
        }
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Anomaly resolved successfully."));
    }
}
