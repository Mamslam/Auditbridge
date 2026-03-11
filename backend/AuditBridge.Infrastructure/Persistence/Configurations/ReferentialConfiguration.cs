using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class ReferentialCategoryConfiguration : IEntityTypeConfiguration<ReferentialCategory>
{
    public void Configure(EntityTypeBuilder<ReferentialCategory> builder)
    {
        builder.ToTable("referential_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Slug).HasColumnName("slug").IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Label).HasColumnName("label").IsRequired();
        builder.Property(c => c.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
        builder.Property(c => c.Icon).HasColumnName("icon");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
    }
}

public class ReferentialConfiguration : IEntityTypeConfiguration<Referential>
{
    public void Configure(EntityTypeBuilder<Referential> builder)
    {
        builder.ToTable("referentials");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.OrgId).HasColumnName("org_id");
        builder.Property(r => r.CategoryId).HasColumnName("category_id");
        builder.Property(r => r.Code).HasColumnName("code").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.Version).HasColumnName("version");
        builder.Property(r => r.Description).HasColumnName("description");
        builder.Property(r => r.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        builder.Property(r => r.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
        builder.Property(r => r.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(r => r.Category).WithMany()
            .HasForeignKey(r => r.CategoryId).IsRequired(false);

        builder.Navigation(r => r.Sections).HasField("_sections").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Questions).HasField("_questions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class TemplateSectionConfiguration : IEntityTypeConfiguration<TemplateSection>
{
    public void Configure(EntityTypeBuilder<TemplateSection> builder)
    {
        builder.ToTable("template_sections");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.ReferentialId).HasColumnName("referential_id");
        builder.Property(s => s.ParentId).HasColumnName("parent_id");
        builder.Property(s => s.OrderIndex).HasColumnName("order_index");
        builder.Property(s => s.Code).HasColumnName("code");
        builder.Property(s => s.Title).HasColumnName("title").IsRequired();
        builder.Property(s => s.Description).HasColumnName("description");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");

        builder.HasOne<Referential>().WithMany(r => r.Sections)
            .HasForeignKey(s => s.ReferentialId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Questions).HasField("_questions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class TemplateQuestionConfiguration : IEntityTypeConfiguration<TemplateQuestion>
{
    public void Configure(EntityTypeBuilder<TemplateQuestion> builder)
    {
        builder.ToTable("template_questions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("id");
        builder.Property(q => q.ReferentialId).HasColumnName("referential_id");
        builder.Property(q => q.SectionId).HasColumnName("section_id");
        builder.Property(q => q.OrderIndex).HasColumnName("order_index");
        builder.Property(q => q.Code).HasColumnName("code");
        builder.Property(q => q.Question).HasColumnName("question").IsRequired();
        builder.Property(q => q.Guidance).HasColumnName("guidance");
        builder.Property(q => q.AnswerType).HasColumnName("answer_type").HasMaxLength(20);
        builder.Property(q => q.AnswerOptions).HasColumnName("answer_options").HasColumnType("jsonb");
        builder.Property(q => q.IsMandatory).HasColumnName("is_mandatory").HasDefaultValue(true);
        builder.Property(q => q.Criticality).HasColumnName("criticality").HasMaxLength(20);
        builder.Property(q => q.ExpectedEvidence).HasColumnName("expected_evidence");
        builder.Property(q => q.Tags).HasColumnName("tags");
        builder.Property(q => q.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(q => q.CreatedAt).HasColumnName("created_at");

        builder.HasOne<Referential>().WithMany(r => r.Questions)
            .HasForeignKey(q => q.ReferentialId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TemplateSection>().WithMany(s => s.Questions)
            .HasForeignKey(q => q.SectionId).IsRequired(false);
    }
}
