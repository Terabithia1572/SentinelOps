using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        builder.ToTable("InAppNotifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.LinkUrl)
            .HasMaxLength(500);

        builder.Property(n => n.UserId)
            .HasMaxLength(150);

        builder.Property(n => n.Role)
            .HasMaxLength(50);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.Role);
        builder.HasIndex(n => n.ApplicationId);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.CreatedAtUtc);
    }
}
