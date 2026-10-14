using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToTable("PaymentWebhookEvents");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.EventId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(w => w.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.PayloadJson)
            .IsRequired();

        builder.Property(w => w.SignatureHash)
            .HasMaxLength(256);

        builder.Property(w => w.ErrorMessage)
            .HasMaxLength(1000);

        // Deduplication unique index on Provider + EventId
        builder.HasIndex(w => new { w.Provider, w.EventId })
            .IsUnique();

        builder.HasIndex(w => w.IsProcessed);
    }
}
