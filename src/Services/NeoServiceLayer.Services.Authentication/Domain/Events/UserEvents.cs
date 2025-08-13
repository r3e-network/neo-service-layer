using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.Events;

namespace NeoServiceLayer.Services.Authentication.Domain.Events
{
    // User lifecycle events
    public record UserCreatedEvent(
        Guid UserId,
        string Username,
        string Email,
        string PasswordHash,
        List<string> InitialRoles,
        DateTime CreatedAt) : DomainEvent;

    public record UserDeletedEvent(
        Guid UserId,
        DateTime DeletedAt) : DomainEvent;

    public record UserSuspendedEvent(
        Guid UserId,
        string Reason,
        DateTime SuspendedAt) : DomainEvent;

    public record UserReactivatedEvent(
        Guid UserId,
        DateTime ReactivatedAt) : DomainEvent;

    // Email verification events
    public record EmailVerifiedEvent(
        Guid UserId,
        DateTime VerifiedAt) : DomainEvent;

    // Authentication events
    public record UserLoggedInEvent(
        Guid UserId,
        Guid SessionId,
        string IpAddress,
        string UserAgent,
        string? DeviceId,
        DateTime LoginTime) : DomainEvent;

    public record UserLoggedOutEvent(
        Guid UserId,
        Guid SessionId,
        DateTime LogoutTime) : DomainEvent;

    public record LoginFailedEvent(
        Guid UserId,
        string IpAddress,
        string Reason,
        int FailedAttemptCount,
        DateTime AttemptTime) : DomainEvent;

    public record AccountLockedEvent(
        Guid UserId,
        DateTime LockedUntil,
        string Reason) : DomainEvent;

    // Password events
    public record PasswordChangedEvent(
        Guid UserId,
        string NewPasswordHash,
        DateTime ChangedAt) : DomainEvent;

    public record PasswordResetInitiatedEvent(
        Guid UserId,
        string ResetToken,
        DateTime ExpiresAt,
        DateTime InitiatedAt) : DomainEvent;

    public record PasswordResetCompletedEvent(
        Guid UserId,
        string NewPasswordHash,
        DateTime CompletedAt) : DomainEvent;

    // Two-factor authentication events
    public record TwoFactorEnabledEvent(
        Guid UserId,
        string TotpSecret,
        DateTime EnabledAt) : DomainEvent;

    public record TwoFactorDisabledEvent(
        Guid UserId,
        DateTime DisabledAt) : DomainEvent;

    // Token events
    public record RefreshTokenIssuedEvent(
        Guid UserId,
        Guid TokenId,
        string Token,
        DateTime IssuedAt,
        DateTime ExpiresAt,
        string? DeviceId) : DomainEvent;

    public record RefreshTokenRevokedEvent(
        Guid UserId,
        Guid TokenId,
        DateTime RevokedAt,
        string Reason) : DomainEvent;

    // Role and permission events
    public record RoleAssignedEvent(
        Guid UserId,
        string Role,
        DateTime AssignedAt) : DomainEvent;

    public record RoleRemovedEvent(
        Guid UserId,
        string Role,
        DateTime RemovedAt) : DomainEvent;

    public record PermissionGrantedEvent(
        Guid UserId,
        string Permission,
        DateTime GrantedAt) : DomainEvent;

    public record PermissionRevokedEvent(
        Guid UserId,
        string Permission,
        DateTime RevokedAt) : DomainEvent;
}