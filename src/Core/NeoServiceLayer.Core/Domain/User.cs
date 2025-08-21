using System;
using System.Collections.Generic;
using System.Linq;
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// User domain aggregate root with rich business behavior
    /// </summary>
    public class User : AggregateRoot<UserId>
    {
        private readonly List<FailedLoginAttempt> _failedLoginAttempts = new();

        /// <summary>
        /// Gets the username
        /// </summary>
        public Username Username { get; private set; }

        /// <summary>
        /// Gets the email address
        /// </summary>
        public EmailAddress Email { get; private set; }

        /// <summary>
        /// Gets the password
        /// </summary>
        public Password Password { get; private set; }

        /// <summary>
        /// Gets the user's roles
        /// </summary>
        public IReadOnlyList<Role> Roles { get; private set; } = new List<Role>();

        /// <summary>
        /// Gets whether the account is locked
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// Gets the lock reason if account is locked
        /// </summary>
        public string? LockReason { get; private set; }

        /// <summary>
        /// Gets when the account was locked
        /// </summary>
        public DateTime? LockedAt { get; private set; }

        /// <summary>
        /// Gets whether MFA is enabled
        /// </summary>
        public bool IsMfaEnabled { get; private set; }

        /// <summary>
        /// Gets the MFA secret
        /// </summary>
        public string? MfaSecret { get; private set; }

        /// <summary>
        /// Gets when the user was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets when the user was last updated
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Gets when the password was last changed
        /// </summary>
        public DateTime PasswordChangedAt { get; private set; }

        /// <summary>
        /// Gets the failed login attempts
        /// </summary>
        public IReadOnlyList<FailedLoginAttempt> FailedLoginAttempts => _failedLoginAttempts.AsReadOnly();

        // Private constructor for EF Core
        private User() { }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="username">Username</param>
        /// <param name="email">Email address</param>
        /// <param name="password">Password</param>
        /// <param name="roles">Initial roles</param>
        public User(UserId id, Username username, EmailAddress email, Password password, IEnumerable<Role>? roles = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            Roles = roles?.ToList() ?? new List<Role>();
            
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            PasswordChangedAt = DateTime.UtcNow;

            AddDomainEvent(new UserCreatedEvent(Id, Username, Email));
        }

        /// <summary>
        /// Authenticates the user with the provided password
        /// </summary>
        /// <param name="plainTextPassword">The plain text password</param>
        /// <param name="policy">The password policy to enforce</param>
        /// <returns>Authentication result</returns>
        public AuthenticationAttemptResult Authenticate(string plainTextPassword, IPasswordPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new ArgumentException("Password cannot be null or empty", nameof(plainTextPassword));

            if (IsLocked)
            {
                AddDomainEvent(new AuthenticationAttemptedEvent(Id, false, "Account is locked"));
                return AuthenticationAttemptResult.AccountLocked(LockReason ?? "Account locked");
            }

            var isValidPassword = Password.Verify(plainTextPassword);
            
            if (isValidPassword)
            {
                // Reset failed attempts on successful authentication
                _failedLoginAttempts.Clear();
                UpdatedAt = DateTime.UtcNow;
                
                AddDomainEvent(new AuthenticationSucceededEvent(Id, Username));
                return AuthenticationAttemptResult.Success();
            }
            else
            {
                // Record failed attempt
                var attempt = new FailedLoginAttempt(DateTime.UtcNow, "Invalid password");
                _failedLoginAttempts.Add(attempt);

                // Check if account should be locked due to too many failed attempts
                var recentAttempts = _failedLoginAttempts
                    .Where(a => a.AttemptedAt > DateTime.UtcNow.AddMinutes(-15))
                    .Count();

                if (recentAttempts >= policy.MaxFailedLoginAttempts)
                {
                    LockAccount("Too many failed login attempts");
                }

                AddDomainEvent(new AuthenticationFailedEvent(Id, Username, "Invalid password"));
                return AuthenticationAttemptResult.InvalidCredentials();
            }
        }

        /// <summary>
        /// Changes the user's password
        /// </summary>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <param name="policy">Password policy to enforce</param>
        public void ChangePassword(string currentPassword, Password newPassword, IPasswordPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
                throw new ArgumentException("Current password cannot be null or empty", nameof(currentPassword));

            if (newPassword == null)
                throw new ArgumentNullException(nameof(newPassword));

            if (!Password.Verify(currentPassword))
                throw new BusinessRuleViolationException("INVALID_CURRENT_PASSWORD", "Current password is invalid");

            if (IsLocked)
                throw new BusinessRuleViolationException("ACCOUNT_LOCKED", "Cannot change password on locked account");

            // Validate new password against policy
            policy.ValidatePassword(newPassword.Value);

            // Ensure new password is different from current
            if (Password.Verify(newPassword.Value))
                throw new BusinessRuleViolationException("SAME_PASSWORD", "New password must be different from current password");

            Password = newPassword;
            PasswordChangedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new PasswordChangedEvent(Id, Username));
        }

        /// <summary>
        /// Locks the user account
        /// </summary>
        /// <param name="reason">Reason for locking</param>
        public void LockAccount(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lock reason cannot be null or empty", nameof(reason));

            if (IsLocked)
                return; // Already locked

            IsLocked = true;
            LockReason = reason;
            LockedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new AccountLockedEvent(Id, Username, reason));
        }

        /// <summary>
        /// Unlocks the user account
        /// </summary>
        public void UnlockAccount()
        {
            if (!IsLocked)
                return; // Already unlocked

            IsLocked = false;
            LockReason = null;
            LockedAt = null;
            UpdatedAt = DateTime.UtcNow;

            // Clear failed attempts when unlocking
            _failedLoginAttempts.Clear();

            AddDomainEvent(new AccountUnlockedEvent(Id, Username));
        }

        /// <summary>
        /// Enables MFA for the user
        /// </summary>
        /// <param name="secret">MFA secret</param>
        public void EnableMfa(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("MFA secret cannot be null or empty", nameof(secret));

            if (IsMfaEnabled)
                throw new BusinessRuleViolationException("MFA_ALREADY_ENABLED", "MFA is already enabled for this user");

            IsMfaEnabled = true;
            MfaSecret = secret;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new MfaEnabledEvent(Id, Username));
        }

        /// <summary>
        /// Disables MFA for the user
        /// </summary>
        public void DisableMfa()
        {
            if (!IsMfaEnabled)
                return; // Already disabled

            IsMfaEnabled = false;
            MfaSecret = null;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new MfaDisabledEvent(Id, Username));
        }

        /// <summary>
        /// Adds a role to the user
        /// </summary>
        /// <param name="role">Role to add</param>
        public void AddRole(Role role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var roles = new List<Role>(Roles);
            if (roles.Contains(role))
                return; // Role already exists

            roles.Add(role);
            Roles = roles;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new RoleAddedEvent(Id, Username, role.Name));
        }

        /// <summary>
        /// Removes a role from the user
        /// </summary>
        /// <param name="role">Role to remove</param>
        public void RemoveRole(Role role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var roles = new List<Role>(Roles);
            if (!roles.Remove(role))
                return; // Role doesn't exist

            Roles = roles;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new RoleRemovedEvent(Id, Username, role.Name));
        }
    }

    /// <summary>
    /// Represents a failed login attempt
    /// </summary>
    public class FailedLoginAttempt : ValueObject
    {
        public DateTime AttemptedAt { get; }
        public string Reason { get; }

        public FailedLoginAttempt(DateTime attemptedAt, string reason)
        {
            AttemptedAt = attemptedAt;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AttemptedAt;
            yield return Reason;
        }
    }

    /// <summary>
    /// Authentication attempt result
    /// </summary>
    public class AuthenticationAttemptResult : ValueObject
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public AuthenticationErrorType? ErrorType { get; }

        private AuthenticationAttemptResult(bool isSuccess, string? errorMessage = null, AuthenticationErrorType? errorType = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorType = errorType;
        }

        public static AuthenticationAttemptResult Success() => 
            new AuthenticationAttemptResult(true);

        public static AuthenticationAttemptResult InvalidCredentials() => 
            new AuthenticationAttemptResult(false, "Invalid credentials", AuthenticationErrorType.InvalidCredentials);

        public static AuthenticationAttemptResult AccountLocked(string reason) => 
            new AuthenticationAttemptResult(false, $"Account locked: {reason}", AuthenticationErrorType.AccountLocked);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return IsSuccess;
            yield return ErrorMessage ?? string.Empty;
            yield return ErrorType ?? AuthenticationErrorType.Unknown;
        }
    }

    /// <summary>
    /// Authentication error types
    /// </summary>
    public enum AuthenticationErrorType
    {
        Unknown,
        InvalidCredentials,
        AccountLocked,
        AccountDisabled,
        MfaRequired
    }
}