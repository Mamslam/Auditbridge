namespace AuditBridge.Domain.Entities;

public class ReferentialCategory
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string ColorHex { get; private set; } = "#6B7280";
    public string? Icon { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ReferentialCategory() { }

    public static ReferentialCategory Create(string slug, string label, string colorHex, string? icon = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Label = label,
            ColorHex = colorHex,
            Icon = icon,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
