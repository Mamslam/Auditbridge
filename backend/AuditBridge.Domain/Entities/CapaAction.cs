namespace AuditBridge.Domain.Entities;

public class CapaAction
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid? ResponseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CapaSeverity Severity { get; private set; }
    public CapaStatus Status { get; private set; } = CapaStatus.Open;
    public Guid? AssignedTo { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? ClosingEvidence { get; private set; }
    public bool AiGenerated { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CapaAction() { }

    public static CapaAction Create(
        Guid campaignId,
        string title,
        CapaSeverity severity,
        Guid? responseId = null,
        string? description = null,
        Guid? assignedTo = null,
        DateOnly? dueDate = null,
        bool aiGenerated = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("CAPA title is required.", nameof(title));

        return new CapaAction
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ResponseId = responseId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Severity = severity,
            AssignedTo = assignedTo,
            DueDate = dueDate,
            AiGenerated = aiGenerated,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkInProgress()
    {
        if (Status != CapaStatus.Open)
            throw new InvalidOperationException("Can only start an open CAPA.");
        Status = CapaStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SubmitForVerification()
    {
        if (Status != CapaStatus.InProgress)
            throw new InvalidOperationException("CAPA must be in progress to submit for verification.");
        Status = CapaStatus.PendingVerification;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close(string closingEvidence)
    {
        if (Status != CapaStatus.PendingVerification)
            throw new InvalidOperationException("CAPA must be pending verification to close.");
        if (string.IsNullOrWhiteSpace(closingEvidence))
            throw new ArgumentException("Closing evidence is required.", nameof(closingEvidence));

        Status = CapaStatus.Closed;
        ClosingEvidence = closingEvidence;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkOverdue()
    {
        if (Status is CapaStatus.Closed or CapaStatus.Overdue) return;
        Status = CapaStatus.Overdue;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum CapaSeverity { Minor, Major, Critical }

public enum CapaStatus
{
    Open,
    InProgress,
    PendingVerification,
    Closed,
    Overdue
}
