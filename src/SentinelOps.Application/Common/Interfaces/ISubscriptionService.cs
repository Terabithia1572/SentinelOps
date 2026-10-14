using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Common.Interfaces;

public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default);

    Task<OrganizationSubscriptionDto?> GetSubscriptionAsync(Guid orgId, string userId, CancellationToken ct = default);

    Task<(bool Success, OrganizationSubscriptionDto? Subscription, string? Error)> ChangePlanAsync(
        Guid orgId,
        ChangePlanRequest request,
        string userId,
        CancellationToken ct = default);

    Task<(bool Success, string? Error)> CancelSubscriptionAsync(
        Guid orgId,
        CancelSubscriptionRequest request,
        string userId,
        CancellationToken ct = default);

    Task<(bool Success, string? Error)> ReactivateSubscriptionAsync(
        Guid orgId,
        string userId,
        CancellationToken ct = default);

    Task<(bool Success, string? Error)> ProcessSubscriptionRenewalAsync(
        Guid subscriptionId,
        CancellationToken ct = default);
}
