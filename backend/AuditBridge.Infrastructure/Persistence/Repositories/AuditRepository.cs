using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class AuditRepository(AppDbContext dbContext) : IAuditRepository
{
    public async Task<IEnumerable<Audit>> GetByOrgAsync(Guid orgId, CancellationToken ct = default)
        => await dbContext.Audits
            .Include(a => a.Referential)
            .Where(a => a.OrgId == orgId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<Audit?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Audits
            .Include(a => a.Referential)
            .Include(a => a.Responses)
            .Include(a => a.Capas)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Audit?> GetByClientTokenAsync(string token, CancellationToken ct = default)
        => await dbContext.Audits
            .Include(a => a.Referential)
            .Include(a => a.Responses)
            .FirstOrDefaultAsync(a => a.ClientToken == token
                && a.ClientTokenExpiresAt > DateTimeOffset.UtcNow, ct);

    public async Task AddAsync(Audit audit, CancellationToken ct = default)
        => await dbContext.Audits.AddAsync(audit, ct);

    public async Task AddResponseAsync(AuditResponse response, CancellationToken ct = default)
        => await dbContext.AuditResponses.AddAsync(response, ct);

    public async Task AddCapaAsync(AuditCapa capa, CancellationToken ct = default)
        => await dbContext.AuditCapas.AddAsync(capa, ct);

    public async Task AddReportAsync(AuditReport report, CancellationToken ct = default)
        => await dbContext.AuditReports.AddAsync(report, ct);
}
