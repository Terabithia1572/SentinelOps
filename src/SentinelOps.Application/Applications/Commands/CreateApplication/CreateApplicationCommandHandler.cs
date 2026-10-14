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
    private readonly IEntitlementService _entitlementService;

    public CreateApplicationCommandHandler(
        IApplicationDbContext context,
        ICurrentTenantService tenantService,
        IEntitlementService entitlementService)
    {
        _context = context;
        _tenantService = tenantService;
        _entitlementService = entitlementService;
    }

    public async Task<ApplicationDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        if (_tenantService.OrganizationId.HasValue && _tenantService.OrganizationId.Value != Guid.Empty)
        {
            var (allowed, reason) = await _entitlementService.CheckFeatureAccessAsync(
                _tenantService.OrganizationId.Value,
                EntitlementFeature.RegisterApplication,
                cancellationToken);

            if (!allowed)
            {
                throw new InvalidOperationException(reason ?? "Application limit reached for your subscription plan.");
            }
        }
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
