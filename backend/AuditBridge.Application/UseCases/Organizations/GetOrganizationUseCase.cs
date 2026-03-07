using AuditBridge.Application.DTOs;
using AuditBridge.Application.Exceptions;
using AuditBridge.Domain.Interfaces;

namespace AuditBridge.Application.UseCases.Organizations;

public class GetOrganizationUseCase(IUnitOfWork unitOfWork)
{
    public async Task<OrganizationDto> ExecuteAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await unitOfWork.Organizations.GetByIdAsync(organizationId, ct)
            ?? throw new NotFoundException("Organization", organizationId);

        return new OrganizationDto(
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
}
