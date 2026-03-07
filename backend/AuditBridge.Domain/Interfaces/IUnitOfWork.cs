namespace AuditBridge.Domain.Interfaces;

public interface IUnitOfWork
{
    IOrganizationRepository Organizations { get; }
    IUserRepository Users { get; }
    IAuditTrailRepository AuditTrail { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves all changes within an explicit transaction that sets app.current_org_id,
    /// required for onboarding flows where the HTTP context has no org yet.
    /// </summary>
    Task<int> SaveChangesWithTenantAsync(Guid orgId, CancellationToken ct = default);
}
