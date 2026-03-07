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
        if (string.IsNullOrWhiteSpace(request.OwnerClerkId))
            throw new ValidationException(nameof(request.OwnerClerkId), "Owner Clerk ID is required.");

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
            request.OwnerClerkId,
            org.Id,
            request.OwnerEmail,
            ownerRole,
            request.OwnerFullName);

        await unitOfWork.Users.AddAsync(owner, ct);

        // Log creation in audit trail
        await unitOfWork.AuditTrail.LogAsync(
            AuditTrail.Create(
                action: "organization.created",
                resourceType: "organization",
                userId: owner.Id,
                organizationId: org.Id,
                resourceId: org.Id),
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
