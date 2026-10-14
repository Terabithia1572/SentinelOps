using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<OnboardingStatusDto>>> GetStatus(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OnboardingStatusDto>.Fail("Unauthorized"));

        var status = await _onboardingService.GetStatusAsync(userId, ct);
        return Ok(ApiResponse<OnboardingStatusDto>.Ok(status));
    }

    [HttpPost("provision")]
    public async Task<ActionResult<ApiResponse<OnboardingProvisionedResponse>>> Provision(
        [FromBody] OnboardingProvisionRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OnboardingProvisionedResponse>.Fail("Unauthorized"));

        var (success, response, error) = await _onboardingService.ProvisionTenantAndAppAsync(request, userId, ct);
        if (!success || response == null)
        {
            return BadRequest(ApiResponse<OnboardingProvisionedResponse>.Fail(error ?? "Failed to provision onboarding workspace."));
        }

        return Ok(ApiResponse<OnboardingProvisionedResponse>.Ok(response, "Workspace provisioned successfully."));
    }

    [HttpGet("verify/{applicationId:guid}")]
    public async Task<ActionResult<ApiResponse<FirstTelemetryVerificationResponse>>> VerifyTelemetry(
        Guid applicationId,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<FirstTelemetryVerificationResponse>.Fail("Unauthorized"));

        var verification = await _onboardingService.VerifyFirstTelemetryAsync(applicationId, userId, ct);
        return Ok(ApiResponse<FirstTelemetryVerificationResponse>.Ok(verification));
    }

    [HttpPost("complete")]
    public async Task<ActionResult<ApiResponse<bool>>> Complete(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized"));

        var (success, error) = await _onboardingService.CompleteOnboardingAsync(userId, ct);
        if (!success)
            return BadRequest(ApiResponse<bool>.Fail(error ?? "Failed to complete onboarding."));

        return Ok(ApiResponse<bool>.Ok(true, "Onboarding completed successfully."));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
