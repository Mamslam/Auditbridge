using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditBridge.Infrastructure.Persistence.Configurations;

public class AuditCampaignConfiguration : IEntityTypeConfiguration<AuditCampaign>
{
    public void Configure(EntityTypeBuilder<AuditCampaign> builder)
    {
        builder.ToTable("audit_campaigns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TemplateId).HasColumnName("template_id");
        builder.Property(c => c.AuditorOrgId).HasColumnName("auditor_org_id");
        builder.Property(c => c.ClientOrgId).HasColumnName("client_org_id");
        builder.Property(c => c.LeadAuditorId).HasColumnName("lead_auditor_id");
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status")
            .HasConversion(
                v => ToDbValue(v),
                v => ParseStatus(v));
        builder.Property(c => c.ScheduledDate).HasColumnName("scheduled_date");
        builder.Property(c => c.Deadline).HasColumnName("deadline");
        builder.Property(c => c.Scope).HasColumnName("scope");
        builder.Property(c => c.ClientAccessToken).HasColumnName("client_access_token").HasMaxLength(255);
        builder.HasIndex(c => c.ClientAccessToken).IsUnique();
        builder.Property(c => c.ClientAccessExpiresAt).HasColumnName("client_access_expires_at");
        builder.Property(c => c.ComplianceScore).HasColumnName("compliance_score").HasPrecision(5, 2);
        builder.Property(c => c.AiAnalysisJson).HasColumnName("ai_analysis").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
    }

    private static string ToDbValue(CampaignStatus s) => s switch
    {
        CampaignStatus.Draft => "draft",
        CampaignStatus.Sent => "sent",
        CampaignStatus.InProgress => "in_progress",
        CampaignStatus.ClientSubmitted => "client_submitted",
        CampaignStatus.UnderReview => "under_review",
        CampaignStatus.ReportGenerated => "report_generated",
        CampaignStatus.Closed => "closed",
        _ => s.ToString().ToLowerInvariant()
    };

    private static CampaignStatus ParseStatus(string s) => s switch
    {
        "draft" => CampaignStatus.Draft,
        "sent" => CampaignStatus.Sent,
        "in_progress" => CampaignStatus.InProgress,
        "client_submitted" => CampaignStatus.ClientSubmitted,
        "under_review" => CampaignStatus.UnderReview,
        "report_generated" => CampaignStatus.ReportGenerated,
        "closed" => CampaignStatus.Closed,
        _ => throw new InvalidOperationException($"Unknown status: {s}")
    };
}
