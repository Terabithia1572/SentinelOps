using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class PredictiveForecastConfiguration : IEntityTypeConfiguration<PredictiveForecast>
{
    public void Configure(EntityTypeBuilder<PredictiveForecast> builder)
    {
        builder.ToTable("PredictiveForecasts");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(f => f.MetricName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.CalculationMethod)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(f => f.OrganizationId);
        builder.HasIndex(f => new { f.ApplicationId, f.MetricName });
        builder.HasIndex(f => new { f.OrganizationId, f.ExpiresAtUtc });
    }
}
