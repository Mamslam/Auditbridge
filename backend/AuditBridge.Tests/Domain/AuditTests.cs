using AuditBridge.Domain.Entities;

namespace AuditBridge.Tests.Domain;

public class AuditTests
{
    private static Audit MakeAudit() =>
        Audit.Create(Guid.NewGuid(), Guid.NewGuid(), "Audit ISO 9001", Guid.NewGuid(), "Client XYZ", "client@xyz.fr", "{}");

    [Fact]
    public void Create_ValidData_ReturnsAuditWithDraftStatus()
    {
        var orgId = Guid.NewGuid();
        var refId = Guid.NewGuid();
        var auditorId = Guid.NewGuid();

        var audit = Audit.Create(orgId, refId, "Audit ISO 9001", auditorId, "Client XYZ", "client@xyz.fr", "{}");

        audit.OrgId.Should().Be(orgId);
        audit.Status.Should().Be("draft");
        audit.Title.Should().Be("Audit ISO 9001");
        audit.ClientToken.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        var act = () => Audit.Create(Guid.NewGuid(), Guid.NewGuid(), "", Guid.NewGuid(), "Client", "c@c.fr", "{}");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_SetsActiveStatusAndToken()
    {
        var audit = MakeAudit();
        audit.Activate();

        audit.Status.Should().Be("active");
        audit.ClientToken.Should().NotBeNullOrEmpty();
        audit.ClientTokenExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Activate_FromNonDraft_Throws()
    {
        var audit = MakeAudit();
        audit.Activate(); // now active

        var act = () => audit.Activate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*draft*");
    }

    [Fact]
    public void Submit_SetsSubmittedStatus()
    {
        var audit = MakeAudit();
        audit.Activate();
        audit.Submit();

        audit.Status.Should().Be("submitted");
    }

    [Fact]
    public void Submit_FromNonActive_Throws()
    {
        var audit = MakeAudit(); // draft

        var act = () => audit.Submit();
        act.Should().Throw<InvalidOperationException>().WithMessage("*active*");
    }

    [Fact]
    public void Complete_SetsCompletedStatus()
    {
        var audit = MakeAudit();
        audit.Activate();
        audit.Submit();
        audit.Complete();

        audit.Status.Should().Be("completed");
    }

    [Fact]
    public void Complete_FromNonSubmitted_Throws()
    {
        var audit = MakeAudit();
        audit.Activate(); // active, not submitted

        var act = () => audit.Complete();
        act.Should().Throw<InvalidOperationException>().WithMessage("*submitted*");
    }

    [Fact]
    public void Archive_FromCompleted_SetsArchivedStatus()
    {
        var audit = MakeAudit();
        audit.Activate();
        audit.Submit();
        audit.Complete();
        audit.Archive();

        audit.Status.Should().Be("archived");
    }

    [Fact]
    public void Archive_FromNonCompleted_Throws()
    {
        var audit = MakeAudit();
        audit.Activate();
        audit.Submit();
        // status = submitted, not completed

        var act = () => audit.Archive();
        act.Should().Throw<InvalidOperationException>().WithMessage("*completed*");
    }

    [Fact]
    public void ForceClose_SetsCompletedFromAnyStatus()
    {
        var audit = MakeAudit(); // draft
        audit.ForceClose();

        audit.Status.Should().Be("completed");
    }

    [Fact]
    public void Update_OnCompletedAudit_Throws()
    {
        var audit = MakeAudit();
        audit.Activate();
        audit.Submit();
        audit.Complete();

        var act = () => audit.Update("New Title", null, null, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RefreshClientToken_OnNonActive_Throws()
    {
        var audit = MakeAudit(); // draft

        var act = () => audit.RefreshClientToken();
        act.Should().Throw<InvalidOperationException>().WithMessage("*active*");
    }
}
