using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.EventSourcing;
using NeoServiceLayer.Services.Authentication.Domain.Aggregates;
using NeoServiceLayer.Services.Authentication.Services;

namespace NeoServiceLayer.Services.Authentication.Commands
{
    public class AuthenticationCommandHandlers :
        ICommandHandler<CreateUserCommand, Guid>,
        ICommandHandler<DeleteUserCommand>,
        ICommandHandler<SuspendUserCommand>,
        ICommandHandler<ReactivateUserCommand>,
        ICommandHandler<LoginCommand, LoginResult>,
        ICommandHandler<LogoutCommand>,
        ICommandHandler<RefreshTokenCommand, TokenResult>,
        ICommandHandler<RevokeRefreshTokenCommand>,
        ICommandHandler<VerifyEmailCommand>,
        ICommandHandler<ResendVerificationEmailCommand>,
        ICommandHandler<ChangePasswordCommand>,
        ICommandHandler<InitiatePasswordResetCommand>,
        ICommandHandler<CompletePasswordResetCommand>,
        ICommandHandler<EnableTwoFactorCommand, TwoFactorSetupResult>,
        ICommandHandler<DisableTwoFactorCommand>,
        ICommandHandler<VerifyTwoFactorCommand, bool>,
        ICommandHandler<AssignRoleCommand>,
        ICommandHandler<RemoveRoleCommand>,
        ICommandHandler<GrantPermissionCommand>,
        ICommandHandler<RevokePermissionCommand>
    {
        private readonly IEventStore _eventStore;
        private readonly IAuthenticationService _authService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthenticationCommandHandlers> _logger;

        public AuthenticationCommandHandlers(
            IEventStore eventStore,
            IAuthenticationService authService,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            ITwoFactorService twoFactorService,
            IEmailService emailService,
            ILogger<AuthenticationCommandHandlers> logger)
        {
            _eventStore = eventStore;
            _authService = authService;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating user {Username}", command.Username);

            // Check if username or email already exists
            if (await _authService.UserExistsAsync(command.Username, command.Email))
            {
                throw new InvalidOperationException("Username or email already exists");
            }

            // Hash the password
            var passwordHash = _passwordHasher.HashPassword(command.Password);

            // Create the user aggregate
            var user = User.Create(
                command.Username,
                command.Email,
                passwordHash,
                command.InitialRoles);

            // Save to event store
            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);

            // Send verification email
            await _emailService.SendVerificationEmailAsync(
                user.Email,
                user.Username,
                user.EmailVerificationToken!);

            _logger.LogInformation("User {Username} created with ID {UserId}", command.Username, user.Id);
            return user.Id;
        }

        public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.Delete();

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(SuspendUserCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Suspending user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.Suspend(command.Reason);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(ReactivateUserCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reactivating user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.Reactivate();

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Login attempt for {UsernameOrEmail}", command.UsernameOrEmail);

            // Find user by username or email
            var userId = await _authService.FindUserIdByUsernameOrEmailAsync(command.UsernameOrEmail);
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var user = await LoadUserAsync(userId.Value);

            // Verify password
            if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            {
                user.RecordFailedLogin(command.IpAddress, "Invalid password");
                await _eventStore.SaveEventsAsync(
                    user.Id,
                    user.GetUncommittedEvents(),
                    user.Version,
                    cancellationToken);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Check if two-factor is required
            if (user.IsTwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(command.TotpCode))
                {
                    return new LoginResult(
                        user.Id,
                        Guid.Empty,
                        string.Empty,
                        string.Empty,
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        RequiresTwoFactor: true);
                }

                if (!_twoFactorService.ValidateTotp(user.TotpSecret!, command.TotpCode))
                {
                    user.RecordFailedLogin(command.IpAddress, "Invalid TOTP code");
                    await _eventStore.SaveEventsAsync(
                        user.Id,
                        user.GetUncommittedEvents(),
                        user.Version,
                        cancellationToken);
                    throw new UnauthorizedAccessException("Invalid two-factor code");
                }
            }

            // Perform login
            user.Login(command.IpAddress, command.UserAgent, command.DeviceId);

            // Generate tokens
            var sessionId = user.Sessions.Last().Id;
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Roles.ToList());
            var refreshTokenValue = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            
            var refreshToken = user.IssueRefreshToken(refreshTokenValue, refreshTokenExpiry, command.DeviceId);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return new LoginResult(
                user.Id,
                sessionId,
                accessToken,
                refreshToken.Token,
                DateTime.UtcNow.AddHours(1),
                refreshTokenExpiry);
        }

