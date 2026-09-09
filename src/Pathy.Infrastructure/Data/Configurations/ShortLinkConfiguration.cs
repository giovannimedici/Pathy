using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pathy.Domain.Entities;

namespace Pathy.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ShortLink entity.
/// </summary>
public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("short_links");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OriginalUrl)
            .HasColumnName("original_url")
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Unique index on slug for fast lookups during redirect
        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_short_links_slug");

        // Index on status for filtering active links
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_short_links_status");
    }
}
