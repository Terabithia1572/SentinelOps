using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class OrganizationUsageConfiguration : IEntityTypeConfiguration<OrganizationUsage>
{
    public void Configure(EntityTypeBuilder<OrganizationUsage> builder)
    {
        builder.ToTable("OrganizationUsages");

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.OrganizationId)
            .IsUnique();
    }
}
