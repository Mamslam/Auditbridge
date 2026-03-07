namespace AuditBridge.Domain.Entities;

public class AuditReport
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid GeneratedBy { get; private set; }
    public int Version { get; private set; } = 1;
    public ReportStatus Status { get; private set; } = ReportStatus.Draft;
    public string? StoragePath { get; private set; }
    public string? FileHash { get; private set; }
    public string? AiSummary { get; private set; }
    public string? ExecutiveSummary { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public Guid? SignedBy { get; private set; }

    private AuditReport() { }

    public static AuditReport Create(Guid campaignId, Guid generatedBy, int version = 1)
    {
        return new AuditReport
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            GeneratedBy = generatedBy,
            Version = version,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetStoragePath(string path, string hash)
    {
        StoragePath = path;
        FileHash = hash;
    }

    public void SetAiContent(string aiSummary, string executiveSummary)
    {
        AiSummary = aiSummary;
        ExecutiveSummary = executiveSummary;
    }

    public void MarkFinal()
    {
        Status = ReportStatus.Final;
    }

    public void Sign(Guid signedBy)
    {
        if (Status != ReportStatus.Final)
            throw new InvalidOperationException("Only finalized reports can be signed.");
        Status = ReportStatus.Signed;
        SignedBy = signedBy;
        SignedAt = DateTimeOffset.UtcNow;
    }
}

public enum ReportStatus { Draft, Final, Signed }
