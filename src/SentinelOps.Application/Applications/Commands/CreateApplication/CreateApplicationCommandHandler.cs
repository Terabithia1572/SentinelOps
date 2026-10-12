using MediatR;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Commands.CreateApplication;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, ApplicationDto>
{
    private readonly IApplicationDbContext _context;

    public CreateApplicationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = new ApplicationEntity
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = true,
            ApiKeyHash = Guid.NewGuid().ToString("N")
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync(cancellationToken);

        return application.ToDto();
    }
}
