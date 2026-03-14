namespace AuditBridge.Application.DTOs;

public record CreateFindingRequest(
    string FindingType,        // nc_critical | nc_major | nc_minor | observation | ofi
    string Title,
    Guid? QuestionId = null,
    Guid? ResponseId = null,
    string? Description = null,
    string? ObservedEvidence = null,
    string? RegulatoryRef = null,
    double? Latitude = null,
    double? Longitude = null,
    string? LocationName = null
);

public record UpdateFindingRequest(
    string FindingType,
    string Title,
    string? Description = null,
    string? ObservedEvidence = null,
    string? RegulatoryRef = null
);

public record FindingDto(
    Guid Id,
    Guid AuditId,
    Guid? QuestionId,
    Guid? ResponseId,
    string FindingType,
    string Title,
    string? Description,
    string? ObservedEvidence,
    string? RegulatoryRef,
    string Status,
    List<CapaDto> Capas,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    double? Latitude = null,
    double? Longitude = null,
    string? LocationName = null
);

public record SignAuditRequest(
    string SignatureData,   // base64 PNG
    string SignerRole       // "auditor" | "auditee"
);

public record SignatureStatusDto(
    bool AuditorSigned,
    DateTimeOffset? SignedByAuditorAt,
    bool AuditeeSigned,
    DateTimeOffset? SignedByAuditeeAt
);

public record UpdateCapaRequest(
    string Title,
    string ActionType,
    string Priority,
    string? Description = null,
    string? RootCause = null,
    string? AssignedToEmail = null,
    string? DueDate = null
);

public record CapaStatusRequest(string Status);
