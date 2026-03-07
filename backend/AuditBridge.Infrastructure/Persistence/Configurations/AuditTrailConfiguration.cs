using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.ToTable("audit_trail");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.OrganizationId).HasColumnName("organization_id");
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(a => a.ResourceType).HasColumnName("resource_type").HasMaxLength(50).IsRequired();
        builder.Property(a => a.ResourceId).HasColumnName("resource_id");
        builder.Property(a => a.CampaignId).HasColumnName("campaign_id");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address");
        builder.Property(a => a.UserAgent).HasColumnName("user_agent");
        builder.Property(a => a.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        // FK relationships — no navigation properties, but EF needs to know the order of inserts
        builder.HasOne<User>().WithMany()
            .HasForeignKey(a => a.UserId).IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(a => a.OrganizationId).IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Immutable: no updates or deletes allowed (enforced by DB trigger)
        builder.Metadata.SetIsTableExcludedFromMigrations(false);
    }
}
