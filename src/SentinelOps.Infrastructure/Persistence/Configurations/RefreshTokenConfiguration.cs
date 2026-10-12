using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(t => t.RevokedByIp)
            .HasMaxLength(50);

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => new { t.UserId, t.RevokedAtUtc });
    }
}
