using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/controls")]
[Authorize]
public class ControlsController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContext) : ControllerBase
{
    private Guid? CurrentOrgId => httpContext.HttpContext?.Items["CurrentOrgId"] as Guid?;

    // ── CRUD ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var controls = await unitOfWork.Controls.GetByOrgAsync(CurrentOrgId.Value, ct);
        return Ok(controls.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var control = await unitOfWork.Controls.GetByIdAsync(id, ct);
        if (control is null || control.OrgId != CurrentOrgId) return NotFound();
        return Ok(MapToDetailDto(control));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateControlRequest request, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var control = Control.Create(CurrentOrgId.Value, request.Code, request.Title,
            request.Description, request.Category, request.Owner);
        await unitOfWork.Controls.AddAsync(control, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = control.Id }, MapToDetailDto(control));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateControlRequest request, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var control = await unitOfWork.Controls.GetByIdAsync(id, ct);
        if (control is null || control.OrgId != CurrentOrgId) return NotFound();
        control.Update(request.Title, request.Description, request.Category, request.Owner, request.Status);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDetailDto(control));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var control = await unitOfWork.Controls.GetByIdAsync(id, ct);
        if (control is null || control.OrgId != CurrentOrgId) return NotFound();
        unitOfWork.Controls.Remove(control);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Mappings ──────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/mappings")]
    public async Task<IActionResult> AddMapping(Guid id, [FromBody] AddMappingRequest request, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var control = await unitOfWork.Controls.GetByIdAsync(id, ct);
        if (control is null || control.OrgId != CurrentOrgId) return NotFound();
        var mapping = ControlMapping.Create(id, request.ReferentialId, request.SectionId, request.QuestionId, request.Notes);
        await unitOfWork.Controls.AddMappingAsync(mapping, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapMappingDto(mapping));
    }

    [HttpDelete("{id:guid}/mappings/{mappingId:guid}")]
    public async Task<IActionResult> RemoveMapping(Guid id, Guid mappingId, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var control = await unitOfWork.Controls.GetByIdAsync(id, ct);
        if (control is null || control.OrgId != CurrentOrgId) return NotFound();
        var mapping = await unitOfWork.Controls.GetMappingByIdAsync(mappingId, ct);
        if (mapping is null || mapping.ControlId != id) return NotFound();
        unitOfWork.Controls.RemoveMapping(mapping);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Coverage Analysis ─────────────────────────────────────────────────

    [HttpGet("coverage/{referentialId:guid}")]
    public async Task<IActionResult> GetCoverage(Guid referentialId, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();

        var referential = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(referentialId, ct);
        if (referential is null) return NotFound();

        var mappings = (await unitOfWork.Controls.GetMappingsByReferentialAsync(referentialId, ct)).ToList();

        // Only include mappings from this org's controls
        mappings = mappings
            .Where(m => m.Control?.OrgId == CurrentOrgId)
            .ToList();

        var allQuestions = referential.Sections
            .SelectMany(s => s.Questions.Select(q => (Section: s, Question: q)))
            .ToList();

        var questionDtos = allQuestions.Select(sq =>
        {
            var qMappings = mappings
                .Where(m => m.QuestionId == sq.Question.Id ||
                            (m.SectionId == sq.Question.SectionId && m.QuestionId is null) ||
                            (m.SectionId is null && m.QuestionId is null))
                .Select(m => new CoverageControlRef(m.Control!.Id, m.Control.Code, m.Control.Title, m.Control.Status))
                .DistinctBy(c => c.ControlId)
                .ToList();

            return new QuestionCoverageDto(
                sq.Question.Id,
                sq.Question.Code,
                sq.Question.Question,
                sq.Question.Criticality,
                sq.Question.SectionId,
                qMappings
            );
        }).ToList();

        var sectionDtos = referential.Sections.Select(s =>
        {
            var sQuestions = questionDtos.Where(q => q.SectionId == s.Id).ToList();
            var covered = sQuestions.Count(q => q.Controls.Count > 0);
            return new SectionCoverageDto(
                s.Id, s.Title,
                sQuestions.Count, covered,
                sQuestions.Count == 0 ? 0 : Math.Round((double)covered / sQuestions.Count * 100, 1)
            );
        }).ToList();

        var totalQ = questionDtos.Count;
        var coveredQ = questionDtos.Count(q => q.Controls.Count > 0);

        return Ok(new ReferentialCoverageDto(
            referentialId,
            referential.Name,
            referential.Code,
            totalQ, coveredQ,
            totalQ == 0 ? 0 : Math.Round((double)coveredQ / totalQ * 100, 1),
            sectionDtos,
            questionDtos
        ));
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static ControlDto MapToDto(Control c) => new(
        c.Id, c.OrgId, c.Code, c.Title, c.Description, c.Category, c.Owner,
        c.Status, c.Mappings.Count, c.CreatedAt, c.UpdatedAt);

    private static ControlDetailDto MapToDetailDto(Control c) => new(
        c.Id, c.OrgId, c.Code, c.Title, c.Description, c.Category, c.Owner, c.Status,
        c.Mappings.Select(MapMappingDto).ToList(), c.CreatedAt, c.UpdatedAt);

    private static ControlMappingDto MapMappingDto(ControlMapping m) => new(
        m.Id, m.ControlId, m.ReferentialId, m.SectionId, m.QuestionId, m.Notes, m.CreatedAt);
}
