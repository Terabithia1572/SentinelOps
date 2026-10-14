using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class OrganizationSubscriptionConfiguration : IEntityTypeConfiguration<OrganizationSubscription>
{
    public void Configure(EntityTypeBuilder<OrganizationSubscription> builder)
    {
        builder.ToTable("OrganizationSubscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ConcurrencyToken)
            .IsRequired()
            .HasMaxLength(64)
            .IsConcurrencyToken();

        builder.Property(s => s.PaymentProviderSubscriptionId)
            .HasMaxLength(100);

        builder.HasIndex(s => s.OrganizationId)
            .IsUnique();

        builder.HasIndex(s => s.Status);

        builder.HasIndex(s => s.PaymentProviderSubscriptionId);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.History)
            .WithOne(h => h.Subscription)
            .HasForeignKey(h => h.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
