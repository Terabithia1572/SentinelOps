using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IApplicationDbContext context,
        IAuditService auditService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default)
    {
        await EnsureCanonicalPlansAsync(ct);

        var plans = await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPriceUsd)
            .ToListAsync(ct);

        return plans.Select(MapPlanToDto);
    }

    public async Task<OrganizationSubscriptionDto?> GetSubscriptionAsync(Guid orgId, string userId, CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null) return null;

        var sub = await _context.OrganizationSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.Organization)
            .Include(s => s.History.OrderByDescending(h => h.TimestampUtc).Take(20))
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub == null) return null;

        return MapSubscriptionToDto(sub);
    }

    public async Task<(bool Success, OrganizationSubscriptionDto? Subscription, string? Error)> ChangePlanAsync(
        Guid orgId,
        ChangePlanRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null || (member.Role != OrganizationRole.Owner && member.Role != OrganizationRole.Admin))
        {
            return (false, null, "Only an organization Owner or Admin can modify the subscription plan.");
        }

        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Organization)
            .ThenInclude(o => o.Settings)
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub == null)
        {
            return (false, null, "Organization subscription record not found.");
        }

        await EnsureCanonicalPlansAsync(ct);

        var targetTier = request.TargetTier.Trim();
        var targetPlan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Tier.ToLower() == targetTier.ToLower() && p.IsActive, ct);

        if (targetPlan == null)
        {
            return (false, null, $"Plan tier '{request.TargetTier}' does not exist or is inactive.");
        }

        var prevPlan = sub.Plan;
        var prevTier = prevPlan?.Tier ?? "Unknown";

        // Update Subscription plan reference and interval
        var oldPlanId = sub.PlanId;
        sub.PlanId = targetPlan.Id;
        sub.Plan = targetPlan;

        if (Enum.TryParse<BillingInterval>(request.BillingInterval, true, out var interval))
        {
            sub.BillingInterval = interval;
        }

        // Apply new limits to OrganizationSettings immediately
        if (sub.Organization.Settings != null)
        {
            sub.Organization.Settings.PlanTier = targetPlan.Tier;
            sub.Organization.Settings.MaxApplications = targetPlan.MaxApplications;
            sub.Organization.Settings.MaxMembers = targetPlan.MaxMembers;
            sub.Organization.Settings.MaxCustomDashboards = targetPlan.MaxCustomDashboards;
            sub.Organization.Settings.MaxMonthlyLogs = targetPlan.MaxMonthlyLogs;
            sub.Organization.Settings.MaxMonthlyTraces = targetPlan.MaxMonthlyTraces;
            sub.Organization.Settings.DefaultRetentionDays = targetPlan.RetentionDays;
            sub.Organization.Settings.MaxMonthlyAiTokens = targetPlan.MaxMonthlyAiTokens;
        }

        // If currently in trial and upgrading to a paid plan, mark Active if Immediate
        if (sub.Status == SubscriptionStatus.Trialing && targetPlan.MonthlyPriceUsd > 0 && request.Immediate)
        {
            sub.TransitionTo(SubscriptionStatus.Active, $"Upgraded from {prevTier} to {targetPlan.Tier}", userId);
        }
        else
        {
            // Log history
            sub.History.Add(new SubscriptionHistory
            {
                SubscriptionId = sub.Id,
                OrganizationId = orgId,
                PreviousStatus = sub.Status,
                NewStatus = sub.Status,
                PreviousPlanId = oldPlanId,
                NewPlanId = targetPlan.Id,
                Reason = $"Plan changed from {prevTier} to {targetPlan.Tier} ({sub.BillingInterval})",
                ChangedByUserId = userId,
                TimestampUtc = DateTime.UtcNow
            });
            sub.ConcurrencyToken = Guid.NewGuid().ToString("N");
            sub.UpdatedAtUtc = DateTime.UtcNow;
        }

        TrackSubscriptionHistoriesAsAdded(sub);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Subscription.PlanChanged",
            targetEntityType: "OrganizationSubscription",
            targetEntityId: sub.Id.ToString(),
            outcome: "Success",
            details: $"Organization '{sub.Organization.Name}' changed plan from {prevTier} to {targetPlan.Tier}.",
            cancellationToken: ct
        );

        return (true, MapSubscriptionToDto(sub), null);
    }

    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(
        Guid orgId,
        CancelSubscriptionRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null || (member.Role != OrganizationRole.Owner && member.Role != OrganizationRole.Admin))
        {
            return (false, "Only an organization Owner or Admin can cancel the subscription.");
        }

        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.Organization)
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub == null) return (false, "Subscription not found.");

        if (request.Immediate)
        {
            sub.TransitionTo(SubscriptionStatus.Canceled, $"Immediate cancellation requested: {request.Reason}", userId);
            sub.CancelAtPeriodEnd = false;
        }
        else
        {
            sub.CancelAtPeriodEnd = true;
            sub.History.Add(new SubscriptionHistory
            {
                SubscriptionId = sub.Id,
                OrganizationId = orgId,
                PreviousStatus = sub.Status,
                NewStatus = sub.Status,
                NewPlanId = sub.PlanId,
                Reason = $"Scheduled cancellation at period end ({sub.CurrentPeriodEndUtc:u}): {request.Reason}",
                ChangedByUserId = userId,
                TimestampUtc = DateTime.UtcNow
            });
            sub.ConcurrencyToken = Guid.NewGuid().ToString("N");
            sub.UpdatedAtUtc = DateTime.UtcNow;
        }

        TrackSubscriptionHistoriesAsAdded(sub);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Subscription.Canceled",
            targetEntityType: "OrganizationSubscription",
            targetEntityId: sub.Id.ToString(),
            outcome: "Success",
            details: $"Organization '{sub.Organization.Name}' subscription canceled (Immediate: {request.Immediate}, Reason: {request.Reason}).",
            cancellationToken: ct
        );

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReactivateSubscriptionAsync(
        Guid orgId,
        string userId,
        CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null || (member.Role != OrganizationRole.Owner && member.Role != OrganizationRole.Admin))
        {
            return (false, "Only an organization Owner or Admin can reactivate the subscription.");
        }

        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.Organization)
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub == null) return (false, "Subscription not found.");

        if (sub.CancelAtPeriodEnd)
        {
            sub.CancelAtPeriodEnd = false;
            sub.History.Add(new SubscriptionHistory
            {
                SubscriptionId = sub.Id,
                OrganizationId = orgId,
                PreviousStatus = sub.Status,
                NewStatus = sub.Status,
                NewPlanId = sub.PlanId,
                Reason = "Cancellation revoked by customer.",
                ChangedByUserId = userId,
                TimestampUtc = DateTime.UtcNow
            });
            sub.ConcurrencyToken = Guid.NewGuid().ToString("N");
            sub.UpdatedAtUtc = DateTime.UtcNow;
        }
        else if (sub.Status == SubscriptionStatus.Canceled || sub.Status == SubscriptionStatus.Suspended)
        {
            sub.TransitionTo(SubscriptionStatus.Active, "Customer reactivated subscription.", userId);
            sub.CanceledAtUtc = null;
        }

        TrackSubscriptionHistoriesAsAdded(sub);
        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Subscription.Reactivated",
            targetEntityType: "OrganizationSubscription",
            targetEntityId: sub.Id.ToString(),
            outcome: "Success",
            details: $"Organization '{sub.Organization.Name}' subscription reactivated.",
            cancellationToken: ct
        );

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ProcessSubscriptionRenewalAsync(
        Guid subscriptionId,
        CancellationToken ct = default)
    {
        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Organization)
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);

        if (sub == null) return (false, "Subscription not found.");

        if (sub.CancelAtPeriodEnd)
        {
            sub.TransitionTo(SubscriptionStatus.Canceled, "Scheduled cancellation executed at billing period end.");
            sub.CancelAtPeriodEnd = false;
            TrackSubscriptionHistoriesAsAdded(sub);
            await _context.SaveChangesAsync(ct);
            return (true, null);
        }

        // Advance billing period
        sub.CurrentPeriodStartUtc = sub.CurrentPeriodEndUtc;
        sub.CurrentPeriodEndUtc = sub.BillingInterval == BillingInterval.Annual
            ? sub.CurrentPeriodEndUtc.AddYears(1)
            : sub.CurrentPeriodEndUtc.AddMonths(1);

        sub.History.Add(new SubscriptionHistory
        {
            SubscriptionId = sub.Id,
            OrganizationId = sub.OrganizationId,
            PreviousStatus = sub.Status,
            NewStatus = sub.Status,
            NewPlanId = sub.PlanId,
            Reason = $"Subscription cycle renewed: {sub.CurrentPeriodStartUtc:yyyy-MM-dd} to {sub.CurrentPeriodEndUtc:yyyy-MM-dd}",
            TimestampUtc = DateTime.UtcNow
        });

        sub.ConcurrencyToken = Guid.NewGuid().ToString("N");
        sub.UpdatedAtUtc = DateTime.UtcNow;

        TrackSubscriptionHistoriesAsAdded(sub);
        await _context.SaveChangesAsync(ct);
        return (true, null);
    }

    private static SubscriptionPlanDto MapPlanToDto(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Tier = plan.Tier,
        Name = plan.Name,
        Description = plan.Description,
        MonthlyPriceUsd = plan.MonthlyPriceUsd,
        AnnualPriceUsd = plan.AnnualPriceUsd,
        MaxApplications = plan.MaxApplications,
        MaxMembers = plan.MaxMembers,
        MaxCustomDashboards = plan.MaxCustomDashboards,
        MaxMonthlyLogs = plan.MaxMonthlyLogs,
        MaxMonthlyTraces = plan.MaxMonthlyTraces,
        MaxMonthlyMetrics = plan.MaxMonthlyMetrics,
        RetentionDays = plan.RetentionDays,
        MaxMonthlyAiTokens = plan.MaxMonthlyAiTokens,
        AllowCustomReports = plan.AllowCustomReports,
        AllowSloAlerts = plan.AllowSloAlerts,
        AllowApiAccess = plan.AllowApiAccess,
        AllowDedicatedSupport = plan.AllowDedicatedSupport
    };

    private static OrganizationSubscriptionDto MapSubscriptionToDto(OrganizationSubscription sub) => new()
    {
        Id = sub.Id,
        OrganizationId = sub.OrganizationId,
        OrganizationName = sub.Organization?.Name ?? string.Empty,
        PlanId = sub.PlanId,
        PlanTier = sub.Plan?.Tier ?? "Starter",
        PlanName = sub.Plan?.Name ?? "Starter Plan",
        Status = sub.Status.ToString(),
        BillingInterval = sub.BillingInterval.ToString(),
        MonthlyPriceUsd = sub.Plan?.MonthlyPriceUsd ?? 0m,
        CurrentPeriodStartUtc = sub.CurrentPeriodStartUtc,
        CurrentPeriodEndUtc = sub.CurrentPeriodEndUtc,
        TrialEndsAtUtc = sub.TrialEndsAtUtc,
        GracePeriodEndsAtUtc = sub.GracePeriodEndsAtUtc,
        CanceledAtUtc = sub.CanceledAtUtc,
        CancelAtPeriodEnd = sub.CancelAtPeriodEnd,
        ConcurrencyToken = sub.ConcurrencyToken,
        History = sub.History.Select(h => new SubscriptionHistoryDto
        {
            Id = h.Id,
            PreviousStatus = h.PreviousStatus?.ToString(),
            NewStatus = h.NewStatus.ToString(),
            Reason = h.Reason,
            ChangedByUserId = h.ChangedByUserId,
            TimestampUtc = h.TimestampUtc
        }).ToList()
    };

    private async Task EnsureCanonicalPlansAsync(CancellationToken ct = default)
    {
        var existing = await _context.SubscriptionPlans.Select(p => p.Tier.ToLower()).ToListAsync(ct);

        if (!existing.Contains("starter"))
        {
            _context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Tier = "Starter",
                Name = "Starter Plan",
                Description = "Essential telemetry and observability for small teams and experimental services.",
                IsActive = true,
                MonthlyPriceUsd = 0m,
                AnnualPriceUsd = 0m,
                MaxApplications = 3,
                MaxMembers = 3,
                MaxCustomDashboards = 3,
                MaxMonthlyLogs = 100_000,
                MaxMonthlyTraces = 50_000,
                MaxMonthlyMetrics = 100_000,
                RetentionDays = 7,
                MaxMonthlyAiTokens = 10_000,
                AllowCustomReports = false,
                AllowSloAlerts = false,
                AllowApiAccess = true,
                AllowDedicatedSupport = false
            });
        }

        if (!existing.Contains("pro"))
        {
            _context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Tier = "Pro",
                Name = "Professional Plan",
                Description = "Full-featured observability, distributed tracing, and statistical anomaly detection for growing engineering organizations.",
                IsActive = true,
                MonthlyPriceUsd = 49m,
                AnnualPriceUsd = 490m,
                MaxApplications = 20,
                MaxMembers = 25,
                MaxCustomDashboards = 15,
                MaxMonthlyLogs = 1_000_000,
                MaxMonthlyTraces = 500_000,
                MaxMonthlyMetrics = 1_000_000,
                RetentionDays = 30,
                MaxMonthlyAiTokens = 100_000,
                AllowCustomReports = true,
                AllowSloAlerts = true,
                AllowApiAccess = true,
                AllowDedicatedSupport = false
            });
        }

        if (!existing.Contains("enterprise"))
        {
            _context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Tier = "Enterprise",
                Name = "Enterprise Plan",
                Description = "High-scale multi-cluster observability, predictive operations, long-term retention, and dedicated SLA.",
                IsActive = true,
                MonthlyPriceUsd = 249m,
                AnnualPriceUsd = 2490m,
                MaxApplications = 100,
                MaxMembers = 100,
                MaxCustomDashboards = 50,
                MaxMonthlyLogs = 10_000_000,
                MaxMonthlyTraces = 5_000_000,
                MaxMonthlyMetrics = 10_000_000,
                RetentionDays = 90,
                MaxMonthlyAiTokens = 1_000_000,
                AllowCustomReports = true,
                AllowSloAlerts = true,
                AllowApiAccess = true,
                AllowDedicatedSupport = true
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    private void TrackSubscriptionHistoriesAsAdded(OrganizationSubscription sub)
    {
        if (_context is DbContext db)
        {
            foreach (var h in sub.History)
            {
                var entry = db.Entry(h);
                if (entry.State == EntityState.Modified || entry.State == EntityState.Detached)
                {
                    entry.State = EntityState.Added;
                }
            }
        }
    }
}
