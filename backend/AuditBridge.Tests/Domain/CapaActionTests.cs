using AuditBridge.Domain.Entities;
using FluentAssertions;

namespace AuditBridge.Tests.Domain;

public class CapaActionTests
{
    private static readonly Guid CampaignId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldCreateCapa()
    {
        // Act
        var capa = CapaAction.Create(CampaignId, "Corriger la documentation", CapaSeverity.Major);

        // Assert
        capa.Id.Should().NotBe(Guid.Empty);
        capa.Title.Should().Be("Corriger la documentation");
        capa.Severity.Should().Be(CapaSeverity.Major);
        capa.Status.Should().Be(CapaStatus.Open);
        capa.AiGenerated.Should().BeFalse();
    }

    [Fact]
    public void MarkInProgress_FromOpen_ShouldSucceed()
    {
        // Arrange
        var capa = CapaAction.Create(CampaignId, "Test", CapaSeverity.Minor);

        // Act
        capa.MarkInProgress();

        // Assert
        capa.Status.Should().Be(CapaStatus.InProgress);
    }

    [Fact]
    public void MarkInProgress_WhenNotOpen_ShouldThrow()
    {
        // Arrange
        var capa = CapaAction.Create(CampaignId, "Test", CapaSeverity.Minor);
        capa.MarkInProgress();

        // Act
        var act = () => capa.MarkInProgress();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Close_WithEvidence_ShouldCloseCapaAndSetEvidence()
    {
        // Arrange
        var capa = CapaAction.Create(CampaignId, "Test", CapaSeverity.Critical);
        capa.MarkInProgress();
        capa.SubmitForVerification();

        // Act
        capa.Close("Photo de la correction effectuée");

        // Assert
        capa.Status.Should().Be(CapaStatus.Closed);
        capa.ClosingEvidence.Should().Be("Photo de la correction effectuée");
        capa.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Close_WithoutEvidence_ShouldThrow()
    {
        // Arrange
        var capa = CapaAction.Create(CampaignId, "Test", CapaSeverity.Minor);
        capa.MarkInProgress();
        capa.SubmitForVerification();

        // Act
        var act = () => capa.Close("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*evidence*");
    }

    [Fact]
    public void Close_WhenNotPendingVerification_ShouldThrow()
    {
        // Arrange
        var capa = CapaAction.Create(CampaignId, "Test", CapaSeverity.Minor);
        capa.MarkInProgress();

        // Act — trying to close without submitting for verification
        var act = () => capa.Close("Some evidence");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
