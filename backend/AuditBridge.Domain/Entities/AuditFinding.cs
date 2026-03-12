namespace AuditBridge.Domain.Entities;

/// <summary>
/// A formal finding documented during an audit.
/// Distinct from AuditResponse (raw answer) — a finding is the auditor's professional conclusion:
/// a non-conformity, observation, or opportunity for improvement.
///
/// FindingType:   nc_critical | nc_major | nc_minor | observation | ofi
/// Status:        open | acknowledged | closed
/// </summary>
public class AuditFinding
{
    public Guid Id { get; private set; }
    public Guid AuditId { get; private set; }

    /// <summary>The question that triggered this finding (nullable — can be standalone).</summary>
    public Guid? QuestionId { get; private set; }

    /// <summary>The response that triggered this finding (nullable).</summary>
    public Guid? ResponseId { get; private set; }

    /// <summary>nc_critical | nc_major | nc_minor | observation | ofi</summary>
    public string FindingType { get; private set; } = "nc_minor";

    public string Title { get; private set; } = string.Empty;

    /// <summary>What was observed / what the gap is.</summary>
    public string? Description { get; private set; }

    /// <summary>Evidence observed on-site (text, not a file).</summary>
    public string? ObservedEvidence { get; private set; }

    /// <summary>Standard article reference, e.g. "ISO 27001 A.8.24".</summary>
    public string? RegulatoryRef { get; private set; }

    /// <summary>open | acknowledged | closed</summary>
    public string Status { get; private set; } = "open";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    private readonly List<AuditCapa> _capas = [];
    public IReadOnlyCollection<AuditCapa> Capas => _capas.AsReadOnly();

    private AuditFinding() { }

    public static AuditFinding Create(
        Guid auditId,
        string findingType,
        string title,
        Guid? questionId = null,
        Guid? responseId = null,
        string? description = null,
        string? observedEvidence = null,
        string? regulatoryRef = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        var validTypes = new[] { "nc_critical", "nc_major", "nc_minor", "observation", "ofi" };
        if (!validTypes.Contains(findingType))
            throw new ArgumentException($"Invalid finding type '{findingType}'.", nameof(findingType));

        return new()
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            FindingType = findingType,
            Title = title,
            QuestionId = questionId,
            ResponseId = responseId,
            Description = description,
            ObservedEvidence = observedEvidence,
            RegulatoryRef = regulatoryRef,
            Status = "open",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string title, string findingType, string? description,
        string? observedEvidence, string? regulatoryRef)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        Title = title;
        FindingType = findingType;
        Description = description;
        ObservedEvidence = observedEvidence;
        RegulatoryRef = regulatoryRef;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Acknowledge()
    {
        if (Status != "open")
            throw new InvalidOperationException($"Cannot acknowledge a finding with status '{Status}'.");
        Status = "acknowledged";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        Status = "closed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
