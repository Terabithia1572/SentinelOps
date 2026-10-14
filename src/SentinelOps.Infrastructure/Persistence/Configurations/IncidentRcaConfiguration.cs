using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class IncidentRcaConfiguration : IEntityTypeConfiguration<IncidentRca>
{
    public void Configure(EntityTypeBuilder<IncidentRca> builder)
    {
        builder.ToTable("IncidentRcas");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.IncidentSummary)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.ImpactedServicesJson)
            .IsRequired();

        builder.Property(r => r.HypothesesJson)
            .IsRequired();

        builder.Property(r => r.CorrelatedAnomaliesJson)
            .IsRequired();

        builder.Property(r => r.CorrelatedTraceIdsJson)
            .IsRequired();

        builder.Property(r => r.GeneratedByProvider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.OperatorVerificationStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.VerifiedBy)
            .HasMaxLength(100);

        builder.Property(r => r.OperatorNotes)
            .HasMaxLength(1000);

        builder.HasIndex(r => r.OrganizationId);
        builder.HasIndex(r => r.IncidentId);
    }
}
