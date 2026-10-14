using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class BillingAccountConfiguration : IEntityTypeConfiguration<BillingAccount>
{
    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        builder.ToTable("BillingAccounts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BillingEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.CompanyName)
            .HasMaxLength(200);

        builder.Property(b => b.TaxId)
            .HasMaxLength(50);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(b => b.BillingAddress)
            .HasMaxLength(300);

        builder.Property(b => b.BillingCity)
            .HasMaxLength(100);

        builder.Property(b => b.BillingCountry)
            .HasMaxLength(100);

        builder.Property(b => b.BillingPostalCode)
            .HasMaxLength(30);

        builder.Property(b => b.PaymentProviderCustomerId)
            .HasMaxLength(100);

        builder.Property(b => b.PaymentMethodSummary)
            .HasMaxLength(100);

        builder.HasIndex(b => b.OrganizationId)
            .IsUnique();

        builder.HasIndex(b => b.PaymentProviderCustomerId);
    }
}
