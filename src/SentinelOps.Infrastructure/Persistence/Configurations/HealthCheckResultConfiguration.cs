using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class HealthCheckResultConfiguration : IEntityTypeConfiguration<HealthCheckResult>
{
    public void Configure(EntityTypeBuilder<HealthCheckResult> builder)
    {
        builder.ToTable("HealthCheckResults");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(h => h.EnvironmentName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(h => h.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(h => h.Description)
            .HasMaxLength(500);

        builder.HasIndex(h => h.ApplicationId);
        builder.HasIndex(h => h.CheckedAtUtc);
        builder.HasIndex(h => new { h.ApplicationId, h.ServiceName, h.EnvironmentName });
    }
}
