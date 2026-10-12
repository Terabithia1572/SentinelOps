using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Applications.Commands.CreateService;

public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateServiceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.ApplicationId}' was not found.");
        }

        var exists = await _context.Services
            .AnyAsync(s => s.ApplicationId == request.ApplicationId && s.Name == request.Name, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A service with name '{request.Name}' already exists for this application.");
        }

        var service = new Service
        {
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            DisplayName = request.DisplayName,
            IsActive = true
        };

        _context.Services.Add(service);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "ServiceCreated",
            EntityType = "Service",
            EntityId = service.Id.ToString(),
            Details = $"Service '{service.Name}' created for application '{application.Name}'",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return service.ToDto();
    }
}