        public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Logging out user {UserId} session {SessionId}", 
                command.UserId, command.SessionId);

            var user = await LoadUserAsync(command.UserId);
            user.Logout(command.SessionId);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task<TokenResult> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing token");

            // Validate and decode the refresh token
            var (userId, tokenId) = await _tokenService.ValidateRefreshTokenAsync(command.RefreshToken);
            
            var user = await LoadUserAsync(userId);
            
            // Find the refresh token
            var token = user.RefreshTokens.FirstOrDefault(t => t.Id == tokenId);
            if (token == null || !token.IsValid)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // Revoke old token and issue new one
            user.RevokeRefreshToken(tokenId, "Token refresh");
            
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            var newToken = user.IssueRefreshToken(newRefreshToken, refreshTokenExpiry, token.DeviceId);

            // Generate new access token
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Roles.ToList());

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);

            return new TokenResult(
                accessToken,
                newToken.Token,
                DateTime.UtcNow.AddHours(1),
                refreshTokenExpiry);
        }

        public async Task HandleAsync(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Revoking refresh token {TokenId} for user {UserId}", 
                command.TokenId, command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.RevokeRefreshToken(command.TokenId, command.Reason);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Verifying email for user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.VerifyEmail(command.VerificationToken);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resending verification email for user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);
            
            if (user.EmailVerifiedAt.HasValue)
            {
                throw new InvalidOperationException("Email already verified");
            }

            await _emailService.SendVerificationEmailAsync(
                user.Email,
                user.Username,
                user.EmailVerificationToken!);
        }

        public async Task HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Changing password for user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);

            // Verify current password
            if (!_passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Hash new password
            var newPasswordHash = _passwordHasher.HashPassword(command.NewPassword);
            user.ChangePassword(newPasswordHash);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(InitiatePasswordResetCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initiating password reset for email {Email}", command.Email);

            var userId = await _authService.FindUserIdByEmailAsync(command.Email);
            if (!userId.HasValue)
            {
                // Don't reveal if email exists
                _logger.LogWarning("Password reset requested for non-existent email {Email}", command.Email);
                return;
            }

            var user = await LoadUserAsync(userId.Value);
            
            var resetToken = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(1);
            
            user.InitiatePasswordReset(resetToken, expiresAt);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);

            await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                user.Username,
                resetToken);
        }

        public async Task HandleAsync(CompletePasswordResetCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Completing password reset");

            // Find user by reset token
            var userId = await _authService.FindUserIdByResetTokenAsync(command.ResetToken);
            if (!userId.HasValue)
            {
                throw new InvalidOperationException("Invalid or expired reset token");
            }

            var user = await LoadUserAsync(userId.Value);
            
            var newPasswordHash = _passwordHasher.HashPassword(command.NewPassword);
            user.CompletePasswordReset(command.ResetToken, newPasswordHash);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task<TwoFactorSetupResult> HandleAsync(EnableTwoFactorCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Enabling two-factor for user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);

            // Verify password
            if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid password");
            }

            // Generate TOTP secret and backup codes
            var secret = _twoFactorService.GenerateSecret();
            var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Username, secret);
            var backupCodes = _twoFactorService.GenerateBackupCodes(8);

            user.EnableTwoFactorAuthentication(secret);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);

            return new TwoFactorSetupResult(secret, qrCodeUri, backupCodes);
        }

        public async Task HandleAsync(DisableTwoFactorCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Disabling two-factor for user {UserId}", command.UserId);

            var user = await LoadUserAsync(command.UserId);

            // Verify password
            if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid password");
            }

            // Verify TOTP code
            if (!_twoFactorService.ValidateTotp(user.TotpSecret!, command.TotpCode))
            {
                throw new UnauthorizedAccessException("Invalid two-factor code");
            }

            user.DisableTwoFactorAuthentication();

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task<bool> HandleAsync(VerifyTwoFactorCommand command, CancellationToken cancellationToken = default)
        {
            var user = await LoadUserAsync(command.UserId);
            
            if (!user.IsTwoFactorEnabled)
            {
                throw new InvalidOperationException("Two-factor authentication not enabled");
            }

            return _twoFactorService.ValidateTotp(user.TotpSecret!, command.TotpCode);
        }

        public async Task HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Assigning role {Role} to user {UserId}", command.Role, command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.AssignRole(command.Role);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(RemoveRoleCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Removing role {Role} from user {UserId}", command.Role, command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.RemoveRole(command.Role);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(GrantPermissionCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Granting permission {Permission} to user {UserId}", 
                command.Permission, command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.GrantPermission(command.Permission);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        public async Task HandleAsync(RevokePermissionCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Revoking permission {Permission} from user {UserId}", 
                command.Permission, command.UserId);

            var user = await LoadUserAsync(command.UserId);
            user.RevokePermission(command.Permission);

            await _eventStore.SaveEventsAsync(
                user.Id,
                user.GetUncommittedEvents(),
                user.Version,
                cancellationToken);
        }

        private async Task<User> LoadUserAsync(Guid userId)
        {
            var events = await _eventStore.GetEventsAsync(userId);
            if (!events.Any())
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            var user = new User();
            user.LoadFromHistory(events);
            return user;
        }
    }
}