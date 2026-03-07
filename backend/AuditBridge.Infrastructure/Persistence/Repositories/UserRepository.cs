using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByClerkIdAsync(string clerkId, CancellationToken ct = default)
        => await dbContext.Users.FirstOrDefaultAsync(u => u.ClerkId == clerkId, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await dbContext.Users.AddAsync(user, ct);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        dbContext.Users.Update(user);
        return Task.CompletedTask;
    }
}
