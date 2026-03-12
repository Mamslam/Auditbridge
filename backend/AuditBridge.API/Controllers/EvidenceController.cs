using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using AuditBridge.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBridge.API.Controllers;

/// <summary>
/// Evidence file management for audits.
///
/// Upload flow:
///   1. POST /api/audits/{id}/evidence/presign  → get signed upload URL
///   2. Client PUTs file directly to Supabase Storage using that URL
///   3. POST /api/audits/{id}/evidence           → register evidence metadata
///
/// Download flow:
///   GET /api/audits/{id}/evidence/{evidenceId}/download → signed download URL (1h TTL)
/// </summary>
[ApiController]
[Route("api/audits/{auditId:guid}/evidence")]
[Authorize]
public class EvidenceController(
    IUnitOfWork unitOfWork,
    StorageService storageService,
    IHttpContextAccessor httpContext)
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

        var evidence = await unitOfWork.Audits.GetEvidenceByAuditIdAsync(auditId, ct);
        return Ok(evidence.Select(MapDto));
    }

    // ── Presign upload URL ────────────────────────────────────────────────

    [HttpPost("presign")]
    public async Task<IActionResult> Presign(
        Guid auditId, [FromQuery] string fileName, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("fileName query parameter is required.");

        try
        {
            var signed = await storageService.GetSignedUploadUrlAsync(
                CurrentOrgId!.Value, auditId, fileName, ct);
            return Ok(new SignedUploadUrlResponse(signed.SignedUrl, signed.StoragePath, signed.ExpiresAt));
        }
        catch (InvalidOperationException ex)
        {
            // Storage not configured (dev environment) — return a mock
            return Ok(new SignedUploadUrlResponse(
                SignedUrl: $"/dev-upload/{auditId}/{fileName}",
                StoragePath: $"{CurrentOrgId}/{auditId}/{Guid.NewGuid():N}/{fileName}",
                ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10)));
        }
    }

    // ── Register evidence after upload ────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Register(
        Guid auditId, [FromBody] RegisterEvidenceRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var evidence = AuditEvidence.Create(
            auditId: auditId,
            uploadedBy: CurrentUserId.Value,
            fileName: request.FileName,
            storagePath: request.StoragePath,
            fileSizeBytes: request.FileSizeBytes,
            mimeType: request.MimeType,
            findingId: request.FindingId,
            responseId: request.ResponseId,
            capaId: request.CapaId,
            description: request.Description);

        await unitOfWork.Audits.AddEvidenceAsync(evidence, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapDto(evidence));
    }

    // ── Download ──────────────────────────────────────────────────────────

    [HttpGet("{evidenceId:guid}/download")]
    public async Task<IActionResult> GetDownloadUrl(
        Guid auditId, Guid evidenceId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var evidenceList = await unitOfWork.Audits.GetEvidenceByAuditIdAsync(auditId, ct);
        var evidence = evidenceList.FirstOrDefault(e => e.Id == evidenceId);
        if (evidence is null) return NotFound();

        try
        {
            var url = await storageService.GetSignedDownloadUrlAsync(evidence.StoragePath, ct);
            return Ok(new SignedDownloadUrlResponse(url));
        }
        catch (InvalidOperationException)
        {
            // Dev fallback
            return Ok(new SignedDownloadUrlResponse($"/dev-download/{evidence.StoragePath}"));
        }
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [HttpDelete("{evidenceId:guid}")]
    public async Task<IActionResult> Delete(Guid auditId, Guid evidenceId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(auditId, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var evidenceList = await unitOfWork.Audits.GetEvidenceByAuditIdAsync(auditId, ct);
        var evidence = evidenceList.FirstOrDefault(e => e.Id == evidenceId);
        if (evidence is null) return NotFound();

        // Attempt to delete from storage (non-blocking)
        _ = storageService.DeleteFileAsync(evidence.StoragePath, ct);

        await unitOfWork.Audits.DeleteEvidenceAsync(evidenceId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Mapping ───────────────────────────────────────────────────────────

    private static EvidenceDto MapDto(AuditEvidence e) => new(
        e.Id, e.AuditId, e.FindingId, e.ResponseId, e.CapaId,
        e.FileName, e.StoragePath, e.FileSizeBytes, e.MimeType,
        e.Description, e.CreatedAt);
}
