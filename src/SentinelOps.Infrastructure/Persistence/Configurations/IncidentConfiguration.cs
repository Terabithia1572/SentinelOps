using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IncidentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(i => i.EnvironmentName)
            .HasMaxLength(50);

        builder.Property(i => i.ServiceName)
            .HasMaxLength(100);

        builder.Property(i => i.AssignedToUserId)
            .HasMaxLength(150);

        builder.Property(i => i.AssignedToUserName)
            .HasMaxLength(150);

        builder.Property(i => i.AcknowledgedBy)
            .HasMaxLength(150);

        builder.Property(i => i.MitigatedBy)
            .HasMaxLength(150);

        builder.Property(i => i.ResolvedBy)
            .HasMaxLength(150);

        builder.Property(i => i.ClosedBy)
            .HasMaxLength(150);

        builder.Property(i => i.RootCauseSummary)
            .HasMaxLength(2000);

        builder.Property(i => i.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(i => i.RelatedTraceId)
            .HasMaxLength(64);

        builder.HasIndex(i => i.IncidentNumber)
            .IsUnique();

        builder.HasIndex(i => i.ApplicationId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Severity);
        builder.HasIndex(i => i.CreatedAtUtc);

        builder.HasMany(i => i.TimelineEvents)
            .WithOne(e => e.Incident)
            .HasForeignKey(e => e.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Comments)
            .WithOne(c => c.Incident)
            .HasForeignKey(c => c.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.AlertRelations)
            .WithOne(r => r.Incident)
            .HasForeignKey(r => r.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
