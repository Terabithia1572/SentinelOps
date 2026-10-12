using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.ApiKeyHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(a => a.Name)
            .IsUnique();

        builder.HasMany(a => a.Environments)
            .WithOne(e => e.Application)
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Services)
            .WithOne(s => s.Application)
            .HasForeignKey(s => s.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
