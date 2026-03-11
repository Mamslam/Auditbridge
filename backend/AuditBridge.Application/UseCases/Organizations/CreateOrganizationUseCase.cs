using AuditBridge.Application.DTOs;
using AuditBridge.Application.Exceptions;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;

namespace AuditBridge.Application.UseCases.Organizations;

public class CreateOrganizationUseCase(IUnitOfWork unitOfWork)
{
    public async Task<OrganizationDto> ExecuteAsync(
        CreateOrganizationRequest request,
        CancellationToken ct = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException(nameof(request.Name), "Organization name is required.");

        // Dev fallback when no Clerk session present
        var ownerClerkId = string.IsNullOrWhiteSpace(request.OwnerClerkId)
            ? $"dev_{Guid.NewGuid():N}"
            : request.OwnerClerkId;
        var ownerEmail = string.IsNullOrWhiteSpace(request.OwnerEmail)
            ? $"{ownerClerkId}@dev.local"
            : request.OwnerEmail;

        // Create organization
        var org = Organization.Create(
            request.Name,
            request.Type,
            request.CountryCode,
            request.Language);

        await unitOfWork.Organizations.AddAsync(org, ct);

        // Create owner user
        var ownerRole = request.Type == OrganizationType.Auditor
            ? UserRole.AuditorLead
            : UserRole.ClientAdmin;

        var owner = User.Create(
            ownerClerkId,
            org.Id,
            ownerEmail,
            ownerRole,
            request.OwnerFullName);

        await unitOfWork.Users.AddAsync(owner, ct);

        await unitOfWork.AuditTrail.LogAsync(
            AuditTrail.Create(
                tenantId: org.Id,
                action: "organization.created",
                entityType: "organization",
                entityId: org.Id,
                actorId: owner.Id,
                actorType: "system"),
            ct);

        await unitOfWork.SaveChangesWithTenantAsync(org.Id, ct);

        return MapToDto(org);
    }

    private static OrganizationDto MapToDto(Organization org) => new(
        org.Id,
        org.Name,
        org.Type.ToString().ToLowerInvariant(),
        org.Plan,
        org.CountryCode,
        org.Language,
        org.LogoUrl,
        org.IsActive,
        org.CreatedAt);
}
