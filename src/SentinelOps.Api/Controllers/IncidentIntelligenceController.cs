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
    private readonly IRootCauseAnalysisService _rcaService;
    private readonly IIncidentAnalysisService _analysisService;

    public IncidentIntelligenceController(
        IIncidentIntelligenceProvider intelligenceProvider,
        IRootCauseAnalysisService rcaService,
        IIncidentAnalysisService analysisService)
    {
        _intelligenceProvider = intelligenceProvider;
        _rcaService = rcaService;
        _analysisService = analysisService;
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

    [HttpPost("rca")]
    public async Task<ActionResult<ApiResponse<RcaAnalysisResult>>> PerformRca(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _rcaService.PerformRcaAsync(incidentId, cancellationToken);
        return Ok(ApiResponse<RcaAnalysisResult>.SuccessResponse(result));
    }

    [HttpPost("rca/{rcaId:guid}/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyRcaHypothesis(
        Guid incidentId,
        Guid rcaId,
        [FromQuery] string hypothesis,
        [FromQuery] bool isConfirmed,
        [FromQuery] string? notes,
        CancellationToken cancellationToken)
    {
        var success = await _rcaService.VerifyHypothesisAsync(rcaId, hypothesis, isConfirmed, notes, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("RCA record not found."));
        }
        return Ok(ApiResponse<bool>.SuccessResponse(true, "RCA hypothesis verification recorded."));
    }

    [HttpPost("copilot/chat")]
    public async Task<ActionResult<ApiResponse<AiCopilotChatResponse>>> ChatWithCopilot(
        Guid incidentId,
        [FromBody] AiCopilotChatRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analysisService.AskCopilotAsync(incidentId, request.Message, cancellationToken);
        return Ok(ApiResponse<AiCopilotChatResponse>.SuccessResponse(result));
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
