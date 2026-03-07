using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.ClerkId).HasColumnName("clerk_id").HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.ClerkId).IsUnique();
        builder.Property(u => u.OrganizationId).HasColumnName("organization_id");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(255);
        builder.Property(u => u.Role).HasColumnName("role")
            .HasConversion(
                v => ToSnakeCase(v.ToString()),
                v => ParseRole(v));
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");

        builder.HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId);
    }

    private static string ToSnakeCase(string value) => value switch
    {
        "AuditorLead" => "auditor_lead",
        "AuditorJunior" => "auditor_junior",
        "AuditorViewer" => "auditor_viewer",
        "ClientAdmin" => "client_admin",
        "ClientContributor" => "client_contributor",
        "ClientViewer" => "client_viewer",
        "PlatformAdmin" => "platform_admin",
        _ => value.ToLowerInvariant()
    };

    private static UserRole ParseRole(string value) => value switch
    {
        "auditor_lead" => UserRole.AuditorLead,
        "auditor_junior" => UserRole.AuditorJunior,
        "auditor_viewer" => UserRole.AuditorViewer,
        "client_admin" => UserRole.ClientAdmin,
        "client_contributor" => UserRole.ClientContributor,
        "client_viewer" => UserRole.ClientViewer,
        "platform_admin" => UserRole.PlatformAdmin,
        _ => throw new InvalidOperationException($"Unknown role: {value}")
    };
}
