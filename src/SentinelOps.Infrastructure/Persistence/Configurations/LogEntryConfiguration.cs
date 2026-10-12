using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("LogEntries");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(l => l.EnvironmentName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Message)
            .IsRequired();

        builder.Property(l => l.MessageTemplate)
            .HasMaxLength(1000);

        builder.Property(l => l.Properties)
            .HasColumnType("jsonb");

        builder.Property(l => l.TraceId)
            .HasMaxLength(64);

        builder.Property(l => l.SpanId)
            .HasMaxLength(64);

        builder.Property(l => l.CorrelationId)
            .HasMaxLength(64);

        builder.HasIndex(l => l.ApplicationId);
        builder.HasIndex(l => l.Severity);
        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => new { l.ApplicationId, l.Timestamp });
        builder.HasIndex(l => l.TraceId);
    }
}
