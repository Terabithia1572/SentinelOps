using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services;

public class SavedLogQueryService : ISavedLogQueryService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;

    public SavedLogQueryService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
    }

    private async Task<Guid> ResolveOrganizationIdAsync(CancellationToken ct)
    {
        if (_currentTenantService.OrganizationId.HasValue && _currentTenantService.OrganizationId.Value != Guid.Empty)
        {
            return _currentTenantService.OrganizationId.Value;
        }

        var userId = _currentUserService.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            var userOrgId = await _context.OrganizationMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.OrganizationId)
                .FirstOrDefaultAsync(ct);

            if (userOrgId != Guid.Empty) return userOrgId;
        }

        var defaultOrg = await _context.Organizations
            .Where(o => o.Slug == "default-org")
            .Select(o => o.Id)
            .FirstOrDefaultAsync(ct);

        if (defaultOrg != Guid.Empty) return defaultOrg;

        var firstOrg = await _context.Organizations
            .Select(o => o.Id)
            .FirstOrDefaultAsync(ct);

        return firstOrg != Guid.Empty ? firstOrg : Guid.NewGuid();
    }

    public async Task<List<SavedLogQueryDto>> GetSavedQueriesAsync(Guid? applicationId = null, CancellationToken ct = default)
    {
        var orgId = await ResolveOrganizationIdAsync(ct);
        var currentUserId = _currentUserService.UserId;

        var query = _context.SavedLogQueries
            .AsNoTracking()
            .Include(q => q.Application)
            .Where(q => q.OrganizationId == orgId);

        if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            query = query.Where(q => q.ApplicationId == applicationId.Value);
        }

        // Shared or created by user
        if (!string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(q => q.IsShared || q.CreatedByUserId == currentUserId);
        }

        var list = await query
            .OrderByDescending(q => q.UpdatedAtUtc ?? q.CreatedAtUtc)
            .ToListAsync(ct);

        return list.Select(q => q.ToDto()).ToList();
    }

    public async Task<SavedLogQueryDto?> GetSavedQueryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = await ResolveOrganizationIdAsync(ct);

        var query = await _context.SavedLogQueries
            .AsNoTracking()
            .Include(q => q.Application)
            .FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == orgId, ct);

        return query?.ToDto();
    }

    public async Task<SavedLogQueryDto> CreateSavedQueryAsync(CreateSavedLogQueryRequest request, CancellationToken ct = default)
    {
        var orgId = await ResolveOrganizationIdAsync(ct);

        if (request.ApplicationId.HasValue)
        {
            var isAppInOrg = await _context.Applications
                .AnyAsync(a => a.Id == request.ApplicationId.Value && a.OrganizationId == orgId, ct);
            if (!isAppInOrg)
            {
                throw new InvalidOperationException("Selected application does not belong to the current organization.");
            }
        }

        var tagsJson = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : "[]";

        var saved = new SavedLogQuery(
            organizationId: orgId,
            name: request.Name.Trim(),
            description: request.Description?.Trim(),
            applicationId: request.ApplicationId,
            searchTerm: request.SearchTerm?.Trim(),
            severity: request.Severity?.Trim(),
            serviceName: request.ServiceName?.Trim(),
            environmentName: request.EnvironmentName?.Trim(),
            traceId: request.TraceId?.Trim(),
            propertyFiltersJson: request.PropertyFiltersJson?.Trim(),
            timeRange: string.IsNullOrWhiteSpace(request.TimeRange) ? "1h" : request.TimeRange.Trim(),
            createdByUserId: _currentUserService.UserId,
            createdByUserName: _currentUserService.UserName,
            isShared: request.IsShared,
            tagsJson: tagsJson);

        _context.SavedLogQueries.Add(saved);
        await _context.SaveChangesAsync(ct);

        return saved.ToDto();
    }

    public async Task<SavedLogQueryDto?> UpdateSavedQueryAsync(Guid id, UpdateSavedLogQueryRequest request, CancellationToken ct = default)
    {
        var orgId = await ResolveOrganizationIdAsync(ct);

        var saved = await _context.SavedLogQueries
            .Include(q => q.Application)
            .FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == orgId, ct);

        if (saved == null) return null;

        saved.Name = request.Name.Trim();
        saved.Description = request.Description?.Trim();
        saved.SearchTerm = request.SearchTerm?.Trim();
        saved.Severity = request.Severity?.Trim();
        saved.ServiceName = request.ServiceName?.Trim();
        saved.EnvironmentName = request.EnvironmentName?.Trim();
        saved.TraceId = request.TraceId?.Trim();
        saved.PropertyFiltersJson = request.PropertyFiltersJson?.Trim();
        saved.TimeRange = string.IsNullOrWhiteSpace(request.TimeRange) ? "1h" : request.TimeRange.Trim();
        saved.IsShared = request.IsShared;

        if (request.Tags != null)
        {
            saved.TagsJson = JsonSerializer.Serialize(request.Tags);
        }

        await _context.SaveChangesAsync(ct);
        return saved.ToDto();
    }

    public async Task<bool> DeleteSavedQueryAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = await ResolveOrganizationIdAsync(ct);

        var saved = await _context.SavedLogQueries
            .FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == orgId, ct);

        if (saved == null) return false;

        _context.SavedLogQueries.Remove(saved);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportLogsAsync(ExportLogsRequest request, CancellationToken ct = default)
    {
        var orgId = await ResolveOrganizationIdAsync(ct);
        var query = _context.LogEntries.AsNoTracking();

        if (request.ApplicationId.HasValue)
        {
            var isAppInOrg = await _context.Applications
                .AnyAsync(a => a.Id == request.ApplicationId.Value && a.OrganizationId == orgId, ct);
            if (!isAppInOrg)
            {
                throw new InvalidOperationException("Application access denied.");
            }
            query = query.Where(l => l.ApplicationId == request.ApplicationId.Value);
        }
        else
        {
            var orgAppIds = _context.Applications
                .Where(a => a.OrganizationId == orgId)
                .Select(a => a.Id);
            query = query.Where(l => orgAppIds.Contains(l.ApplicationId));
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
            query = query.Where(l => l.ServiceName == request.ServiceName);

        if (!string.IsNullOrWhiteSpace(request.EnvironmentName))
            query = query.Where(l => l.EnvironmentName == request.EnvironmentName);

        if (!string.IsNullOrWhiteSpace(request.Severity) && Enum.TryParse<Severity>(request.Severity, true, out var parsedSeverity))
            query = query.Where(l => l.Severity == parsedSeverity);

        if (!string.IsNullOrWhiteSpace(request.TraceId))
            query = query.Where(l => l.TraceId == request.TraceId);

        if (request.FromUtc.HasValue)
            query = query.Where(l => l.Timestamp >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(l => l.Timestamp <= request.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(l => l.Message.Contains(term) ||
                                     l.MessageTemplate.Contains(term) ||
                                     (l.ExceptionDetail != null && l.ExceptionDetail.Contains(term)) ||
                                     l.Properties.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.PropertyFilter))
        {
            var filter = request.PropertyFilter.Trim();
            if (filter.Contains('='))
            {
                var parts = filter.Split('=', 2);
                var key = parts[0].Trim();
                var val = parts[1].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                {
                    query = query.Where(l => l.Properties.Contains(key) && l.Properties.Contains(val));
                }
            }
            else
            {
                query = query.Where(l => l.Properties.Contains(filter));
            }
        }

        var limit = Math.Clamp(request.MaxRecords, 1, 5000);
        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync(ct);

        var timestampStr = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        if (string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Severity,ServiceName,EnvironmentName,Message,TraceId,SpanId");

            foreach (var l in logs)
            {
                var escapedMessage = "\"" + l.Message.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
                sb.AppendLine($"{l.Timestamp:O},{l.Severity},{l.ServiceName},{l.EnvironmentName},{escapedMessage},{l.TraceId ?? string.Empty},{l.SpanId ?? string.Empty}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return (bytes, "text/csv", $"sentinelops_logs_{timestampStr}.csv");
        }
        else
        {
            var dtos = logs.Select(l => l.ToDto()).ToList();
            var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(json);
            return (bytes, "application/json", $"sentinelops_logs_{timestampStr}.json");
        }
    }
}
