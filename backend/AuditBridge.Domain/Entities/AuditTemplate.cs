namespace AuditBridge.Domain.Entities;

public class AuditTemplate
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AuditFramework Framework { get; private set; }
    public string Version { get; private set; } = "1.0";
    public string Language { get; private set; } = "fr";
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Organization? Organization { get; private set; }
    public List<TemplateSection> Sections { get; private set; } = [];

    private AuditTemplate() { }

    public static AuditTemplate Create(
        Guid organizationId,
        string name,
        AuditFramework framework,
        Guid createdBy,
        string language = "fr",
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));

        return new AuditTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name.Trim(),
            Framework = framework,
            Language = language,
            Description = description?.Trim(),
            IsPublic = false,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void BumpVersion(string newVersion)
    {
        Version = newVersion;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePublic() => IsPublic = true;
    public void MakePrivate() => IsPublic = false;
}

public class TemplateSection
{
    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int OrderIndex { get; private set; }
    public string? FrameworkReference { get; private set; }
    public bool IsMandatory { get; private set; } = true;

    public List<TemplateQuestion> Questions { get; private set; } = [];

    private TemplateSection() { }

    public static TemplateSection Create(
        Guid templateId,
        string title,
        int orderIndex,
        string? description = null,
        string? frameworkReference = null,
        bool isMandatory = true)
    {
        return new TemplateSection
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Title = title.Trim(),
            Description = description?.Trim(),
            OrderIndex = orderIndex,
            FrameworkReference = frameworkReference,
            IsMandatory = isMandatory,
        };
    }
}

public class TemplateQuestion
{
    public Guid Id { get; private set; }
    public Guid SectionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public QuestionType Type { get; private set; }
    public bool IsMandatory { get; private set; } = true;
    public int Weight { get; private set; } = 1;
    public bool GmpCritical { get; private set; }
    public string? RegulatoryReference { get; private set; }
    public string? Guidance { get; private set; }
    public int OrderIndex { get; private set; }

    private TemplateQuestion() { }

    public static TemplateQuestion Create(
        Guid sectionId,
        string text,
        QuestionType type,
        int orderIndex,
        bool isMandatory = true,
        int weight = 1,
        bool gmpCritical = false,
        string? regulatoryReference = null,
        string? guidance = null)
    {
        return new TemplateQuestion
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Text = text.Trim(),
            Type = type,
            IsMandatory = isMandatory,
            Weight = weight,
            GmpCritical = gmpCritical,
            RegulatoryReference = regulatoryReference,
            Guidance = guidance,
            OrderIndex = orderIndex,
        };
    }
}

public enum AuditFramework
{
    GMP,
    EU_GMP,
    ISO_9001,
    ISO_27001,
    ISO_14001,
    NIS2,
    RGPD,
    HACCP,
    CSRD,
    DORA,
    CUSTOM
}

public enum QuestionType
{
    YesNo,
    Rating,
    Text,
    FileUpload,
    MultiSelect
}
