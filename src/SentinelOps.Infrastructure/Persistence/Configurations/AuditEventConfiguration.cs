using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .HasMaxLength(100);

        builder.Property(a => a.UserName)
            .HasMaxLength(150);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Details)
            .HasMaxLength(2000);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(50);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.Timestamp);
    }
}
