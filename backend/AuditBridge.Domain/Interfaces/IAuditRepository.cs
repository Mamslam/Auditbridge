using AuditBridge.Domain.Entities;

namespace AuditBridge.Domain.Interfaces;

public interface IAuditRepository
{
    Task<IEnumerable<Audit>> GetByOrgAsync(Guid orgId, CancellationToken ct = default);
    Task<Audit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Audit?> GetByClientTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(Audit audit, CancellationToken ct = default);
    Task AddResponseAsync(AuditResponse response, CancellationToken ct = default);
    Task AddCapaAsync(AuditCapa capa, CancellationToken ct = default);
    Task AddReportAsync(AuditReport report, CancellationToken ct = default);
}
