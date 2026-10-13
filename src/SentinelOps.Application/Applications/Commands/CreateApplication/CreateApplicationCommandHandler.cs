using MediatR;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Commands.CreateApplication;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, ApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public CreateApplicationCommandHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ApplicationDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = new ApplicationEntity
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = true,
            ApiKeyHash = Guid.NewGuid().ToString("N"),
            OrganizationId = _tenantService.OrganizationId
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync(cancellationToken);

        return application.ToDto();
    }
}
