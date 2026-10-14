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
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ICurrentUserService _currentUserService;

    public OrganizationsController(
        IOrganizationService organizationService,
        ICurrentUserService currentUserService)
    {
        _organizationService = organizationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationDto>>>> GetUserOrganizations(CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<IEnumerable<OrganizationDto>>.Fail("Unauthorized."));

        var orgs = await _organizationService.GetUserOrganizationsAsync(userId, ct);
        return Ok(ApiResponse<IEnumerable<OrganizationDto>>.Ok(orgs));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrganizationDetailDto>>> GetOrganizationById(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OrganizationDetailDto>.Fail("Unauthorized."));

        var org = await _organizationService.GetOrganizationByIdAsync(id, userId, ct);
        if (org == null)
            return NotFound(ApiResponse<OrganizationDetailDto>.Fail("Organization not found or access denied."));

        return Ok(ApiResponse<OrganizationDetailDto>.Ok(org));
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<ApiResponse<OrganizationDetailDto>>> GetOrganizationBySlug(string slug, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OrganizationDetailDto>.Fail("Unauthorized."));

        var org = await _organizationService.GetOrganizationBySlugAsync(slug, userId, ct);
        if (org == null)
            return NotFound(ApiResponse<OrganizationDetailDto>.Fail("Organization not found or access denied."));

        return Ok(ApiResponse<OrganizationDetailDto>.Ok(org));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OrganizationDto>.Fail("Unauthorized."));

        var (success, org, error) = await _organizationService.CreateOrganizationAsync(request, userId, ct);
        if (!success || org == null)
            return BadRequest(ApiResponse<OrganizationDto>.Fail(error ?? "Failed to create organization."));

        return CreatedAtAction(nameof(GetOrganizationById), new { id = org.Id }, ApiResponse<OrganizationDto>.Ok(org));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateOrganization(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.UpdateOrganizationAsync(id, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to update organization."));

        return Ok(ApiResponse<object>.Ok(new { message = "Organization updated successfully." }));
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationMemberDto>>>> GetMembers(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<IEnumerable<OrganizationMemberDto>>.Fail("Unauthorized."));

        var members = await _organizationService.GetMembersAsync(id, userId, ct);
        return Ok(ApiResponse<IEnumerable<OrganizationMemberDto>>.Ok(members));
    }

    [HttpPost("{id:guid}/members/invite")]
    public async Task<ActionResult<ApiResponse<InvitationCreatedResponse>>> InviteMember(
        Guid id,
        [FromBody] InviteMemberRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<InvitationCreatedResponse>.Fail("Unauthorized."));

        var (success, response, error) = await _organizationService.InviteMemberAsync(id, request, userId, ct);
        if (!success || response == null)
            return BadRequest(ApiResponse<InvitationCreatedResponse>.Fail(error ?? "Failed to invite member."));

        return Ok(ApiResponse<InvitationCreatedResponse>.Ok(response));
    }

    [HttpPost("invitations/accept")]
    public async Task<ActionResult<ApiResponse<object>>> AcceptInvitation(
        [FromBody] AcceptInvitationRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.AcceptInvitationAsync(request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to accept invitation."));

        return Ok(ApiResponse<object>.Ok(new { message = "Invitation accepted successfully." }));
    }

    [HttpDelete("{id:guid}/invitations/{invitationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeInvitation(
        Guid id,
        Guid invitationId,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.RevokeInvitationAsync(id, invitationId, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to revoke invitation."));

        return Ok(ApiResponse<object>.Ok(new { message = "Invitation revoked successfully." }));
    }

    [HttpDelete("{id:guid}/members/{targetUserId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveMember(
        Guid id,
        string targetUserId,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.RemoveMemberAsync(id, targetUserId, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to remove member."));

        return Ok(ApiResponse<object>.Ok(new { message = "Member removed successfully." }));
    }

    [HttpPut("{id:guid}/members/{targetUserId}/role")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeMemberRole(
        Guid id,
        string targetUserId,
        [FromBody] ChangeMemberRoleRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.ChangeMemberRoleAsync(id, targetUserId, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to change member role."));

        return Ok(ApiResponse<object>.Ok(new { message = "Member role updated successfully." }));
    }

    [HttpPut("{id:guid}/settings")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSettings(
        Guid id,
        [FromBody] UpdateOrganizationSettingsRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.UpdateSettingsAsync(id, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to update organization settings."));

        return Ok(ApiResponse<object>.Ok(new { message = "Settings updated successfully." }));
    }

    [HttpGet("{id:guid}/quota")]
    public async Task<ActionResult<ApiResponse<OrganizationQuotaStatusDto>>> GetQuotaStatus(
        Guid id,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<OrganizationQuotaStatusDto>.Fail("Unauthorized."));

        var status = await _organizationService.GetQuotaStatusAsync(id, userId, ct);
        if (status == null)
            return NotFound(ApiResponse<OrganizationQuotaStatusDto>.Fail("Organization not found or access denied."));

        return Ok(ApiResponse<OrganizationQuotaStatusDto>.Ok(status));
    }

    [HttpPut("{id:guid}/quota")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateQuota(
        Guid id,
        [FromBody] UpdateOrganizationQuotaRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.UpdateQuotaAsync(id, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to update organization quota."));

        return Ok(ApiResponse<object>.Ok(new { message = "Organization quota limits updated successfully." }));
    }

    [HttpGet("quotas/summary")]
    public async Task<ActionResult<ApiResponse<List<OrganizationQuotaStatusDto>>>> GetAllQuotasSummary(
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<List<OrganizationQuotaStatusDto>>.Fail("Unauthorized."));

        var summary = await _organizationService.GetAllOrganizationsQuotaSummaryAsync(userId, ct);
        return Ok(ApiResponse<List<OrganizationQuotaStatusDto>>.Ok(summary));
    }

    [HttpPost("{id:guid}/ownership-transfer")]
    public async Task<ActionResult<ApiResponse<object>>> TransferOwnership(
        Guid id,
        [FromBody] TransferOwnershipRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.TransferOwnershipAsync(id, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to transfer ownership."));

        return Ok(ApiResponse<object>.Ok(new { message = "Ownership transferred successfully." }));
    }

    [HttpPost("{id:guid}/export")]
    public async Task<ActionResult<ApiResponse<TenantDataExportDto>>> ExportTenantData(
        Guid id,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<TenantDataExportDto>.Fail("Unauthorized."));

        var (success, data, error) = await _organizationService.ExportTenantDataAsync(id, userId, ct);
        if (!success || data == null)
            return BadRequest(ApiResponse<TenantDataExportDto>.Fail(error ?? "Failed to export tenant data."));

        return Ok(ApiResponse<TenantDataExportDto>.Ok(data));
    }

    [HttpDelete("{id:guid}/account")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAccount(
        Guid id,
        [FromBody] DeleteAccountRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var (success, error) = await _organizationService.DeleteOrganizationAccountAsync(id, request, userId, ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to delete organization account."));

        return Ok(ApiResponse<object>.Ok(new { message = "Organization account soft-deleted successfully." }));
    }
}
