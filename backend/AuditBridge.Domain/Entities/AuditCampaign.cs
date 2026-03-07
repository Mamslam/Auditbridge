namespace AuditBridge.Domain.Entities;

public class AuditCampaign
{
    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public Guid AuditorOrgId { get; private set; }
    public Guid ClientOrgId { get; private set; }
    public Guid LeadAuditorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public CampaignStatus Status { get; private set; } = CampaignStatus.Draft;
    public DateOnly? ScheduledDate { get; private set; }
    public DateOnly? Deadline { get; private set; }
    public string? Scope { get; private set; }
    public string? ClientAccessToken { get; private set; }
    public DateTimeOffset? ClientAccessExpiresAt { get; private set; }
    public decimal? ComplianceScore { get; private set; }
    public string? AiAnalysisJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Organization? AuditorOrg { get; private set; }
    public Organization? ClientOrg { get; private set; }
    public AuditTemplate? Template { get; private set; }
    public List<AuditResponse> Responses { get; private set; } = [];
    public List<AuditDocument> Documents { get; private set; } = [];
    public List<CapaAction> CapaActions { get; private set; } = [];
    public List<AuditReport> Reports { get; private set; } = [];

    private AuditCampaign() { }

    public static AuditCampaign Create(
        Guid templateId,
        Guid auditorOrgId,
        Guid clientOrgId,
        Guid leadAuditorId,
        string title,
        string? scope = null,
        DateOnly? scheduledDate = null,
        DateOnly? deadline = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Campaign title is required.", nameof(title));

        return new AuditCampaign
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            AuditorOrgId = auditorOrgId,
            ClientOrgId = clientOrgId,
            LeadAuditorId = leadAuditorId,
            Title = title.Trim(),
            Scope = scope?.Trim(),
            ScheduledDate = scheduledDate,
            Deadline = deadline,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void GenerateClientAccessToken(int expirationHours = 72)
    {
        ClientAccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        ClientAccessExpiresAt = DateTimeOffset.UtcNow.AddHours(expirationHours);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool TransitionTo(CampaignStatus newStatus)
    {
        var valid = (Status, newStatus) switch
        {
            (CampaignStatus.Draft, CampaignStatus.Sent) => true,
            (CampaignStatus.Sent, CampaignStatus.InProgress) => true,
            (CampaignStatus.InProgress, CampaignStatus.ClientSubmitted) => true,
            (CampaignStatus.ClientSubmitted, CampaignStatus.UnderReview) => true,
            (CampaignStatus.UnderReview, CampaignStatus.ReportGenerated) => true,
            (CampaignStatus.ReportGenerated, CampaignStatus.Closed) => true,
            _ => false
        };

        if (!valid) return false;

        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void UpdateComplianceScore(decimal score)
    {
        ComplianceScore = Math.Clamp(score, 0, 100);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum CampaignStatus
{
    Draft,
    Sent,
    InProgress,
    ClientSubmitted,
    UnderReview,
    ReportGenerated,
    Closed
}
