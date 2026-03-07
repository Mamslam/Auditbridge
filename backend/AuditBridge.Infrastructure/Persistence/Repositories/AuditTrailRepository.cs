using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class AuditTrailRepository(AppDbContext dbContext) : IAuditTrailRepository
{
    public async Task LogAsync(AuditTrail entry, CancellationToken ct = default)
    {
        await dbContext.AuditTrails.AddAsync(entry, ct);
        // Note: SaveChanges is called by UnitOfWork
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByCampaignAsync(
        Guid campaignId,
        int limit = 100,
        string? afterCursor = null,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditTrails
            .Where(a => a.CampaignId == campaignId)
            .OrderByDescending(a => a.CreatedAt);

        if (afterCursor is not null && DateTimeOffset.TryParse(afterCursor, out var cursor))
        {
            query = (IOrderedQueryable<AuditTrail>)query.Where(a => a.CreatedAt < cursor);
        }

        return await query.Take(limit).ToListAsync(ct);
    }
}
