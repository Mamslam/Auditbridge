using AuditBridge.Application.DTOs;
using AuditBridge.Application.Exceptions;
using AuditBridge.Application.UseCases.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Produces("application/json")]
public class OrganizationsController(
    CreateOrganizationUseCase createOrg,
    GetOrganizationUseCase getOrg) : ControllerBase
{
    /// <summary>Create a new organization (called during onboarding).</summary>
    [HttpPost]
    [AllowAnonymous] // Auth happens after org creation (onboarding flow)
    [ProducesResponseType(typeof(OrganizationDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken ct)
    {
        try
        {
            var org = await createOrg.ExecuteAsync(request, ct);
            return Created($"/api/organizations/{org.Id}", org);
        }
        catch (Application.Exceptions.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get the current user's organization.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(OrganizationDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var orgIdStr = HttpContext.Items["CurrentOrgId"]?.ToString();
        if (orgIdStr is null || !Guid.TryParse(orgIdStr, out var orgId))
            return Unauthorized(new { message = "Organization context not set." });

        try
        {
            var org = await getOrg.ExecuteAsync(orgId, ct);
            return Ok(org);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
