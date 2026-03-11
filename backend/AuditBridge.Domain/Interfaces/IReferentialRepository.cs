using AuditBridge.Domain.Entities;

namespace AuditBridge.Domain.Interfaces;

public interface IReferentialRepository
{
    Task<IEnumerable<Referential>> GetAllAccessibleAsync(Guid? orgId, CancellationToken ct = default);
    Task<Referential?> GetByIdWithQuestionsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ReferentialCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task AddAsync(Referential referential, CancellationToken ct = default);
    Task AddCategoryAsync(ReferentialCategory category, CancellationToken ct = default);
    Task AddSectionAsync(TemplateSection section, CancellationToken ct = default);
    Task AddQuestionAsync(TemplateQuestion question, CancellationToken ct = default);
    void Remove(Referential referential);
    void RemoveSection(TemplateSection section);
    void RemoveQuestion(TemplateQuestion question);
}
