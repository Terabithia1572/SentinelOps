using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class SaaSInvoiceConfiguration : IEntityTypeConfiguration<SaaSInvoice>
{
    public void Configure(EntityTypeBuilder<SaaSInvoice> builder)
    {
        builder.ToTable("SaaSInvoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(i => i.AmountDue)
            .HasPrecision(18, 2);

        builder.Property(i => i.AmountPaid)
            .HasPrecision(18, 2);

        builder.Property(i => i.BillingReason)
            .HasMaxLength(100);

        builder.Property(i => i.PaymentProviderInvoiceId)
            .HasMaxLength(100);

        builder.Property(i => i.PaymentProviderPaymentIntentId)
            .HasMaxLength(100);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.DueDateUtc);

        builder.HasMany(i => i.LineItems)
            .WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Transactions)
            .WithOne(t => t.Invoice)
            .HasForeignKey(t => t.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
