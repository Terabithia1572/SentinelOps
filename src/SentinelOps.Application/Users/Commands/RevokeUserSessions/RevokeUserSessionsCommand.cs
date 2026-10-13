using MediatR;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Application.Users.Commands.RevokeUserSessions;

public record RevokeUserSessionsCommand(string UserId) : IRequest<(bool Success, IEnumerable<string> Errors)>;

public class RevokeUserSessionsCommandHandler : IRequestHandler<RevokeUserSessionsCommand, (bool Success, IEnumerable<string> Errors)>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RevokeUserSessionsCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> Handle(RevokeUserSessionsCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? "system";
        var (success, errors) = await _identityService.RevokeUserSessionsAsync(request.UserId, currentUserId);

        await _auditService.RecordAsync(
            action: "User.SessionsRevoked",
            targetEntityType: "User",
            targetEntityId: request.UserId,
            outcome: success ? "Success" : "Failure",
            details: success
                ? "All user sessions and refresh tokens were invalidated."
                : $"Failed to revoke sessions: {string.Join("; ", errors)}",
            metadata: new { TargetUserId = request.UserId },
            cancellationToken: cancellationToken);

        return (success, errors);
    }
}
