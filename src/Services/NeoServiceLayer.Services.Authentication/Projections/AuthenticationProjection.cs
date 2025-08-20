using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Services.Authentication.Domain.Events;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Queries;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Projections
{
    public class AuthenticationProjection
    {
        private readonly IUserReadModelStore _userStore;
        private readonly ISessionReadModelStore _sessionStore;
        private readonly ITokenReadModelStore _tokenStore;
        private readonly ILogger<AuthenticationProjection> Logger;
        private long _position = 0;

        public AuthenticationProjection(
            IUserReadModelStore userStore,
            ISessionReadModelStore sessionStore,
            ITokenReadModelStore tokenStore,
            ILogger<AuthenticationProjection> logger)
        {
            _userStore = userStore;
            _sessionStore = sessionStore;
            _tokenStore = tokenStore;
            Logger = logger;
        }

        public long Position => _position;

        public async Task HandleAsync(object @event)
        {
            try
            {
                switch (@event)
                {
                    case UserCreatedEvent e:
                        await HandleUserCreated(e);
                        break;

                    case UserDeletedEvent e:
                        await HandleUserDeleted(e);
                        break;

                    case UserSuspendedEvent e:
                        await HandleUserSuspended(e);
                        break;

                    case UserReactivatedEvent e:
                        await HandleUserReactivated(e);
                        break;

                    case EmailVerifiedEvent e:
                        await HandleEmailVerified(e);
                        break;

                    case UserLoggedInEvent e:
                        await HandleUserLoggedIn(e);
                        break;

                    case UserLoggedOutEvent e:
                        await HandleUserLoggedOut(e);
                        break;

                    case LoginFailedEvent e:
                        await HandleLoginFailed(e);
                        break;

                    case AccountLockedEvent e:
                        await HandleAccountLocked(e);
                        break;

                    case PasswordChangedEvent e:
                        await HandlePasswordChanged(e);
                        break;

                    case PasswordResetInitiatedEvent e:
                        // No projection updates needed
                        break;

                    case PasswordResetCompletedEvent e:
                        await HandlePasswordResetCompleted(e);
                        break;

                    case TwoFactorEnabledEvent e:
                        await HandleTwoFactorEnabled(e);
                        break;

                    case TwoFactorDisabledEvent e:
                        await HandleTwoFactorDisabled(e);
                        break;

                    case RefreshTokenIssuedEvent e:
                        await HandleRefreshTokenIssued(e);
                        break;

                    case RefreshTokenRevokedEvent e:
                        await HandleRefreshTokenRevoked(e);
                        break;

                    case RoleAssignedEvent e:
                        await HandleRoleAssigned(e);
                        break;

                    case RoleRemovedEvent e:
                        await HandleRoleRemoved(e);
                        break;

                    case PermissionGrantedEvent e:
                        await HandlePermissionGranted(e);
                        break;

                    case PermissionRevokedEvent e:
                        await HandlePermissionRevoked(e);
                        break;

                    default:
                        Logger.LogWarning("Unknown event type: {EventType}", @event.GetType().Name);
                        break;
                }

                // Update position tracking (position should be passed in or tracked differently)
                _position++;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling event {EventType}", @event.GetType().Name);
                throw;
            }
        }

        private async Task HandleUserCreated(UserCreatedEvent e)
        {
            var user = new UserReadModel
            {
                Id = e.UserId,
                Username = e.Username,
                Email = e.Email,
                Status = "Active",
                EmailVerified = false,
                TwoFactorEnabled = false,
                CreatedAt = e.CreatedAt,
                Roles = e.InitialRoles,
                Permissions = new(),
                RecentLoginAttempts = new(),
                FailedLoginAttempts = 0
            };

            await _userStore.SaveAsync(user);
            Logger.LogInformation("Created user projection for {UserId}", e.UserId);
        }

        private async Task HandleUserDeleted(UserDeletedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.Status = "Deleted";
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Marked user {UserId} as deleted", e.UserId);
            }
        }

        private async Task HandleUserSuspended(UserSuspendedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.Status = "Suspended";
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Marked user {UserId} as suspended", e.UserId);
            }
        }

        private async Task HandleUserReactivated(UserReactivatedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.Status = "Active";
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Reactivated user {UserId}", e.UserId);
            }
        }

        private async Task HandleEmailVerified(EmailVerifiedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.EmailVerified = true;
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Marked email as verified for user {UserId}", e.UserId);
            }
        }

        private async Task HandleUserLoggedIn(UserLoggedInEvent e)
        {
            // Update user last login
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.LastLoginAt = e.LoginTime;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;

                // Add successful login attempt
                user.RecentLoginAttempts.Add(new LoginAttemptReadModel
                {
                    IpAddress = e.IpAddress,
                    Success = true,
                    AttemptTime = e.LoginTime
                });

                // Keep only last 10 attempts
                if (user.RecentLoginAttempts.Count > 10)
                {
                    user.RecentLoginAttempts = user.RecentLoginAttempts
                        .OrderByDescending(a => a.AttemptTime)
                        .Take(10)
                        .ToList();
                }

                await _userStore.SaveAsync(user);
            }

            // Create session
            var session = new SessionReadModel
            {
                Id = e.SessionId,
                UserId = e.UserId,
                IpAddress = e.IpAddress,
                UserAgent = e.UserAgent,
                DeviceId = e.DeviceId,
                StartedAt = e.LoginTime,
                LastActivityAt = e.LoginTime
            };

            await _sessionStore.SaveAsync(session);
            Logger.LogInformation("Created session {SessionId} for user {UserId}", e.SessionId, e.UserId);
        }

        private async Task HandleUserLoggedOut(UserLoggedOutEvent e)
        {
            var session = await _sessionStore.GetByIdAsync(e.UserId, e.SessionId);
            if (session != null)
            {
                session.LoggedOutAt = e.LogoutTime;
                await _sessionStore.SaveAsync(session);
                Logger.LogInformation("Marked session {SessionId} as logged out", e.SessionId);
            }
        }

        private async Task HandleLoginFailed(LoginFailedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.FailedLoginAttempts = e.FailedAttemptCount;

                // Add failed login attempt
                user.RecentLoginAttempts.Add(new LoginAttemptReadModel
                {
                    IpAddress = e.IpAddress,
                    Success = false,
                    AttemptTime = e.AttemptTime,
                    FailureReason = e.Reason
                });

                // Keep only last 10 attempts
                if (user.RecentLoginAttempts.Count > 10)
                {
                    user.RecentLoginAttempts = user.RecentLoginAttempts
                        .OrderByDescending(a => a.AttemptTime)
                        .Take(10)
                        .ToList();
                }

                await _userStore.SaveAsync(user);
                Logger.LogInformation("Recorded failed login for user {UserId}", e.UserId);
            }
        }

        private async Task HandleAccountLocked(AccountLockedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.LockedUntil = e.LockedUntil;
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Locked user {UserId} until {LockedUntil}", e.UserId, e.LockedUntil);
            }
        }

        private async Task HandlePasswordChanged(PasswordChangedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.LastPasswordChangeAt = e.ChangedAt;
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Updated password change time for user {UserId}", e.UserId);
            }
        }

        private async Task HandlePasswordResetCompleted(PasswordResetCompletedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.LastPasswordChangeAt = e.CompletedAt;
                await _userStore.SaveAsync(user);

                // Logout all sessions
                await _sessionStore.LogoutAllSessionsAsync(e.UserId, e.CompletedAt);

                // Revoke all tokens
                await _tokenStore.RevokeAllTokensAsync(e.UserId, e.CompletedAt, "Password reset");

                Logger.LogInformation("Completed password reset for user {UserId}", e.UserId);
            }
        }

        private async Task HandleTwoFactorEnabled(TwoFactorEnabledEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.TwoFactorEnabled = true;
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Enabled two-factor for user {UserId}", e.UserId);
            }
        }

        private async Task HandleTwoFactorDisabled(TwoFactorDisabledEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.TwoFactorEnabled = false;
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Disabled two-factor for user {UserId}", e.UserId);
            }
        }

        private async Task HandleRefreshTokenIssued(RefreshTokenIssuedEvent e)
        {
            var token = new RefreshTokenReadModel
            {
                Id = e.TokenId,
                UserId = e.UserId,
                Token = e.Token,
                IssuedAt = e.IssuedAt,
                ExpiresAt = e.ExpiresAt,
                DeviceId = e.DeviceId
            };

            await _tokenStore.SaveAsync(token);
            Logger.LogInformation("Issued refresh token {TokenId} for user {UserId}", e.TokenId, e.UserId);
        }

        private async Task HandleRefreshTokenRevoked(RefreshTokenRevokedEvent e)
        {
            var token = await _tokenStore.GetByIdAsync(e.TokenId);
            if (token != null)
            {
                token.RevokedAt = e.RevokedAt;
                token.RevokedReason = e.Reason;
                await _tokenStore.SaveAsync(token);
                Logger.LogInformation("Revoked refresh token {TokenId}", e.TokenId);
            }
        }

        private async Task HandleRoleAssigned(RoleAssignedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                if (!user.Roles.Contains(e.Role))
                {
                    user.Roles.Add(e.Role);
                    await _userStore.SaveAsync(user);
                    Logger.LogInformation("Assigned role {Role} to user {UserId}", e.Role, e.UserId);
                }
            }
        }

        private async Task HandleRoleRemoved(RoleRemovedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.Roles.Remove(e.Role);
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Removed role {Role} from user {UserId}", e.Role, e.UserId);
            }
        }

        private async Task HandlePermissionGranted(PermissionGrantedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                if (!user.Permissions.Contains(e.Permission))
                {
                    user.Permissions.Add(e.Permission);
                    await _userStore.SaveAsync(user);
                    Logger.LogInformation("Granted permission {Permission} to user {UserId}", e.Permission, e.UserId);
                }
            }
        }

        private async Task HandlePermissionRevoked(PermissionRevokedEvent e)
        {
            var user = await _userStore.GetByIdAsync(e.UserId);
            if (user != null)
            {
                user.Permissions.Remove(e.Permission);
                await _userStore.SaveAsync(user);
                Logger.LogInformation("Revoked permission {Permission} from user {UserId}", e.Permission, e.UserId);
            }
        }

        public async Task RebuildAsync()
        {
            Logger.LogInformation("Rebuilding authentication projections from position 0");

            // Clear all read models
            await _userStore.ClearAsync();
            await _sessionStore.ClearAsync();
            await _tokenStore.ClearAsync();

            _position = 0;

            // Projection rebuild will be triggered by replaying all events
            Logger.LogInformation("Authentication projections cleared and ready for rebuild");
        }
    }
}