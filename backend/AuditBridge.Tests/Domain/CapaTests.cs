using AuditBridge.Domain.Entities;

namespace AuditBridge.Tests.Domain;

public class CapaTests
{
    [Fact]
    public void Create_ValidData_DefaultStatusIsOpen()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Mise à jour SOP documentation");

        capa.Status.Should().Be("open");
        capa.Priority.Should().Be("high");
        capa.ActionType.Should().Be("corrective");
        capa.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        var act = () => AuditCapa.Create(Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_SetsCompletedStatus()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action corrective");
        capa.Complete("/evidence/proof.pdf");

        capa.Status.Should().Be("completed");
        capa.CompletedAt.Should().NotBeNull();
        capa.EvidencePath.Should().Be("/evidence/proof.pdf");
    }

    [Fact]
    public void Verify_SetsVerifiedStatus()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action");
        capa.Complete();
        var verifierId = Guid.NewGuid();
        capa.Verify(verifierId);

        capa.Status.Should().Be("verified");
        capa.VerifiedBy.Should().Be(verifierId);
    }
}
