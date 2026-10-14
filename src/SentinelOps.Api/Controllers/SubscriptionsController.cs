using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<SubscriptionPlanDto>>>> GetPlans(CancellationToken ct)
    {
        var plans = await _subscriptionService.GetPlansAsync(ct);
        return Ok(ApiResponse<IEnumerable<SubscriptionPlanDto>>.Ok(plans));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<OrganizationSubscriptionDto>>> GetCurrent(
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<OrganizationSubscriptionDto>.Fail("Organization ID is required via X-Organization-Id header or query parameter."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OrganizationSubscriptionDto>.Fail("Unauthorized"));

        var sub = await _subscriptionService.GetSubscriptionAsync(orgId.Value, userId, ct);
        if (sub == null)
            return NotFound(ApiResponse<OrganizationSubscriptionDto>.Fail("Subscription not found for this organization."));

        return Ok(ApiResponse<OrganizationSubscriptionDto>.Ok(sub));
    }

    [HttpPost("change-plan")]
    public async Task<ActionResult<ApiResponse<OrganizationSubscriptionDto>>> ChangePlan(
        [FromBody] ChangePlanRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<OrganizationSubscriptionDto>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OrganizationSubscriptionDto>.Fail("Unauthorized"));

        var (success, sub, error) = await _subscriptionService.ChangePlanAsync(orgId.Value, request, userId, ct);
        if (!success || sub == null)
            return BadRequest(ApiResponse<OrganizationSubscriptionDto>.Fail(error ?? "Failed to change subscription plan."));

        return Ok(ApiResponse<OrganizationSubscriptionDto>.Ok(sub, "Plan updated successfully."));
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(
        [FromBody] CancelSubscriptionRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<bool>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized"));

        var (success, error) = await _subscriptionService.CancelSubscriptionAsync(orgId.Value, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<bool>.Fail(error ?? "Failed to cancel subscription."));

        return Ok(ApiResponse<bool>.Ok(true, "Subscription cancellation processed successfully."));
    }

    [HttpPost("reactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> Reactivate(
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<bool>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized"));

        var (success, error) = await _subscriptionService.ReactivateSubscriptionAsync(orgId.Value, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<bool>.Fail(error ?? "Failed to reactivate subscription."));

        return Ok(ApiResponse<bool>.Ok(true, "Subscription reactivated successfully."));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
