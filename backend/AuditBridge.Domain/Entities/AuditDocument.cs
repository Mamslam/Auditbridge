namespace AuditBridge.Domain.Entities;

public class AuditDocument
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid? QuestionId { get; private set; }
    public Guid UploadedBy { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public bool IsAuditorOnly { get; private set; }
    public string? WatermarkDataJson { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/zip",
    ];

    private AuditDocument() { }

    public static AuditDocument Create(
        Guid campaignId,
        Guid uploadedBy,
        string fileName,
        long fileSize,
        string mimeType,
        string storagePath,
        string fileHash,
        Guid? questionId = null,
        bool isAuditorOnly = false)
    {
        if (!AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
            throw new InvalidOperationException($"MIME type '{mimeType}' is not allowed.");

        return new AuditDocument
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            QuestionId = questionId,
            UploadedBy = uploadedBy,
            FileName = fileName,
            FileSize = fileSize,
            MimeType = mimeType.ToLowerInvariant(),
            StoragePath = storagePath,
            FileHash = fileHash,
            IsAuditorOnly = isAuditorOnly,
            UploadedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
