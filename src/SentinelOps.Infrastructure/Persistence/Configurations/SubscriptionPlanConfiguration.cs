using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public static readonly Guid StarterPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ProPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid EnterprisePlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Tier)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.MonthlyPriceUsd)
            .HasPrecision(18, 2);

        builder.Property(p => p.AnnualPriceUsd)
            .HasPrecision(18, 2);

        builder.HasIndex(p => p.Tier)
            .IsUnique();

        // Seed canonical plans
        builder.HasData(
            new SubscriptionPlan
            {
                Id = StarterPlanId,
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
                AllowDedicatedSupport = false,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SubscriptionPlan
            {
                Id = ProPlanId,
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
                AllowDedicatedSupport = false,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SubscriptionPlan
            {
                Id = EnterprisePlanId,
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
                AllowDedicatedSupport = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
