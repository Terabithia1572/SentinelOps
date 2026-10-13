using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;

namespace SentinelOps.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IApplicationDbContext _context;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<(bool Success, string UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string userName,
        string email,
        string fullName,
        string password,
        string role)
    {
        var existingByEmail = await _userManager.FindByEmailAsync(email);
        if (existingByEmail != null)
        {
            return (false, string.Empty, ["A user with this email address already exists."]);
        }

        var existingByName = await _userManager.FindByNameAsync(userName);
        if (existingByName != null)
        {
            return (false, string.Empty, ["A user with this username already exists."]);
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            FullName = fullName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, string.Empty, result.Errors.Select(e => e.Description));
        }

        // Ensure role exists
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return (false, user.Id, roleResult.Errors.Select(e => e.Description));
        }

        return (true, user.Id, []);
    }

    public async Task<(bool Success, string UserId, string UserName, string Email, string FullName, IList<string> Roles)> CheckPasswordSignInAsync(
        string emailOrUserName,
        string password)
    {
        ApplicationUser? user = null;

        if (emailOrUserName.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(emailOrUserName);
        }

        user ??= await _userManager.FindByNameAsync(emailOrUserName);

        if (user == null || !user.IsActive)
        {
            return (false, string.Empty, string.Empty, string.Empty, string.Empty, []);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, string.Empty, string.Empty, string.Empty, string.Empty, []);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return (false, string.Empty, string.Empty, string.Empty, string.Empty, []);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return (true, user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.FullName, roles);
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = roles.ToList()
        };
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<PagedResponse<UserDto>> GetUsersAsync(
        string? search,
        string? role,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                u.FullName.ToLower().Contains(searchLower));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(u => u.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(u => u.CreatedAtUtc <= toUtc.Value);
        }

        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.ToLowerInvariant())
            {
                case "active":
                    query = query.Where(u => u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= now));
                    break;
                case "inactive":
                    query = query.Where(u => !u.IsActive);
                    break;
                case "locked":
                    query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > now);
                    break;
            }
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > now;

            dtos.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList(),
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                LockoutEnd = user.LockoutEnd,
                CreatedAtUtc = user.CreatedAtUtc
            });
        }

        return PagedResponse<UserDto>.Create(dtos, totalCount, page, pageSize);
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        var memberships = await _context.ApplicationMembers
            .AsNoTracking()
            .Include(m => m.Application)
            .Where(m => m.UserId == userId)
            .Select(m => new ApplicationMemberDto
            {
                Id = m.Id,
                ApplicationId = m.ApplicationId,
                ApplicationName = m.Application.DisplayName.Length > 0 ? m.Application.DisplayName : m.Application.Name,
                UserId = m.UserId,
                UserName = user.UserName ?? string.Empty,
                Role = m.Role,
                CreatedAtUtc = m.CreatedAtUtc
            })
            .ToListAsync();

        var recentActivities = await _context.AuditEvents
            .AsNoTracking()
            .Where(a => a.UserId == userId || a.EntityId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .Select(a => new SecurityActivityDto
            {
                Id = a.Id,
                Action = a.Action,
                Outcome = a.Outcome,
                Details = a.Details,
                IpAddress = a.IpAddress,
                TimestampUtc = a.Timestamp
            })
            .ToListAsync();

        return new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            IsLockedOut = isLocked,
            LockoutEnd = user.LockoutEnd,
            CreatedAtUtc = user.CreatedAtUtc,
            ApplicationMemberships = memberships,
            RecentSecurityActivity = recentActivities
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserRolesAsync(
        string targetUserId,
        IEnumerable<string> newRoles,
        string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
        {
            return (false, ["User not found."]);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRoleList = newRoles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Guardrail: Final Administrator protection
        if (currentRoles.Contains(Roles.Admin) && !targetRoleList.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var otherActiveAdmins = adminUsers.Count(u => u.Id != targetUserId && u.IsActive);
            if (otherActiveAdmins == 0)
            {
                return (false, ["Cannot remove the Administrator role from the last active Administrator."]);
            }
        }

        // Validate roles
        foreach (var role in targetRoleList)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return (false, removeResult.Errors.Select(e => e.Description));
        }

        var addResult = await _userManager.AddToRolesAsync(user, targetRoleList);
        if (!addResult.Succeeded)
        {
            return (false, addResult.Errors.Select(e => e.Description));
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return (true, []);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserStatusAsync(
        string targetUserId,
        bool isActive,
        bool isLocked,
        int? lockoutDurationMinutes,
        string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
        {
            return (false, ["User not found."]);
        }

        // Guardrail: Cannot deactivate or lock oneself
        if (targetUserId == currentUserId && (!isActive || isLocked))
        {
            return (false, ["Administrators cannot deactivate or lock their own account."]);
        }

        // Guardrail: Cannot deactivate or lock the last active Administrator
        var isTargetAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
        if (isTargetAdmin && (!isActive || isLocked))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var otherActiveAdmins = adminUsers.Count(u => u.Id != targetUserId && u.IsActive);
            if (otherActiveAdmins == 0)
            {
                return (false, ["Cannot deactivate or lock the last active Administrator."]);
            }
        }

        user.IsActive = isActive;
        if (isLocked)
        {
            var lockoutMinutes = lockoutDurationMinutes.HasValue && lockoutDurationMinutes.Value > 0 ? lockoutDurationMinutes.Value : 1440;
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes));
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, updateResult.Errors.Select(e => e.Description));
        }

        // If inactive or locked, invalidate sessions and refresh tokens
        if (!isActive || isLocked)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == targetUserId && !r.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.Revoke("admin");
            }

            await _context.SaveChangesAsync();
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return (true, []);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> RevokeUserSessionsAsync(
        string targetUserId,
        string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
        {
            return (false, ["User not found."]);
        }

        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == targetUserId && !r.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoke("admin");
        }

        await _context.SaveChangesAsync();
        await _userManager.UpdateSecurityStampAsync(user);

        return (true, []);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> AdminResetPasswordAsync(
        string targetUserId,
        string newPassword,
        string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
        {
            return (false, ["User not found."]);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        // Revoke active sessions upon password reset
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == targetUserId && !r.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoke("admin");
        }

        await _context.SaveChangesAsync();
        await _userManager.UpdateSecurityStampAsync(user);

        return (true, []);
    }
}
