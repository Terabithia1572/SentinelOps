using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class TraceRecordConfiguration : IEntityTypeConfiguration<TraceRecord>
{
    public void Configure(EntityTypeBuilder<TraceRecord> builder)
    {
        builder.ToTable("TraceRecords");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TraceId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.EnvironmentName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.RootSpanName)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(t => t.TraceId)
            .IsUnique();

        builder.HasIndex(t => t.ApplicationId);
        builder.HasIndex(t => t.StartedAtUtc);
        builder.HasIndex(t => new { t.ApplicationId, t.StartedAtUtc });
    }
}
