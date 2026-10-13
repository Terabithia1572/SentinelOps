using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.EnvironmentName)
            .HasMaxLength(50);

        builder.Property(a => a.ServiceName)
            .HasMaxLength(100);

        builder.Property(a => a.MetricName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.ConditionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Operator)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        builder.HasIndex(a => a.ApplicationId);
        builder.HasIndex(a => a.IsEnabled);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.LastEvaluatedAtUtc);

        builder.HasMany(a => a.Incidents)
            .WithOne(i => i.AlertRule)
            .HasForeignKey(i => i.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Evaluations)
            .WithOne(e => e.AlertRule)
            .HasForeignKey(e => e.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Events)
            .WithOne(e => e.AlertRule)
            .HasForeignKey(e => e.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.NotificationChannels)
            .WithOne(nc => nc.AlertRule)
            .HasForeignKey(nc => nc.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
