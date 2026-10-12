using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class MetricRecordConfiguration : IEntityTypeConfiguration<MetricRecord>
{
    public void Configure(EntityTypeBuilder<MetricRecord> builder)
    {
        builder.ToTable("MetricRecords");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.EnvironmentName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.MetricName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.MetricType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.Tags)
            .HasColumnType("jsonb");

        builder.HasIndex(m => m.ApplicationId);
        builder.HasIndex(m => m.MetricName);
        builder.HasIndex(m => m.Timestamp);
        builder.HasIndex(m => new { m.ApplicationId, m.MetricName, m.Timestamp });
    }
}
