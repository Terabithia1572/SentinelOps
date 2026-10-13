using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class StatusPageConfiguration : IEntityTypeConfiguration<StatusPage>
{
    public void Configure(EntityTypeBuilder<StatusPage> builder)
    {
        builder.ToTable("StatusPages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.LogoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.SupportUrl)
            .HasMaxLength(500);

        builder.Property(p => p.HeaderNotice)
            .HasMaxLength(1000);

        builder.Property(p => p.ComponentsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(p => p.MaintenanceWindowsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Incidents)
            .WithOne(i => i.StatusPage)
            .HasForeignKey(i => i.StatusPageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
