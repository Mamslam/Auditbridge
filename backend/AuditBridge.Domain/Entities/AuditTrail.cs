namespace AuditBridge.Domain.Entities;

/// <summary>
/// Immutable audit trail entry. Protected at DB level by trigger.
/// </summary>
public class AuditTrail
{
    public long Id { get; private set; }        // BIGSERIAL
    public Guid TenantId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string? ActorType { get; private set; }  // 'auditor' | 'client' | 'system' | 'ai'
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string? OldValues { get; private set; }  // JSON
    public string? NewValues { get; private set; }  // JSON
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditTrail() { }

    public static AuditTrail Create(
        Guid tenantId,
        string action,
        string entityType,
        Guid entityId,
        Guid? actorId = null,
        string actorType = "system",
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        return new()
        {
            TenantId = tenantId,
            ActorId = actorId,
            ActorType = actorType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
