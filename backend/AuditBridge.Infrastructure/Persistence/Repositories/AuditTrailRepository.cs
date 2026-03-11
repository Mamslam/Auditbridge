using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class AuditTrailRepository(AppDbContext dbContext) : IAuditTrailRepository
{
    public async Task LogAsync(AuditTrail entry, CancellationToken ct = default)
        => await dbContext.AuditTrails.AddAsync(entry, ct);

    public async Task<IReadOnlyList<AuditTrail>> GetByTenantAsync(
        Guid tenantId, int limit = 100, CancellationToken ct = default)
        => await dbContext.AuditTrails
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}
