using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class RetentionController : BaseApiController
{
    private readonly IRetentionService _retentionService;
    private readonly ICurrentUserService _currentUserService;

    public RetentionController(
        IRetentionService retentionService,
        ICurrentUserService currentUserService)
    {
        _retentionService = retentionService;
        _currentUserService = currentUserService;
    }

    [HttpGet("policy")]
    [ProducesResponseType(typeof(ApiResponse<RetentionPolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRetentionPolicy(CancellationToken cancellationToken)
    {
        var policy = await _retentionService.GetRetentionPolicyAsync(cancellationToken);
        return Ok(ApiResponse<RetentionPolicyDto>.SuccessResponse(policy));
    }

    [HttpPut("policy")]
    [ProducesResponseType(typeof(ApiResponse<RetentionPolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRetentionPolicy(
        [FromBody] UpdateRetentionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var updatedBy = _currentUserService.UserName ?? "admin";
        var result = await _retentionService.UpdateRetentionPolicyAsync(request, updatedBy, cancellationToken);
        return Ok(ApiResponse<RetentionPolicyDto>.SuccessResponse(result, "Retention policy updated successfully."));
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(ApiResponse<RetentionDryRunResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewRetention(CancellationToken cancellationToken)
    {
        var preview = await _retentionService.PreviewRetentionCleanupAsync(cancellationToken);
        return Ok(ApiResponse<RetentionDryRunResultDto>.SuccessResponse(preview));
    }

    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<RetentionExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteRetention(
        [FromBody] ExecuteRetentionCleanupRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirm)
        {
            return BadRequest(ApiResponse<RetentionExecutionResultDto>.FailureResponse("Explicit confirmation is required to execute data retention cleanup."));
        }

        var executedBy = _currentUserService.UserName ?? "admin";
        var result = await _retentionService.ExecuteRetentionCleanupAsync(request, executedBy, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<RetentionExecutionResultDto>.FailureResponse(result.Message));
        }

        return Ok(ApiResponse<RetentionExecutionResultDto>.SuccessResponse(result, result.Message));
    }
}
