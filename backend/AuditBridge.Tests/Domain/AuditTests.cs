using AuditBridge.Domain.Entities;

namespace AuditBridge.Tests.Domain;

public class AuditTests
{
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
        var audit = Audit.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), "Client", "c@c.fr", "{}");

        audit.Activate();

        audit.Status.Should().Be("active");
        audit.ClientToken.Should().NotBeNullOrEmpty();
        audit.ClientTokenExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Submit_SetsSubmittedStatus()
    {
        var audit = Audit.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), "Client", "c@c.fr", "{}");
        audit.Activate();
        audit.Submit();
        audit.Status.Should().Be("submitted");
    }
}
