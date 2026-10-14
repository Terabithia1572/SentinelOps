using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(t => t.PaymentProvider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.PaymentProviderTransactionId)
            .HasMaxLength(100);

        builder.Property(t => t.FailureReason)
            .HasMaxLength(500);

        builder.Property(t => t.SettledByUserId)
            .HasMaxLength(128);

        builder.Property(t => t.SettledReason)
            .HasMaxLength(500);

        builder.HasIndex(t => t.InvoiceId);
        builder.HasIndex(t => t.OrganizationId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.PaymentProviderTransactionId);
    }
}
