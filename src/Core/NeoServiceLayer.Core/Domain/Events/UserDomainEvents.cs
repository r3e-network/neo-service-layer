using System;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Base class for user-related domain events
    /// </summary>
    public abstract class UserDomainEvent : IDomainEvent
    {
        /// <summary>
        /// Gets the user ID
        /// </summary>
        public UserId UserId { get; }

        /// <summary>
        /// Gets the username
        /// </summary>
        public Username Username { get; }

        /// <summary>
        /// Gets when the event occurred
        /// </summary>
        public DateTime OccurredAt { get; }

        /// <summary>
        /// Gets the event ID
        /// </summary>
        public Guid EventId { get; }

        /// <summary>
        /// Initializes a new instance of UserDomainEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        protected UserDomainEvent(UserId userId, Username username)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            OccurredAt = DateTime.UtcNow;
            EventId = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Domain event raised when a user is created
    /// </summary>
    public class UserCreatedEvent : UserDomainEvent
    {
        /// <summary>
        /// Gets the email address
        /// </summary>
        public EmailAddress Email { get; }

        /// <summary>
        /// Initializes a new instance of UserCreatedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        /// <param name="email">The email address</param>
        public UserCreatedEvent(UserId userId, Username username, EmailAddress email)
            : base(userId, username)
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }
    }

    /// <summary>
    /// Domain event raised when authentication is attempted
    /// </summary>
    public class AuthenticationAttemptedEvent : UserDomainEvent
    {
        /// <summary>
        /// Gets whether the authentication was successful
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Gets the reason for failure (if applicable)
        /// </summary>
        public string? FailureReason { get; }

        /// <summary>
        /// Initializes a new instance of AuthenticationAttemptedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="isSuccessful">Whether authentication was successful</param>
        /// <param name="failureReason">Reason for failure</param>
        public AuthenticationAttemptedEvent(UserId userId, bool isSuccessful, string? failureReason = null)
            : base(userId, Username.Create("Unknown")) // Username might not be available during failed attempts
        {
            IsSuccessful = isSuccessful;
            FailureReason = failureReason;
        }
    }

    /// <summary>
    /// Domain event raised when authentication succeeds
    /// </summary>
    public class AuthenticationSucceededEvent : UserDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of AuthenticationSucceededEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        public AuthenticationSucceededEvent(UserId userId, Username username)
            : base(userId, username)
        {
        }
    }

    /// <summary>
    /// Domain event raised when authentication fails
    /// </summary>
    public class AuthenticationFailedEvent : UserDomainEvent
    {
        /// <summary>
        /// Gets the failure reason
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Initializes a new instance of AuthenticationFailedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        /// <param name="failureReason">The failure reason</param>
        public AuthenticationFailedEvent(UserId userId, Username username, string failureReason)
            : base(userId, username)
        {
            FailureReason = failureReason ?? throw new ArgumentNullException(nameof(failureReason));
        }
    }

    /// <summary>
    /// Domain event raised when password is changed
    /// </summary>
    public class PasswordChangedEvent : UserDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of PasswordChangedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        public PasswordChangedEvent(UserId userId, Username username)
            : base(userId, username)
        {
        }
    }

    /// <summary>
    /// Domain event raised when account is locked
    /// </summary>
    public class AccountLockedEvent : UserDomainEvent
    {
        /// <summary>
        /// Gets the lock reason
        /// </summary>
        public string LockReason { get; }

        /// <summary>
        /// Initializes a new instance of AccountLockedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        /// <param name="lockReason">The lock reason</param>
        public AccountLockedEvent(UserId userId, Username username, string lockReason)
            : base(userId, username)
        {
            LockReason = lockReason ?? throw new ArgumentNullException(nameof(lockReason));
        }
    }

    /// <summary>
    /// Domain event raised when account is unlocked
    /// </summary>
    public class AccountUnlockedEvent : UserDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of AccountUnlockedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        public AccountUnlockedEvent(UserId userId, Username username)
            : base(userId, username)
        {
        }
    }

    /// <summary>
    /// Domain event raised when MFA is enabled
    /// </summary>
    public class MfaEnabledEvent : UserDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of MfaEnabledEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        public MfaEnabledEvent(UserId userId, Username username)
            : base(userId, username)
        {
        }
    }

    /// <summary>
    /// Domain event raised when MFA is disabled
    /// </summary>
    public class MfaDisabledEvent : UserDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of MfaDisabledEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        public MfaDisabledEvent(UserId userId, Username username)
            : base(userId, username)
        {
        }
    }

    /// <summary>
    /// Domain event raised when a role is added to a user
    /// </summary>
    public class RoleAddedEvent : UserDomainEvent
    {
        /// <summary>
        /// Gets the role name
        /// </summary>
        public string RoleName { get; }

        /// <summary>
        /// Initializes a new instance of RoleAddedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        /// <param name="roleName">The role name</param>
        public RoleAddedEvent(UserId userId, Username username, string roleName)
            : base(userId, username)
        {
            RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        }
    }

    /// <summary>
    /// Domain event raised when a role is removed from a user
    /// </summary>
    public class RoleRemovedEvent : UserDomainEvent
    {
        /// <summary>
        /// Gets the role name
        /// </summary>
        public string RoleName { get; }

        /// <summary>
        /// Initializes a new instance of RoleRemovedEvent
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="username">The username</param>
        /// <param name="roleName">The role name</param>
        public RoleRemovedEvent(UserId userId, Username username, string roleName)
            : base(userId, username)
        {
            RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        }
    }
}