using SentinelOps.Contracts.DTOs;

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
}
