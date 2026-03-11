using AuditBridge.Domain.Entities;

namespace AuditBridge.Application.DTOs;

public record CreateOrganizationRequest(
    string Name,
    OrganizationType Type,
    string CountryCode,
    string Language,
    string? OwnerClerkId,
    string? OwnerEmail,
    string? OwnerFullName,
    IReadOnlyList<InviteMemberRequest>? Invites = null
);

public record InviteMemberRequest(string Email, string Role);

public record OrganizationDto(
    Guid Id,
    string Name,
    string Type,
    string Plan,
    string CountryCode,
    string Language,
    string? LogoUrl,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record UserDto(
    Guid Id,
    string ClerkId,
    Guid OrganizationId,
    string Email,
    string? FullName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record SyncClerkUserRequest(
    string ClerkId,
    string Email,
    string? FullName,
    string EventType // "user.created" | "user.updated" | "user.deleted"
);
