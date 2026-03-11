namespace AuditBridge.Domain.Interfaces;

public interface IUnitOfWork
{
    IOrganizationRepository Organizations { get; }
    IUserRepository Users { get; }
    IAuditTrailRepository AuditTrail { get; }
    IReferentialRepository Referentials { get; }
    IAuditRepository Audits { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Saves within a transaction that sets app.current_org_id for RLS.</summary>
    Task<int> SaveChangesWithTenantAsync(Guid orgId, CancellationToken ct = default);
}
