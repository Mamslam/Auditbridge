using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/audits")]
[Authorize]
public class AuditsController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContext)
    : ControllerBase
{
    private Guid? CurrentOrgId =>
        httpContext.HttpContext?.Items["CurrentOrgId"] as Guid?;
    private Guid? CurrentUserId =>
        httpContext.HttpContext?.Items["CurrentUserId"] as Guid?;

    // GET /api/audits
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var audits = await unitOfWork.Audits.GetByOrgAsync(CurrentOrgId.Value, ct);
        return Ok(audits.Select(MapToDto));
    }

    // GET /api/audits/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        return Ok(MapToDetailDto(audit));
    }

    // POST /api/audits
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuditRequest request, CancellationToken ct)
    {
        if (CurrentOrgId is null || CurrentUserId is null) return Unauthorized();

        var referential = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(request.ReferentialId, ct);
        if (referential is null) return BadRequest("Referential not found.");

        // Build template snapshot
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

        DateOnly? dueDate = null;
        if (!string.IsNullOrEmpty(request.DueDate) && DateOnly.TryParse(request.DueDate, out var parsed))
            dueDate = parsed;

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

    // POST /api/audits/{id}/activate
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        audit.Activate();
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(new { audit.ClientToken, audit.ClientTokenExpiresAt });
    }

    // POST /api/audits/{id}/submit
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();
        audit.Submit();
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(audit));
    }

    // PUT /api/audits/{id}/responses
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

    // POST /api/audits/{id}/capas
    [HttpPost("{id:guid}/capas")]
    public async Task<IActionResult> CreateCapa(
        Guid id, [FromBody] CreateCapaRequest request, CancellationToken ct)
    {
        var audit = await unitOfWork.Audits.GetByIdAsync(id, ct);
        if (audit is null || audit.OrgId != CurrentOrgId) return NotFound();

        DateOnly? dueDate = null;
        if (!string.IsNullOrEmpty(request.DueDate) && DateOnly.TryParse(request.DueDate, out var parsed))
            dueDate = parsed;

        var capa = AuditCapa.Create(
            id, request.Title, request.Priority, request.ActionType,
            request.QuestionId, request.ResponseId, request.Description,
            request.AssignedToEmail, dueDate);
        await unitOfWork.Audits.AddCapaAsync(capa, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapCapaDto(capa));
    }

    // Client portal
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
        audit.Submit();
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
        else
        {
            existing.SetAnswer(request.AnswerValue, request.AnswerNotes, true);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }

    private static AuditDto MapToDto(Audit a) => new(
        a.Id, a.OrgId, a.ReferentialId,
        a.Referential?.Name ?? "", a.Referential?.Code ?? "",
        a.Title, a.Description, a.Status,
        a.ClientOrgName, a.ClientEmail,
        a.DueDate?.ToString("yyyy-MM-dd"), a.Scope,
        a.CreatedAt, a.UpdatedAt);

    private static AuditDetailDto MapToDetailDto(Audit a)
    {
        // Parse template snapshot to get sections/questions for the portal
        var sections = new List<AuditSectionDto>();
        try
        {
            var snapshot = JsonDocument.Parse(a.TemplateSnapshot);
            if (snapshot.RootElement.TryGetProperty("Sections", out var sectionsEl) ||
                snapshot.RootElement.TryGetProperty("sections", out sectionsEl))
            {
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
                                Code: q.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : q.GetProperty("code").GetString() ?? "",
                                Text: text ?? "",
                                Guidance: q.TryGetProperty("Guidance", out var g) ? g.GetString() : q.TryGetProperty("guidance", out g) ? g.GetString() : null,
                                AnswerType: q.TryGetProperty("AnswerType", out var at) ? at.GetString() ?? "yes_no" : q.TryGetProperty("answerType", out at) ? at.GetString() ?? "yes_no" : "yes_no",
                                IsMandatory: q.TryGetProperty("IsMandatory", out var im) ? im.GetBoolean() : q.TryGetProperty("isMandatory", out im) && im.GetBoolean(),
                                Criticality: q.TryGetProperty("Criticality", out var cr) ? cr.GetString() ?? "minor" : q.TryGetProperty("criticality", out cr) ? cr.GetString() ?? "minor" : "minor"
                            ));
                        }
                    }
                    sections.Add(new AuditSectionDto(
                        Id: s.TryGetProperty("Id", out var sid) ? sid.GetString()! : s.GetProperty("id").GetString()!,
                        Title: s.TryGetProperty("Title", out var title) ? title.GetString() ?? "" : s.GetProperty("title").GetString() ?? "",
                        OrderIndex: s.TryGetProperty("OrderIndex", out var oi) ? oi.GetInt32() : s.TryGetProperty("orderIndex", out oi) ? oi.GetInt32() : 0,
                        Questions: questions
                    ));
                }
            }
        }
        catch { /* snapshot parse failure — sections stays empty */ }

        return new(
            a.Id, a.OrgId,
            a.Referential is null ? new(Guid.Empty, null, "", "", null, null, false, false, null, 0, 0, default) :
                new(a.Referential.Id, a.Referential.OrgId, a.Referential.Code, a.Referential.Name,
                    a.Referential.Version, a.Referential.Description, a.Referential.IsSystem, a.Referential.IsPublic,
                    null, 0, 0, a.Referential.CreatedAt),
            a.Title, a.Status, a.ClientOrgName, a.ClientEmail,
            a.ClientToken, a.DueDate?.ToString("yyyy-MM-dd"), a.Scope,
            sections,
            a.Responses.Select(r => new ResponseDto(
                r.Id, r.QuestionId, r.AnswerValue, r.AnswerNotes,
                r.Conformity, r.AuditorComment, r.IsFlagged, r.AiAnalysis)).ToList(),
            a.Capas.Select(MapCapaDto).ToList(),
            a.CreatedAt);
    }

    private static CapaDto MapCapaDto(AuditCapa c) => new(
        c.Id, c.Title, c.Description, c.ActionType, c.Priority, c.Status,
        c.AssignedToEmail, c.DueDate?.ToString("yyyy-MM-dd"), c.AiGenerated, c.QuestionId);
}
