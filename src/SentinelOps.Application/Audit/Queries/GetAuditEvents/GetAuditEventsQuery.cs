using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Audit.Queries.GetAuditEvents;

public record GetAuditEventsQuery(
    string? Search,
    string? Actor,
    string? Action,
    string? TargetType,
    string? Outcome,
    string? ApplicationScope,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResponse<AuditEventDto>>;

public class GetAuditEventsQueryHandler : IRequestHandler<GetAuditEventsQuery, PagedResponse<AuditEventDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditEventsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<AuditEventDto>> Handle(GetAuditEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.Trim().ToLower();
            query = query.Where(a =>
                (a.Details != null && a.Details.ToLower().Contains(searchLower)) ||
                (a.Action != null && a.Action.ToLower().Contains(searchLower)) ||
                (a.UserName != null && a.UserName.ToLower().Contains(searchLower)) ||
                (a.EntityType != null && a.EntityType.ToLower().Contains(searchLower)) ||
                (a.EntityId != null && a.EntityId.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            var actorLower = request.Actor.Trim().ToLower();
            query = query.Where(a =>
                (a.UserName != null && a.UserName.ToLower() == actorLower) ||
                (a.UserId != null && a.UserId.ToLower() == actorLower));
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetType))
        {
            query = query.Where(a => a.EntityType == request.TargetType);
        }

        if (!string.IsNullOrWhiteSpace(request.Outcome))
        {
            query = query.Where(a => a.Outcome.ToLower() == request.Outcome.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.ApplicationScope))
        {
            query = query.Where(a => a.ApplicationScope == request.ApplicationScope);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(a => a.Timestamp >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(a => a.Timestamp <= request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditEventDto
            {
                Id = a.Id,
                TimestampUtc = a.Timestamp,
                ActorId = a.UserId,
                ActorName = a.UserName,
                Action = a.Action,
                TargetEntityType = a.EntityType,
                TargetEntityId = a.EntityId,
                ApplicationScope = a.ApplicationScope,
                Outcome = a.Outcome,
                CorrelationId = a.CorrelationId,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Details = a.Details,
                MetadataJson = a.MetadataJson
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<AuditEventDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}
