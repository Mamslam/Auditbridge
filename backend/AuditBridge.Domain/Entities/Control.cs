namespace AuditBridge.Domain.Entities;

/// <summary>
/// An organizational control — a reusable policy, procedure, or safeguard.
/// A single control can be mapped to multiple questions across multiple referentials,
/// enabling "test once, cover everywhere" evidence reuse.
///
/// Status: draft | active | retired
/// </summary>
public class Control
{
    public Guid Id { get; private set; }
    public Guid OrgId { get; private set; }

    /// <summary>Short unique identifier within the org, e.g. "CTL-001".</summary>
    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>E.g. "access_control" | "data_protection" | "physical" | "organizational" | "technical"</summary>
    public string? Category { get; private set; }

    /// <summary>Email of the person/team responsible for this control.</summary>
    public string? Owner { get; private set; }

    /// <summary>draft | active | retired</summary>
    public string Status { get; private set; } = "draft";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<ControlMapping> _mappings = [];
    public IReadOnlyCollection<ControlMapping> Mappings => _mappings.AsReadOnly();

    private Control() { }

    public static Control Create(Guid orgId, string code, string title,
        string? description = null, string? category = null, string? owner = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));

        return new()
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            Code = code.Trim().ToUpperInvariant(),
            Title = title,
            Description = description,
            Category = category,
            Owner = owner,
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string title, string? description, string? category, string? owner, string status)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        Title = title;
        Description = description;
        Category = category;
        Owner = owner;
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate() { Status = "active"; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Retire() { Status = "retired"; UpdatedAt = DateTimeOffset.UtcNow; }
}
