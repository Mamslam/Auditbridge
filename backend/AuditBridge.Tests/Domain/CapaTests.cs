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
    public void Create_WithFindingId_LinksProperly()
    {
        var findingId = Guid.NewGuid();
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action", findingId: findingId);

        capa.FindingId.Should().Be(findingId);
    }

    [Fact]
    public void StartProgress_FromOpen_SetsInProgress()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action corrective");
        capa.StartProgress();

        capa.Status.Should().Be("in_progress");
    }

    [Fact]
    public void StartProgress_FromNonOpen_Throws()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action");
        capa.StartProgress();

        var act = () => capa.StartProgress();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_SetsPendingVerificationStatus()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action corrective");
        capa.Complete("/evidence/proof.pdf");

        // Complete() goes to pending_verification, not "completed"
        capa.Status.Should().Be("pending_verification");
        capa.CompletedAt.Should().NotBeNull();
        capa.EvidencePath.Should().Be("/evidence/proof.pdf");
    }

    [Fact]
    public void Complete_FromInProgress_Works()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action");
        capa.StartProgress();
        capa.Complete();

        capa.Status.Should().Be("pending_verification");
    }

    [Fact]
    public void Complete_FromVerified_Throws()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action");
        capa.Complete();
        capa.Verify(Guid.NewGuid());

        var act = () => capa.Complete();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Verify_SetsPendingVerificationToVerified()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action");
        capa.Complete();
        var verifierId = Guid.NewGuid();
        capa.Verify(verifierId);

        capa.Status.Should().Be("verified");
        capa.VerifiedBy.Should().Be(verifierId);
    }

    [Fact]
    public void Verify_FromNonPendingVerification_Throws()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action"); // open

        var act = () => capa.Verify(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*pending_verification*");
    }

    [Fact]
    public void Cancel_SetsStatusToCancelled()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action");
        capa.StartProgress();
        capa.Cancel();

        capa.Status.Should().Be("cancelled");
    }

    [Fact]
    public void FullWorkflow_OpenToVerified()
    {
        var capa = AuditCapa.Create(Guid.NewGuid(), "Action", priority: "critical", actionType: "corrective");

        capa.Status.Should().Be("open");
        capa.StartProgress();
        capa.Status.Should().Be("in_progress");
        capa.Complete("/evidence/report.pdf");
        capa.Status.Should().Be("pending_verification");
        capa.Verify(Guid.NewGuid());
        capa.Status.Should().Be("verified");
    }
}
