using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class CustomDashboardConfiguration : IEntityTypeConfiguration<CustomDashboard>
{
    public void Configure(EntityTypeBuilder<CustomDashboard> builder)
    {
        builder.ToTable("CustomDashboards");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.CreatedByUserId)
            .HasMaxLength(100);

        builder.Property(d => d.CreatedByUserName)
            .HasMaxLength(100);

        builder.Property(d => d.DefaultTimeRange)
            .HasMaxLength(20);

        builder.Property(d => d.TagsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(d => d.LayoutJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => new { d.OrganizationId, d.IsDefault });

        builder.HasOne(d => d.Organization)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
