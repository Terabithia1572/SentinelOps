using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
{
    public void Configure(EntityTypeBuilder<NotificationChannel> builder)
    {
        builder.ToTable("NotificationChannels");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.Destination)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.WebhookSecret)
            .HasMaxLength(256);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.HasIndex(c => c.ApplicationId);
        builder.HasIndex(c => c.IsEnabled);
    }
}
