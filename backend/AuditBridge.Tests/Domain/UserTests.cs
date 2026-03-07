using AuditBridge.Domain.Entities;
using FluentAssertions;

namespace AuditBridge.Tests.Domain;

public class UserTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        // Act
        var user = User.Create("clerk_abc123", OrgId, "test@example.com", UserRole.AuditorLead, "Jean Dupont");

        // Assert
        user.Id.Should().NotBe(Guid.Empty);
        user.ClerkId.Should().Be("clerk_abc123");
        user.Email.Should().Be("test@example.com");
        user.FullName.Should().Be("Jean Dupont");
        user.Role.Should().Be(UserRole.AuditorLead);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowerCase()
    {
        // Act
        var user = User.Create("clerk_123", OrgId, "TEST@EXAMPLE.COM", UserRole.ClientAdmin);

        // Assert
        user.Email.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyClerkId_ShouldThrow(string clerkId)
    {
        // Act
        var act = () => User.Create(clerkId, OrgId, "test@example.com", UserRole.AuditorLead);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(UserRole.AuditorLead, true)]
    [InlineData(UserRole.AuditorJunior, true)]
    [InlineData(UserRole.AuditorViewer, true)]
    [InlineData(UserRole.ClientAdmin, false)]
    [InlineData(UserRole.ClientContributor, false)]
    public void IsAuditor_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = User.Create("clerk_123", OrgId, "test@test.com", role);

        // Assert
        user.IsAuditor.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.ClientAdmin, true)]
    [InlineData(UserRole.ClientContributor, true)]
    [InlineData(UserRole.AuditorLead, false)]
    public void IsClient_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = User.Create("clerk_123", OrgId, "test@test.com", role);

        // Assert
        user.IsClient.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.AuditorLead, true)]
    [InlineData(UserRole.AuditorJunior, false)]
    [InlineData(UserRole.ClientAdmin, false)]
    public void CanCreateReports_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = User.Create("clerk_123", OrgId, "test@test.com", role);

        // Assert
        user.CanCreateReports.Should().Be(expected);
    }

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAt()
    {
        // Arrange
        var user = User.Create("clerk_123", OrgId, "test@test.com", UserRole.AuditorLead);
        var before = DateTimeOffset.UtcNow;

        // Act
        user.RecordLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(before);
    }
}
