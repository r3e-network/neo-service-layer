using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.Events;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Domain.Events
{
    // User lifecycle events
    public class UserCreatedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string Username { get; }
        public string Email { get; }
        public string PasswordHash { get; }
        public List<string> InitialRoles { get; }
        public DateTime CreatedAt { get; }

        public UserCreatedEvent(Guid userId, string username, string email, string passwordHash, List<string> initialRoles, DateTime createdAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            InitialRoles = initialRoles;
            CreatedAt = createdAt;
        }
    }

    public class UserDeletedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public DateTime DeletedAt { get; }

        public UserDeletedEvent(Guid userId, DateTime deletedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            DeletedAt = deletedAt;
        }
    }

    public class UserSuspendedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string Reason { get; }
        public DateTime SuspendedAt { get; }

        public UserSuspendedEvent(Guid userId, string reason, DateTime suspendedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            Reason = reason;
            SuspendedAt = suspendedAt;
        }
    }

    public class UserReactivatedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public DateTime ReactivatedAt { get; }

        public UserReactivatedEvent(Guid userId, DateTime reactivatedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            ReactivatedAt = reactivatedAt;
        }
    }

    // Email verification events
    public class EmailVerifiedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public DateTime VerifiedAt { get; }

        public EmailVerifiedEvent(Guid userId, DateTime verifiedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            VerifiedAt = verifiedAt;
        }
    }

    // Authentication events
    public class UserLoggedInEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public Guid SessionId { get; }
        public string IpAddress { get; }
        public string UserAgent { get; }
        public string? DeviceId { get; }
        public DateTime LoginTime { get; }

        public UserLoggedInEvent(Guid userId, Guid sessionId, string ipAddress, string userAgent, string? deviceId, DateTime loginTime)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            SessionId = sessionId;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            DeviceId = deviceId;
            LoginTime = loginTime;
        }
    }

    public class UserLoggedOutEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public Guid SessionId { get; }
        public DateTime LogoutTime { get; }

        public UserLoggedOutEvent(Guid userId, Guid sessionId, DateTime logoutTime)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            SessionId = sessionId;
            LogoutTime = logoutTime;
        }
    }

    public class LoginFailedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string IpAddress { get; }
        public string Reason { get; }
        public int FailedAttemptCount { get; }
        public DateTime AttemptTime { get; }

        public LoginFailedEvent(Guid userId, string ipAddress, string reason, int failedAttemptCount, DateTime attemptTime)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            IpAddress = ipAddress;
            Reason = reason;
            FailedAttemptCount = failedAttemptCount;
            AttemptTime = attemptTime;
        }
    }

    public class AccountLockedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public DateTime LockedUntil { get; }
        public string Reason { get; }

        public AccountLockedEvent(Guid userId, DateTime lockedUntil, string reason)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            LockedUntil = lockedUntil;
            Reason = reason;
        }
    }

    // Password events
    public class PasswordChangedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string NewPasswordHash { get; }
        public DateTime ChangedAt { get; }

        public PasswordChangedEvent(Guid userId, string newPasswordHash, DateTime changedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            NewPasswordHash = newPasswordHash;
            ChangedAt = changedAt;
        }
    }

    public class PasswordResetInitiatedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string ResetToken { get; }
        public DateTime ExpiresAt { get; }
        public DateTime InitiatedAt { get; }

        public PasswordResetInitiatedEvent(Guid userId, string resetToken, DateTime expiresAt, DateTime initiatedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            ResetToken = resetToken;
            ExpiresAt = expiresAt;
            InitiatedAt = initiatedAt;
        }
    }

    public class PasswordResetCompletedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string NewPasswordHash { get; }
        public DateTime CompletedAt { get; }

        public PasswordResetCompletedEvent(Guid userId, string newPasswordHash, DateTime completedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            NewPasswordHash = newPasswordHash;
            CompletedAt = completedAt;
        }
    }

    // Two-factor authentication events
    public class TwoFactorEnabledEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string TotpSecret { get; }
        public DateTime EnabledAt { get; }

        public TwoFactorEnabledEvent(Guid userId, string totpSecret, DateTime enabledAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            TotpSecret = totpSecret;
            EnabledAt = enabledAt;
        }
    }

    public class TwoFactorDisabledEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public DateTime DisabledAt { get; }

        public TwoFactorDisabledEvent(Guid userId, DateTime disabledAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            DisabledAt = disabledAt;
        }
    }

    // Token events
    public class RefreshTokenIssuedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public Guid TokenId { get; }
        public string Token { get; }
        public DateTime IssuedAt { get; }
        public DateTime ExpiresAt { get; }
        public string? DeviceId { get; }

        public RefreshTokenIssuedEvent(Guid userId, Guid tokenId, string token, DateTime issuedAt, DateTime expiresAt, string? deviceId)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            TokenId = tokenId;
            Token = token;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            DeviceId = deviceId;
        }
    }

    public class RefreshTokenRevokedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public Guid TokenId { get; }
        public DateTime RevokedAt { get; }
        public string Reason { get; }

        public RefreshTokenRevokedEvent(Guid userId, Guid tokenId, DateTime revokedAt, string reason)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            TokenId = tokenId;
            RevokedAt = revokedAt;
            Reason = reason;
        }
    }

    // Role and permission events
    public class RoleAssignedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string Role { get; }
        public DateTime AssignedAt { get; }

        public RoleAssignedEvent(Guid userId, string role, DateTime assignedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            Role = role;
            AssignedAt = assignedAt;
        }
    }

    public class RoleRemovedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string Role { get; }
        public DateTime RemovedAt { get; }

        public RoleRemovedEvent(Guid userId, string role, DateTime removedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            Role = role;
            RemovedAt = removedAt;
        }
    }

    public class PermissionGrantedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string Permission { get; }
        public DateTime GrantedAt { get; }

        public PermissionGrantedEvent(Guid userId, string permission, DateTime grantedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            Permission = permission;
            GrantedAt = grantedAt;
        }
    }

    public class PermissionRevokedEvent : DomainEventBase
    {
        public Guid UserId { get; }
        public string Permission { get; }
        public DateTime RevokedAt { get; }

        public PermissionRevokedEvent(Guid userId, string permission, DateTime revokedAt)
            : base(userId.ToString(), nameof(Domain.Aggregates.User), 0, "System")
        {
            UserId = userId;
            Permission = permission;
            RevokedAt = revokedAt;
        }
    }
}