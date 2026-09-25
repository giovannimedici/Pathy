using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pathy.Domain.Entities;

namespace Pathy.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the LinkAuditLog entity.
/// </summary>
public class LinkAuditLogConfiguration : IEntityTypeConfiguration<LinkAuditLog>
{
    public void Configure(EntityTypeBuilder<LinkAuditLog> builder)
    {
        builder.ToTable("link_audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.LinkId)
            .HasColumnName("link_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.FieldName)
            .HasColumnName("field_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OldValue)
            .HasColumnName("old_value")
            .HasMaxLength(2048);

        builder.Property(x => x.NewValue)
            .HasColumnName("new_value")
            .HasMaxLength(2048);

        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .IsRequired();

        // Relationship with ShortLink - cascade delete for LGPD/GDPR compliance
        builder.HasOne<ShortLink>()
            .WithMany()
            .HasForeignKey(x => x.LinkId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on link_id for querying audit history of a specific link
        builder.HasIndex(x => x.LinkId)
            .HasDatabaseName("ix_link_audit_logs_link_id");

        // Index on user_id for querying all changes made by a user
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_link_audit_logs_user_id");

        // Index on changed_at for time-based queries
        builder.HasIndex(x => x.ChangedAt)
            .HasDatabaseName("ix_link_audit_logs_changed_at");
    }
}
