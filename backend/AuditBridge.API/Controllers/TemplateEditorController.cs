using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditBridge.Infrastructure.Persistence;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/referentials/{referentialId:guid}")]
[Authorize]
public class TemplateEditorController(IUnitOfWork unitOfWork, AppDbContext db) : ControllerBase
{
    // POST /api/referentials/{id}/sections
    [HttpPost("sections")]
    public async Task<IActionResult> AddSection(
        Guid referentialId, [FromBody] CreateSectionRequest request, CancellationToken ct)
    {
        var ref_ = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(referentialId, ct);
        if (ref_ is null) return NotFound();
        if (ref_.IsSystem) return Forbid();

        var section = TemplateSection.Create(
            referentialId, request.Title, request.OrderIndex,
            request.Code, request.ParentId, request.Description);
        await unitOfWork.Referentials.AddSectionAsync(section, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new SectionDto(section.Id, section.Code, section.Title, section.Description,
            section.OrderIndex, section.ParentId, []));
    }

    // PUT /api/referentials/{id}/sections/{sectionId}
    [HttpPut("sections/{sectionId:guid}")]
    public async Task<IActionResult> UpdateSection(
        Guid referentialId, Guid sectionId,
        [FromBody] CreateSectionRequest request, CancellationToken ct)
    {
        var section = await db.TemplateSections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.ReferentialId == referentialId, ct);
        if (section is null) return NotFound();

        section.Update(request.Title, request.Code, request.Description, request.OrderIndex);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }

    // DELETE /api/referentials/{id}/sections/{sectionId}
    [HttpDelete("sections/{sectionId:guid}")]
    public async Task<IActionResult> DeleteSection(
        Guid referentialId, Guid sectionId, CancellationToken ct)
    {
        var section = await db.TemplateSections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.ReferentialId == referentialId, ct);
        if (section is null) return NotFound();

        unitOfWork.Referentials.RemoveSection(section);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST /api/referentials/{id}/sections/{sectionId}/questions (convenience route)
    [HttpPost("sections/{sectionId:guid}/questions")]
    public Task<IActionResult> AddQuestionToSection(
        Guid referentialId, Guid sectionId,
        [FromBody] CreateQuestionRequest request, CancellationToken ct)
        => AddQuestion(referentialId, request with { SectionId = sectionId }, ct);

    // POST /api/referentials/{id}/questions
    [HttpPost("questions")]
    public async Task<IActionResult> AddQuestion(
        Guid referentialId, [FromBody] CreateQuestionRequest request, CancellationToken ct)
    {
        var ref_ = await unitOfWork.Referentials.GetByIdWithQuestionsAsync(referentialId, ct);
        if (ref_ is null) return NotFound();
        if (ref_.IsSystem) return Forbid();

        var question = TemplateQuestion.Create(
            referentialId, request.Question, request.AnswerType, request.Criticality,
            request.OrderIndex, request.Code, request.SectionId, request.Guidance,
            request.IsMandatory, request.ExpectedEvidence, request.Tags);
        await unitOfWork.Referentials.AddQuestionAsync(question, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new QuestionDto(question.Id, question.Code, question.Question, question.Guidance,
            question.AnswerType, question.AnswerOptions, question.IsMandatory,
            question.Criticality, question.ExpectedEvidence, question.Tags, question.OrderIndex));
    }

    // PUT /api/referentials/{id}/questions/{qId}
    [HttpPut("questions/{qId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid referentialId, Guid qId,
        [FromBody] UpdateQuestionRequest request, CancellationToken ct)
    {
        var question = await db.TemplateQuestions
            .FirstOrDefaultAsync(q => q.Id == qId && q.ReferentialId == referentialId, ct);
        if (question is null) return NotFound();

        question.Update(request.Question, request.Guidance, request.AnswerType,
            request.Criticality, request.IsMandatory, request.OrderIndex,
            request.Code, request.ExpectedEvidence, request.Tags);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }

    // DELETE /api/referentials/{id}/questions/{qId}
    [HttpDelete("questions/{qId:guid}")]
    public async Task<IActionResult> DeleteQuestion(
        Guid referentialId, Guid qId, CancellationToken ct)
    {
        var question = await db.TemplateQuestions
            .FirstOrDefaultAsync(q => q.Id == qId && q.ReferentialId == referentialId, ct);
        if (question is null) return NotFound();

        unitOfWork.Referentials.RemoveQuestion(question);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST /api/referentials/{id}/questions/reorder
    [HttpPost("questions/reorder")]
    public async Task<IActionResult> ReorderQuestions(
        Guid referentialId, [FromBody] ReorderRequest request, CancellationToken ct)
    {
        var questions = await db.TemplateQuestions
            .Where(q => q.ReferentialId == referentialId && request.OrderedIds.Contains(q.Id))
            .ToListAsync(ct);

        for (int i = 0; i < request.OrderedIds.Length; i++)
        {
            var q = questions.FirstOrDefault(x => x.Id == request.OrderedIds[i]);
            if (q is not null)
                q.Update(q.Question, q.Guidance, q.AnswerType, q.Criticality,
                    q.IsMandatory, i, q.Code, q.ExpectedEvidence, q.Tags);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Ok();
    }
}
