namespace AuditBridge.Domain.Entities;

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
    public IReadOnlyCollection<AuditResponse> Responses => _responses.AsReadOnly();
    public IReadOnlyCollection<AuditCapa> Capas => _capas.AsReadOnly();

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

    public void Activate()
    {
        Status = "active";
        ClientToken = Guid.NewGuid().ToString("N");
        ClientTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Submit()
    {
        Status = "submitted";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        Status = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = "archived";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
