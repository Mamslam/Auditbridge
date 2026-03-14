namespace AuditBridge.Domain.Entities;

/// <summary>
/// Maps a Control to a specific question (or section, or whole referential) in a referential.
/// Enables cross-framework evidence reuse and gap analysis.
/// </summary>
public class ControlMapping
{
    public Guid Id { get; private set; }
    public Guid ControlId { get; private set; }
    public Guid ReferentialId { get; private set; }

    /// <summary>Optional — narrows to a section within the referential.</summary>
    public Guid? SectionId { get; private set; }

    /// <summary>Optional — narrows to a specific question.</summary>
    public Guid? QuestionId { get; private set; }

    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public Control? Control { get; private set; }

    private ControlMapping() { }

    public static ControlMapping Create(Guid controlId, Guid referentialId,
        Guid? sectionId = null, Guid? questionId = null, string? notes = null)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            ControlId = controlId,
            ReferentialId = referentialId,
            SectionId = sectionId,
            QuestionId = questionId,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
