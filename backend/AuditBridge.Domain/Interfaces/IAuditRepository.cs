using AuditBridge.Domain.Entities;

namespace AuditBridge.Domain.Interfaces;

public interface IAuditRepository
{
    // ── Audits ────────────────────────────────────────────────────────────
    Task<IEnumerable<Audit>> GetByOrgAsync(Guid orgId, CancellationToken ct = default);
    Task<Audit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Audit?> GetByClientTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(Audit audit, CancellationToken ct = default);

    // ── Responses ────────────────────────────────────────────────────────
    Task AddResponseAsync(AuditResponse response, CancellationToken ct = default);
    Task AddReportAsync(AuditReport report, CancellationToken ct = default);
    Task<AuditReport?> GetReportByAuditIdAsync(Guid auditId, CancellationToken ct = default);

    // ── Findings ─────────────────────────────────────────────────────────
    Task<IEnumerable<AuditFinding>> GetFindingsByAuditIdAsync(Guid auditId, CancellationToken ct = default);
    Task<AuditFinding?> GetFindingByIdAsync(Guid id, CancellationToken ct = default);
    Task AddFindingAsync(AuditFinding finding, CancellationToken ct = default);
    Task DeleteFindingAsync(Guid id, CancellationToken ct = default);

    // ── CAPAs ─────────────────────────────────────────────────────────────
    Task AddCapaAsync(AuditCapa capa, CancellationToken ct = default);
    Task<AuditCapa?> GetCapaByIdAsync(Guid id, CancellationToken ct = default);

    // ── Evidence ─────────────────────────────────────────────────────────
    Task<IEnumerable<AuditEvidence>> GetEvidenceByAuditIdAsync(Guid auditId, CancellationToken ct = default);
    Task AddEvidenceAsync(AuditEvidence evidence, CancellationToken ct = default);
    Task DeleteEvidenceAsync(Guid id, CancellationToken ct = default);

    // ── Analytics ─────────────────────────────────────────────────────────
    Task<IEnumerable<AuditCapa>> GetOpenCapasByOrgAsync(Guid orgId, CancellationToken ct = default);
    Task<IEnumerable<AuditReport>> GetReportsByOrgAsync(Guid orgId, CancellationToken ct = default);
    Task<IEnumerable<AuditFinding>> GetAllFindingsByOrgAsync(Guid orgId, CancellationToken ct = default);
}
