using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class ControlConfiguration : IEntityTypeConfiguration<Control>
{
    public void Configure(EntityTypeBuilder<Control> builder)
    {
        builder.ToTable("controls");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.OrgId).HasColumnName("org_id");
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.Category).HasColumnName("category").HasMaxLength(50);
        builder.Property(c => c.Owner).HasColumnName("owner");
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(c => new { c.OrgId, c.Code }).IsUnique();

        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(c => c.OrgId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Mappings).HasField("_mappings").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ControlMappingConfiguration : IEntityTypeConfiguration<ControlMapping>
{
    public void Configure(EntityTypeBuilder<ControlMapping> builder)
    {
        builder.ToTable("control_mappings");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.ControlId).HasColumnName("control_id");
        builder.Property(m => m.ReferentialId).HasColumnName("referential_id");
        builder.Property(m => m.SectionId).HasColumnName("section_id");
        builder.Property(m => m.QuestionId).HasColumnName("question_id");
        builder.Property(m => m.Notes).HasColumnName("notes");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(m => m.ControlId);
        builder.HasIndex(m => m.ReferentialId);

        builder.HasOne(m => m.Control).WithMany(c => c.Mappings)
            .HasForeignKey(m => m.ControlId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Referential>().WithMany()
            .HasForeignKey(m => m.ReferentialId).OnDelete(DeleteBehavior.Cascade);
    }
}
