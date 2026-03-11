namespace AuditBridge.Domain.Entities;

public class AuditResponse
{
    public Guid Id { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid? AnsweredBy { get; private set; }
    public bool AnsweredByClient { get; private set; }
    public string? AnswerValue { get; private set; }
    public string? AnswerNotes { get; private set; }
    public string? Conformity { get; private set; }
    // 'conform' | 'non_conform' | 'partial' | 'na' | 'pending'
    public string? AuditorComment { get; private set; }
    public bool IsFlagged { get; private set; }
    public string? AiAnalysis { get; private set; }  // JSON
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AuditResponse() { }

    public static AuditResponse Create(
        Guid auditId, Guid questionId,
        Guid? answeredBy = null, bool answeredByClient = false)
        => new()
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            QuestionId = questionId,
            AnsweredBy = answeredBy,
            AnsweredByClient = answeredByClient,
            Conformity = "pending",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public void SetAnswer(string? value, string? notes, bool byClient = false)
    {
        AnswerValue = value;
        AnswerNotes = notes;
        AnsweredByClient = byClient;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetConformity(string conformity, string? auditorComment = null)
    {
        Conformity = conformity;
        AuditorComment = auditorComment;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAiAnalysis(string analysisJson)
    {
        AiAnalysis = analysisJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Flag(bool flagged = true)
    {
        IsFlagged = flagged;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
