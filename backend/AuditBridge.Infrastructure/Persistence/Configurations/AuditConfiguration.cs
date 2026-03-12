using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class AuditConfiguration : IEntityTypeConfiguration<Audit>
{
    public void Configure(EntityTypeBuilder<Audit> builder)
    {
        builder.ToTable("audits");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.OrgId).HasColumnName("org_id");
        builder.Property(a => a.ReferentialId).HasColumnName("referential_id");
        builder.Property(a => a.TemplateSnapshot).HasColumnName("template_snapshot").HasColumnType("jsonb");
        builder.Property(a => a.Title).HasColumnName("title").IsRequired();
        builder.Property(a => a.Description).HasColumnName("description");
        builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(a => a.AuditorId).HasColumnName("auditor_id");
        builder.Property(a => a.ClientOrgName).HasColumnName("client_org_name");
        builder.Property(a => a.ClientEmail).HasColumnName("client_email");
        builder.Property(a => a.ClientToken).HasColumnName("client_token");
        builder.Property(a => a.ClientTokenExpiresAt).HasColumnName("client_token_expires_at");
        builder.Property(a => a.DueDate).HasColumnName("due_date");
        builder.Property(a => a.Scope).HasColumnName("scope");
        builder.Property(a => a.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(a => a.Referential).WithMany()
            .HasForeignKey(a => a.ReferentialId);
        builder.HasOne<Organization>().WithMany(o => o.Audits)
            .HasForeignKey(a => a.OrgId);
        builder.HasOne<User>().WithMany()
            .HasForeignKey(a => a.AuditorId);

        builder.Navigation(a => a.Responses).HasField("_responses").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.Capas).HasField("_capas").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.Findings).HasField("_findings").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.Evidence).HasField("_evidence").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class AuditResponseConfiguration : IEntityTypeConfiguration<AuditResponse>
{
    public void Configure(EntityTypeBuilder<AuditResponse> builder)
    {
        builder.ToTable("audit_responses");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.AuditId).HasColumnName("audit_id");
        builder.Property(r => r.QuestionId).HasColumnName("question_id");
        builder.Property(r => r.AnsweredBy).HasColumnName("answered_by");
        builder.Property(r => r.AnsweredByClient).HasColumnName("answered_by_client");
        builder.Property(r => r.AnswerValue).HasColumnName("answer_value");
        builder.Property(r => r.AnswerNotes).HasColumnName("answer_notes");
        // Conformity values: compliant | minor | major | critical | na | pending
        builder.Property(r => r.Conformity).HasColumnName("conformity").HasMaxLength(20);
        builder.Property(r => r.AuditorComment).HasColumnName("auditor_comment");
        builder.Property(r => r.IsFlagged).HasColumnName("is_flagged");
        builder.Property(r => r.AiAnalysis).HasColumnName("ai_analysis").HasColumnType("jsonb");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Audit>().WithMany(a => a.Responses)
            .HasForeignKey(r => r.AuditId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TemplateQuestion>().WithMany()
            .HasForeignKey(r => r.QuestionId);
    }
}

