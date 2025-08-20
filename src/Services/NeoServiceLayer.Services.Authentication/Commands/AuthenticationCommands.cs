using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Commands
{
    // User management commands
    public class CreateUserCommand : CommandBase<Guid>
    {
        public string Username { get; }
        public string Email { get; }
        public string Password { get; }
        public List<string>? InitialRoles { get; }

        public CreateUserCommand(string username, string email, string password, List<string>? initialRoles, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            Username = username;
            Email = email;
            Password = password;
            InitialRoles = initialRoles;
        }
    }

    public class DeleteUserCommand : CommandBase
    {
        public Guid UserId { get; }

        public DeleteUserCommand(Guid userId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
        }
    }

    public class SuspendUserCommand : CommandBase
    {
        public Guid UserId { get; }
        public string Reason { get; }

        public SuspendUserCommand(Guid userId, string reason, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Reason = reason;
        }
    }

    public class ReactivateUserCommand : CommandBase
    {
        public Guid UserId { get; }

        public ReactivateUserCommand(Guid userId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
        }
    }

    // Authentication commands
    public class LoginCommand : CommandBase<LoginResult>
    {
        public string UsernameOrEmail { get; }
        public string Password { get; }
        public string IpAddress { get; }
        public string UserAgent { get; }
        public string? DeviceId { get; }
        public string? TotpCode { get; }

        public LoginCommand(string usernameOrEmail, string password, string ipAddress, string userAgent, string initiatedBy, string? deviceId = null, string? totpCode = null, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UsernameOrEmail = usernameOrEmail;
            Password = password;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            DeviceId = deviceId;
            TotpCode = totpCode;
        }
    }

    public class LogoutCommand : CommandBase
    {
        public Guid UserId { get; }
        public Guid SessionId { get; }

        public LogoutCommand(Guid userId, Guid sessionId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            SessionId = sessionId;
        }
    }

    public class RefreshTokenCommand : CommandBase<TokenResult>
    {
        public string RefreshToken { get; }
        public string IpAddress { get; }
        public string UserAgent { get; }

        public RefreshTokenCommand(string refreshToken, string ipAddress, string userAgent, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            RefreshToken = refreshToken;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
    }

    public class RevokeRefreshTokenCommand : CommandBase
    {
        public Guid UserId { get; }
        public Guid TokenId { get; }
        public string Reason { get; }

        public RevokeRefreshTokenCommand(Guid userId, Guid tokenId, string reason, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            TokenId = tokenId;
            Reason = reason;
        }
    }

    // Email verification commands
    public class VerifyEmailCommand : CommandBase
    {
        public Guid UserId { get; }
        public string VerificationToken { get; }

        public VerifyEmailCommand(Guid userId, string verificationToken, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            VerificationToken = verificationToken;
        }
    }

    public class ResendVerificationEmailCommand : CommandBase
    {
        public Guid UserId { get; }

        public ResendVerificationEmailCommand(Guid userId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
        }
    }

    // Password management commands
    public class ChangePasswordCommand : CommandBase
    {
        public Guid UserId { get; }
        public string CurrentPassword { get; }
        public string NewPassword { get; }

        public ChangePasswordCommand(Guid userId, string currentPassword, string newPassword, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
        }
    }

    public class InitiatePasswordResetCommand : CommandBase
    {
        public string Email { get; }

        public InitiatePasswordResetCommand(string email, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            Email = email;
        }
    }

    public class CompletePasswordResetCommand : CommandBase
    {
        public string ResetToken { get; }
        public string NewPassword { get; }

        public CompletePasswordResetCommand(string resetToken, string newPassword, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ResetToken = resetToken;
            NewPassword = newPassword;
        }
    }

    // Two-factor authentication commands
    public class EnableTwoFactorCommand : CommandBase<TwoFactorSetupResult>
    {
        public Guid UserId { get; }
        public string Password { get; }

        public EnableTwoFactorCommand(Guid userId, string password, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Password = password;
        }
    }

    public class DisableTwoFactorCommand : CommandBase
    {
        public Guid UserId { get; }
        public string Password { get; }
        public string TotpCode { get; }

        public DisableTwoFactorCommand(Guid userId, string password, string totpCode, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Password = password;
            TotpCode = totpCode;
        }
    }

    public class VerifyTwoFactorCommand : CommandBase<bool>
    {
        public Guid UserId { get; }
        public string TotpCode { get; }

        public VerifyTwoFactorCommand(Guid userId, string totpCode, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            TotpCode = totpCode;
        }
    }

    // Role and permission commands
    public class AssignRoleCommand : CommandBase
    {
        public Guid UserId { get; }
        public string Role { get; }

        public AssignRoleCommand(Guid userId, string role, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Role = role;
        }
    }

    public class RemoveRoleCommand : CommandBase
    {
        public Guid UserId { get; }
        public string Role { get; }

        public RemoveRoleCommand(Guid userId, string role, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Role = role;
        }
    }

    public class GrantPermissionCommand : CommandBase
    {
        public Guid UserId { get; }
        public string Permission { get; }

        public GrantPermissionCommand(Guid userId, string permission, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Permission = permission;
        }
    }

    public class RevokePermissionCommand : CommandBase
    {
        public Guid UserId { get; }
        public string Permission { get; }

        public RevokePermissionCommand(Guid userId, string permission, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            UserId = userId;
            Permission = permission;
        }
    }

    // Command results
    public record LoginResult(
        Guid UserId,
        Guid SessionId,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        DateTime RefreshTokenExpiresAt,
        bool RequiresTwoFactor = false);

    public record TokenResult(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        DateTime RefreshTokenExpiresAt);

    public record TwoFactorSetupResult(
        string Secret,
        string QrCodeUri,
        List<string> BackupCodes);
}