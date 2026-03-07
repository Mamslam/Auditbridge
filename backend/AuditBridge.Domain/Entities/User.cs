namespace AuditBridge.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string ClerkId { get; private set; } = string.Empty;
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public Organization? Organization { get; private set; }

    private User() { }

    public static User Create(
        string clerkId,
        Guid organizationId,
        string email,
        UserRole role,
        string? fullName = null)
    {
        if (string.IsNullOrWhiteSpace(clerkId))
            throw new ArgumentException("Clerk ID is required.", nameof(clerkId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new User
        {
            Id = Guid.NewGuid(),
            ClerkId = clerkId,
            OrganizationId = organizationId,
            Email = email.ToLowerInvariant().Trim(),
            FullName = fullName?.Trim(),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public bool IsAuditor =>
        Role is UserRole.AuditorLead or UserRole.AuditorJunior or UserRole.AuditorViewer;

    public bool IsClient =>
        Role is UserRole.ClientAdmin or UserRole.ClientContributor or UserRole.ClientViewer;

    public bool CanCreateReports => Role is UserRole.AuditorLead or UserRole.PlatformAdmin;

    public bool CanUploadDocuments =>
        Role is not UserRole.AuditorViewer and not UserRole.ClientViewer;
}

public enum UserRole
{
    AuditorLead,
    AuditorJunior,
    AuditorViewer,
    ClientAdmin,
    ClientContributor,
    ClientViewer,
    PlatformAdmin
}
