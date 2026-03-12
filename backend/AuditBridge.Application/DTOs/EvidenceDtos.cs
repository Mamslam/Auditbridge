namespace AuditBridge.Application.DTOs;

public record RegisterEvidenceRequest(
    string FileName,
    string StoragePath,
    long FileSizeBytes,
    string MimeType,
    Guid? FindingId = null,
    Guid? ResponseId = null,
    Guid? CapaId = null,
    string? Description = null
);

public record EvidenceDto(
    Guid Id,
    Guid AuditId,
    Guid? FindingId,
    Guid? ResponseId,
    Guid? CapaId,
    string FileName,
    string StoragePath,
    long FileSizeBytes,
    string MimeType,
    string? Description,
    DateTimeOffset CreatedAt
);

public record SignedUploadUrlResponse(
    string SignedUrl,
    string StoragePath,
    DateTimeOffset ExpiresAt
);

public record SignedDownloadUrlResponse(string Url);
