using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/referentials")]
[Authorize]
public class ReferentialsController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContext)
    : ControllerBase
{
    private Guid? CurrentOrgId =>
        httpContext.HttpContext?.Items["CurrentOrgId"] as Guid?;

    // GET /api/referentials/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var cats = await unitOfWork.Referentials.GetCategoriesAsync(ct);
        var result = cats.Select(c => new ReferentialCategoryDto(c.Id, c.Slug, c.Label, c.ColorHex, c.Icon));
        return Ok(result);
    }

    // GET /api/referentials
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var refs = await unitOfWork.Referentials.GetAllAccessibleAsync(CurrentOrgId, ct);
        var result = refs.Select(MapToDto);
        return Ok(result);
    }

    // GET /api/referentials/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var ref_ = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(id, ct);
        if (ref_ is null) return NotFound();
        return Ok(MapToDetailDto(ref_));
    }

    // GET /api/referentials/search?q=&category=
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? category, CancellationToken ct)
    {
        var all = await unitOfWork.Referentials.GetAllAccessibleAsync(CurrentOrgId, ct);
        var filtered = all.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            filtered = filtered.Where(r =>
                r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Code.Contains(q, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(r => r.Category != null && r.Category.Slug == category);
        return Ok(filtered.Select(MapToDto));
    }

    // POST /api/referentials
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReferentialRequest request, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var ref_ = Referential.CreateCustom(CurrentOrgId.Value, request.Code, request.Name, request.CategoryId);
        await unitOfWork.Referentials.AddAsync(ref_, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = ref_.Id }, MapToDto(ref_));
    }

    // POST /api/referentials/{id}/duplicate
    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var source = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(id, ct);
        if (source is null) return NotFound();

        var copy = Referential.DuplicateFrom(source, CurrentOrgId.Value, $"COPY_{source.Code}");
        await unitOfWork.Referentials.AddAsync(copy, ct);

        // Copy sections + questions
        foreach (var section in source.Sections)
        {
            var newSection = TemplateSection.Create(copy.Id, section.Title, section.OrderIndex, section.Code);
            await unitOfWork.Referentials.AddSectionAsync(newSection, ct);
            await unitOfWork.SaveChangesAsync(ct);

            foreach (var q in section.Questions)
            {
                var newQ = TemplateQuestion.Create(
                    copy.Id, q.Question, q.AnswerType, q.Criticality,
                    q.OrderIndex, q.Code, newSection.Id, q.Guidance,
                    q.IsMandatory, q.ExpectedEvidence, q.Tags);
                await unitOfWork.Referentials.AddQuestionAsync(newQ, ct);
            }
        }
        await unitOfWork.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = copy.Id }, MapToDto(copy));
    }

    // PUT /api/referentials/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReferentialRequest request, CancellationToken ct)
    {
        var ref_ = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(id, ct);
        if (ref_ is null) return NotFound();
        if (ref_.IsSystem) return Forbid();
        ref_.Update(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(MapToDto(ref_));
    }

    // DELETE /api/referentials/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ref_ = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(id, ct);
        if (ref_ is null) return NotFound();
        if (ref_.IsSystem) return Forbid();
        unitOfWork.Referentials.Remove(ref_);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ReferentialDto MapToDto(Referential r) => new(
        r.Id, r.OrgId, r.Code, r.Name, r.Version, r.Description, r.IsSystem, r.IsPublic,
        r.Category is null ? null : new(r.Category.Id, r.Category.Slug, r.Category.Label, r.Category.ColorHex, r.Category.Icon),
        r.Sections.Count, r.Questions.Count, r.CreatedAt);

    private static ReferentialDetailDto MapToDetailDto(Referential r) => new(
        r.Id, r.OrgId, r.Code, r.Name, r.Version, r.Description, r.IsSystem,
        r.Category is null ? null : new(r.Category.Id, r.Category.Slug, r.Category.Label, r.Category.ColorHex, r.Category.Icon),
        r.Sections.OrderBy(s => s.OrderIndex).Select(s => new SectionDto(
            s.Id, s.Code, s.Title, s.Description, s.OrderIndex, s.ParentId,
            s.Questions.OrderBy(q => q.OrderIndex).Select(MapQuestionDto).ToList()
        )).ToList(),
        r.CreatedAt);

    private static QuestionDto MapQuestionDto(TemplateQuestion q) => new(
        q.Id, q.Code, q.Question, q.Guidance, q.AnswerType, q.AnswerOptions,
        q.IsMandatory, q.Criticality, q.ExpectedEvidence, q.Tags, q.OrderIndex);
}
