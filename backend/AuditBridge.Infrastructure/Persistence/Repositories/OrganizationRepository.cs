using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class OrganizationRepository(AppDbContext dbContext) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Organization> AddAsync(Organization organization, CancellationToken ct = default)
    {
        await dbContext.Organizations.AddAsync(organization, ct);
        return organization;
    }

    public Task UpdateAsync(Organization organization, CancellationToken ct = default)
    {
        dbContext.Organizations.Update(organization);
        return Task.CompletedTask;
    }
}
