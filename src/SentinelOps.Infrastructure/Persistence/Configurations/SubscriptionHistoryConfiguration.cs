using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class SubscriptionHistoryConfiguration : IEntityTypeConfiguration<SubscriptionHistory>
{
    public void Configure(EntityTypeBuilder<SubscriptionHistory> builder)
    {
        builder.ToTable("SubscriptionHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(h => h.ChangedByUserId)
            .HasMaxLength(128);

        builder.HasIndex(h => h.SubscriptionId);
        builder.HasIndex(h => h.OrganizationId);
        builder.HasIndex(h => h.TimestampUtc);
    }
}
