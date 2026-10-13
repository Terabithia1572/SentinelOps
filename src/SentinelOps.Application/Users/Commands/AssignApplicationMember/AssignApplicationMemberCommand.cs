using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Users.Commands.AssignApplicationMember;

public record AssignApplicationMemberCommand(
    Guid ApplicationId,
    string UserId,
    string Role
) : IRequest<(bool Success, string? Error)>;

public class AssignApplicationMemberCommandHandler : IRequestHandler<AssignApplicationMemberCommand, (bool Success, string? Error)>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AssignApplicationMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<(bool Success, string? Error)> Handle(AssignApplicationMemberCommand request, CancellationToken cancellationToken)
    {
        var app = await _context.Applications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);
        if (app == null)
        {
            return (false, "Application not found.");
        }

        var validRoles = new[] { "Admin", "Operator", "Viewer" };
        if (!validRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Invalid role. Role must be Admin, Operator, or Viewer.");
        }

        var existing = await _context.ApplicationMembers
            .FirstOrDefaultAsync(m => m.ApplicationId == request.ApplicationId && m.UserId == request.UserId, cancellationToken);

        if (existing != null)
        {
            existing.Role = request.Role;
        }
        else
        {
            _context.ApplicationMembers.Add(new ApplicationMember
            {
                ApplicationId = request.ApplicationId,
                UserId = request.UserId,
                Role = request.Role,
                AddedByUserId = _currentUserService.UserId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Application.MemberAssigned",
            targetEntityType: "ApplicationMember",
            targetEntityId: request.UserId,
            applicationScope: request.ApplicationId.ToString(),
            outcome: "Success",
            details: $"Assigned role {request.Role} on application {app.Name}",
            metadata: new { request.ApplicationId, request.UserId, request.Role },
            cancellationToken: cancellationToken);

        return (true, null);
    }
}
