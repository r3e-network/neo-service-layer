using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication;
using Swashbuckle.AspNetCore.Annotations;

namespace NeoServiceLayer.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [EnableRateLimiting("authentication")]
    [SwaggerTag("Authentication and authorization services")]
    public class AuthenticationController : BaseApiController
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IAuthenticationService authService,
            ILogger<AuthenticationController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "User login", Description = "Authenticate with username and password")]
        [SwaggerResponse(200, "Authentication successful", typeof(LoginResponse))]
        [SwaggerResponse(401, "Invalid credentials")]
        [SwaggerResponse(423, "Account locked")]
        [SwaggerResponse(429, "Too many requests")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (!result.Success)
            {
                if (result.ErrorCode == AuthenticationErrorCode.AccountLocked)
                {
                    return StatusCode(423, new { message = result.ErrorMessage });
                }

                if (result.ErrorCode == AuthenticationErrorCode.RateLimitExceeded)
                {
                    return StatusCode(429, new { message = result.ErrorMessage });
                }

                if (result.RequiresMfa)
                {
                    return Ok(new MfaRequiredResponse
                    {
                        RequiresMfa = true,
                        MfaToken = result.MfaToken,
                        Message = "MFA verification required"
                    });
                }

                return Unauthorized(new { message = result.ErrorMessage });
            }

            return Ok(new LoginResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresAt = result.ExpiresAt,
                UserId = result.UserId,
                Roles = result.Roles
            });
        }

        /// <summary>
        /// Authenticate with MFA code
        /// </summary>
        [HttpPost("login/mfa")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "MFA verification", Description = "Complete login with MFA code")]
        [SwaggerResponse(200, "Authentication successful", typeof(LoginResponse))]
        [SwaggerResponse(401, "Invalid MFA code")]
        public async Task<IActionResult> LoginWithMfa([FromBody] MfaLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.AuthenticateWithMfaAsync(
                request.Username,
                request.Password,
                request.MfaCode);

            if (!result.Success)
            {
                return Unauthorized(new { message = result.ErrorMessage });
            }

            return Ok(new LoginResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresAt = result.ExpiresAt,
                UserId = result.UserId,
                Roles = result.Roles
            });
        }

        /// <summary>
        /// Register new user account
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "User registration", Description = "Create new user account")]
        [SwaggerResponse(201, "Registration successful", typeof(RegistrationResponse))]
        [SwaggerResponse(400, "Invalid registration data")]
        [SwaggerResponse(409, "User already exists")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(new UserRegistrationRequest
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                AcceptTerms = request.AcceptTerms
            });

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetProfile), new { id = result.UserId }, new RegistrationResponse
            {
                Success = true,
                UserId = result.UserId,
                Message = result.Message,
                RequiresEmailVerification = result.RequiresEmailVerification
            });
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Refresh token", Description = "Get new access token using refresh token")]
        [SwaggerResponse(200, "Token refreshed", typeof(TokenRefreshResponse))]
        [SwaggerResponse(401, "Invalid refresh token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var tokenPair = await _authService.RefreshTokenAsync(request.RefreshToken);

                return Ok(new TokenRefreshResponse
                {
                    AccessToken = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = tokenPair.AccessTokenExpiry
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token refresh failed");
                return Unauthorized(new { message = "Invalid refresh token" });
            }
        }

        /// <summary>
        /// Logout and revoke tokens
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logout", Description = "Logout and revoke current tokens")]
        [SwaggerResponse(200, "Logout successful")]
        public async Task<IActionResult> Logout()
        {
            var token = GetAccessToken();
            var sessionId = GetSessionId();

            if (!string.IsNullOrEmpty(token))
            {
                await _authService.RevokeTokenAsync(token);
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                await _authService.RevokeSessionAsync(sessionId);
            }

            return Ok(new { message = "Logout successful" });
        }

        /// <summary>
        /// Setup MFA for current user
        /// </summary>
        [HttpPost("mfa/setup")]
        [Authorize]
        [SwaggerOperation(Summary = "Setup MFA", Description = "Enable multi-factor authentication")]
        [SwaggerResponse(200, "MFA setup successful", typeof(MfaSetupResponse))]
        public async Task<IActionResult> SetupMfa([FromBody] MfaSetupRequest request)
        {
            var userId = GetUserId();
            var result = await _authService.SetupMfaAsync(userId, request.Type);

            return Ok(new MfaSetupResponse
            {
                Success = result.Success,
                Type = result.Type,
                Secret = result.Secret,
                QrCodeUrl = result.QrCodeUrl,
                BackupCodes = result.BackupCodes
            });
        }

        /// <summary>
        /// Disable MFA
        /// </summary>
        [HttpPost("mfa/disable")]
        [Authorize]
        [SwaggerOperation(Summary = "Disable MFA", Description = "Disable multi-factor authentication")]
        [SwaggerResponse(200, "MFA disabled")]
        [SwaggerResponse(400, "Invalid verification code")]
        public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request)
        {
            var userId = GetUserId();
            var success = await _authService.DisableMfaAsync(userId, request.VerificationCode);

            if (!success)
            {
                return BadRequest(new { message = "Invalid verification code" });
            }

            return Ok(new { message = "MFA disabled successfully" });
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("password/change")]
        [Authorize]
        [SwaggerOperation(Summary = "Change password", Description = "Change current user password")]
        [SwaggerResponse(200, "Password changed successfully")]
        [SwaggerResponse(400, "Invalid current password or weak new password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserId();
            var success = await _authService.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword);

            if (!success)
            {
                return BadRequest(new { message = "Failed to change password. Check current password and new password strength." });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        [HttpPost("password/reset")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Request password reset", Description = "Send password reset email")]
        [SwaggerResponse(200, "Reset email sent if account exists")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            // Always return success to prevent email enumeration
            await _authService.InitiatePasswordResetAsync(request.Email);
            return Ok(new { message = "If an account exists with this email, a reset link has been sent." });
        }

        /// <summary>
        /// Complete password reset
        /// </summary>
        [HttpPost("password/reset/confirm")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Complete password reset", Description = "Set new password with reset token")]
        [SwaggerResponse(200, "Password reset successful")]
        [SwaggerResponse(400, "Invalid or expired token")]
        public async Task<IActionResult> CompletePasswordReset([FromBody] PasswordResetConfirmRequest request)
        {
            var success = await _authService.CompletePasswordResetAsync(
                request.Token,
                request.NewPassword);

            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired reset token" });
            }

            return Ok(new { message = "Password reset successful" });
        }

        /// <summary>
        /// Get active sessions
        /// </summary>
        [HttpGet("sessions")]
        [Authorize]
        [SwaggerOperation(Summary = "Get sessions", Description = "Get all active sessions for current user")]
        [SwaggerResponse(200, "Sessions retrieved", typeof(SessionInfo[]))]
        public async Task<IActionResult> GetSessions()
        {
            var userId = GetUserId();
            var sessions = await _authService.GetActiveSessionsAsync(userId);
            return Ok(sessions);
        }

        /// <summary>
        /// Revoke session
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        [Authorize]
        [SwaggerOperation(Summary = "Revoke session", Description = "Revoke specific session")]
        [SwaggerResponse(200, "Session revoked")]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            await _authService.RevokeSessionAsync(sessionId);
            return Ok(new { message = "Session revoked" });
        }

        /// <summary>
        /// Get account security status
        /// </summary>
        [HttpGet("security/status")]
        [Authorize]
        [SwaggerOperation(Summary = "Security status", Description = "Get account security information")]
        [SwaggerResponse(200, "Security status retrieved", typeof(AccountSecurityStatus))]
        public async Task<IActionResult> GetSecurityStatus()
        {
            var userId = GetUserId();
            var status = await _authService.GetAccountSecurityStatusAsync(userId);
            return Ok(status);
        }

        /// <summary>
        /// Get recent login attempts
        /// </summary>
        [HttpGet("security/login-attempts")]
        [Authorize]
        [SwaggerOperation(Summary = "Login attempts", Description = "Get recent login attempts")]
        [SwaggerResponse(200, "Login attempts retrieved", typeof(LoginAttempt[]))]
        public async Task<IActionResult> GetLoginAttempts([FromQuery] int count = 10)
        {
            var userId = GetUserId();
            var attempts = await _authService.GetRecentLoginAttemptsAsync(userId, count);
            return Ok(attempts);
        }

        /// <summary>
        /// Get user profile (placeholder for profile endpoint)
        /// </summary>
        [HttpGet("profile/{id}")]
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetProfile(string id)
        {
            // Placeholder for profile endpoint
            return Ok(new { id });
        }
    }

    // Request/Response DTOs
    public class LoginRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        public string DeviceId { get; set; }
    }

    public class MfaLoginRequest : LoginRequest
    {
        [Required]
        [StringLength(10, MinimumLength = 6)]
        public string MfaCode { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string UserId { get; set; }
        public string[] Roles { get; set; }
    }

    public class MfaRequiredResponse
    {
        public bool RequiresMfa { get; set; }
        public string MfaToken { get; set; }
        public string Message { get; set; }
    }

    public class RegistrationRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 12)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public bool AcceptTerms { get; set; }
    }

    public class RegistrationResponse
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public bool RequiresEmailVerification { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class TokenRefreshResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class MfaSetupRequest
    {
        [Required]
        public MfaType Type { get; set; }
    }

    public class MfaSetupResponse
    {
        public bool Success { get; set; }
        public MfaType Type { get; set; }
        public string Secret { get; set; }
        public string QrCodeUrl { get; set; }
        public string[] BackupCodes { get; set; }
    }

    public class DisableMfaRequest
    {
        [Required]
        public string VerificationCode { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 12)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }

    public class PasswordResetRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class PasswordResetConfirmRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 12)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }
}
