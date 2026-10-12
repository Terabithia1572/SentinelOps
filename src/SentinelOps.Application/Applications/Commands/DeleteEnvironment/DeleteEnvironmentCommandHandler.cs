using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Applications.Commands.DeleteEnvironment;

public class DeleteEnvironmentCommandHandler : IRequestHandler<DeleteEnvironmentCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteEnvironmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await _context.Environments
            .FirstOrDefaultAsync(e => e.Id == request.EnvironmentId && e.ApplicationId == request.ApplicationId, cancellationToken);

        if (environment == null)
        {
            return false;
        }

        _context.Environments.Remove(environment);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "EnvironmentDeleted",
            EntityType = "Environment",
            EntityId = environment.Id.ToString(),
            Details = $"Environment '{environment.Name}' deleted",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
