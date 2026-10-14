using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class LogPatternConfiguration : IEntityTypeConfiguration<LogPattern>
{
    public void Configure(EntityTypeBuilder<LogPattern> builder)
    {
        builder.ToTable("LogPatterns");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ServiceName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.PatternFingerprint)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.Template)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.SampleMessage)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(p => p.SampleTraceId)
            .HasMaxLength(64);

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => new { p.ApplicationId, p.PatternFingerprint }).IsUnique();
        builder.HasIndex(p => new { p.OrganizationId, p.LastSeenAtUtc });
    }
}
