using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/incidents/{incidentId:guid}/intelligence")]
[Authorize]
public class IncidentIntelligenceController : ControllerBase
{
    private readonly IIncidentIntelligenceProvider _intelligenceProvider;

    public IncidentIntelligenceController(IIncidentIntelligenceProvider intelligenceProvider)
    {
        _intelligenceProvider = intelligenceProvider;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<ApiResponse<IncidentAnalysisResult>>> AnalyzeIncident(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _intelligenceProvider.AnalyzeIncidentAsync(incidentId, cancellationToken);
        return Ok(ApiResponse<IncidentAnalysisResult>.SuccessResponse(result));
    }

    [HttpGet("analysis")]
    public async Task<ActionResult<ApiResponse<IncidentAnalysisResult>>> GetAnalysis(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _intelligenceProvider.AnalyzeIncidentAsync(incidentId, cancellationToken);
        return Ok(ApiResponse<IncidentAnalysisResult>.SuccessResponse(result));
    }

    [HttpPost("postmortem")]
    public async Task<ActionResult<ApiResponse<IncidentPostmortemResult>>> GeneratePostmortem(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _intelligenceProvider.GeneratePostmortemAsync(incidentId, cancellationToken);
        return Ok(ApiResponse<IncidentPostmortemResult>.SuccessResponse(result));
    }

    [HttpPost("remediate")]
    public async Task<ActionResult<ApiResponse<RemediationSuggestionsResult>>> SuggestRemediation(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _intelligenceProvider.SuggestRemediationAsync(incidentId, cancellationToken);
        return Ok(ApiResponse<RemediationSuggestionsResult>.SuccessResponse(result));
    }

    [HttpPost("feedback")]
    public async Task<ActionResult<ApiResponse<bool>>> RecordFeedback(
        Guid incidentId,
        [FromBody] IntelligenceFeedbackRequest feedback,
        CancellationToken cancellationToken)
    {
        var success = await _intelligenceProvider.RecordFeedbackAsync(incidentId, feedback, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(success, "Feedback received successfully."));
    }
}
