using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Application.Exceptions.Queries.GetExceptions;

public record GetExceptionsQuery(GetExceptionsRequest Request) : IRequest<IReadOnlyList<ExceptionGroupDto>>;

public class GetExceptionsQueryHandler : IRequestHandler<GetExceptionsQuery, IReadOnlyList<ExceptionGroupDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetExceptionsQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<IReadOnlyList<ExceptionGroupDto>> Handle(GetExceptionsQuery request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var query = _context.LogEntries
            .AsNoTracking()
            .Where(l => l.ExceptionDetail != null || l.Severity == Severity.Error || l.Severity == Severity.Critical);

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (req.ApplicationId.HasValue)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == req.ApplicationId.Value && a.OrganizationId == tenantOrgId, cancellationToken);
                if (!isAppInTenant)
                {
                    return [];
                }
                query = query.Where(l => l.ApplicationId == req.ApplicationId.Value);
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                query = query.Where(l => tenantAppIds.Contains(l.ApplicationId));
            }
        }
        else if (req.ApplicationId.HasValue)
        {
            query = query.Where(l => l.ApplicationId == req.ApplicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.ServiceName))
        {
            query = query.Where(l => l.ServiceName == req.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(req.EnvironmentName))
        {
            query = query.Where(l => l.EnvironmentName == req.EnvironmentName);
        }

        if (req.FromUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp >= req.FromUtc.Value);
        }

        if (req.ToUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp <= req.ToUtc.Value);
        }

        // Project necessary fields to avoid loading large properties payloads
        var errorLogs = await query
            .OrderByDescending(l => l.Timestamp)
            .Take(1000)
            .Select(l => new
            {
                l.Id,
                l.ServiceName,
                l.EnvironmentName,
                l.Message,
                l.ExceptionDetail,
                l.TraceId,
                l.Timestamp
            })
            .ToListAsync(cancellationToken);

        if (errorLogs.Count == 0)
        {
            return [];
        }

        var groups = errorLogs
            .GroupBy(l =>
            {
                var (type, summary, fingerprint) = ExtractFingerprint(l.ExceptionDetail, l.Message);
                return (Fingerprint: fingerprint, Type: type, Summary: summary, l.ServiceName, l.EnvironmentName);
            })
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Max(l => l.Timestamp))
            .Take(Math.Clamp(req.Limit > 0 ? req.Limit : 30, 1, 100))
            .Select(g => new ExceptionGroupDto
            {
                Fingerprint = g.Key.Fingerprint,
                ExceptionType = g.Key.Type,
                Message = g.Key.Summary,
                ServiceName = g.Key.ServiceName,
                EnvironmentName = g.Key.EnvironmentName,
                Count = g.Count(),
                FirstSeenAtUtc = g.Min(l => l.Timestamp),
                LastSeenAtUtc = g.Max(l => l.Timestamp),
                SampleTraceId = g.FirstOrDefault(l => !string.IsNullOrEmpty(l.TraceId))?.TraceId,
                SampleLogId = g.First().Id
            })
            .ToList();

        return groups;
    }

    private static (string Type, string Summary, string Fingerprint) ExtractFingerprint(string? exceptionDetail, string message)
    {
        string type = "Exception";
        string summary = message;

        if (!string.IsNullOrWhiteSpace(exceptionDetail))
        {
            var firstLine = exceptionDetail.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? message;
            var colonIndex = firstLine.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 100)
            {
                type = firstLine[..colonIndex].Trim();
                summary = firstLine[(colonIndex + 1)..].Trim();
            }
            else
            {
                summary = firstLine.Trim();
            }
        }
        else if (message.Contains(':'))
        {
            var parts = message.Split(':', 2);
            if (parts[0].Length < 50 && !parts[0].Contains(' '))
            {
                type = parts[0].Trim();
                summary = parts[1].Trim();
            }
        }

        var rawKey = $"{type}:{summary}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var fingerprint = Convert.ToHexString(hashBytes)[..16];

        return (type, summary, fingerprint);
    }
}
