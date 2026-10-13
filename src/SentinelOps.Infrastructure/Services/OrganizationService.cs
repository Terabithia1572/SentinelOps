using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Identity;

namespace SentinelOps.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ILogger<OrganizationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IEnumerable<OrganizationDto>> GetUserOrganizationsAsync(string userId, CancellationToken ct = default)
    {
        var memberships = await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .ThenInclude(o => o.Applications)
            .Where(m => m.UserId == userId && m.Organization.IsActive)
            .OrderBy(m => m.Organization.Name)
            .ToListAsync(ct);

        var orgIds = memberships.Select(m => m.OrganizationId).ToList();
        var memberCounts = await _context.OrganizationMembers
            .AsNoTracking()
            .Where(m => orgIds.Contains(m.OrganizationId))
            .GroupBy(m => m.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgId, x => x.Count, ct);

        return memberships.Select(m => new OrganizationDto
        {
            Id = m.Organization.Id,
            Name = m.Organization.Name,
            Slug = m.Organization.Slug,
            Description = m.Organization.Description,
            IsActive = m.Organization.IsActive,
            UserRole = m.Role.ToString(),
            MembersCount = memberCounts.GetValueOrDefault(m.OrganizationId, 1),
            ApplicationsCount = m.Organization.Applications.Count,
            CreatedAtUtc = m.Organization.CreatedAtUtc
        });
    }

    public async Task<OrganizationDetailDto?> GetOrganizationByIdAsync(Guid orgId, string userId, CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null) return null;

        var org = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Settings)
            .Include(o => o.Usage)
            .FirstOrDefaultAsync(o => o.Id == orgId && o.IsActive, ct);

        if (org == null) return null;

        var members = await GetMembersAsync(orgId, userId, ct);

        var pendingInvites = await _context.OrganizationInvitations
            .AsNoTracking()
            .Where(i => i.OrganizationId == orgId && i.AcceptedAtUtc == null && i.RevokedAtUtc == null && i.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new OrganizationInvitationDto
            {
                Id = i.Id,
                OrganizationId = i.OrganizationId,
                Email = i.Email,
                Role = i.Role.ToString(),
                ExpiresAtUtc = i.ExpiresAtUtc,
                InvitedByUserName = i.InvitedByUserId,
                CreatedAtUtc = i.CreatedAtUtc,
                IsPending = true
            })
            .ToListAsync(ct);

        return new OrganizationDetailDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Description = org.Description,
            IsActive = org.IsActive,
            UserRole = member.Role.ToString(),
            CreatedAtUtc = org.CreatedAtUtc,
            Members = members.ToList(),
            PendingInvitations = pendingInvites,
            Settings = new OrganizationSettingsDto
            {
                AllowMemberInvites = org.Settings?.AllowMemberInvites ?? true,
                RequireTwoFactor = org.Settings?.RequireTwoFactor ?? false,
                DefaultRetentionDays = org.Settings?.DefaultRetentionDays ?? 30
            },
            Usage = new OrganizationUsageDto
            {
                LogsIngestedMonthToDate = org.Usage?.LogsIngestedMonthToDate ?? 0,
                TracesIngestedMonthToDate = org.Usage?.TracesIngestedMonthToDate ?? 0,
                MetricsIngestedMonthToDate = org.Usage?.MetricsIngestedMonthToDate ?? 0,
                ApiRequestsMonthToDate = org.Usage?.ApiRequestsMonthToDate ?? 0,
                EstimatedStorageBytes = org.Usage?.EstimatedStorageBytes ?? 0,
                LastResetAtUtc = org.Usage?.LastResetAtUtc ?? org.CreatedAtUtc
            }
        };
    }

    public async Task<OrganizationDetailDto?> GetOrganizationBySlugAsync(string slug, string userId, CancellationToken ct = default)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Slug == slug.Trim().ToLowerInvariant() && o.IsActive, ct);

        if (org == null) return null;
        return await GetOrganizationByIdAsync(org.Id, userId, ct);
    }

    public async Task<(bool Success, OrganizationDto? Org, string? Error)> CreateOrganizationAsync(CreateOrganizationRequest request, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Organization name is required.");

        var normalizedSlug = (string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug)
            .Trim().ToLowerInvariant().Replace(' ', '-');

        // Verify slug uniqueness
        var slugExists = await _context.Organizations.AnyAsync(o => o.Slug == normalizedSlug, ct);
        if (slugExists)
        {
            normalizedSlug = $"{normalizedSlug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        var org = new Organization
        {
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            Settings = new OrganizationSettings
            {
                AllowMemberInvites = true,
                RequireTwoFactor = false,
                DefaultRetentionDays = 30
            },
            Usage = new OrganizationUsage
            {
                LastResetAtUtc = DateTime.UtcNow
            }
        };

        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAtUtc = DateTime.UtcNow
        };

        _context.Organizations.Add(org);
        _context.OrganizationMembers.Add(member);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.Created",
            targetEntityType: "Organization",
            targetEntityId: org.Id.ToString(),
            outcome: "Success",
            details: $"Organization '{org.Name}' created by user {userId}.",
            cancellationToken: ct);

        var dto = new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Description = org.Description,
            IsActive = org.IsActive,
            UserRole = OrganizationRole.Owner.ToString(),
            MembersCount = 1,
            ApplicationsCount = 0,
            CreatedAtUtc = org.CreatedAtUtc
        };

        return (true, dto, null);
    }

    public async Task<(bool Success, string? Error)> UpdateOrganizationAsync(Guid orgId, UpdateOrganizationRequest request, string userId, CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null || (member.Role != OrganizationRole.Owner && member.Role != OrganizationRole.Admin))
            return (false, "Only Organization Owners or Admins can update organization details.");

        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org == null) return (false, "Organization not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Organization name cannot be empty.");

        org.Name = request.Name.Trim();
        org.Description = request.Description?.Trim();
        org.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.Updated",
            targetEntityType: "Organization",
            targetEntityId: org.Id.ToString(),
            outcome: "Success",
            details: $"Organization '{org.Name}' updated.",
            cancellationToken: ct);

        return (true, null);
    }

    public async Task<IEnumerable<OrganizationMemberDto>> GetMembersAsync(Guid orgId, string userId, CancellationToken ct = default)
    {
        var isMember = await _context.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);
        if (!isMember) return Enumerable.Empty<OrganizationMemberDto>();

        var members = await _context.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAtUtc)
            .ToListAsync(ct);

        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        return members.Select(m =>
        {
            users.TryGetValue(m.UserId, out var u);
            return new OrganizationMemberDto
            {
                Id = m.Id,
                OrganizationId = m.OrganizationId,
                UserId = m.UserId,
                UserName = u?.UserName ?? "Unknown",
                Email = u?.Email ?? string.Empty,
                FullName = u?.FullName ?? string.Empty,
                Role = m.Role.ToString(),
                JoinedAtUtc = m.JoinedAtUtc
            };
        });
    }

    public async Task<(bool Success, InvitationCreatedResponse? Response, string? Error)> InviteMemberAsync(Guid orgId, InviteMemberRequest request, string userId, CancellationToken ct = default)
    {
        var caller = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (caller == null || (caller.Role != OrganizationRole.Owner && caller.Role != OrganizationRole.Admin))
            return (false, null, "Only Organization Owners or Admins can invite members.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return (false, null, "A valid email address is required.");

        var cleanEmail = request.Email.Trim().ToLowerInvariant();

        // Check if user is already a member
        var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingUser != null)
        {
            var alreadyMember = await _context.OrganizationMembers
                .AnyAsync(m => m.OrganizationId == orgId && m.UserId == existingUser.Id, ct);
            if (alreadyMember)
                return (false, null, "This user is already a member of this organization.");
        }

        // Parse requested role
        if (!Enum.TryParse<OrganizationRole>(request.Role, true, out var roleEnum))
            roleEnum = OrganizationRole.Member;

        if (roleEnum == OrganizationRole.Owner && caller.Role != OrganizationRole.Owner)
            return (false, null, "Only Owners can invite new Owners.");

        // Revoke any previous active invitation for the same email
        var pendingInvites = await _context.OrganizationInvitations
            .Where(i => i.OrganizationId == orgId && i.Email == cleanEmail && i.AcceptedAtUtc == null && i.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var pi in pendingInvites)
        {
            pi.Revoke();
        }

        // Generate 32-byte cryptographic random token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var tokenHash = HashToken(rawToken);

        var expiresAt = DateTime.UtcNow.AddDays(7);
        var invitation = new OrganizationInvitation
        {
            OrganizationId = orgId,
            Email = cleanEmail,
            Role = roleEnum,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAt,
            InvitedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.OrganizationInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.MemberInvited",
            targetEntityType: "OrganizationInvitation",
            targetEntityId: invitation.Id.ToString(),
            outcome: "Success",
            details: $"Invited {cleanEmail} as {roleEnum} to organization {orgId}.",
            cancellationToken: ct);

        var response = new InvitationCreatedResponse
        {
            InvitationId = invitation.Id,
            Email = cleanEmail,
            Role = roleEnum.ToString(),
            ExpiresAtUtc = expiresAt,
            RawToken = rawToken
        };

        return (true, response, null);
    }

    public async Task<(bool Success, string? Error)> AcceptInvitationAsync(AcceptInvitationRequest request, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return (false, "Invalid invitation token.");

        var tokenHash = HashToken(request.Token.Trim());
        var invitation = await _context.OrganizationInvitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

        if (invitation == null || !invitation.IsActive)
            return (false, "Invitation is invalid, expired, or has already been used.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "User not found.");

        // Check if user already joined
        var alreadyMember = await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == invitation.OrganizationId && m.UserId == userId, ct);

        if (alreadyMember)
        {
            invitation.Accept();
            await _context.SaveChangesAsync(ct);
            return (true, null);
        }

        var member = new OrganizationMember
        {
            OrganizationId = invitation.OrganizationId,
            UserId = userId,
            Role = invitation.Role,
            JoinedAtUtc = DateTime.UtcNow
        };

        invitation.Accept();
        _context.OrganizationMembers.Add(member);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.InvitationAccepted",
            targetEntityType: "Organization",
            targetEntityId: invitation.OrganizationId.ToString(),
            outcome: "Success",
            details: $"User {user.UserName} accepted invitation and joined organization '{invitation.Organization.Name}'.",
            cancellationToken: ct);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RevokeInvitationAsync(Guid orgId, Guid invitationId, string userId, CancellationToken ct = default)
    {
        var caller = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (caller == null || (caller.Role != OrganizationRole.Owner && caller.Role != OrganizationRole.Admin))
            return (false, "Only Organization Owners or Admins can revoke invitations.");

        var invitation = await _context.OrganizationInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.OrganizationId == orgId, ct);

        if (invitation == null) return (false, "Invitation not found.");

        invitation.Revoke();
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.InvitationRevoked",
            targetEntityType: "OrganizationInvitation",
            targetEntityId: invitationId.ToString(),
            outcome: "Success",
            details: $"Revoked invitation {invitationId} for organization {orgId}.",
            cancellationToken: ct);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveMemberAsync(Guid orgId, string targetUserId, string currentUserId, CancellationToken ct = default)
    {
        var caller = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == currentUserId, ct);

        if (caller == null || (caller.Role != OrganizationRole.Owner && caller.Role != OrganizationRole.Admin))
            return (false, "Only Organization Owners or Admins can remove members.");

        var targetMember = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == targetUserId, ct);

        if (targetMember == null) return (false, "Member not found in organization.");

        // Guardrails:
        // 1. Cannot remove the only remaining Owner
        if (targetMember.Role == OrganizationRole.Owner)
        {
            var otherOwners = await _context.OrganizationMembers
                .CountAsync(m => m.OrganizationId == orgId && m.Role == OrganizationRole.Owner && m.UserId != targetUserId, ct);

            if (otherOwners == 0)
                return (false, "Cannot remove the only Owner of the organization.");
        }

        // 2. Admin cannot remove an Owner
        if (caller.Role == OrganizationRole.Admin && targetMember.Role == OrganizationRole.Owner)
            return (false, "Admins cannot remove an Organization Owner.");

        _context.OrganizationMembers.Remove(targetMember);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.MemberRemoved",
            targetEntityType: "OrganizationMember",
            targetEntityId: targetMember.Id.ToString(),
            outcome: "Success",
            details: $"User {targetUserId} was removed from organization {orgId}.",
            cancellationToken: ct);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangeMemberRoleAsync(Guid orgId, string targetUserId, ChangeMemberRoleRequest request, string currentUserId, CancellationToken ct = default)
    {
        var caller = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == currentUserId, ct);

        if (caller == null || caller.Role != OrganizationRole.Owner)
            return (false, "Only Organization Owners can change member roles.");

        if (!Enum.TryParse<OrganizationRole>(request.Role, true, out var newRoleEnum))
            return (false, "Invalid organization role specified.");

        var targetMember = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == targetUserId, ct);

        if (targetMember == null) return (false, "Member not found in organization.");

        // Guardrail: Cannot demote the last Owner
        if (targetMember.Role == OrganizationRole.Owner && newRoleEnum != OrganizationRole.Owner)
        {
            var otherOwners = await _context.OrganizationMembers
                .CountAsync(m => m.OrganizationId == orgId && m.Role == OrganizationRole.Owner && m.UserId != targetUserId, ct);

            if (otherOwners == 0)
                return (false, "Cannot demote the only Owner of the organization.");
        }

        targetMember.Role = newRoleEnum;
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.MemberRoleChanged",
            targetEntityType: "OrganizationMember",
            targetEntityId: targetMember.Id.ToString(),
            outcome: "Success",
            details: $"User {targetUserId} role changed to {newRoleEnum} in organization {orgId}.",
            cancellationToken: ct);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateSettingsAsync(Guid orgId, UpdateOrganizationSettingsRequest request, string userId, CancellationToken ct = default)
    {
        var caller = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (caller == null || (caller.Role != OrganizationRole.Owner && caller.Role != OrganizationRole.Admin))
            return (false, "Only Organization Owners or Admins can update organization settings.");

        var settings = await _context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (settings == null)
        {
            settings = new OrganizationSettings
            {
                OrganizationId = orgId
            };
            _context.OrganizationSettings.Add(settings);
        }

        settings.AllowMemberInvites = request.AllowMemberInvites;
        settings.RequireTwoFactor = request.RequireTwoFactor;
        settings.DefaultRetentionDays = Math.Clamp(request.DefaultRetentionDays, 7, 365);
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Organization.SettingsUpdated",
            targetEntityType: "OrganizationSettings",
            targetEntityId: settings.Id.ToString(),
            outcome: "Success",
            details: $"Updated settings for organization {orgId}.",
            cancellationToken: ct);

        return (true, null);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
