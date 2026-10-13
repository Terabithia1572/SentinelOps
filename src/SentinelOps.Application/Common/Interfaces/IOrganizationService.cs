using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Common.Interfaces;

public interface IOrganizationService
{
    Task<IEnumerable<OrganizationDto>> GetUserOrganizationsAsync(string userId, CancellationToken ct = default);
    Task<OrganizationDetailDto?> GetOrganizationByIdAsync(Guid orgId, string userId, CancellationToken ct = default);
    Task<OrganizationDetailDto?> GetOrganizationBySlugAsync(string slug, string userId, CancellationToken ct = default);
    Task<(bool Success, OrganizationDto? Org, string? Error)> CreateOrganizationAsync(CreateOrganizationRequest request, string userId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateOrganizationAsync(Guid orgId, UpdateOrganizationRequest request, string userId, CancellationToken ct = default);
    Task<IEnumerable<OrganizationMemberDto>> GetMembersAsync(Guid orgId, string userId, CancellationToken ct = default);
    Task<(bool Success, InvitationCreatedResponse? Response, string? Error)> InviteMemberAsync(Guid orgId, InviteMemberRequest request, string userId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> AcceptInvitationAsync(AcceptInvitationRequest request, string userId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> RevokeInvitationAsync(Guid orgId, Guid invitationId, string userId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> RemoveMemberAsync(Guid orgId, string targetUserId, string currentUserId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> ChangeMemberRoleAsync(Guid orgId, string targetUserId, ChangeMemberRoleRequest request, string currentUserId, CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateSettingsAsync(Guid orgId, UpdateOrganizationSettingsRequest request, string userId, CancellationToken ct = default);
}
