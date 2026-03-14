using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class ControlRepository(AppDbContext db) : IControlRepository
{
    public Task<IEnumerable<Control>> GetByOrgAsync(Guid orgId, CancellationToken ct = default)
        => db.Controls
            .Include(c => c.Mappings)
            .Where(c => c.OrgId == orgId)
            .OrderBy(c => c.Code)
            .ToListAsync(ct)
            .ContinueWith(t => (IEnumerable<Control>)t.Result, ct);

    public Task<Control?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Controls
            .Include(c => c.Mappings)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<IEnumerable<ControlMapping>> GetMappingsByReferentialAsync(Guid referentialId, CancellationToken ct = default)
        => db.ControlMappings
            .Include(m => m.Control)
            .Where(m => m.ReferentialId == referentialId)
            .ToListAsync(ct)
            .ContinueWith(t => (IEnumerable<ControlMapping>)t.Result, ct);

    public Task<ControlMapping?> GetMappingByIdAsync(Guid mappingId, CancellationToken ct = default)
        => db.ControlMappings.FirstOrDefaultAsync(m => m.Id == mappingId, ct);

    public async Task AddAsync(Control control, CancellationToken ct = default)
        => await db.Controls.AddAsync(control, ct);

    public async Task AddMappingAsync(ControlMapping mapping, CancellationToken ct = default)
        => await db.ControlMappings.AddAsync(mapping, ct);

    public void Remove(Control control) => db.Controls.Remove(control);

    public void RemoveMapping(ControlMapping mapping) => db.ControlMappings.Remove(mapping);
}
