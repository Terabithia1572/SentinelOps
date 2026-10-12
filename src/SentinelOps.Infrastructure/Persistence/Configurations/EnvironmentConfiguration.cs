using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Environment = SentinelOps.Domain.Entities.Environment;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Environment> builder)
    {
        builder.ToTable("Environments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EnvironmentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(e => new { e.ApplicationId, e.Name })
            .IsUnique();
    }
}
