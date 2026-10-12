using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using EnvironmentEntity = SentinelOps.Domain.Entities.Environment;

namespace SentinelOps.Application.Applications.Commands.CreateEnvironment;

public class CreateEnvironmentCommandHandler : IRequestHandler<CreateEnvironmentCommand, EnvironmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateEnvironmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<EnvironmentDto> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.ApplicationId}' was not found.");
        }

        var exists = await _context.Environments
            .AnyAsync(e => e.ApplicationId == request.ApplicationId && e.Name == request.Name, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"An environment with name '{request.Name}' already exists for this application.");
        }

        var environment = new EnvironmentEntity
        {
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            EnvironmentType = request.EnvironmentType,
            IsActive = true
        };

        _context.Environments.Add(environment);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "EnvironmentCreated",
            EntityType = "Environment",
            EntityId = environment.Id.ToString(),
            Details = $"Environment '{environment.Name}' created for application '{application.Name}'",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return environment.ToDto();
    }
}
