namespace AuditBridge.Domain.Entities;

public class Referential
{
    public Guid Id { get; private set; }
    public Guid? OrgId { get; private set; }           // NULL = système
    public Guid? CategoryId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Version { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsPublic { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public ReferentialCategory? Category { get; private set; }
    private readonly List<TemplateSection> _sections = [];
    private readonly List<TemplateQuestion> _questions = [];
    public IReadOnlyCollection<TemplateSection> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<TemplateQuestion> Questions => _questions.AsReadOnly();

    private Referential() { }

    public static Referential CreateSystem(
        string code, string name, Guid? categoryId, string? version = null, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            OrgId = null,
            CategoryId = categoryId,
            Code = code,
            Name = name,
            Version = version,
            Description = description,
            IsSystem = true,
            IsPublic = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public static Referential CreateCustom(Guid orgId, string code, string name, Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        return new()
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            CategoryId = categoryId,
            Code = code,
            Name = name,
            IsSystem = false,
            IsPublic = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static Referential DuplicateFrom(Referential source, Guid orgId, string newCode)
        => new()
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            CategoryId = source.CategoryId,
            Code = newCode,
            Name = $"{source.Name} (copie)",
            Version = source.Version,
            Description = source.Description,
            IsSystem = false,
            IsPublic = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