public class AuditCapaConfiguration : IEntityTypeConfiguration<AuditCapa>
{
    public void Configure(EntityTypeBuilder<AuditCapa> builder)
    {
        builder.ToTable("audit_capas");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.AuditId).HasColumnName("audit_id");
        builder.Property(c => c.FindingId).HasColumnName("finding_id");
        builder.Property(c => c.ResponseId).HasColumnName("response_id");
        builder.Property(c => c.QuestionId).HasColumnName("question_id");
        builder.Property(c => c.Title).HasColumnName("title").IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.RootCause).HasColumnName("root_cause");
        builder.Property(c => c.ActionType).HasColumnName("action_type").HasMaxLength(20);
        builder.Property(c => c.Priority).HasColumnName("priority").HasMaxLength(20);
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(c => c.AssignedToEmail).HasColumnName("assigned_to_email");
        builder.Property(c => c.DueDate).HasColumnName("due_date");
        builder.Property(c => c.CompletedAt).HasColumnName("completed_at");
        builder.Property(c => c.VerifiedBy).HasColumnName("verified_by");
        builder.Property(c => c.EvidencePath).HasColumnName("evidence_path");
        builder.Property(c => c.AiGenerated).HasColumnName("ai_generated");
        builder.Property(c => c.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Audit>().WithMany(a => a.Capas)
            .HasForeignKey(c => c.AuditId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditFinding>().WithMany(f => f.Capas)
            .HasForeignKey(c => c.FindingId).OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}

public class AuditFindingConfiguration : IEntityTypeConfiguration<AuditFinding>
{
    public void Configure(EntityTypeBuilder<AuditFinding> builder)
    {
        builder.ToTable("audit_findings");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.AuditId).HasColumnName("audit_id");
        builder.Property(f => f.QuestionId).HasColumnName("question_id");
        builder.Property(f => f.ResponseId).HasColumnName("response_id");
        // FindingType: nc_critical | nc_major | nc_minor | observation | ofi
        builder.Property(f => f.FindingType).HasColumnName("finding_type").HasMaxLength(20);
        builder.Property(f => f.Title).HasColumnName("title").IsRequired();
        builder.Property(f => f.Description).HasColumnName("description");
        builder.Property(f => f.ObservedEvidence).HasColumnName("observed_evidence");
        builder.Property(f => f.RegulatoryRef).HasColumnName("regulatory_ref").HasMaxLength(255);
        // Status: open | acknowledged | closed
        builder.Property(f => f.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Audit>().WithMany(a => a.Findings)
            .HasForeignKey(f => f.AuditId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(f => f.Capas).HasField("_capas").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class AuditEvidenceConfiguration : IEntityTypeConfiguration<AuditEvidence>
{
    public void Configure(EntityTypeBuilder<AuditEvidence> builder)
    {
        builder.ToTable("audit_evidence");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.AuditId).HasColumnName("audit_id");
        builder.Property(e => e.FindingId).HasColumnName("finding_id");
        builder.Property(e => e.ResponseId).HasColumnName("response_id");
        builder.Property(e => e.CapaId).HasColumnName("capa_id");
        builder.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
        builder.Property(e => e.StoragePath).HasColumnName("storage_path").IsRequired();
        builder.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne<Audit>().WithMany(a => a.Evidence)
            .HasForeignKey(e => e.AuditId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditFinding>().WithMany()
            .HasForeignKey(e => e.FindingId).OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}

public class AuditReportConfiguration : IEntityTypeConfiguration<AuditReport>
{
    public void Configure(EntityTypeBuilder<AuditReport> builder)
    {
        builder.ToTable("audit_reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.AuditId).HasColumnName("audit_id");
        builder.Property(r => r.GeneratedBy).HasColumnName("generated_by");
        builder.Property(r => r.GeneratedAt).HasColumnName("generated_at");
        builder.Property(r => r.ConformityScore).HasColumnName("conformity_score").HasColumnType("decimal(5,2)");
        builder.Property(r => r.TotalQuestions).HasColumnName("total_questions");
        builder.Property(r => r.ConformCount).HasColumnName("conform_count");
        builder.Property(r => r.NonConformCount).HasColumnName("non_conform_count");
        builder.Property(r => r.PartialCount).HasColumnName("partial_count");
        builder.Property(r => r.NaCount).HasColumnName("na_count");
        builder.Property(r => r.CriticalNc).HasColumnName("critical_nc");
        builder.Property(r => r.MajorNc).HasColumnName("major_nc");
        builder.Property(r => r.MinorNc).HasColumnName("minor_nc");
        builder.Property(r => r.ExecutiveSummary).HasColumnName("executive_summary");
        builder.Property(r => r.AiNarrative).HasColumnName("ai_narrative");
        builder.Property(r => r.ReportData).HasColumnName("report_data").HasColumnType("jsonb");
        builder.Property(r => r.PdfStoragePath).HasColumnName("pdf_storage_path");
        builder.Property(r => r.PdfSha256).HasColumnName("pdf_sha256");
        builder.Property(r => r.Version).HasColumnName("version");
        builder.Property(r => r.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        builder.HasOne<Audit>().WithOne()
            .HasForeignKey<AuditReport>(r => r.AuditId);
    }
}
