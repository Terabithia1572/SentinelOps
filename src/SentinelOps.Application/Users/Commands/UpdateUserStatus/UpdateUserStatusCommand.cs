using MediatR;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Application.Users.Commands.UpdateUserStatus;

public record UpdateUserStatusCommand(
    string UserId,
    bool IsActive,
    bool IsLocked,
    int? LockoutDurationMinutes
) : IRequest<(bool Success, IEnumerable<string> Errors)>;

public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, (bool Success, IEnumerable<string> Errors)>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public UpdateUserStatusCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? "system";
        var (success, errors) = await _identityService.UpdateUserStatusAsync(
            request.UserId,
            request.IsActive,
            request.IsLocked,
            request.LockoutDurationMinutes,
            currentUserId);

        var actionName = !request.IsActive ? "User.Deactivated" : (request.IsLocked ? "User.Locked" : "User.Activated");

        await _auditService.RecordAsync(
            action: actionName,
            targetEntityType: "User",
            targetEntityId: request.UserId,
            outcome: success ? "Success" : "Failure",
            details: success
                ? $"Status updated: IsActive={request.IsActive}, IsLocked={request.IsLocked}"
                : $"Failed to update status: {string.Join("; ", errors)}",
            metadata: new { request.UserId, request.IsActive, request.IsLocked, request.LockoutDurationMinutes },
            cancellationToken: cancellationToken);

        return (success, errors);
    }
}
