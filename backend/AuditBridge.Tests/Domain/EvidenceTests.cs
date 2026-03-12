using AuditBridge.Domain.Entities;

namespace AuditBridge.Tests.Domain;

public class EvidenceTests
{
    private static readonly Guid AuditId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_PopulatesAllFields()
    {
        var findingId = Guid.NewGuid();

        var evidence = AuditEvidence.Create(
            auditId: AuditId,
            uploadedBy: UserId,
            fileName: "checklist.pdf",
            storagePath: "org1/audit1/abc/checklist.pdf",
            fileSizeBytes: 204_800,
            mimeType: "application/pdf",
            findingId: findingId,
            description: "Audit checklist scan");

        evidence.AuditId.Should().Be(AuditId);
        evidence.UploadedBy.Should().Be(UserId);
        evidence.FileName.Should().Be("checklist.pdf");
        evidence.StoragePath.Should().Be("org1/audit1/abc/checklist.pdf");
        evidence.FileSizeBytes.Should().Be(204_800);
        evidence.MimeType.Should().Be("application/pdf");
        evidence.FindingId.Should().Be(findingId);
        evidence.Description.Should().Be("Audit checklist scan");
        evidence.ResponseId.Should().BeNull();
        evidence.CapaId.Should().BeNull();
        evidence.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_MinimalData_Works()
    {
        var evidence = AuditEvidence.Create(
            AuditId, UserId, "photo.jpg", "org/audit/uuid/photo.jpg", 512_000, "image/jpeg");

        evidence.Id.Should().NotBeEmpty();
        evidence.FindingId.Should().BeNull();
        evidence.Description.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyFileName_Throws()
    {
        var act = () => AuditEvidence.Create(AuditId, UserId, "", "path/file.pdf", 100, "application/pdf");
        act.Should().Throw<ArgumentException>().WithMessage("*FileName*");
    }

    [Fact]
    public void Create_EmptyStoragePath_Throws()
    {
        var act = () => AuditEvidence.Create(AuditId, UserId, "file.pdf", "", 100, "application/pdf");
        act.Should().Throw<ArgumentException>().WithMessage("*StoragePath*");
    }

    [Fact]
    public void Create_LinkedToCapaOnly_Works()
    {
        var capaId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(
            AuditId, UserId, "proof.xlsx", "path/proof.xlsx", 10_240, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            capaId: capaId);

        evidence.CapaId.Should().Be(capaId);
        evidence.FindingId.Should().BeNull();
        evidence.ResponseId.Should().BeNull();
    }

    [Fact]
    public void Create_LinkedToResponse_Works()
    {
        var responseId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(
            AuditId, UserId, "answer.docx", "path/answer.docx", 45_000, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            responseId: responseId);

        evidence.ResponseId.Should().Be(responseId);
    }

    [Fact]
    public void Create_TwoInstances_HaveDifferentIds()
    {
        var e1 = AuditEvidence.Create(AuditId, UserId, "f1.pdf", "path/f1.pdf", 100, "application/pdf");
        var e2 = AuditEvidence.Create(AuditId, UserId, "f2.pdf", "path/f2.pdf", 100, "application/pdf");

        e1.Id.Should().NotBe(e2.Id);
    }
}
