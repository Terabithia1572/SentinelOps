using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class TelemetryAnomalyConfiguration : IEntityTypeConfiguration<TelemetryAnomaly>
{
    public void Configure(EntityTypeBuilder<TelemetryAnomaly> builder)
    {
        builder.ToTable("TelemetryAnomalies");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.SignalType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.DetectionMethod)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Explanation)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(a => a.ResolvedBy)
            .HasMaxLength(100);

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.ApplicationId, a.Status });
        builder.HasIndex(a => new { a.OrganizationId, a.DetectionTimestamp });
    }
}
