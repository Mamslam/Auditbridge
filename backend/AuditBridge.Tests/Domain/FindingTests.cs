using AuditBridge.Domain.Entities;

namespace AuditBridge.Tests.Domain;

public class FindingTests
{
    private static AuditFinding MakeFinding(string type = "nc_minor", string title = "Missing document") =>
        AuditFinding.Create(Guid.NewGuid(), type, title);

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_DefaultStatusIsOpen()
    {
        var auditId = Guid.NewGuid();
        var finding = AuditFinding.Create(auditId, "nc_major", "Procédure non documentée");

        finding.AuditId.Should().Be(auditId);
        finding.FindingType.Should().Be("nc_major");
        finding.Title.Should().Be("Procédure non documentée");
        finding.Status.Should().Be("open");
        finding.QuestionId.Should().BeNull();
        finding.Capas.Should().BeEmpty();
    }

    [Theory]
    [InlineData("nc_critical")]
    [InlineData("nc_major")]
    [InlineData("nc_minor")]
    [InlineData("observation")]
    [InlineData("ofi")]
    public void Create_AllValidTypes_Succeeds(string type)
    {
        var finding = AuditFinding.Create(Guid.NewGuid(), type, "Title");
        finding.FindingType.Should().Be(type);
    }

    [Fact]
    public void Create_InvalidType_Throws()
    {
        var act = () => AuditFinding.Create(Guid.NewGuid(), "blocker", "Title");
        act.Should().Throw<ArgumentException>().WithMessage("*blocker*");
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        var act = () => AuditFinding.Create(Guid.NewGuid(), "nc_minor", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithOptionalFields_PopulatesCorrectly()
    {
        var questionId = Guid.NewGuid();
        var responseId = Guid.NewGuid();

        var finding = AuditFinding.Create(
            Guid.NewGuid(), "nc_critical", "Missing SOP",
            questionId: questionId,
            responseId: responseId,
            description: "SOP 4.2 not implemented",
            observedEvidence: "Checked on-site, no document",
            regulatoryRef: "ISO 9001 §7.5.1");

        finding.QuestionId.Should().Be(questionId);
        finding.ResponseId.Should().Be(responseId);
        finding.Description.Should().Be("SOP 4.2 not implemented");
        finding.ObservedEvidence.Should().Be("Checked on-site, no document");
        finding.RegulatoryRef.Should().Be("ISO 9001 §7.5.1");
    }

    // ── Acknowledge ───────────────────────────────────────────────────────

    [Fact]
    public void Acknowledge_FromOpen_SetsAcknowledgedStatus()
    {
        var finding = MakeFinding();
        finding.Acknowledge();

        finding.Status.Should().Be("acknowledged");
    }

    [Fact]
    public void Acknowledge_FromNonOpen_Throws()
    {
        var finding = MakeFinding();
        finding.Acknowledge();

        var act = () => finding.Acknowledge();
        act.Should().Throw<InvalidOperationException>().WithMessage("*acknowledged*");
    }

    // ── Close ─────────────────────────────────────────────────────────────

    [Fact]
    public void Close_SetsClosedStatus()
    {
        var finding = MakeFinding();
        finding.Acknowledge();
        finding.Close();

        finding.Status.Should().Be("closed");
    }

    [Fact]
    public void Close_FromOpen_AlsoWorks()
    {
        // Close() has no guard — can close from any state
        var finding = MakeFinding();
        finding.Close();

        finding.Status.Should().Be("closed");
    }

    // ── Update ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ChangesTitle_AndType()
    {
        var finding = MakeFinding();
        finding.Update("New Title", "observation", "New desc", null, null);

        finding.Title.Should().Be("New Title");
        finding.FindingType.Should().Be("observation");
        finding.Description.Should().Be("New desc");
    }

    [Fact]
    public void Update_EmptyTitle_Throws()
    {
        var finding = MakeFinding();

        var act = () => finding.Update("", "nc_minor", null, null, null);
        act.Should().Throw<ArgumentException>();
    }

    // ── Full workflow ─────────────────────────────────────────────────────

    [Fact]
    public void FullWorkflow_OpenToAcknowledgedToClosed()
    {
        var finding = AuditFinding.Create(
            Guid.NewGuid(), "nc_critical", "Critical gap in ISMS scope",
            regulatoryRef: "ISO 27001 A.5.1");

        finding.Status.Should().Be("open");
        finding.Acknowledge();
        finding.Status.Should().Be("acknowledged");
        finding.Close();
        finding.Status.Should().Be("closed");
    }
}
