namespace AuditBridge.Domain.Entities;

/// <summary>
/// Corrective and Preventive Action (CAPA) linked to an audit finding.
///
/// ActionType:  corrective | preventive | improvement
/// Priority:    critical | high | medium | low
/// Status:      open | in_progress | pending_verification | verified | cancelled
/// </summary>
public class AuditCapa
{
    public Guid Id { get; private set; }
    public Guid AuditId { get; private set; }

    /// <summary>The finding that triggered this CAPA (most common path).</summary>
    public Guid? FindingId { get; private set; }

    /// <summary>Direct link to a response (alternative to FindingId).</summary>
    public Guid? ResponseId { get; private set; }

    /// <summary>Direct link to a question (for context).</summary>
    public Guid? QuestionId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? RootCause { get; private set; }

    /// <summary>corrective | preventive | improvement</summary>
    public string ActionType { get; private set; } = "corrective";

    /// <summary>critical | high | medium | low</summary>
    public string Priority { get; private set; } = "high";

    /// <summary>open | in_progress | pending_verification | verified | cancelled</summary>
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
        Guid? findingId = null, Guid? questionId = null, Guid? responseId = null,
        string? description = null, string? assignedToEmail = null,
        DateOnly? dueDate = null, bool aiGenerated = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new()
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            FindingId = findingId,
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

    public void Update(string title, string? description, string? rootCause,
        string actionType, string priority, string? assignedToEmail, DateOnly? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        Title = title;
        Description = description;
        RootCause = rootCause;
        ActionType = actionType;
        Priority = priority;
        AssignedToEmail = assignedToEmail;
        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartProgress()
    {
        if (Status != "open")
            throw new InvalidOperationException($"Cannot start a CAPA with status '{Status}'.");
        Status = "in_progress";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(string? evidencePath = null)
    {
        if (Status is not ("open" or "in_progress"))
            throw new InvalidOperationException($"Cannot complete a CAPA with status '{Status}'.");
        Status = "pending_verification";
        CompletedAt = DateTimeOffset.UtcNow;
        EvidencePath = evidencePath;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Verify(Guid verifiedBy)
    {
        if (Status != "pending_verification")
            throw new InvalidOperationException($"Cannot verify a CAPA with status '{Status}'. Expected 'pending_verification'.");
        Status = "verified";
        VerifiedBy = verifiedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
