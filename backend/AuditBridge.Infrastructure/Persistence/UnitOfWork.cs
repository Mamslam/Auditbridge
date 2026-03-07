using AuditBridge.Domain.Interfaces;
using AuditBridge.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private IOrganizationRepository? _organizations;
    private IUserRepository? _users;
    private IAuditTrailRepository? _auditTrail;

    public IOrganizationRepository Organizations =>
        _organizations ??= new OrganizationRepository(dbContext);

    public IUserRepository Users =>
        _users ??= new UserRepository(dbContext);

    public IAuditTrailRepository AuditTrail =>
        _auditTrail ??= new AuditTrailRepository(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);

    public Task<int> SaveChangesWithTenantAsync(Guid orgId, CancellationToken ct = default)
        => SaveChangesAsync(ct);
}
