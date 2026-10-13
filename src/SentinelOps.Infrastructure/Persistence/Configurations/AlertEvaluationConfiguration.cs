using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class AlertEvaluationConfiguration : IEntityTypeConfiguration<AlertEvaluation>
{
    public void Configure(EntityTypeBuilder<AlertEvaluation> builder)
    {
        builder.ToTable("AlertEvaluations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Details)
            .HasMaxLength(500);

        builder.HasIndex(e => e.AlertRuleId);
        builder.HasIndex(e => e.EvaluatedAtUtc);
    }
}
