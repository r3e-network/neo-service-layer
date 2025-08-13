using System;
using System.Collections.Generic;
using System.Linq;
using NeoServiceLayer.Core.Aggregates;
using NeoServiceLayer.Services.Authentication.Domain.Events;
using NeoServiceLayer.Services.Authentication.Domain.ValueObjects;

namespace NeoServiceLayer.Services.Authentication.Domain.Aggregates
{
    public class User : AggregateRoot
    {
        private readonly List<RefreshToken> _refreshTokens = new();
        private readonly List<UserSession> _sessions = new();
        private readonly List<LoginAttempt> _recentLoginAttempts = new();
        private readonly HashSet<string> _roles = new();
        private readonly HashSet<string> _permissions = new();

        public string Username { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string? TotpSecret { get; private set; }
        public bool IsTwoFactorEnabled { get; private set; }
        public UserStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        public DateTime? LastPasswordChangeAt { get; private set; }
        public int FailedLoginAttempts { get; private set; }
        public DateTime? LockedUntil { get; private set; }
        public string? EmailVerificationToken { get; private set; }
        public DateTime? EmailVerifiedAt { get; private set; }
        public string? PasswordResetToken { get; private set; }
        public DateTime? PasswordResetTokenExpiresAt { get; private set; }
        public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        public IReadOnlyList<UserSession> Sessions => _sessions.AsReadOnly();
        public IReadOnlyList<LoginAttempt> RecentLoginAttempts => _recentLoginAttempts.AsReadOnly();
        public IReadOnlySet<string> Roles => _roles;
        public IReadOnlySet<string> Permissions => _permissions;

        // For Event Sourcing reconstruction
        private User() : base(Guid.Empty)
        {
        }

        // Factory method for creating new users
        public static User Create(
            string username,
            string email,
            string passwordHash,
            IEnumerable<string>? initialRoles = null)
        {
            var userId = Guid.NewGuid();
            var user = new User();
            
            var @event = new UserCreatedEvent(
                userId,
                username,
                email,
                passwordHash,
                initialRoles?.ToList() ?? new List<string>(),
                DateTime.UtcNow);
            
            user.RaiseEvent(@event);
            return user;
        }

        public void VerifyEmail(string verificationToken)
        {
            if (EmailVerifiedAt.HasValue)
                throw new InvalidOperationException("Email already verified");
            
            if (EmailVerificationToken != verificationToken)
                throw new InvalidOperationException("Invalid verification token");

            RaiseEvent(new EmailVerifiedEvent(Id, DateTime.UtcNow));
        }

        public void Login(string ipAddress, string userAgent, string? deviceId = null)
        {
            if (Status != UserStatus.Active)
                throw new InvalidOperationException($"Cannot login - user status is {Status}");

            if (LockedUntil.HasValue && LockedUntil > DateTime.UtcNow)
                throw new InvalidOperationException($"Account locked until {LockedUntil}");

            var sessionId = Guid.NewGuid();
            RaiseEvent(new UserLoggedInEvent(
                Id,
                sessionId,
                ipAddress,
                userAgent,
                deviceId,
                DateTime.UtcNow));
        }

        public void RecordFailedLogin(string ipAddress, string reason)
        {
            RaiseEvent(new LoginFailedEvent(
                Id,
                ipAddress,
                reason,
                FailedLoginAttempts + 1,
                DateTime.UtcNow));

            // Lock account after 5 failed attempts
            if (FailedLoginAttempts >= 4) // Will be 5 after event is applied
            {
                var lockUntil = DateTime.UtcNow.AddMinutes(30);
                RaiseEvent(new AccountLockedEvent(Id, lockUntil, "Too many failed login attempts"));
            }
        }

        public void Logout(Guid sessionId)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null || session.LoggedOutAt.HasValue)
                throw new InvalidOperationException("Session not found or already logged out");

            RaiseEvent(new UserLoggedOutEvent(Id, sessionId, DateTime.UtcNow));
        }

        public RefreshToken IssueRefreshToken(string token, DateTime expiresAt, string? deviceId = null)
        {
            var refreshToken = new RefreshToken(
                Guid.NewGuid(),
                token,
                DateTime.UtcNow,
                expiresAt,
                deviceId);

            RaiseEvent(new RefreshTokenIssuedEvent(
                Id,
                refreshToken.Id,
                refreshToken.Token,
                refreshToken.IssuedAt,
                refreshToken.ExpiresAt,
                deviceId));

            return refreshToken;
        }

