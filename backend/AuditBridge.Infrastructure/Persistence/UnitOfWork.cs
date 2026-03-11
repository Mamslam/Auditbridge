using AuditBridge.Domain.Interfaces;
using AuditBridge.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private IOrganizationRepository? _organizations;
    private IUserRepository? _users;
    private IAuditTrailRepository? _auditTrail;
    private IReferentialRepository? _referentials;
    private IAuditRepository? _audits;

    public IOrganizationRepository Organizations =>
        _organizations ??= new OrganizationRepository(dbContext);
    public IUserRepository Users =>
        _users ??= new UserRepository(dbContext);
    public IAuditTrailRepository AuditTrail =>
        _auditTrail ??= new AuditTrailRepository(dbContext);
    public IReferentialRepository Referentials =>
        _referentials ??= new ReferentialRepository(dbContext);
    public IAuditRepository Audits =>
        _audits ??= new AuditRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);

    public Task<int> SaveChangesWithTenantAsync(Guid orgId, CancellationToken ct = default)
        => SaveChangesAsync(ct);
}
