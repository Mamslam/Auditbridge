using AuditBridge.Domain.Entities;
using FluentAssertions;

namespace AuditBridge.Tests.Domain;

public class OrganizationTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateOrganization()
    {
        // Arrange & Act
        var org = Organization.Create("Dupont Audit", OrganizationType.Auditor, "FR", "fr");

        // Assert
        org.Id.Should().NotBe(Guid.Empty);
        org.Name.Should().Be("Dupont Audit");
        org.Type.Should().Be(OrganizationType.Auditor);
        org.CountryCode.Should().Be("FR");
        org.Language.Should().Be("fr");
        org.IsActive.Should().BeTrue();
        org.Plan.Should().Be("starter");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrow(string name)
    {
        // Act
        var act = () => Organization.Create(name, OrganizationType.Auditor, "FR", "fr");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData("FRA")]
    [InlineData("F")]
    [InlineData("")]
    public void Create_WithInvalidCountryCode_ShouldThrow(string countryCode)
    {
        // Act
        var act = () => Organization.Create("Test", OrganizationType.Auditor, countryCode, "fr");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Country code*");
    }

    [Fact]
    public void UpdatePlan_ShouldChangePlan()
    {
        // Arrange
        var org = Organization.Create("Test Org", OrganizationType.Client, "DE", "de");

        // Act
        org.UpdatePlan("client_business");

        // Assert
        org.Plan.Should().Be("client_business");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var org = Organization.Create("Test", OrganizationType.Auditor, "BE", "fr");

        // Act
        org.Deactivate();

        // Assert
        org.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CountryCode_ShouldBeUpperCase()
    {
        // Act
        var org = Organization.Create("Test", OrganizationType.Auditor, "fr", "fr");

        // Assert
        org.CountryCode.Should().Be("FR");
    }
}
