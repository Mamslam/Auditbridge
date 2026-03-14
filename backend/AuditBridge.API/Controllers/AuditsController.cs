using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using AuditBridge.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/audits")]
[Authorize]
public class AuditsController(
    IUnitOfWork unitOfWork,
    ReportService reportService,
    AiAnalysisService aiAnalysis,
    IHttpContextAccessor httpContext)
    : ControllerBase
{
    private Guid? CurrentOrgId =>
        httpContext.HttpContext?.Items["CurrentOrgId"] as Guid?;
    private Guid? CurrentUserId =>
        httpContext.HttpContext?.Items["CurrentUserId"] as Guid?;

    // ── List ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var audits = await unitOfWork.Audits.GetByOrgAsync(CurrentOrgId.Value, ct);
        return Ok(audits.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        return Ok(MapToDetailDto(audit));
    }

    // ── Create / Update ───────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuditRequest request, CancellationToken ct)
    {
        if (CurrentOrgId is null || CurrentUserId is null) return Unauthorized();

        var referential = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(request.ReferentialId, ct);
        if (referential is null) return BadRequest("Referential not found.");

        var snapshot = JsonSerializer.Serialize(new
        {
            referential.Id,
            referential.Code,
            referential.Name,
            referential.Version,
            Sections = referential.Sections.OrderBy(s => s.OrderIndex).Select(s => new
            {
                s.Id, s.Code, s.Title, s.OrderIndex,
                Questions = s.Questions.OrderBy(q => q.OrderIndex).Select(q => new
                {
                    q.Id, q.Code, q.Question, q.Guidance, q.AnswerType,
                    q.IsMandatory, q.Criticality, q.ExpectedEvidence
                })
            })
        });

        DateOnly? dueDate = ParseDate(request.DueDate);
        var audit = Audit.Create(
            CurrentOrgId.Value, request.ReferentialId, request.Title,
            CurrentUserId.Value, request.ClientOrgName, request.ClientEmail,
            snapshot, dueDate, request.Scope, request.Description);

        await unitOfWork.Audits.AddAsync(audit, ct);
        await unitOfWork.AuditTrail.LogAsync(AuditTrail.Create(
            tenantId: CurrentOrgId.Value,
            action: "audit.created",
            entityType: "audit",
            entityId: audit.Id,
            actorId: CurrentUserId.Value,
            actorType: "auditor"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = audit.Id }, MapToDto(audit));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAuditRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        try { audit.Update(request.Title, request.Description, request.Scope, ParseDate(request.DueDate)); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(audit));
    }

    // ── State transitions ─────────────────────────────────────────────────

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        try { audit.Activate(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(new { audit.ClientToken, audit.ClientTokenExpiresAt });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        try { audit.Submit(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(audit));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        try { audit.Complete(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        await unitOfWork.AuditTrail.LogAsync(AuditTrail.Create(
            tenantId: CurrentOrgId!.Value, action: "audit.completed",
            entityType: "audit", entityId: audit.Id,
            actorId: CurrentUserId!.Value, actorType: "auditor"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(audit));
    }

    [HttpPost("{id:guid}/force-close")]
    public async Task<IActionResult> ForceClose(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        audit.ForceClose();
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(audit));
    }

    [HttpPost("{id:guid}/refresh-token")]
    public async Task<IActionResult> RefreshToken(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        try { audit.RefreshClientToken(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(new { audit.ClientToken, audit.ClientTokenExpiresAt });
    }

    // ── Scoring ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/score")]
    public async Task<IActionResult> GetScore(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var score = reportService.ComputeScore(audit);
        return Ok(MapScoreDto(score));
    }

    // ── Responses ─────────────────────────────────────────────────────────

    [HttpPut("{id:guid}/responses")]
    public async Task<IActionResult> UpsertResponse(
        Guid id, [FromBody] UpsertResponseRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var existing = audit.Responses.FirstOrDefault(r => r.QuestionId == request.QuestionId);
        if (existing is null)
        {
            var response = AuditResponse.Create(id, request.QuestionId, CurrentUserId, request.ByClient);
            response.SetAnswer(request.AnswerValue, request.AnswerNotes, request.ByClient);
            await unitOfWork.Audits.AddResponseAsync(response, ct);
        }
        else
        {
            existing.SetAnswer(request.AnswerValue, request.AnswerNotes, request.ByClient);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPut("{id:guid}/responses/{responseId:guid}/conformity")]
    public async Task<IActionResult> SetConformity(
        Guid id, Guid responseId, [FromBody] SetConformityRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var response = audit.Responses.FirstOrDefault(r => r.Id == responseId);
        if (response is null) return NotFound("Response not found.");

        var validConformity = new[] { "compliant", "minor", "major", "critical", "na", "pending" };
        if (!validConformity.Contains(request.Conformity))
            return BadRequest($"Invalid conformity value '{request.Conformity}'.");

        response.SetConformity(request.Conformity, request.AuditorComment);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(new ResponseDto(response.Id, response.QuestionId, response.AnswerValue,
            response.AnswerNotes, response.Conformity, response.AuditorComment,
            response.IsFlagged, response.AiAnalysis));
    }

    [HttpPut("{id:guid}/responses/{responseId:guid}/flag")]
    public async Task<IActionResult> FlagResponse(
        Guid id, Guid responseId, [FromBody] FlagRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var response = audit.Responses.FirstOrDefault(r => r.Id == responseId);
        if (response is null) return NotFound();
        response.Flag(request.Flagged);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }

    // ── AI Analysis ───────────────────────────────────────────────────────

    [HttpPost("{id:guid}/responses/{responseId:guid}/analyze")]
    public async Task<IActionResult> AnalyzeResponse(
        Guid id, Guid responseId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var response = audit.Responses.FirstOrDefault(r => r.Id == responseId);
        if (response is null) return NotFound("Response not found.");
        if (string.IsNullOrEmpty(response.AnswerValue))
            return BadRequest("Response has no answer to analyze.");

        // Find the question text from snapshot
        var snapshot = JsonDocument.Parse(audit.TemplateSnapshot);
        string questionText = "", questionCode = "", criticality = "major";
        foreach (var s in snapshot.RootElement.GetProperty(
            snapshot.RootElement.TryGetProperty("Sections", out _) ? "Sections" : "sections").EnumerateArray())
        {
            var qsKey = s.TryGetProperty("Questions", out _) ? "Questions" : "questions";
            foreach (var q in s.GetProperty(qsKey).EnumerateArray())
            {
                var qIdProp = q.TryGetProperty("Id", out var qid) ? qid : q.GetProperty("id");
                if (qIdProp.TryGetGuid(out var qGuid) && qGuid == response.QuestionId)
                {
                    questionText = (q.TryGetProperty("Question", out var t) ? t : q.GetProperty("question")).GetString() ?? "";
                    questionCode = (q.TryGetProperty("Code", out var c) ? c : q.TryGetProperty("code", out c) ? c : default).GetString() ?? "";
                    criticality  = (q.TryGetProperty("Criticality", out var cr) ? cr : q.TryGetProperty("criticality", out cr) ? cr : default).GetString() ?? "major";
                    break;
                }
            }
        }

        var result = await aiAnalysis.ClassifyAsync(
            audit.Referential?.Code ?? "", audit.Referential?.Name ?? "",
            questionCode, questionText,
            response.AnswerValue + (response.AnswerNotes is not null ? "\n\nNotes: " + response.AnswerNotes : ""),
            [], ct);

        if (result is null) return StatusCode(502, "AI analysis failed.");

        var analysisJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
            { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        response.SetAiAnalysis(analysisJson);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new ResponseDto(response.Id, response.QuestionId, response.AnswerValue,
            response.AnswerNotes, response.Conformity, response.AuditorComment,
            response.IsFlagged, response.AiAnalysis));
    }

    [HttpPost("{id:guid}/report/narrative")]
    public async Task<IActionResult> GenerateNarrative(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var score = reportService.ComputeScore(audit);
        var findings = await unitOfWork.Audits.GetFindingsByAuditIdAsync(id, ct);
        var capas = audit.Capas.Where(c => c.Status != "cancelled").Select(c => new { c.Title, c.Status, c.Priority }).ToList();

        var narrative = await aiAnalysis.GenerateReportAsync(
            audit.Referential?.Code ?? "", audit.Referential?.Name ?? "",
            audit.ClientOrgName ?? "Non renseigné", audit.Scope ?? "Non défini",
            score.GlobalScore, score.CriticalCount, score.MajorCount, score.MinorCount,
            findings.Select(f => new { f.FindingType, f.Title, f.Description }).ToList(),
            capas, ct);

        if (narrative is null) return StatusCode(502, "AI narrative generation failed.");

        var existing = await unitOfWork.Audits.GetReportByAuditIdAsync(id, ct);
        if (existing is not null)
        {
            existing.SetNarrative(narrative.ExecutiveSummary, JsonSerializer.Serialize(narrative));
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Ok(narrative);
    }

    [HttpPost("{id:guid}/findings/summarize")]
    public async Task<IActionResult> SummarizeFinding(
        Guid id, [FromBody] SummarizeFindingRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        if (string.IsNullOrWhiteSpace(request.RawNotes))
            return BadRequest("rawNotes is required.");

        var result = await aiAnalysis.SummarizeFindingAsync(
            audit.Referential?.Code ?? "", audit.Referential?.Name ?? "",
            request.RawNotes, ct);

        if (result is null) return StatusCode(502, "AI summarization failed.");

        return Ok(new FindingSummaryDto(result.FindingType, result.Title, result.Description,
            result.ObservedEvidence, result.RegulatoryRef, result.Recommendation));
    }

    [HttpPost("{id:guid}/findings/{findingId:guid}/suggest-capa")]
    public async Task<IActionResult> SuggestCapa(
        Guid id, Guid findingId, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var finding = await unitOfWork.Audits.GetFindingByIdAsync(findingId, ct);
        if (finding is null || finding.AuditId != id) return NotFound("Finding not found.");

        var result = await aiAnalysis.SuggestCapaAsync(
            finding.FindingType, finding.Title, finding.Description,
            audit.Referential?.Code ?? "", audit.Referential?.Name ?? "",
            ct);

        if (result is null) return StatusCode(502, "AI CAPA suggestion failed.");

        return Ok(new SuggestCapaDto(result.Title, result.Description, result.RootCause,
            result.ActionType, result.Priority, result.Rationale));
    }

    [HttpPost("{id:guid}/ask")]
    public async Task<IActionResult> AskAudit(
        Guid id, [FromBody] AskAuditRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("question is required.");

        var result = await aiAnalysis.AnswerAuditQuestionAsync(
            request.Question,
            audit.Referential?.Code ?? "", audit.Referential?.Name ?? "",
            request.AuditContext, ct);

        if (result is null) return StatusCode(502, "AI question answering failed.");

        return Ok(new AskAuditDto(result.Answer, result.References, result.Confidence, result.Disclaimer));
    }

    // ── CAPAs ─────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/capas")]
    public async Task<IActionResult> CreateCapa(
        Guid id, [FromBody] CreateCapaRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var capa = AuditCapa.Create(id, request.Title, request.Priority, request.ActionType,
            request.FindingId, request.QuestionId, request.ResponseId,
            request.Description, request.AssignedToEmail, ParseDate(request.DueDate));
        await unitOfWork.Audits.AddCapaAsync(capa, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapCapaDto(capa));
    }

    [HttpPut("{id:guid}/capas/{capaId:guid}")]
    public async Task<IActionResult> UpdateCapa(
        Guid id, Guid capaId, [FromBody] UpdateCapaRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var capa = await unitOfWork.Audits.GetCapaByIdAsync(capaId, ct);
        if (capa is null || capa.AuditId != id) return NotFound();
        capa.Update(request.Title, request.Description, request.RootCause,
            request.ActionType, request.Priority, request.AssignedToEmail, ParseDate(request.DueDate));
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapCapaDto(capa));
    }

    [HttpPost("{id:guid}/capas/{capaId:guid}/status")]
    public async Task<IActionResult> UpdateCapaStatus(
        Guid id, Guid capaId, [FromBody] CapaStatusRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var capa = await unitOfWork.Audits.GetCapaByIdAsync(capaId, ct);
        if (capa is null || capa.AuditId != id) return NotFound();
        try
        {
            switch (request.Status)
            {
                case "in_progress":          capa.StartProgress(); break;
                case "pending_verification": capa.Complete(); break;
                case "verified":             capa.Verify(CurrentUserId!.Value); break;
                case "cancelled":            capa.Cancel(); break;
                default: return BadRequest($"Unknown status transition '{request.Status}'.");
            }
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapCapaDto(capa));
    }

    // ── Report ────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/report")]
    public async Task<IActionResult> GenerateReport(
        Guid id, [FromBody] GenerateReportRequest? request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        var score = reportService.ComputeScore(audit);
        var findings = await unitOfWork.Audits.GetFindingsByAuditIdAsync(id, ct);
        var pdfBytes = reportService.GeneratePdf(audit, score, findings);

        var reportRecord = AuditReport.Create(
            auditId: id, generatedBy: CurrentUserId,
            conformityScore: score.GlobalScore, totalQuestions: score.TotalQuestions,
            conformCount: score.ConformCount, nonConformCount: score.TotalNonConform,
            partialCount: score.MinorCount, naCount: score.NaCount,
            criticalNc: score.CriticalCount, majorNc: score.MajorCount, minorNc: score.MinorCount,
            reportDataJson: JsonSerializer.Serialize(MapScoreDto(score)));

        if (request?.ExecutiveSummary is not null)
            reportRecord.SetNarrative(request.ExecutiveSummary, "");

        reportRecord.SetPdf($"reports/{id}.pdf",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pdfBytes)));

        var existing = await unitOfWork.Audits.GetReportByAuditIdAsync(id, ct);
        if (existing is null)
        {
            await unitOfWork.Audits.AddReportAsync(reportRecord, ct);
        }
        else
        {
            existing.SetPdf(reportRecord.PdfStoragePath!, reportRecord.PdfSha256 ?? "");
            if (request?.ExecutiveSummary is not null)
                existing.SetNarrative(request.ExecutiveSummary, "");
        }

        await unitOfWork.SaveChangesAsync(ct);
        return File(pdfBytes, "application/pdf", $"audit-report-{id}.pdf");
    }

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        var report = await unitOfWork.Audits.GetReportByAuditIdAsync(id, ct);
        if (report is null) return NotFound("No report generated yet.");
        return Ok(new ReportDto(report.Id, report.AuditId, report.ConformityScore,
            report.TotalQuestions, report.ConformCount, report.NonConformCount,
            report.CriticalNc, report.MajorNc, report.MinorNc,
            report.ExecutiveSummary, report.PdfStoragePath, report.GeneratedAt));
    }

    // ── Client portal ─────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet("portal/{token}")]
    public async Task<IActionResult> GetByToken(string token, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByClientTokenAsync(token, ct);
        if (audit is null) return NotFound();
        return Ok(MapToDetailDto(audit));
    }

    [AllowAnonymous]
    [HttpPost("portal/{token}/submit")]
    public async Task<IActionResult> ClientSubmit(string token, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByClientTokenAsync(token, ct);
        if (audit is null) return NotFound();
        try { audit.Submit(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(audit));
    }

    [AllowAnonymous]
    [HttpPut("portal/{token}/responses")]
    public async Task<IActionResult> ClientUpsertResponse(
        string token, [FromBody] UpsertResponseRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByClientTokenAsync(token, ct);
        if (audit is null) return NotFound();
        var existing = audit.Responses.FirstOrDefault(r => r.QuestionId == request.QuestionId);
        if (existing is null)
        {
            var response = AuditResponse.Create(audit.Id, request.QuestionId, null, true);
            response.SetAnswer(request.AnswerValue, request.AnswerNotes, true);
            await unitOfWork.Audits.AddResponseAsync(response, ct);
        }
        else { existing.SetAnswer(request.AnswerValue, request.AnswerNotes, true); }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────

    private static AuditDto MapToDto(Audit a) => new(
        a.Id, a.OrgId, a.ReferentialId,
        a.Referential?.Name ?? "", a.Referential?.Code ?? "",
        a.Title, a.Description, a.Status,
        a.ClientOrgName, a.ClientEmail,
        a.DueDate?.ToString("yyyy-MM-dd"), a.Scope,
        a.CreatedAt, a.UpdatedAt);

    private static AuditDetailDto MapToDetailDto(Audit a) => new(
        a.Id, a.OrgId,
        a.Referential is null
            ? new(Guid.Empty, null, "", "", null, null, false, false, null, 0, 0, default)
            : new(a.Referential.Id, a.Referential.OrgId, a.Referential.Code, a.Referential.Name,
                a.Referential.Version, a.Referential.Description, a.Referential.IsSystem, a.Referential.IsPublic,
                null, 0, 0, a.Referential.CreatedAt),
        a.Title, a.Description, a.Status, a.ClientOrgName, a.ClientEmail,
        a.ClientToken, a.DueDate?.ToString("yyyy-MM-dd"), a.Scope,
        ParseSections(a.TemplateSnapshot),
        a.Responses.Select(r => new ResponseDto(
            r.Id, r.QuestionId, r.AnswerValue, r.AnswerNotes,
            r.Conformity, r.AuditorComment, r.IsFlagged, r.AiAnalysis)).ToList(),
        a.Capas.Select(MapCapaDto).ToList(),
        a.Findings.Select(MapFindingDto).ToList(),
        a.CreatedAt);

    internal static CapaDto MapCapaDto(AuditCapa c) => new(
        c.Id, c.Title, c.Description, c.RootCause, c.ActionType, c.Priority, c.Status,
        c.AssignedToEmail, c.DueDate?.ToString("yyyy-MM-dd"),
        c.CompletedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        c.AiGenerated, c.QuestionId, c.FindingId);

    internal static FindingDto MapFindingDto(AuditFinding f) => new(
        f.Id, f.AuditId, f.QuestionId, f.ResponseId,
        f.FindingType, f.Title, f.Description, f.ObservedEvidence,
        f.RegulatoryRef, f.Status,
        f.Capas.Select(MapCapaDto).ToList(),
        f.CreatedAt, f.UpdatedAt);

    private static AuditScoreDto MapScoreDto(AuditScoreResult s) => new(
        s.GlobalScore, s.TotalQuestions, s.TotalAnswered,
        s.ConformCount, s.MinorCount, s.MajorCount, s.CriticalCount,
        s.NaCount, s.PendingCount,
        s.SectionScores.Select(ss => new SectionScoreDto(
            ss.SectionId.ToString(), ss.Title, ss.ConformityPct,
            ss.ConformCount, ss.MinorCount, ss.MajorCount, ss.CriticalCount,
            ss.NaCount, ss.TotalQuestions)).ToList());

    private static List<AuditSectionDto> ParseSections(string snapshotJson)
    {
        var sections = new List<AuditSectionDto>();
        try
        {
            var snapshot = JsonDocument.Parse(snapshotJson);
            if (!snapshot.RootElement.TryGetProperty("Sections", out var sectionsEl) &&
                !snapshot.RootElement.TryGetProperty("sections", out sectionsEl))
                return sections;

            foreach (var s in sectionsEl.EnumerateArray())
            {
                var questions = new List<AuditQuestionDto>();
                if (s.TryGetProperty("Questions", out var qsEl) || s.TryGetProperty("questions", out qsEl))
                {
                    foreach (var q in qsEl.EnumerateArray())
                    {
                        var text = q.TryGetProperty("Question", out var t) ? t.GetString()
                                 : q.TryGetProperty("question", out t) ? t.GetString() : null;
                        questions.Add(new AuditQuestionDto(
                            Id: q.TryGetProperty("Id", out var id) ? id.GetString()! : q.GetProperty("id").GetString()!,
                            Code: q.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : q.TryGetProperty("code", out code) ? code.GetString() ?? "" : "",
                            Text: text ?? "",
                            Guidance: q.TryGetProperty("Guidance", out var g) ? g.GetString() : q.TryGetProperty("guidance", out g) ? g.GetString() : null,
                            AnswerType: q.TryGetProperty("AnswerType", out var at) ? at.GetString() ?? "yes_no" : q.TryGetProperty("answerType", out at) ? at.GetString() ?? "yes_no" : "yes_no",
                            IsMandatory: q.TryGetProperty("IsMandatory", out var im) ? im.GetBoolean() : q.TryGetProperty("isMandatory", out im) && im.GetBoolean(),
                            Criticality: q.TryGetProperty("Criticality", out var cr) ? cr.GetString() ?? "major" : q.TryGetProperty("criticality", out cr) ? cr.GetString() ?? "major" : "major"
                        ));
                    }
                }
                sections.Add(new AuditSectionDto(
                    Id: s.TryGetProperty("Id", out var sid) ? sid.GetString()! : s.GetProperty("id").GetString()!,
                    Title: s.TryGetProperty("Title", out var title) ? title.GetString() ?? "" : s.TryGetProperty("title", out title) ? title.GetString() ?? "" : "",
                    OrderIndex: s.TryGetProperty("OrderIndex", out var oi) ? oi.GetInt32() : s.TryGetProperty("orderIndex", out oi) ? oi.GetInt32() : 0,
                    Questions: questions
                ));
            }
        }
        catch { /* snapshot parse failure — sections stays empty */ }
        return sections;
    }

    private static DateOnly? ParseDate(string? s) =>
        !string.IsNullOrEmpty(s) && DateOnly.TryParse(s, out var d) ? d : null;
}

public record FlagRequest(bool Flagged);
