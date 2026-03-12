using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class AuditRepository(AppDbContext dbContext) : IAuditRepository
{
    // ── Audits ────────────────────────────────────────────────────────────

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
            .Include(a => a.Findings)
                .ThenInclude(f => f.Capas)
            .Include(a => a.Evidence)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Audit?> GetByClientTokenAsync(string token, CancellationToken ct = default)
        => await dbContext.Audits
            .Include(a => a.Referential)
            .Include(a => a.Responses)
            .FirstOrDefaultAsync(a => a.ClientToken == token
                && a.ClientTokenExpiresAt > DateTimeOffset.UtcNow, ct);

    public async Task AddAsync(Audit audit, CancellationToken ct = default)
        => await dbContext.Audits.AddAsync(audit, ct);

    // ── Responses ─────────────────────────────────────────────────────────

    public async Task AddResponseAsync(AuditResponse response, CancellationToken ct = default)
        => await dbContext.AuditResponses.AddAsync(response, ct);

    public async Task AddReportAsync(AuditReport report, CancellationToken ct = default)
        => await dbContext.AuditReports.AddAsync(report, ct);

    public async Task<AuditReport?> GetReportByAuditIdAsync(Guid auditId, CancellationToken ct = default)
        => await dbContext.AuditReports.FirstOrDefaultAsync(r => r.AuditId == auditId, ct);

    // ── Findings ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<AuditFinding>> GetFindingsByAuditIdAsync(Guid auditId, CancellationToken ct = default)
        => await dbContext.AuditFindings
            .Include(f => f.Capas)
            .Where(f => f.AuditId == auditId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);

    public async Task<AuditFinding?> GetFindingByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.AuditFindings
            .Include(f => f.Capas)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task AddFindingAsync(AuditFinding finding, CancellationToken ct = default)
        => await dbContext.AuditFindings.AddAsync(finding, ct);

    public async Task DeleteFindingAsync(Guid id, CancellationToken ct = default)
    {
        var finding = await dbContext.AuditFindings.FindAsync([id], ct);
        if (finding is not null) dbContext.AuditFindings.Remove(finding);
    }

    // ── CAPAs ─────────────────────────────────────────────────────────────

    public async Task AddCapaAsync(AuditCapa capa, CancellationToken ct = default)
        => await dbContext.AuditCapas.AddAsync(capa, ct);

    public async Task<AuditCapa?> GetCapaByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.AuditCapas.FindAsync([id], ct);

    // ── Evidence ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<AuditEvidence>> GetEvidenceByAuditIdAsync(Guid auditId, CancellationToken ct = default)
        => await dbContext.AuditEvidence
            .Where(e => e.AuditId == auditId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

    public async Task AddEvidenceAsync(AuditEvidence evidence, CancellationToken ct = default)
        => await dbContext.AuditEvidence.AddAsync(evidence, ct);

    public async Task DeleteEvidenceAsync(Guid id, CancellationToken ct = default)
    {
        var evidence = await dbContext.AuditEvidence.FindAsync([id], ct);
        if (evidence is not null) dbContext.AuditEvidence.Remove(evidence);
    }
}
