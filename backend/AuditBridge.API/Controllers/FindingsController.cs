using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBridge.API.Controllers;

/// <summary>
/// CRUD for audit findings (NC major/minor, observations, OFI).
/// A finding documents what the auditor observed on-site; it's the source for CAPAs.
/// </summary>
[ApiController]
[Route("api/audits/{auditId:guid}/findings")]
[Authorize]
public class FindingsController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContext)
    : ControllerBase
{
    private Guid? CurrentOrgId => httpContext.HttpContext?.Items["CurrentOrgId"] as Guid?;
    private Guid? CurrentUserId => httpContext.HttpContext?.Items["CurrentUserId"] as Guid?;

    // ── List ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid auditId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var findings = await unitOfWork.Audits.GetFindingsByAuditIdAsync(auditId, ct);
        return Ok(findings.Select(AuditsController.MapFindingDto));
    }

    [HttpGet("{findingId:guid}")]
    public async Task<IActionResult> GetById(Guid auditId, Guid findingId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != auditId) return NotFound();
        return Ok(AuditsController.MapFindingDto(finding));
    }

    // ── Create ────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(Guid auditId, [FromBody] CreateFindingRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        AuditFinding finding;
        try
        {
            finding = AuditFinding.Create(
                auditId: auditId,
                findingType: request.FindingType,
                title: request.Title,
                questionId: request.QuestionId,
                responseId: request.ResponseId,
                description: request.Description,
                observedEvidence: request.ObservedEvidence,
                regulatoryRef: request.RegulatoryRef);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        await unitOfWork.Audits.AddFindingAsync(finding, ct);

        // Auto-flag the linked response so it stands out in the response list
        if (request.ResponseId.HasValue)
        {
            var response = audit.Responses.FirstOrDefault(r => r.Id == request.ResponseId.Value);
            response?.Flag(true);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById),
            new { auditId, findingId = finding.Id },
            AuditsController.MapFindingDto(finding));
    }

    // ── Update ────────────────────────────────────────────────────────────

    [HttpPut("{findingId:guid}")]
    public async Task<IActionResult> Update(
        Guid auditId, Guid findingId, [FromBody] UpdateFindingRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != auditId) return NotFound();

        try
        {
            finding.Update(request.Title, request.FindingType, request.Description,
                request.ObservedEvidence, request.RegulatoryRef);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

        await unitOfWork.SaveChangesAsync(ct);
        return Ok(AuditsController.MapFindingDto(finding));
    }

    // ── Status transitions ────────────────────────────────────────────────

    [HttpPost("{findingId:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid auditId, Guid findingId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != auditId) return NotFound();
        try { finding.Acknowledge(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(AuditsController.MapFindingDto(finding));
    }

    [HttpPost("{findingId:guid}/close")]
    public async Task<IActionResult> Close(Guid auditId, Guid findingId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != auditId) return NotFound();
        finding.Close();
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(AuditsController.MapFindingDto(finding));
    }

    // ── Create CAPA from finding ──────────────────────────────────────────

    [HttpPost("{findingId:guid}/capas")]
    public async Task<IActionResult> CreateCapaFromFinding(
        Guid auditId, Guid findingId, [FromBody] CreateCapaRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != auditId) return NotFound();

        // Auto-set priority from finding type when not specified
        var priority = request.Priority == "high" && finding.FindingType == "nc_critical"
            ? "critical"
            : request.Priority;

        DateOnly? dueDate = !string.IsNullOrEmpty(request.DueDate)
            && DateOnly.TryParse(request.DueDate, out var d) ? d : null;

        var capa = AuditCapa.Create(
            auditId: auditId,
            title: request.Title,
            priority: priority,
            actionType: request.ActionType,
            findingId: findingId,
            description: request.Description,
            assignedToEmail: request.AssignedToEmail,
            dueDate: dueDate);

        await unitOfWork.Audits.AddCapaAsync(capa, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(AuditsController.MapCapaDto(capa));
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [HttpDelete("{findingId:guid}")]
    public async Task<IActionResult> Delete(Guid auditId, Guid findingId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != auditId) return NotFound();

        await unitOfWork.Audits.DeleteFindingAsync(findingId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }
}
