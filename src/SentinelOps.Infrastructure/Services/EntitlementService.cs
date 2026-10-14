using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services;

public class EntitlementService : IEntitlementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EntitlementService> _logger;

    public EntitlementService(
        IApplicationDbContext context,
        ILogger<EntitlementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Allowed, string? Reason)> CheckFeatureAccessAsync(
        Guid orgId,
        EntitlementFeature feature,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Subscription)
            .ThenInclude(s => s!.Plan)
            .Include(o => o.Settings)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        if (org == null)
        {
            return (false, "Organization not found.");
        }

        var sub = org.Subscription;
        var plan = sub?.Plan;
        var settings = org.Settings ?? new OrganizationSettings { OrganizationId = orgId };

        // 1. Subscription Status Gating
        if (sub != null)
        {
            if (sub.Status == SubscriptionStatus.Canceled)
            {
                return (false, "Subscription is canceled. Reactivate your subscription to access this feature.");
            }
            if (sub.Status == SubscriptionStatus.Suspended)
            {
                return (false, "Subscription is suspended due to billing delinquency. Settle outstanding invoices to restore access.");
            }
            if (sub.Status == SubscriptionStatus.Expired)
            {
                return (false, "Subscription trial has expired. Upgrade to a paid plan to restore access.");
            }
            if (sub.Status == SubscriptionStatus.PastDue)
            {
                // In PastDue status, block new resource creation and AI inference
                if (feature is EntitlementFeature.RegisterApplication or EntitlementFeature.InviteMember or EntitlementFeature.CreateDashboard or EntitlementFeature.ExecuteAiInference)
                {
                    return (false, "Payment past due. Resource creation and AI operations are paused until billing is restored.");
                }
            }
        }

        // 2. Feature / Quota Gating
        var usage = await GetOrInitUsageAsync(orgId, ct);

        switch (feature)
        {
            case EntitlementFeature.RegisterApplication:
                var currentApps = await _context.Applications.CountAsync(a => a.OrganizationId == orgId && a.IsActive, ct);
                if (settings.MaxApplications > 0 && currentApps >= settings.MaxApplications)
                {
                    return (false, $"Application limit reached ({currentApps}/{settings.MaxApplications}) for the {settings.PlanTier} plan. Upgrade your plan to add more applications.");
                }
                break;

            case EntitlementFeature.InviteMember:
                var currentMembers = await _context.OrganizationMembers.CountAsync(m => m.OrganizationId == orgId, ct);
                var pendingInvites = await _context.OrganizationInvitations.CountAsync(i => i.OrganizationId == orgId && i.AcceptedAtUtc == null && i.RevokedAtUtc == null, ct);
                if (settings.MaxMembers > 0 && (currentMembers + pendingInvites) >= settings.MaxMembers)
                {
                    return (false, $"Member seat limit reached ({currentMembers + pendingInvites}/{settings.MaxMembers}) for the {settings.PlanTier} plan. Upgrade your plan to invite more team members.");
                }
                break;

            case EntitlementFeature.CreateDashboard:
                var currentDashboards = await _context.Dashboards.CountAsync(d => d.OrganizationId == orgId, ct);
                if (settings.MaxCustomDashboards > 0 && currentDashboards >= settings.MaxCustomDashboards)
                {
                    return (false, $"Custom dashboard limit reached ({currentDashboards}/{settings.MaxCustomDashboards}) for the {settings.PlanTier} plan. Upgrade your plan to create more dashboards.");
                }
                break;

            case EntitlementFeature.IngestLogs:
                if (settings.MaxMonthlyLogs > 0 && usage.LogsIngestedMonthToDate >= settings.MaxMonthlyLogs)
                {
                    return (false, $"Monthly log ingestion quota ({settings.MaxMonthlyLogs:N0}) reached for billing cycle.");
                }
                break;

            case EntitlementFeature.IngestTraces:
                if (settings.MaxMonthlyTraces > 0 && usage.TracesIngestedMonthToDate >= settings.MaxMonthlyTraces)
                {
                    return (false, $"Monthly trace ingestion quota ({settings.MaxMonthlyTraces:N0}) reached for billing cycle.");
                }
                break;

            case EntitlementFeature.IngestMetrics:
                var maxMetrics = plan?.MaxMonthlyMetrics ?? 100_000;
                if (maxMetrics > 0 && usage.MetricsIngestedMonthToDate >= maxMetrics)
                {
                    return (false, $"Monthly metric ingestion quota ({maxMetrics:N0}) reached for billing cycle.");
                }
                break;

            case EntitlementFeature.ExecuteAiInference:
                if (settings.MaxMonthlyAiTokens > 0 && usage.AiTokensUsedMonthToDate >= settings.MaxMonthlyAiTokens)
                {
                    return (false, $"Monthly AI token budget ({settings.MaxMonthlyAiTokens:N0}) reached for billing cycle.");
                }
                break;

            case EntitlementFeature.CustomReports:
                if (plan != null && !plan.AllowCustomReports)
                {
                    return (false, "Custom reporting is only available on Pro and Enterprise tiers.");
                }
                break;

            case EntitlementFeature.SloAlerts:
                if (plan != null && !plan.AllowSloAlerts)
                {
                    return (false, "SLO alerting is only available on Pro and Enterprise tiers.");
                }
                break;

            case EntitlementFeature.ApiAccess:
                if (plan != null && !plan.AllowApiAccess)
                {
                    return (false, "API access is disabled for this plan tier.");
                }
                break;

            case EntitlementFeature.DedicatedSupport:
                if (plan != null && !plan.AllowDedicatedSupport)
                {
                    return (false, "Dedicated support is reserved for Enterprise tiers.");
                }
                break;
        }

        return (true, null);
    }

    public async Task<TenantEntitlementsDto> GetEntitlementsAsync(
        Guid orgId,
        string userId,
        CancellationToken ct = default)
    {
        var isMember = await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (!isMember)
        {
            return new TenantEntitlementsDto
            {
                OrganizationId = orgId,
                IsSubscriptionActive = false,
                InactiveReasons = ["User does not belong to this organization."]
            };
        }

        var org = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Subscription)
            .ThenInclude(s => s!.Plan)
            .Include(o => o.Settings)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        var sub = org?.Subscription;
        var plan = sub?.Plan;
        var tier = plan?.Tier ?? org?.Settings?.PlanTier ?? "Starter";
        var status = sub?.Status.ToString() ?? "Active";
        var isActive = sub == null || sub.Status == SubscriptionStatus.Active || sub.Status == SubscriptionStatus.Trialing;

        var daysRemaining = sub != null ? Math.Max(0, (int)Math.Ceiling((sub.CurrentPeriodEndUtc - DateTime.UtcNow).TotalDays)) : 30;

        var (canApp, _) = await CheckFeatureAccessAsync(orgId, EntitlementFeature.RegisterApplication, ct);
        var (canMember, _) = await CheckFeatureAccessAsync(orgId, EntitlementFeature.InviteMember, ct);
        var (canDash, _) = await CheckFeatureAccessAsync(orgId, EntitlementFeature.CreateDashboard, ct);
        var (canLog, _) = await CheckFeatureAccessAsync(orgId, EntitlementFeature.IngestLogs, ct);
        var (canAi, _) = await CheckFeatureAccessAsync(orgId, EntitlementFeature.ExecuteAiInference, ct);

        var inactiveReasons = new List<string>();
        if (!isActive)
        {
            inactiveReasons.Add($"Subscription is {status}.");
        }

        return new TenantEntitlementsDto
        {
            OrganizationId = orgId,
            PlanTier = tier,
            SubscriptionStatus = status,
            IsSubscriptionActive = isActive,
            DaysUntilPeriodEnd = daysRemaining,
            CanRegisterApplication = canApp,
            CanInviteMember = canMember,
            CanCreateDashboard = canDash,
            CanIngestTelemetry = canLog,
            CanExecuteAi = canAi,
            AllowCustomReports = plan?.AllowCustomReports ?? false,
            AllowSloAlerts = plan?.AllowSloAlerts ?? false,
            AllowApiAccess = plan?.AllowApiAccess ?? true,
            AllowDedicatedSupport = plan?.AllowDedicatedSupport ?? false,
            InactiveReasons = inactiveReasons
        };
    }

    public async Task<TenantUsageSummaryDto> GetUsageSummaryAsync(
        Guid orgId,
        string userId,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Subscription)
            .ThenInclude(s => s!.Plan)
            .Include(o => o.Settings)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        var sub = org?.Subscription;
        var plan = sub?.Plan;
        var settings = org?.Settings ?? new OrganizationSettings { OrganizationId = orgId };
        var usage = await GetOrInitUsageAsync(orgId, ct);

        var appsUsed = await _context.Applications.CountAsync(a => a.OrganizationId == orgId && a.IsActive, ct);
        var membersUsed = await _context.OrganizationMembers.CountAsync(m => m.OrganizationId == orgId, ct);
        var dashUsed = await _context.Dashboards.CountAsync(d => d.OrganizationId == orgId, ct);

        var periodStart = sub?.CurrentPeriodStartUtc ?? usage.LastResetAtUtc;
        var periodEnd = sub?.CurrentPeriodEndUtc ?? usage.LastResetAtUtc.AddMonths(1);
        var daysRemaining = Math.Max(0, (int)Math.Ceiling((periodEnd - DateTime.UtcNow).TotalDays));

        var logsQuota = Math.Max(settings.MaxMonthlyLogs, 1);
        var tracesQuota = Math.Max(settings.MaxMonthlyTraces, 1);
        var metricsQuota = Math.Max(plan?.MaxMonthlyMetrics ?? 100_000, 1);
        var aiQuota = Math.Max(settings.MaxMonthlyAiTokens, 1);
        var appsQuota = Math.Max(settings.MaxApplications, 1);
        var membersQuota = Math.Max(settings.MaxMembers, 1);
        var dashQuota = Math.Max(settings.MaxCustomDashboards, 1);

        return new TenantUsageSummaryDto
        {
            OrganizationId = orgId,
            PlanTier = plan?.Tier ?? settings.PlanTier,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            DaysRemainingInPeriod = daysRemaining,

            LogsUsed = usage.LogsIngestedMonthToDate,
            LogsQuota = settings.MaxMonthlyLogs,
            LogsPercent = Math.Round((double)usage.LogsIngestedMonthToDate / logsQuota * 100.0, 1),

            TracesUsed = usage.TracesIngestedMonthToDate,
            TracesQuota = settings.MaxMonthlyTraces,
            TracesPercent = Math.Round((double)usage.TracesIngestedMonthToDate / tracesQuota * 100.0, 1),

            MetricsUsed = usage.MetricsIngestedMonthToDate,
            MetricsQuota = plan?.MaxMonthlyMetrics ?? 100_000,
            MetricsPercent = Math.Round((double)usage.MetricsIngestedMonthToDate / metricsQuota * 100.0, 1),

            AiTokensUsed = usage.AiTokensUsedMonthToDate,
            AiTokensQuota = settings.MaxMonthlyAiTokens,
            AiTokensPercent = Math.Round((double)usage.AiTokensUsedMonthToDate / aiQuota * 100.0, 1),

            ApplicationsUsed = appsUsed,
            ApplicationsQuota = settings.MaxApplications,
            ApplicationsPercent = Math.Round((double)appsUsed / appsQuota * 100.0, 1),

            MembersUsed = membersUsed,
            MembersQuota = settings.MaxMembers,
            MembersPercent = Math.Round((double)membersUsed / membersQuota * 100.0, 1),

            DashboardsUsed = dashUsed,
            DashboardsQuota = settings.MaxCustomDashboards,
            DashboardsPercent = Math.Round((double)dashUsed / dashQuota * 100.0, 1)
        };
    }

    public async Task RecordUsageAsync(
        Guid orgId,
        MeteredMetric metric,
        long quantity,
        CancellationToken ct = default)
    {
        var usage = await _context.OrganizationUsages
            .FirstOrDefaultAsync(u => u.OrganizationId == orgId, ct);

        if (usage == null)
        {
            usage = new OrganizationUsage
            {
                OrganizationId = orgId,
                LastResetAtUtc = DateTime.UtcNow
            };
            _context.OrganizationUsages.Add(usage);
        }

        // Check Monthly Period Rollover
        var now = DateTime.UtcNow;
        if (now.Year != usage.LastResetAtUtc.Year || now.Month != usage.LastResetAtUtc.Month)
        {
            usage.LogsIngestedMonthToDate = 0;
            usage.TracesIngestedMonthToDate = 0;
            usage.MetricsIngestedMonthToDate = 0;
            usage.ApiRequestsMonthToDate = 0;
            usage.AiTokensUsedMonthToDate = 0;
            usage.AiInvocationsMonthToDate = 0;
            usage.LastResetAtUtc = now;
        }

        switch (metric)
        {
            case MeteredMetric.Logs:
                usage.LogsIngestedMonthToDate += quantity;
                break;
            case MeteredMetric.Traces:
                usage.TracesIngestedMonthToDate += quantity;
                break;
            case MeteredMetric.Metrics:
                usage.MetricsIngestedMonthToDate += quantity;
                break;
            case MeteredMetric.AiTokens:
                usage.AiTokensUsedMonthToDate += quantity;
                usage.AiInvocationsMonthToDate += 1;
                break;
            case MeteredMetric.ApiRequests:
                usage.ApiRequestsMonthToDate += quantity;
                break;
        }

        usage.UpdatedAtUtc = now;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<OrganizationUsage> GetOrInitUsageAsync(Guid orgId, CancellationToken ct)
    {
        var usage = await _context.OrganizationUsages
            .FirstOrDefaultAsync(u => u.OrganizationId == orgId, ct);

        if (usage == null)
        {
            usage = new OrganizationUsage
            {
                OrganizationId = orgId,
                LastResetAtUtc = DateTime.UtcNow
            };
            _context.OrganizationUsages.Add(usage);
            await _context.SaveChangesAsync(ct);
        }

        return usage;
    }
}
