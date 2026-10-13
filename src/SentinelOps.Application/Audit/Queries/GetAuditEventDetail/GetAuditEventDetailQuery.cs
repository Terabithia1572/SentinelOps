using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Audit.Queries.GetAuditEventDetail;

public record GetAuditEventDetailQuery(Guid Id) : IRequest<AuditEventDto?>;

public class GetAuditEventDetailQueryHandler : IRequestHandler<GetAuditEventDetailQuery, AuditEventDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAuditEventDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditEventDto?> Handle(GetAuditEventDetailQuery request, CancellationToken cancellationToken)
    {
        var ev = await _context.AuditEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (ev == null)
        {
            return null;
        }

        return new AuditEventDto
        {
            Id = ev.Id,
            TimestampUtc = ev.Timestamp,
            ActorId = ev.UserId,
            ActorName = ev.UserName,
            Action = ev.Action,
            TargetEntityType = ev.EntityType,
            TargetEntityId = ev.EntityId,
            ApplicationScope = ev.ApplicationScope,
            Outcome = ev.Outcome,
            CorrelationId = ev.CorrelationId,
            IpAddress = ev.IpAddress,
            UserAgent = ev.UserAgent,
            Details = ev.Details,
            MetadataJson = ev.MetadataJson
        };
    }
}
