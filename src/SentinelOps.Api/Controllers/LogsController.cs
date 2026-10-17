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
    private readonly ISavedLogQueryService _savedQueryService;

    public LogsController(
        ILogPatternAnalysisService patternService,
        ISavedLogQueryService savedQueryService)
    {
        _patternService = patternService;
        _savedQueryService = savedQueryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<LogEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs([FromQuery] GetLogsRequest request, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await Mediator.Send(new GetLogsQuery(request), cancellationToken);
        sw.Stop();

        Response.Headers.Append("X-Query-Duration-Ms", sw.ElapsedMilliseconds.ToString());
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

    [HttpGet("saved")]
    [ProducesResponseType(typeof(ApiResponse<List<SavedLogQueryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedQueries([FromQuery] Guid? applicationId, CancellationToken cancellationToken)
    {
        var queries = await _savedQueryService.GetSavedQueriesAsync(applicationId, cancellationToken);
        return Ok(ApiResponse<List<SavedLogQueryDto>>.SuccessResponse(queries));
    }

    [HttpGet("saved/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SavedLogQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSavedQueryById(Guid id, CancellationToken cancellationToken)
    {
        var query = await _savedQueryService.GetSavedQueryByIdAsync(id, cancellationToken);
        if (query == null)
        {
            return NotFound(ApiResponse<SavedLogQueryDto>.FailureResponse("Saved log query not found or access denied."));
        }
        return Ok(ApiResponse<SavedLogQueryDto>.SuccessResponse(query));
    }

    [HttpPost("saved")]
    [ProducesResponseType(typeof(ApiResponse<SavedLogQueryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSavedQuery([FromBody] CreateSavedLogQueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<SavedLogQueryDto>.FailureResponse("Query name is required."));
        }

        try
        {
            var created = await _savedQueryService.CreateSavedQueryAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSavedQueryById), new { id = created.Id }, ApiResponse<SavedLogQueryDto>.SuccessResponse(created, "Saved log query created."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SavedLogQueryDto>.FailureResponse(ex.Message));
        }
    }

    [HttpPut("saved/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SavedLogQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSavedQuery(Guid id, [FromBody] UpdateSavedLogQueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<SavedLogQueryDto>.FailureResponse("Query name is required."));
        }

        var updated = await _savedQueryService.UpdateSavedQueryAsync(id, request, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<SavedLogQueryDto>.FailureResponse("Saved log query not found or access denied."));
        }
        return Ok(ApiResponse<SavedLogQueryDto>.SuccessResponse(updated, "Saved log query updated."));
    }

    [HttpDelete("saved/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSavedQuery(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _savedQueryService.DeleteSavedQueryAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Saved log query not found or access denied."));
        }
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Saved log query deleted."));
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportLogs([FromQuery] ExportLogsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (content, contentType, fileName) = await _savedQueryService.ExportLogsAsync(request, cancellationToken);
            return File(content, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.FailureResponse(ex.Message));
        }
    }
}
