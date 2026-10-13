using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, string UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string userName,
        string email,
        string fullName,
        string password,
        string role);

    Task<(bool Success, string UserId, string UserName, string Email, string FullName, IList<string> Roles)> CheckPasswordSignInAsync(
        string emailOrUserName,
        string password);

    Task<UserProfileDto?> GetUserByIdAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<PagedResponse<UserDto>> GetUsersAsync(
        string? search,
        string? role,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize);

    Task<UserDetailDto?> GetUserDetailAsync(string userId);

    Task<(bool Success, IEnumerable<string> Errors)> UpdateUserRolesAsync(
        string targetUserId,
        IEnumerable<string> newRoles,
        string currentUserId);

    Task<(bool Success, IEnumerable<string> Errors)> UpdateUserStatusAsync(
        string targetUserId,
        bool isActive,
        bool isLocked,
        int? lockoutDurationMinutes,
        string currentUserId);

    Task<(bool Success, IEnumerable<string> Errors)> RevokeUserSessionsAsync(
        string targetUserId,
        string currentUserId);

    Task<(bool Success, IEnumerable<string> Errors)> AdminResetPasswordAsync(
        string targetUserId,
        string newPassword,
        string currentUserId);
}
