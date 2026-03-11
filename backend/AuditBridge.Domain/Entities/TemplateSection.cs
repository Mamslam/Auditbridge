namespace AuditBridge.Domain.Entities;

public class TemplateSection
{
    public Guid Id { get; private set; }
    public Guid ReferentialId { get; private set; }
    public Guid? ParentId { get; private set; }
    public int OrderIndex { get; private set; }
    public string? Code { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<TemplateQuestion> _questions = [];
    public IReadOnlyCollection<TemplateQuestion> Questions => _questions.AsReadOnly();

    private TemplateSection() { }

    public static TemplateSection Create(
        Guid referentialId, string title, int orderIndex = 0,
        string? code = null, Guid? parentId = null, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ReferentialId = referentialId,
            ParentId = parentId,
            OrderIndex = orderIndex,
            Code = code,
            Title = title,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(string title, string? code, string? description, int orderIndex)
    {
        Title = title;
        Code = code;
        Description = description;
        OrderIndex = orderIndex;
    }
}
