namespace AuditBridge.Domain.Entities;

/// <summary>
/// An audit lifecycle entity.
///
/// Status machine:
///   draft → active     (Activate — generates client token)
///   active → submitted (Submit — client has answered)
///   submitted → completed (Complete — auditor has reviewed all responses)
///   completed → archived (Archive)
///
/// Conformity values stored on AuditResponse:
///   compliant | minor | major | critical | na | pending
/// </summary>
public class Audit
{
    public Guid Id { get; private set; }
    public Guid OrgId { get; private set; }
    public Guid ReferentialId { get; private set; }
    public string TemplateSnapshot { get; private set; } = "{}";  // JSON
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Status { get; private set; } = "draft";
    public Guid AuditorId { get; private set; }
    public string? ClientOrgName { get; private set; }
    public string? ClientEmail { get; private set; }
    public string? ClientToken { get; private set; }
    public DateTimeOffset? ClientTokenExpiresAt { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string? Scope { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Referential? Referential { get; private set; }
    private readonly List<AuditResponse> _responses = [];
    private readonly List<AuditCapa> _capas = [];
    private readonly List<AuditFinding> _findings = [];
    private readonly List<AuditEvidence> _evidence = [];
    public IReadOnlyCollection<AuditResponse> Responses => _responses.AsReadOnly();
    public IReadOnlyCollection<AuditCapa> Capas => _capas.AsReadOnly();
    public IReadOnlyCollection<AuditFinding> Findings => _findings.AsReadOnly();
    public IReadOnlyCollection<AuditEvidence> Evidence => _evidence.AsReadOnly();

    private Audit() { }

    public static Audit Create(
        Guid orgId,
        Guid referentialId,
        string title,
        Guid auditorId,
        string? clientOrgName,
        string? clientEmail,
        string templateSnapshot,
        DateOnly? dueDate = null,
        string? scope = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new()
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            ReferentialId = referentialId,
            TemplateSnapshot = templateSnapshot,
            Title = title,
            Description = description,
            Status = "draft",
            AuditorId = auditorId,
            ClientOrgName = clientOrgName,
            ClientEmail = clientEmail,
            DueDate = dueDate,
            Scope = scope,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Activate the audit: generate a client portal token.
    /// Allowed from: draft.
    /// </summary>
    public void Activate()
    {
        if (Status != "draft")
            throw new InvalidOperationException($"Cannot activate an audit with status '{Status}'. Expected 'draft'.");

        Status = "active";
        ClientToken = Guid.NewGuid().ToString("N");
        ClientTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Submit the audit (client has completed their responses).
    /// Allowed from: active.
    /// </summary>
    public void Submit()
    {
        if (Status != "active")
            throw new InvalidOperationException($"Cannot submit an audit with status '{Status}'. Expected 'active'.");

        Status = "submitted";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Complete the audit: auditor has reviewed all responses and findings.
    /// Allowed from: submitted.
    /// </summary>
    public void Complete()
    {
        if (Status != "submitted")
            throw new InvalidOperationException($"Cannot complete an audit with status '{Status}'. Expected 'submitted'.");

        Status = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Archive the audit. Allowed from: completed.
    /// </summary>
    public void Archive()
    {
        if (Status != "completed")
            throw new InvalidOperationException($"Cannot archive an audit with status '{Status}'. Expected 'completed'.");

        Status = "archived";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Force-close the audit from any state (admin action).
    /// </summary>
    public void ForceClose()
    {
        Status = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Update audit metadata (title, scope, due date, description).
    /// Allowed from: draft or active.
    /// </summary>
    public void Update(string title, string? description, string? scope, DateOnly? dueDate)
    {
        if (Status is "completed" or "archived")
            throw new InvalidOperationException("Cannot update a completed or archived audit.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title;
        Description = description;
        Scope = scope;
        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Extend the client portal token expiry by 30 days.
    /// </summary>
    public void RefreshClientToken()
    {
        if (Status != "active")
            throw new InvalidOperationException("Only active audits have a client token.");

        ClientTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
