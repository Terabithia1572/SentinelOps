using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Application.Logs.Queries.GetLogMetadata;

public record LogMetadataDto
{
    public IReadOnlyList<string> Services { get; init; } = [];
    public IReadOnlyList<string> Environments { get; init; } = [];
}

public record GetLogMetadataQuery(Guid? ApplicationId = null) : IRequest<LogMetadataDto>;

public class GetLogMetadataQueryHandler : IRequestHandler<GetLogMetadataQuery, LogMetadataDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetLogMetadataQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<LogMetadataDto> Handle(GetLogMetadataQuery request, CancellationToken cancellationToken)
    {
        var logsQuery = _context.LogEntries.AsNoTracking();
        var tracesQuery = _context.TraceRecords.AsNoTracking();

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (request.ApplicationId.HasValue)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == request.ApplicationId.Value && a.OrganizationId == tenantOrgId, cancellationToken);
                if (!isAppInTenant)
                {
                    return new LogMetadataDto();
                }
                logsQuery = logsQuery.Where(l => l.ApplicationId == request.ApplicationId.Value);
                tracesQuery = tracesQuery.Where(t => t.ApplicationId == request.ApplicationId.Value);
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                logsQuery = logsQuery.Where(l => tenantAppIds.Contains(l.ApplicationId));
                tracesQuery = tracesQuery.Where(t => tenantAppIds.Contains(t.ApplicationId));
            }
        }
        else if (request.ApplicationId.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.ApplicationId == request.ApplicationId.Value);
            tracesQuery = tracesQuery.Where(t => t.ApplicationId == request.ApplicationId.Value);
        }

        var logServices = await logsQuery
            .Select(l => l.ServiceName)
            .Distinct()
            .ToListAsync(cancellationToken);

        var traceServices = await tracesQuery
            .Select(t => t.ServiceName)
            .Distinct()
            .ToListAsync(cancellationToken);

        var services = logServices
            .Concat(traceServices)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        var logEnvs = await logsQuery
            .Select(l => l.EnvironmentName)
            .Distinct()
            .ToListAsync(cancellationToken);

        var traceEnvs = await tracesQuery
            .Select(t => t.EnvironmentName)
            .Distinct()
            .ToListAsync(cancellationToken);

        var environments = logEnvs
            .Concat(traceEnvs)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .OrderBy(e => e)
            .ToList();

        return new LogMetadataDto
        {
            Services = services,
            Environments = environments
        };
    }
}
