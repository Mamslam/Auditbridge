namespace AuditBridge.Domain.Entities;

public class AuditReport
{
    public Guid Id { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid? GeneratedBy { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public decimal? ConformityScore { get; private set; }
    public int? TotalQuestions { get; private set; }
    public int? ConformCount { get; private set; }
    public int? NonConformCount { get; private set; }
    public int? PartialCount { get; private set; }
    public int? NaCount { get; private set; }
    public int CriticalNc { get; private set; }
    public int MajorNc { get; private set; }
    public int MinorNc { get; private set; }
    public string? ExecutiveSummary { get; private set; }
    public string? AiNarrative { get; private set; }
    public string ReportData { get; private set; } = "{}";  // JSON
    public string? PdfStoragePath { get; private set; }
    public string? PdfSha256 { get; private set; }
    public int Version { get; private set; } = 1;
    public string Metadata { get; private set; } = "{}";

    private AuditReport() { }

    public static AuditReport Create(
        Guid auditId, Guid? generatedBy,
        decimal conformityScore, int totalQuestions,
        int conformCount, int nonConformCount, int partialCount, int naCount,
        int criticalNc, int majorNc, int minorNc,
        string reportDataJson)
        => new()
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            GeneratedBy = generatedBy,
            GeneratedAt = DateTimeOffset.UtcNow,
            ConformityScore = conformityScore,
            TotalQuestions = totalQuestions,
            ConformCount = conformCount,
            NonConformCount = nonConformCount,
            PartialCount = partialCount,
            NaCount = naCount,
            CriticalNc = criticalNc,
            MajorNc = majorNc,
            MinorNc = minorNc,
            ReportData = reportDataJson,
        };

    public void SetNarrative(string executiveSummary, string aiNarrative)
    {
        ExecutiveSummary = executiveSummary;
        AiNarrative = aiNarrative;
    }

    public void SetPdf(string storagePath, string sha256)
    {
        PdfStoragePath = storagePath;
        PdfSha256 = sha256;
    }
}
