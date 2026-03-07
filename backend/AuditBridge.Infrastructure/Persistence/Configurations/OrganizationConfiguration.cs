using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(o => o.Type).HasColumnName("type")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<OrganizationType>(v, true));
        builder.Property(o => o.Plan).HasColumnName("plan").HasMaxLength(20).HasDefaultValue("starter");
        builder.Property(o => o.StripeCustomerId).HasColumnName("stripe_customer_id").HasMaxLength(255);
        builder.Property(o => o.StripeSubscriptionId).HasColumnName("stripe_subscription_id").HasMaxLength(255);
        builder.Property(o => o.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
        builder.Property(o => o.Language).HasColumnName("language").HasMaxLength(10).HasDefaultValue("fr");
        builder.Property(o => o.LogoUrl).HasColumnName("logo_url");
        builder.Property(o => o.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(o => o.Users)
            .WithOne(u => u.Organization)
            .HasForeignKey(u => u.OrganizationId);

        // Map backing fields for read-only collections
        builder.Navigation(o => o.Users).HasField("_users").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(o => o.Templates).HasField("_templates").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
