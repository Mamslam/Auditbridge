namespace AuditBridge.Domain.Entities;

public class AuditCapa
{
    public Guid Id { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid? ResponseId { get; private set; }
    public Guid? QuestionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? RootCause { get; private set; }
    public string ActionType { get; private set; } = "corrective";
    public string Priority { get; private set; } = "high";
    public string Status { get; private set; } = "open";
    public string? AssignedToEmail { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public string? EvidencePath { get; private set; }
    public bool AiGenerated { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AuditCapa() { }

    public static AuditCapa Create(
        Guid auditId, string title,
        string priority = "high", string actionType = "corrective",
        Guid? questionId = null, Guid? responseId = null,
        string? description = null, string? assignedToEmail = null,
        DateOnly? dueDate = null, bool aiGenerated = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new()
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            ResponseId = responseId,
            QuestionId = questionId,
            Title = title,
            Description = description,
            ActionType = actionType,
            Priority = priority,
            Status = "open",
            AssignedToEmail = assignedToEmail,
            DueDate = dueDate,
            AiGenerated = aiGenerated,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Complete(string? evidencePath = null)
    {
        Status = "completed";
        CompletedAt = DateTimeOffset.UtcNow;
        EvidencePath = evidencePath;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Verify(Guid verifiedBy)
    {
        Status = "verified";
        VerifiedBy = verifiedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
