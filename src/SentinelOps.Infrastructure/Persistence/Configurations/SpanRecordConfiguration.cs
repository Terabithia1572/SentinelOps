using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class SpanRecordConfiguration : IEntityTypeConfiguration<SpanRecord>
{
    public void Configure(EntityTypeBuilder<SpanRecord> builder)
    {
        builder.ToTable("SpanRecords");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SpanId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.TraceId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.ParentSpanId)
            .HasMaxLength(64);

        builder.Property(s => s.OperationName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(s => s.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Attributes)
            .HasColumnType("jsonb");

        builder.HasIndex(s => s.TraceId);
        builder.HasIndex(s => s.SpanId);
        builder.HasIndex(s => s.ParentSpanId);
    }
}
