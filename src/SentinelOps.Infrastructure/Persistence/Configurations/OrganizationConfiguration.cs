using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Slug)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Description)
            .HasMaxLength(500);

        builder.HasIndex(o => o.Slug)
            .IsUnique();

        builder.HasMany(o => o.Members)
            .WithOne(m => m.Organization)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Invitations)
            .WithOne(i => i.Organization)
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Settings)
            .WithOne(s => s.Organization)
            .HasForeignKey<OrganizationSettings>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Usage)
            .WithOne(u => u.Organization)
            .HasForeignKey<OrganizationUsage>(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
