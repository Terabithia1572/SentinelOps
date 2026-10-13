using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class AlertRuleNotificationChannelConfiguration : IEntityTypeConfiguration<AlertRuleNotificationChannel>
{
    public void Configure(EntityTypeBuilder<AlertRuleNotificationChannel> builder)
    {
        builder.ToTable("AlertRuleNotificationChannels");

        builder.HasKey(nc => nc.Id);

        builder.HasOne(nc => nc.AlertRule)
            .WithMany(r => r.NotificationChannels)
            .HasForeignKey(nc => nc.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nc => nc.NotificationChannel)
            .WithMany(c => c.AlertRules)
            .HasForeignKey(nc => nc.NotificationChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(nc => new { nc.AlertRuleId, nc.NotificationChannelId })
            .IsUnique();
    }
}
