namespace AuditBridge.Domain.Entities;

/// <summary>
/// A file or document attached as evidence during an audit.
/// Can be linked to a finding, a response, or a CAPA.
/// The file itself is stored in Supabase Storage; this entity holds the metadata.
/// </summary>
public class AuditEvidence
{
    public Guid Id { get; private set; }
    public Guid AuditId { get; private set; }

    /// <summary>Optional link to a specific finding.</summary>
    public Guid? FindingId { get; private set; }

    /// <summary>Optional link to a specific question response.</summary>
    public Guid? ResponseId { get; private set; }

    /// <summary>Optional link to a CAPA (for close-out evidence).</summary>
    public Guid? CapaId { get; private set; }

    public Guid UploadedBy { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    /// <summary>Path in Supabase Storage bucket, e.g. "{orgId}/{auditId}/{uuid}/{filename}".</summary>
    public string StoragePath { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditEvidence() { }

    public static AuditEvidence Create(
        Guid auditId,
        Guid uploadedBy,
        string fileName,
        string storagePath,
        long fileSizeBytes,
        string mimeType,
        Guid? findingId = null,
        Guid? responseId = null,
        Guid? capaId = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("FileName required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("StoragePath required.", nameof(storagePath));

        return new()
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            UploadedBy = uploadedBy,
            FileName = fileName,
            StoragePath = storagePath,
            FileSizeBytes = fileSizeBytes,
            MimeType = mimeType,
            FindingId = findingId,
            ResponseId = responseId,
            CapaId = capaId,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
