namespace AuditBridge.Domain.Entities;

/// <summary>
/// Immutable audit trail entry. Must never be modified or deleted.
/// Protected at the DB level by a PostgreSQL trigger.
/// </summary>
public class AuditTrail
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public Guid? ResourceId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditTrail() { }

    public static AuditTrail Create(
        string action,
        string resourceType,
        Guid? userId = null,
        Guid? organizationId = null,
        Guid? resourceId = null,
        Guid? campaignId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadataJson = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        return new AuditTrail
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            CampaignId = campaignId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            MetadataJson = metadataJson,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
