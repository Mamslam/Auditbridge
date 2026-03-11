namespace AuditBridge.Application.DTOs;

public record ReferentialCategoryDto(
    Guid Id, string Slug, string Label, string ColorHex, string? Icon);

public record ReferentialDto(
    Guid Id,
    Guid? OrgId,
    string Code,
    string Name,
    string? Version,
    string? Description,
    bool IsSystem,
    bool IsPublic,
    ReferentialCategoryDto? Category,
    int SectionCount,
    int QuestionCount,
    DateTimeOffset CreatedAt
);

public record ReferentialDetailDto(
    Guid Id,
    Guid? OrgId,
    string Code,
    string Name,
    string? Version,
    string? Description,
    bool IsSystem,
    ReferentialCategoryDto? Category,
    List<SectionDto> Sections,
    DateTimeOffset CreatedAt
);

public record SectionDto(
    Guid Id, string? Code, string Title, string? Description,
    int OrderIndex, Guid? ParentId, List<QuestionDto> Questions);

public record QuestionDto(
    Guid Id, string? Code, string Question, string? Guidance,
    string AnswerType, string? AnswerOptions, bool IsMandatory,
    string Criticality, string[]? ExpectedEvidence, string[]? Tags,
    int OrderIndex);

public record CreateReferentialRequest(
    string Code,
    string Name,
    string? Version,
    string? Description,
    Guid? CategoryId
);

public record UpdateReferentialRequest(string Name, string? Description);

public record CreateSectionRequest(
    string Title, string? Code, string? Description,
    int OrderIndex = 0, Guid? ParentId = null);

public record CreateQuestionRequest(
    string Question,
    string AnswerType,
    string Criticality = "major",
    Guid? SectionId = null,
    string? Code = null,
    string? Guidance = null,
    bool IsMandatory = true,
    int OrderIndex = 0,
    string[]? ExpectedEvidence = null,
    string[]? Tags = null
);

public record UpdateQuestionRequest(
    string Question,
    string AnswerType,
    string Criticality,
    string? Code,
    string? Guidance,
    bool IsMandatory,
    int OrderIndex,
    string[]? ExpectedEvidence,
    string[]? Tags
);

public record ReorderRequest(Guid[] OrderedIds);
