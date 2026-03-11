using AuditBridge.Application.DTOs;
using AuditBridge.Application.Exceptions;
using AuditBridge.Application.UseCases.Organizations;
using AuditBridge.Domain.Entities;
using AuditBridge.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AuditBridge.Tests.UseCases;

public class CreateOrganizationUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOrganizationRepository> _orgRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAuditTrailRepository> _auditTrailMock;
    private readonly CreateOrganizationUseCase _sut;

    public CreateOrganizationUseCaseTests()
    {
        _orgRepoMock = new Mock<IOrganizationRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _auditTrailMock = new Mock<IAuditTrailRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.Organizations).Returns(_orgRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AuditTrail).Returns(_auditTrailMock.Object);
        _unitOfWorkMock.Setup(u => u.Referentials).Returns(new Mock<IReferentialRepository>().Object);
        _unitOfWorkMock.Setup(u => u.Audits).Returns(new Mock<IAuditRepository>().Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.SaveChangesWithTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _orgRepoMock.Setup(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization o, CancellationToken _) => o);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        _auditTrailMock.Setup(r => r.LogAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new CreateOrganizationUseCase(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Execute_WithAuditorType_ShouldCreateOrgWithAuditorLeadRole()
    {
        // Arrange
        var request = new CreateOrganizationRequest(
            Name: "Cabinet Dupont",
            Type: OrganizationType.Auditor,
            CountryCode: "FR",
            Language: "fr",
            OwnerClerkId: "clerk_abc123",
            OwnerEmail: "dupont@example.com",
            OwnerFullName: "Jean Dupont"
        );

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Cabinet Dupont");
        result.Type.Should().Be("auditor");
        result.Plan.Should().Be("starter");
        result.IsActive.Should().BeTrue();

        // Verify auditor_lead role was used for owner
        _userRepoMock.Verify(r =>
            r.AddAsync(It.Is<User>(u => u.Role == UserRole.AuditorLead), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithClientType_ShouldCreateOrgWithClientAdminRole()
    {
        // Arrange
        var request = new CreateOrganizationRequest(
            Name: "PharmaCorp SAS",
            Type: OrganizationType.Client,
            CountryCode: "FR",
            Language: "fr",
            OwnerClerkId: "clerk_xyz789",
            OwnerEmail: "admin@pharmacorp.fr",
            OwnerFullName: null
        );

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Type.Should().Be("client");

        _userRepoMock.Verify(r =>
            r.AddAsync(It.Is<User>(u => u.Role == UserRole.ClientAdmin), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Execute_WithEmptyName_ShouldThrowValidationException(string name)
    {
        // Arrange
        var request = new CreateOrganizationRequest(
            Name: name,
            Type: OrganizationType.Auditor,
            CountryCode: "FR",
            Language: "fr",
            OwnerClerkId: "clerk_123",
            OwnerEmail: "test@test.com",
            OwnerFullName: null
        );

        // Act
        var act = () => _sut.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Execute_ShouldSaveChangesOnce()
    {
        // Arrange
        var request = new CreateOrganizationRequest(
            Name: "Test Org",
            Type: OrganizationType.Auditor,
            CountryCode: "DE",
            Language: "de",
            OwnerClerkId: "clerk_123",
            OwnerEmail: "test@test.de",
            OwnerFullName: "Hans Müller"
        );

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesWithTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldLogAuditTrailEntry()
    {
        // Arrange
        var request = new CreateOrganizationRequest(
            Name: "Test",
            Type: OrganizationType.Auditor,
            CountryCode: "FR",
            Language: "fr",
            OwnerClerkId: "clerk_123",
            OwnerEmail: "test@test.fr",
            OwnerFullName: null
        );

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        _auditTrailMock.Verify(r =>
            r.LogAsync(
                It.Is<AuditTrail>(a => a.Action == "organization.created"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
