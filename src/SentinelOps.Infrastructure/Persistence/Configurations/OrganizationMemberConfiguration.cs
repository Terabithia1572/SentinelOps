using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("OrganizationMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(m => new { m.OrganizationId, m.UserId })
            .IsUnique();

        builder.HasIndex(m => m.UserId);
    }
}
