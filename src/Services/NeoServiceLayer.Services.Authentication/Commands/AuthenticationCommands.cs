using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;

namespace NeoServiceLayer.Services.Authentication.Commands
{
    // User management commands
    public record CreateUserCommand(
        string Username,
        string Email,
        string Password,
        List<string>? InitialRoles = null) : ICommand<Guid>;

    public record DeleteUserCommand(Guid UserId) : ICommand;

    public record SuspendUserCommand(
        Guid UserId,
        string Reason) : ICommand;

    public record ReactivateUserCommand(Guid UserId) : ICommand;

    // Authentication commands
    public record LoginCommand(
        string UsernameOrEmail,
        string Password,
        string IpAddress,
        string UserAgent,
        string? DeviceId = null,
        string? TotpCode = null) : ICommand<LoginResult>;

    public record LogoutCommand(
        Guid UserId,
        Guid SessionId) : ICommand;

    public record RefreshTokenCommand(
        string RefreshToken,
        string IpAddress,
        string UserAgent) : ICommand<TokenResult>;

    public record RevokeRefreshTokenCommand(
        Guid UserId,
        Guid TokenId,
        string Reason) : ICommand;

    // Email verification commands
    public record VerifyEmailCommand(
        Guid UserId,
        string VerificationToken) : ICommand;

    public record ResendVerificationEmailCommand(Guid UserId) : ICommand;

    // Password management commands
    public record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword) : ICommand;

    public record InitiatePasswordResetCommand(string Email) : ICommand;

    public record CompletePasswordResetCommand(
        string ResetToken,
        string NewPassword) : ICommand;

    // Two-factor authentication commands
    public record EnableTwoFactorCommand(
        Guid UserId,
        string Password) : ICommand<TwoFactorSetupResult>;

    public record DisableTwoFactorCommand(
        Guid UserId,
        string Password,
        string TotpCode) : ICommand;

    public record VerifyTwoFactorCommand(
        Guid UserId,
        string TotpCode) : ICommand<bool>;

    // Role and permission commands
    public record AssignRoleCommand(
        Guid UserId,
        string Role) : ICommand;

    public record RemoveRoleCommand(
        Guid UserId,
        string Role) : ICommand;

    public record GrantPermissionCommand(
        Guid UserId,
        string Permission) : ICommand;

    public record RevokePermissionCommand(
        Guid UserId,
        string Permission) : ICommand;

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