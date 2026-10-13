using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class SloEvaluationConfiguration : IEntityTypeConfiguration<SloEvaluation>
{
    public void Configure(EntityTypeBuilder<SloEvaluation> builder)
    {
        builder.ToTable("SloEvaluations");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.ServiceLevelObjectiveId);
        builder.HasIndex(e => e.EvaluatedAtUtc);
    }
}
