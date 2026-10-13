using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("NotificationOutboxes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.Subject)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(o => o.Message)
            .IsRequired();

        builder.Property(o => o.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(o => o.TraceId)
            .HasMaxLength(64);

        builder.HasOne(o => o.Channel)
            .WithMany(c => c.OutboxMessages)
            .HasForeignKey(o => o.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.NextRetryAtUtc);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}
