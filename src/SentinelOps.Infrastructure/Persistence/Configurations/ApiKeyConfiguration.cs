using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.KeyPrefix)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(k => k.KeyHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(k => k.CreatedBy)
            .HasMaxLength(100);

        builder.HasIndex(k => k.KeyHash)
            .IsUnique();

        builder.HasIndex(k => new { k.ApplicationId, k.RevokedAtUtc });

        builder.HasOne(k => k.Application)
            .WithMany(a => a.ApiKeys)
            .HasForeignKey(k => k.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
