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

        builder.Property(a => a.MetricName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Condition)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(a => a.ApplicationId);
        builder.HasIndex(a => a.IsEnabled);

        builder.HasMany(a => a.Incidents)
            .WithOne(i => i.AlertRule)
            .HasForeignKey(i => i.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
