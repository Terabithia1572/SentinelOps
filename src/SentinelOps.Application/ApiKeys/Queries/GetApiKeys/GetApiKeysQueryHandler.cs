using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.ApiKeys.Queries.GetApiKeys;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, IReadOnlyList<ApiKeyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetApiKeysQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ApiKeyDto>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        var keys = await _context.ApiKeys
            .Where(k => k.ApplicationId == request.ApplicationId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return keys.Select(k => k.ToDto()).ToList();
    }
}
