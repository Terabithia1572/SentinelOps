using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Application.Users.Commands.RemoveApplicationMember;

public record RemoveApplicationMemberCommand(Guid ApplicationId, string UserId) : IRequest<(bool Success, string? Error)>;

public class RemoveApplicationMemberCommandHandler : IRequestHandler<RemoveApplicationMemberCommand, (bool Success, string? Error)>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public RemoveApplicationMemberCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(bool Success, string? Error)> Handle(RemoveApplicationMemberCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.ApplicationMembers
            .FirstOrDefaultAsync(m => m.ApplicationId == request.ApplicationId && m.UserId == request.UserId, cancellationToken);

        if (existing == null)
        {
            return (false, "Membership not found.");
        }

        _context.ApplicationMembers.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Application.MemberRemoved",
            targetEntityType: "ApplicationMember",
            targetEntityId: request.UserId,
            applicationScope: request.ApplicationId.ToString(),
            outcome: "Success",
            details: $"Removed membership for user {request.UserId} on application {request.ApplicationId}",
            metadata: new { request.ApplicationId, request.UserId },
            cancellationToken: cancellationToken);

        return (true, null);
    }
}
