using Microsoft.AspNetCore.Identity;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Constants;

namespace SentinelOps.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
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

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return (false, string.Empty, string.Empty, string.Empty, string.Empty, []);
        }

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
}
