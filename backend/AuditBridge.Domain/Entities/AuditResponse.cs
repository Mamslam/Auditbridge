namespace AuditBridge.Domain.Entities;

public class AuditResponse
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid RespondedBy { get; private set; }
    public string? ResponseValue { get; private set; }
    public string? ResponseDataJson { get; private set; }
    public string? AuditorNote { get; private set; }
    public AuditorRating? AuditorRating { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AuditResponse() { }

    public static AuditResponse Create(
        Guid campaignId,
        Guid questionId,
        Guid respondedBy)
    {
        return new AuditResponse
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            QuestionId = questionId,
            RespondedBy = respondedBy,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetResponse(string? value, string? dataJson = null)
    {
        ResponseValue = value;
        ResponseDataJson = dataJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Submit()
    {
        SubmittedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddAuditorNote(string note, AuditorRating? rating = null)
    {
        AuditorNote = note;
        AuditorRating = rating;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum AuditorRating
{
    Compliant,
    Minor,
    Major,
    Critical,
    NA
}
