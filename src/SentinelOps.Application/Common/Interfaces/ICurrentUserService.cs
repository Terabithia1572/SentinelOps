namespace SentinelOps.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
}
