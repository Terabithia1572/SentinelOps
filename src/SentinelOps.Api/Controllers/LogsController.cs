using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Logs.Queries.GetLogById;
using SentinelOps.Application.Logs.Queries.GetLogMetadata;
using SentinelOps.Application.Logs.Queries.GetLogs;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LogsController : BaseApiController
{
    private readonly ILogPatternAnalysisService _patternService;

    public LogsController(ILogPatternAnalysisService patternService)
    {
        _patternService = patternService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<LogEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs([FromQuery] GetLogsRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLogsQuery(request), cancellationToken);
        return Ok(ApiResponse<PagedResponse<LogEntryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLogByIdQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<LogEntryDto>.Fail($"Log entry with ID '{id}' was not found."));
        }

        return Ok(ApiResponse<LogEntryDto>.Ok(result));
    }

    [HttpGet("metadata")]
    [ProducesResponseType(typeof(ApiResponse<LogMetadataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata([FromQuery] Guid? applicationId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLogMetadataQuery(applicationId), cancellationToken);
        return Ok(ApiResponse<LogMetadataDto>.Ok(result));
    }

    [HttpGet("patterns")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LogPatternDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatterns([FromQuery] Guid applicationId, CancellationToken cancellationToken)
    {
        var patterns = await _patternService.GetPatternsAsync(applicationId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LogPatternDto>>.Ok(patterns));
    }

    [HttpPost("patterns/analyze")]
    [ProducesResponseType(typeof(ApiResponse<LogPatternAnalysisResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzePatterns(
        [FromQuery] Guid applicationId,
        [FromQuery] int windowHours = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await _patternService.AnalyzeApplicationLogsAsync(applicationId, TimeSpan.FromHours(Math.Clamp(windowHours, 1, 168)), cancellationToken);
        return Ok(ApiResponse<LogPatternAnalysisResult>.Ok(result));
    }
}
