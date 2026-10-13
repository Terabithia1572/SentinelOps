using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class StatusPageIncidentConfiguration : IEntityTypeConfiguration<StatusPageIncident>
{
    public void Configure(EntityTypeBuilder<StatusPageIncident> builder)
    {
        builder.ToTable("StatusPageIncidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Severity)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.AffectedComponentIdsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(i => i.UpdatesJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(i => i.StatusPageId);
        builder.HasIndex(i => new { i.StatusPageId, i.CreatedAtUtc });
    }
}
