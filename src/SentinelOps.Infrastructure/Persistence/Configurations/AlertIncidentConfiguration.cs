using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class AlertIncidentConfiguration : IEntityTypeConfiguration<AlertIncident>
{
    public void Configure(EntityTypeBuilder<AlertIncident> builder)
    {
        builder.ToTable("AlertIncidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(i => i.AcknowledgedBy)
            .HasMaxLength(150);

        builder.Property(i => i.Details)
            .HasMaxLength(1000);

        builder.HasIndex(i => i.AlertRuleId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.FiredAtUtc);
    }
}