        public void RevokeRefreshToken(Guid tokenId, string reason)
        {
            var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
            if (token == null)
                throw new InvalidOperationException("Refresh token not found");

            if (token.RevokedAt.HasValue)
                throw new InvalidOperationException("Token already revoked");

            RaiseEvent(new RefreshTokenRevokedEvent(Id, tokenId, DateTime.UtcNow, reason));
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (newPasswordHash == PasswordHash)
                throw new InvalidOperationException("New password must be different from current password");

            RaiseEvent(new PasswordChangedEvent(
                Id,
                newPasswordHash,
                DateTime.UtcNow));

            // Revoke all refresh tokens on password change
            foreach (var token in _refreshTokens.Where(t => !t.RevokedAt.HasValue))
            {
                RaiseEvent(new RefreshTokenRevokedEvent(
                    Id,
                    token.Id,
                    DateTime.UtcNow,
                    "Password changed"));
            }
        }

        public void InitiatePasswordReset(string resetToken, DateTime expiresAt)
        {
            RaiseEvent(new PasswordResetInitiatedEvent(
                Id,
                resetToken,
                expiresAt,
                DateTime.UtcNow));
        }

        public void CompletePasswordReset(string resetToken, string newPasswordHash)
        {
            if (PasswordResetToken != resetToken)
                throw new InvalidOperationException("Invalid reset token");

            if (PasswordResetTokenExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Reset token expired");

            RaiseEvent(new PasswordResetCompletedEvent(
                Id,
                newPasswordHash,
                DateTime.UtcNow));

            // Revoke all sessions and tokens
            foreach (var session in _sessions.Where(s => !s.LoggedOutAt.HasValue))
            {
                RaiseEvent(new UserLoggedOutEvent(Id, session.Id, DateTime.UtcNow));
            }

            foreach (var token in _refreshTokens.Where(t => !t.RevokedAt.HasValue))
            {
                RaiseEvent(new RefreshTokenRevokedEvent(
                    Id,
                    token.Id,
                    DateTime.UtcNow,
                    "Password reset"));
            }
        }

        public void EnableTwoFactorAuthentication(string totpSecret)
        {
            if (IsTwoFactorEnabled)
                throw new InvalidOperationException("Two-factor authentication already enabled");

            RaiseEvent(new TwoFactorEnabledEvent(Id, totpSecret, DateTime.UtcNow));
        }

        public void DisableTwoFactorAuthentication()
        {
            if (!IsTwoFactorEnabled)
                throw new InvalidOperationException("Two-factor authentication not enabled");

            RaiseEvent(new TwoFactorDisabledEvent(Id, DateTime.UtcNow));
        }

        public void AssignRole(string role)
        {
            if (_roles.Contains(role))
                throw new InvalidOperationException($"Role {role} already assigned");

            RaiseEvent(new RoleAssignedEvent(Id, role, DateTime.UtcNow));
        }

        public void RemoveRole(string role)
        {
            if (!_roles.Contains(role))
                throw new InvalidOperationException($"Role {role} not assigned");

            RaiseEvent(new RoleRemovedEvent(Id, role, DateTime.UtcNow));
        }

        public void GrantPermission(string permission)
        {
            if (_permissions.Contains(permission))
                throw new InvalidOperationException($"Permission {permission} already granted");

            RaiseEvent(new PermissionGrantedEvent(Id, permission, DateTime.UtcNow));
        }

        public void RevokePermission(string permission)
        {
            if (!_permissions.Contains(permission))
                throw new InvalidOperationException($"Permission {permission} not granted");

            RaiseEvent(new PermissionRevokedEvent(Id, permission, DateTime.UtcNow));
        }

        public void Suspend(string reason)
        {
            if (Status == UserStatus.Suspended)
                throw new InvalidOperationException("User already suspended");

            RaiseEvent(new UserSuspendedEvent(Id, reason, DateTime.UtcNow));
        }

        public void Reactivate()
        {
            if (Status == UserStatus.Active)
                throw new InvalidOperationException("User already active");

            RaiseEvent(new UserReactivatedEvent(Id, DateTime.UtcNow));
        }

        public void Delete()
        {
            if (Status == UserStatus.Deleted)
                throw new InvalidOperationException("User already deleted");

            RaiseEvent(new UserDeletedEvent(Id, DateTime.UtcNow));
        }

        // Event handlers
        protected override void When(object @event)
        {
            switch (@event)
            {
                case UserCreatedEvent e:
                    Id = e.UserId;
                    Username = e.Username;
                    Email = e.Email;
                    PasswordHash = e.PasswordHash;
                    CreatedAt = e.CreatedAt;
                    Status = UserStatus.Active;
                    EmailVerificationToken = Guid.NewGuid().ToString();
                    foreach (var role in e.InitialRoles)
                    {
                        _roles.Add(role);
                    }
                    break;

                case EmailVerifiedEvent e:
                    EmailVerifiedAt = e.VerifiedAt;
                    EmailVerificationToken = null;
                    break;

                case UserLoggedInEvent e:
                    LastLoginAt = e.LoginTime;
                    FailedLoginAttempts = 0;
                    LockedUntil = null;
                    _sessions.Add(new UserSession(
                        e.SessionId,
                        e.IpAddress,
                        e.UserAgent,
                        e.DeviceId,
                        e.LoginTime));
                    _recentLoginAttempts.Add(new LoginAttempt(
                        e.IpAddress,
                        true,
                        e.LoginTime,
                        null));
                    // Keep only last 10 login attempts
                    while (_recentLoginAttempts.Count > 10)
                    {
                        _recentLoginAttempts.RemoveAt(0);
                    }
                    break;

                case LoginFailedEvent e:
                    FailedLoginAttempts = e.FailedAttemptCount;
                    _recentLoginAttempts.Add(new LoginAttempt(
                        e.IpAddress,
                        false,
                        e.AttemptTime,
                        e.Reason));
                    while (_recentLoginAttempts.Count > 10)
                    {
                        _recentLoginAttempts.RemoveAt(0);
                    }
                    break;

                case AccountLockedEvent e:
                    LockedUntil = e.LockedUntil;
                    break;

                case UserLoggedOutEvent e:
                    var session = _sessions.FirstOrDefault(s => s.Id == e.SessionId);
                    if (session != null)
                    {
                        session.LoggedOutAt = e.LogoutTime;
                    }
                    break;

                case RefreshTokenIssuedEvent e:
                    _refreshTokens.Add(new RefreshToken(
                        e.TokenId,
                        e.Token,
                        e.IssuedAt,
                        e.ExpiresAt,
                        e.DeviceId));
                    break;

                case RefreshTokenRevokedEvent e:
                    var token = _refreshTokens.FirstOrDefault(t => t.Id == e.TokenId);
                    if (token != null)
                    {
                        token.RevokedAt = e.RevokedAt;
                        token.RevokedReason = e.Reason;
                    }
                    break;

                case PasswordChangedEvent e:
                    PasswordHash = e.NewPasswordHash;
                    LastPasswordChangeAt = e.ChangedAt;
                    PasswordResetToken = null;
                    PasswordResetTokenExpiresAt = null;
                    break;

                case PasswordResetInitiatedEvent e:
                    PasswordResetToken = e.ResetToken;
                    PasswordResetTokenExpiresAt = e.ExpiresAt;
                    break;

                case PasswordResetCompletedEvent e:
                    PasswordHash = e.NewPasswordHash;
                    LastPasswordChangeAt = e.CompletedAt;
                    PasswordResetToken = null;
                    PasswordResetTokenExpiresAt = null;
                    break;

                case TwoFactorEnabledEvent e:
                    IsTwoFactorEnabled = true;
                    TotpSecret = e.TotpSecret;
                    break;

                case TwoFactorDisabledEvent e:
                    IsTwoFactorEnabled = false;
                    TotpSecret = null;
                    break;

                case RoleAssignedEvent e:
                    _roles.Add(e.Role);
                    break;

                case RoleRemovedEvent e:
                    _roles.Remove(e.Role);
                    break;

                case PermissionGrantedEvent e:
                    _permissions.Add(e.Permission);
                    break;

                case PermissionRevokedEvent e:
                    _permissions.Remove(e.Permission);
                    break;

                case UserSuspendedEvent e:
                    Status = UserStatus.Suspended;
                    break;

                case UserReactivatedEvent e:
                    Status = UserStatus.Active;
                    break;

                case UserDeletedEvent e:
                    Status = UserStatus.Deleted;
                    break;
            }
        }
    }

    public enum UserStatus
    {
        Active,
        Suspended,
        Deleted
    }
}