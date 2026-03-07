using AuditBridge.Domain.Entities;
using FluentAssertions;

namespace AuditBridge.Tests.Domain;

public class AuditCampaignTests
{
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid AuditorOrgId = Guid.NewGuid();
    private static readonly Guid ClientOrgId = Guid.NewGuid();
    private static readonly Guid LeadAuditorId = Guid.NewGuid();

    private static AuditCampaign CreateCampaign(string title = "Test Audit ISO 27001") =>
        AuditCampaign.Create(TemplateId, AuditorOrgId, ClientOrgId, LeadAuditorId, title);

    [Fact]
    public void Create_WithValidData_ShouldCreateCampaign()
    {
        // Act
        var campaign = CreateCampaign("ISO 27001 Audit 2025");

        // Assert
        campaign.Id.Should().NotBe(Guid.Empty);
        campaign.Title.Should().Be("ISO 27001 Audit 2025");
        campaign.Status.Should().Be(CampaignStatus.Draft);
        campaign.AuditorOrgId.Should().Be(AuditorOrgId);
        campaign.ClientOrgId.Should().Be(ClientOrgId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldThrow(string title)
    {
        // Act
        var act = () => CreateCampaign(title);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*title*");
    }

    [Theory]
    [InlineData(CampaignStatus.Draft, CampaignStatus.Sent, true)]
    [InlineData(CampaignStatus.Sent, CampaignStatus.InProgress, true)]
    [InlineData(CampaignStatus.InProgress, CampaignStatus.ClientSubmitted, true)]
    [InlineData(CampaignStatus.ClientSubmitted, CampaignStatus.UnderReview, true)]
    [InlineData(CampaignStatus.UnderReview, CampaignStatus.ReportGenerated, true)]
    [InlineData(CampaignStatus.ReportGenerated, CampaignStatus.Closed, true)]
    [InlineData(CampaignStatus.Draft, CampaignStatus.Closed, false)]
    [InlineData(CampaignStatus.Sent, CampaignStatus.Draft, false)]
    [InlineData(CampaignStatus.Closed, CampaignStatus.Draft, false)]
    public void TransitionTo_ShouldEnforceValidWorkflow(
        CampaignStatus from, CampaignStatus to, bool shouldSucceed)
    {
        // Arrange - we need to get the campaign into the right state
        var campaign = CreateCampaign();
        // Fast-forward status by transitioning through valid states
        ForceStatus(campaign, from);

        // Act
        var result = campaign.TransitionTo(to);

        // Assert
        result.Should().Be(shouldSucceed);
        if (shouldSucceed)
            campaign.Status.Should().Be(to);
        else
            campaign.Status.Should().Be(from);
    }

    [Fact]
    public void GenerateClientAccessToken_ShouldSetTokenAndExpiry()
    {
        // Arrange
        var campaign = CreateCampaign();
        var before = DateTimeOffset.UtcNow;

        // Act
        campaign.GenerateClientAccessToken(expirationHours: 48);

        // Assert
        campaign.ClientAccessToken.Should().NotBeNullOrEmpty();
        campaign.ClientAccessExpiresAt.Should().NotBeNull();
        campaign.ClientAccessExpiresAt.Should().BeOnOrAfter(before.AddHours(47));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(75.5)]
    public void UpdateComplianceScore_WithValidScore_ShouldUpdate(decimal score)
    {
        // Arrange
        var campaign = CreateCampaign();

        // Act
        campaign.UpdateComplianceScore(score);

        // Assert
        campaign.ComplianceScore.Should().Be(score);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(101, 100)]
    public void UpdateComplianceScore_ShouldClampToValidRange(decimal input, decimal expected)
    {
        // Arrange
        var campaign = CreateCampaign();

        // Act
        campaign.UpdateComplianceScore(input);

        // Assert
        campaign.ComplianceScore.Should().Be(expected);
    }

    // Helper to force a campaign into a specific status for testing
    private static void ForceStatus(AuditCampaign campaign, CampaignStatus targetStatus)
    {
        var transitions = new[]
        {
            CampaignStatus.Draft,
            CampaignStatus.Sent,
            CampaignStatus.InProgress,
            CampaignStatus.ClientSubmitted,
            CampaignStatus.UnderReview,
            CampaignStatus.ReportGenerated,
            CampaignStatus.Closed,
        };

        var targetIndex = Array.IndexOf(transitions, targetStatus);
        for (var i = 0; i < targetIndex; i++)
        {
            campaign.TransitionTo(transitions[i + 1]);
        }
    }
}
