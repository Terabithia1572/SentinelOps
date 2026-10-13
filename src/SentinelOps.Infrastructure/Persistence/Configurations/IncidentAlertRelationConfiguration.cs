using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class IncidentAlertRelationConfiguration : IEntityTypeConfiguration<IncidentAlertRelation>
{
    public void Configure(EntityTypeBuilder<IncidentAlertRelation> builder)
    {
        builder.ToTable("IncidentAlertRelations");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Incident)
            .WithMany(i => i.AlertRelations)
            .HasForeignKey(r => r.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.AlertRule)
            .WithMany()
            .HasForeignKey(r => r.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.IncidentId, r.AlertRuleId })
            .IsUnique();
    }
}
