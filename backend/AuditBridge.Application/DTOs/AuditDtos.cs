namespace AuditBridge.Application.DTOs;

public record CreateAuditRequest(
    Guid ReferentialId,
    string Title,
    string? ClientOrgName = null,
    string? ClientEmail = null,
    string? Description = null,
    string? DueDate = null,     // ISO date string "2025-09-01"
    string? Scope = null
);

public record AuditDto(
    Guid Id,
    Guid OrgId,
    Guid ReferentialId,
    string ReferentialName,
    string ReferentialCode,
    string Title,
    string? Description,
    string Status,
    string? ClientOrgName,
    string? ClientEmail,
    string? DueDate,
    string? Scope,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record UpdateAuditRequest(
    string Title,
    string? Description = null,
    string? Scope = null,
    string? DueDate = null
);

public record AuditDetailDto(
    Guid Id,
    Guid OrgId,
    ReferentialDto Referential,
    string Title,
    string? Description,
    string Status,
    string? ClientOrgName,
    string? ClientEmail,
    string? ClientToken,
    string? DueDate,
    string? Scope,
    List<AuditSectionDto> Sections,   // parsed from TemplateSnapshot
    List<ResponseDto> Responses,
    List<CapaDto> Capas,
    List<FindingDto> Findings,
    DateTimeOffset CreatedAt
);

public record AuditSectionDto(
    string Id,
    string Title,
    int OrderIndex,
    List<AuditQuestionDto> Questions
);

public record AuditQuestionDto(
    string Id,
    string Code,
    string Text,
    string? Guidance,
    string AnswerType,
    bool IsMandatory,
    string Criticality
);

public record ResponseDto(
    Guid Id,
    Guid QuestionId,
    string? AnswerValue,
    string? AnswerNotes,
    string? Conformity,
    string? AuditorComment,
    bool IsFlagged,
    string? AiAnalysis
);

public record UpsertResponseRequest(
    Guid QuestionId,
    string? AnswerValue,
    string? AnswerNotes,
    bool ByClient = false
);

public record SetConformityRequest(
    string Conformity,
    string? AuditorComment = null
);

public record CreateCapaRequest(
    string Title,
    Guid? FindingId = null,
    Guid? QuestionId = null,
    Guid? ResponseId = null,
    string Priority = "high",
    string ActionType = "corrective",
    string? Description = null,
    string? AssignedToEmail = null,
    string? DueDate = null
);

public record CapaDto(
    Guid Id,
    string Title,
    string? Description,
    string? RootCause,
    string ActionType,
    string Priority,
    string Status,
    string? AssignedToEmail,
    string? DueDate,
    string? CompletedAt,
    bool AiGenerated,
    Guid? QuestionId,
    Guid? FindingId
);

public record CreateCapaFromFindingRequest(
    string Title,
    Guid FindingId,
    string Priority = "high",
    string ActionType = "corrective",
    string? Description = null,
    string? AssignedToEmail = null,
    string? DueDate = null
);

public record AuditScoreDto(
    decimal GlobalScore,
    int TotalQuestions,
    int TotalAnswered,
    int ConformCount,
    int MinorCount,
    int MajorCount,
    int CriticalCount,
    int NaCount,
    int PendingCount,
    List<SectionScoreDto> SectionScores
);

public record SectionScoreDto(
    string SectionId,
    string Title,
    double? ConformityPct,
    int ConformCount,
    int MinorCount,
    int MajorCount,
    int CriticalCount,
    int NaCount,
    int TotalQuestions
);

public record GenerateReportRequest(string? ExecutiveSummary = null);

public record ReportDto(
    Guid Id,
    Guid AuditId,
    decimal? ConformityScore,
    int? TotalQuestions,
    int? ConformCount,
    int? NonConformCount,
    int CriticalNc,
    int MajorNc,
    int MinorNc,
    string? ExecutiveSummary,
    string? PdfStoragePath,
    DateTimeOffset GeneratedAt
);
