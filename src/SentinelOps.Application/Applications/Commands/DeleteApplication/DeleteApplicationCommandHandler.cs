using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Applications.Commands.DeleteApplication;

public class DeleteApplicationCommandHandler : IRequestHandler<DeleteApplicationCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteApplicationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (application == null)
        {
            return false;
        }

        _context.Applications.Remove(application);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "ApplicationDeleted",
            EntityType = "Application",
            EntityId = application.Id.ToString(),
            Details = $"Application '{application.Name}' deleted",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
