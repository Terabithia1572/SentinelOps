using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Persistence.Configurations;

public class IncidentCommentConfiguration : IEntityTypeConfiguration<IncidentComment>
{
    public void Configure(EntityTypeBuilder<IncidentComment> builder)
    {
        builder.ToTable("IncidentComments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.UserName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.CommentText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(c => c.IncidentId);
        builder.HasIndex(c => c.CreatedAtUtc);
    }
}
