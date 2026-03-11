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

public record AuditDetailDto(
    Guid Id,
    Guid OrgId,
    ReferentialDto Referential,
    string Title,
    string Status,
    string? ClientOrgName,
    string? ClientEmail,
    string? ClientToken,
    string? DueDate,
    string? Scope,
    List<AuditSectionDto> Sections,   // parsed from TemplateSnapshot
    List<ResponseDto> Responses,
    List<CapaDto> Capas,
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
    string ActionType,
    string Priority,
    string Status,
    string? AssignedToEmail,
    string? DueDate,
    bool AiGenerated,
    Guid? QuestionId
);
