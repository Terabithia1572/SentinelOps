using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Applications.Commands.UpdateApplication;

public class UpdateApplicationCommandHandler : IRequestHandler<UpdateApplicationCommand, ApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateApplicationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApplicationDto> Handle(UpdateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .Include(a => a.Environments)
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (application == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.Id}' was not found.");
        }

        application.DisplayName = request.DisplayName;
        application.Description = request.Description;
        application.IsActive = request.IsActive;

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "ApplicationUpdated",
            EntityType = "Application",
            EntityId = application.Id.ToString(),
            Details = $"Application updated: DisplayName='{request.DisplayName}', IsActive={request.IsActive}",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return application.ToDto();
    }
}
