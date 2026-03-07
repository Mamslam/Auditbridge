namespace AuditBridge.Domain.Entities;

public class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public OrganizationType Type { get; private set; }
    public string Plan { get; private set; } = "starter";
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public string CountryCode { get; private set; } = string.Empty;
    public string Language { get; private set; } = "fr";
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation — mutable list required by EF Core, exposed as read-only
    private readonly List<User> _users = [];
    private readonly List<AuditTemplate> _templates = [];
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    public IReadOnlyCollection<AuditTemplate> Templates => _templates.AsReadOnly();

    private Organization() { }

    public static Organization Create(
        string name,
        OrganizationType type,
        string countryCode,
        string language)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            throw new ArgumentException("Country code must be 2 characters.", nameof(countryCode));

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            CountryCode = countryCode.ToUpperInvariant(),
            Language = language,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateStripeInfo(string customerId, string subscriptionId)
    {
        StripeCustomerId = customerId;
        StripeSubscriptionId = subscriptionId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePlan(string plan)
    {
        Plan = plan;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum OrganizationType
{
    Auditor,
    Client
}
