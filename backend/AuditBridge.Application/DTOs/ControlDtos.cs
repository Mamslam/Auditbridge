namespace AuditBridge.Application.DTOs;

public record CreateControlRequest(
    string Code,
    string Title,
    string? Description = null,
    string? Category = null,
    string? Owner = null
);

public record UpdateControlRequest(
    string Title,
    string? Description,
    string? Category,
    string? Owner,
    string Status
);

public record AddMappingRequest(
    Guid ReferentialId,
    Guid? SectionId = null,
    Guid? QuestionId = null,
    string? Notes = null
);

// ── Response DTOs ─────────────────────────────────────────────────────────

public record ControlMappingDto(
    Guid Id,
    Guid ControlId,
    Guid ReferentialId,
    Guid? SectionId,
    Guid? QuestionId,
    string? Notes,
    DateTimeOffset CreatedAt
);

public record ControlDto(
    Guid Id,
    Guid OrgId,
    string Code,
    string Title,
    string? Description,
    string? Category,
    string? Owner,
    string Status,
    int MappingCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record ControlDetailDto(
    Guid Id,
    Guid OrgId,
    string Code,
    string Title,
    string? Description,
    string? Category,
    string? Owner,
    string Status,
    List<ControlMappingDto> Mappings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// ── Coverage DTOs ─────────────────────────────────────────────────────────

public record CoverageControlRef(Guid ControlId, string Code, string Title, string Status);

public record QuestionCoverageDto(
    Guid QuestionId,
    string? QuestionCode,
    string QuestionText,
    string Criticality,
    Guid? SectionId,
    List<CoverageControlRef> Controls
);

public record SectionCoverageDto(
    Guid SectionId,
    string SectionTitle,
    int TotalQuestions,
    int CoveredQuestions,
    double CoveragePercent
);

public record ReferentialCoverageDto(
    Guid ReferentialId,
    string ReferentialName,
    string ReferentialCode,
    int TotalQuestions,
    int CoveredQuestions,
    double CoveragePercent,
    List<SectionCoverageDto> Sections,
    List<QuestionCoverageDto> Questions
);
