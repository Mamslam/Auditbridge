using AuditBridge.Domain.Entities;

namespace AuditBridge.Domain.Interfaces;

public interface IAuditTrailRepository
{
    /// <summary>Append-only — never update or delete.</summary>
    Task LogAsync(AuditTrail entry, CancellationToken ct = default);

    Task<IReadOnlyList<AuditTrail>> GetByCampaignAsync(
        Guid campaignId,
        int limit = 100,
        string? afterCursor = null,
        CancellationToken ct = default);
}
