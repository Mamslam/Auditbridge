using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence.Repositories;

public class ReferentialRepository(AppDbContext dbContext) : IReferentialRepository
{
    public async Task<IEnumerable<Referential>> GetAllAccessibleAsync(Guid? orgId, CancellationToken ct = default)
        => await dbContext.Referentials
            .Include(r => r.Category)
            .Include(r => r.Sections)
            .Include(r => r.Questions)
            .Where(r => r.OrgId == null || r.OrgId == orgId)
            .OrderBy(r => r.IsSystem ? 0 : 1).ThenBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<Referential?> GetByIdWithQuestionsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Referentials
            .Include(r => r.Category)
            .Include(r => r.Sections.OrderBy(s => s.OrderIndex))
            .Include(r => r.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<ReferentialCategory>> GetCategoriesAsync(CancellationToken ct = default)
        => await dbContext.ReferentialCategories.OrderBy(c => c.Label).ToListAsync(ct);

    public async Task AddAsync(Referential referential, CancellationToken ct = default)
        => await dbContext.Referentials.AddAsync(referential, ct);

    public async Task AddCategoryAsync(ReferentialCategory category, CancellationToken ct = default)
        => await dbContext.ReferentialCategories.AddAsync(category, ct);

    public async Task AddSectionAsync(TemplateSection section, CancellationToken ct = default)
        => await dbContext.TemplateSections.AddAsync(section, ct);

    public async Task AddQuestionAsync(TemplateQuestion question, CancellationToken ct = default)
        => await dbContext.TemplateQuestions.AddAsync(question, ct);

    public void Remove(Referential referential)
        => dbContext.Referentials.Remove(referential);

    public void RemoveSection(TemplateSection section)
        => dbContext.TemplateSections.Remove(section);

    public void RemoveQuestion(TemplateQuestion question)
        => dbContext.TemplateQuestions.Remove(question);
}
