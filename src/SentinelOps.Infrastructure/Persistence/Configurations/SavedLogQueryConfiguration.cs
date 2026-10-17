using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class SavedLogQueryConfiguration : IEntityTypeConfiguration<SavedLogQuery>
{
    public void Configure(EntityTypeBuilder<SavedLogQuery> builder)
    {
        builder.ToTable("SavedLogQueries");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(q => q.Description)
            .HasMaxLength(500);

        builder.Property(q => q.SearchTerm)
            .HasMaxLength(500);

        builder.Property(q => q.Severity)
            .HasMaxLength(50);

        builder.Property(q => q.ServiceName)
            .HasMaxLength(100);

        builder.Property(q => q.EnvironmentName)
            .HasMaxLength(50);

        builder.Property(q => q.TraceId)
            .HasMaxLength(64);

        builder.Property(q => q.TimeRange)
            .HasMaxLength(20);

        builder.Property(q => q.CreatedByUserId)
            .HasMaxLength(100);

        builder.Property(q => q.CreatedByUserName)
            .HasMaxLength(100);

        builder.Property(q => q.PropertyFiltersJson)
            .HasColumnType("jsonb");

        builder.Property(q => q.TagsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(q => q.OrganizationId);
        builder.HasIndex(q => new { q.OrganizationId, q.ApplicationId });
        builder.HasIndex(q => new { q.OrganizationId, q.IsShared });

        builder.HasOne(q => q.Organization)
            .WithMany()
            .HasForeignKey(q => q.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Application)
            .WithMany()
            .HasForeignKey(q => q.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
