using AuditBridge.Domain.Entities;

namespace AuditBridge.Domain.Interfaces;

public interface IAuditTrailRepository
{
    /// <summary>Append-only — never update or delete.</summary>
    Task LogAsync(AuditTrail entry, CancellationToken ct = default);

    Task<IReadOnlyList<AuditTrail>> GetByTenantAsync(
        Guid tenantId,
        int limit = 100,
        CancellationToken ct = default);
}
