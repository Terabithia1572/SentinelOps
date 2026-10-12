using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.ApiKeys.Commands.CreateApiKey;
using SentinelOps.Application.ApiKeys.Commands.RevokeApiKey;
using SentinelOps.Application.ApiKeys.Commands.RotateApiKey;
using SentinelOps.Application.ApiKeys.Queries.GetApiKeys;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Authorize]
public class ApiKeysController : BaseApiController
{
    [HttpGet("/api/applications/{applicationId:guid}/keys")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ApiKeyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiKeys(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApiKeysQuery(applicationId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApiKeyDto>>.SuccessResponse(result));
    }

    [HttpPost("/api/applications/{applicationId:guid}/keys")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyCreatedResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateApiKey(
        Guid applicationId,
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateApiKeyCommand(applicationId, request.Name, request.ExpiresInDays);
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetApiKeys),
            new { applicationId },
            ApiResponse<ApiKeyCreatedResponse>.SuccessResponse(result, "API Key generated successfully. Save this secret now; it will not be shown again."));
    }

    [HttpPost("/api/keys/{keyId:guid}/revoke")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(Guid keyId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RevokeApiKeyCommand(keyId), cancellationToken);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Active API Key not found."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "API Key revoked successfully."));
    }

    [HttpPost("/api/keys/{keyId:guid}/rotate")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyCreatedResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateApiKey(
        Guid keyId,
        [FromBody] RotateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RotateApiKeyCommand(keyId, request.ExpiresInDays);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<ApiKeyCreatedResponse>.SuccessResponse(result, "API Key rotated successfully. The previous key is revoked."));
    }
}
