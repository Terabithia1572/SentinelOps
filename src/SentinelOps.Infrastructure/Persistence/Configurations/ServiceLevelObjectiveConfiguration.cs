using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class ServiceLevelObjectiveConfiguration : IEntityTypeConfiguration<ServiceLevelObjective>
{
    public void Configure(EntityTypeBuilder<ServiceLevelObjective> builder)
    {
        builder.ToTable("ServiceLevelObjectives");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.ServiceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.EnvironmentName)
            .HasMaxLength(50);

        builder.Property(s => s.IndicatorType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.EvaluationWindow)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(100);

        builder.HasIndex(s => s.ApplicationId);
        builder.HasIndex(s => s.ServiceName);
        builder.HasIndex(s => s.IsEnabled);

        builder.HasMany(s => s.Evaluations)
            .WithOne(e => e.ServiceLevelObjective)
            .HasForeignKey(e => e.ServiceLevelObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
