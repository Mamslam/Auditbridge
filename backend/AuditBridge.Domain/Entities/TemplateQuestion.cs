namespace AuditBridge.Domain.Entities;

public class TemplateQuestion
{
    public Guid Id { get; private set; }
    public Guid ReferentialId { get; private set; }
    public Guid? SectionId { get; private set; }
    public int OrderIndex { get; private set; }
    public string? Code { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string? Guidance { get; private set; }
    public string AnswerType { get; private set; } = "text";
    // 'text' | 'yesno' | 'yesno_na' | 'scale_1_5' | 'select' | 'multiselect' | 'date' | 'file_only'
    public string? AnswerOptions { get; private set; }   // JSON array
    public bool IsMandatory { get; private set; } = true;
    public string Criticality { get; private set; } = "major";
    // 'critical' | 'major' | 'minor' | 'observation'
    public string[]? ExpectedEvidence { get; private set; }
    public string[]? Tags { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }

    private TemplateQuestion() { }

    public static TemplateQuestion Create(
        Guid referentialId,
        string question,
        string answerType = "text",
        string criticality = "major",
        int orderIndex = 0,
        string? code = null,
        Guid? sectionId = null,
        string? guidance = null,
        bool isMandatory = true,
        string[]? expectedEvidence = null,
        string[]? tags = null)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question text is required.", nameof(question));

        return new()
        {
            Id = Guid.NewGuid(),
            ReferentialId = referentialId,
            SectionId = sectionId,
            OrderIndex = orderIndex,
            Code = code,
            Question = question,
            Guidance = guidance,
            AnswerType = answerType,
            IsMandatory = isMandatory,
            Criticality = criticality,
            ExpectedEvidence = expectedEvidence,
            Tags = tags,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string question, string? guidance, string answerType,
        string criticality, bool isMandatory, int orderIndex,
        string? code, string[]? expectedEvidence, string[]? tags)
    {
        Question = question;
        Guidance = guidance;
        AnswerType = answerType;
        Criticality = criticality;
        IsMandatory = isMandatory;
        OrderIndex = orderIndex;
        Code = code;
        ExpectedEvidence = expectedEvidence;
        Tags = tags;
    }
}
