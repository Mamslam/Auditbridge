using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;

namespace AuditBridge.Application.UseCases.Users;

public class SyncClerkUserUseCase(IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(SyncClerkUserRequest request, CancellationToken ct = default)
    {
        var existingUser = await unitOfWork.Users.GetByClerkIdAsync(request.ClerkId, ct);

        switch (request.EventType)
        {
            case "user.created":
                // User created — no org yet, org will be assigned on onboarding completion
                break;

            case "user.updated":
                if (existingUser is not null)
                {
                    // User data is updated via the organization creation flow
                    // Additional sync logic can be added here
                }
                break;

            case "user.deleted":
                if (existingUser is not null)
                {
                    await unitOfWork.AuditTrail.LogAsync(
                        AuditTrail.Create(
                            tenantId: existingUser.OrganizationId,
                            action: "user.deleted",
                            entityType: "user",
                            entityId: existingUser.Id,
                            actorId: existingUser.Id,
                            actorType: "system"),
                        ct);
                }
                break;

            default:
                // Unknown event type — log and ignore
                break;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
