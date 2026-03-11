using AuditBridge.Domain.Entities;

namespace AuditBridge.Tests.Domain;

public class ReferentialTests
{
    [Fact]
    public void CreateSystem_SetsSystemFlags()
    {
        var ref_ = Referential.CreateSystem("ISO_9001", "ISO 9001:2015", null, "2015");

        ref_.IsSystem.Should().BeTrue();
        ref_.IsPublic.Should().BeTrue();
        ref_.OrgId.Should().BeNull();
    }

    [Fact]
    public void CreateCustom_SetsOrgId()
    {
        var orgId = Guid.NewGuid();
        var ref_ = Referential.CreateCustom(orgId, "MY_REF", "Mon référentiel");

        ref_.OrgId.Should().Be(orgId);
        ref_.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void CreateCustom_EmptyName_Throws()
    {
        var act = () => Referential.CreateCustom(Guid.NewGuid(), "CODE", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DuplicateFrom_CreatesNewRefWithOrgId()
    {
        var source = Referential.CreateSystem("ISO_9001", "ISO 9001:2015", null);
        var orgId = Guid.NewGuid();

        var copy = Referential.DuplicateFrom(source, orgId, "MY_ISO");

        copy.Id.Should().NotBe(source.Id);
        copy.OrgId.Should().Be(orgId);
        copy.IsSystem.Should().BeFalse();
        copy.Name.Should().Contain("copie");
    }
}
