using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class ApplicationMemberConfiguration : IEntityTypeConfiguration<ApplicationMember>
{
    public void Configure(EntityTypeBuilder<ApplicationMember> builder)
    {
        builder.ToTable("ApplicationMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.AddedByUserId)
            .HasMaxLength(100);

        builder.HasOne(m => m.Application)
            .WithMany(a => a.Members)
            .HasForeignKey(m => m.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ApplicationId, m.UserId })
            .IsUnique();

        builder.HasIndex(m => m.UserId);
    }
}
