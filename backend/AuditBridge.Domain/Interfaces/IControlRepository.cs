using AuditBridge.Domain.Entities;

namespace AuditBridge.Domain.Interfaces;

public interface IControlRepository
{
    Task<IEnumerable<Control>> GetByOrgAsync(Guid orgId, CancellationToken ct = default);
    Task<Control?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ControlMapping>> GetMappingsByReferentialAsync(Guid referentialId, CancellationToken ct = default);
    Task AddAsync(Control control, CancellationToken ct = default);
    Task AddMappingAsync(ControlMapping mapping, CancellationToken ct = default);
    void Remove(Control control);
    void RemoveMapping(ControlMapping mapping);
    Task<ControlMapping?> GetMappingByIdAsync(Guid mappingId, CancellationToken ct = default);
}
