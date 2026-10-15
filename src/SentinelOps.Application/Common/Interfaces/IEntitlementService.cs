using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IEntitlementService
{
    Task<(bool Allowed, string? Reason)> CheckFeatureAccessAsync(
        Guid orgId,
        EntitlementFeature feature,
        CancellationToken ct = default);

    Task<TenantEntitlementsDto?> GetEntitlementsAsync(
        Guid orgId,
        string userId,
        CancellationToken ct = default);

    Task<TenantUsageSummaryDto?> GetUsageSummaryAsync(
        Guid orgId,
        string userId,
        CancellationToken ct = default);

    Task RecordUsageAsync(
        Guid orgId,
        MeteredMetric metric,
        long quantity,
        CancellationToken ct = default);
}
