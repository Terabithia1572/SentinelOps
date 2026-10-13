using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        builder.ToTable("OrganizationSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PlanTier)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.OrganizationId)
            .IsUnique();
    }
}
