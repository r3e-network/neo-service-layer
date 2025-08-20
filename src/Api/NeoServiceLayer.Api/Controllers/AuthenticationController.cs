using NeoServiceLayer.Api.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Services;
// Swashbuckle annotations removed - use XML documentation
using System.Threading;


namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Authentication and authorization endpoints
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    // SwaggerTag removed - use API versioning and grouping instead
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IComprehensiveAuthenticationService _authService;
        private readonly IEnhancedJwtTokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public AuthenticationController(
            ILogger<AuthenticationController> logger,
            IComprehensiveAuthenticationService authService,
            IEnhancedJwtTokenService tokenService,
            IEmailService emailService,
            IAuditService auditService)
        {
            _logger = logger;
            _authService = authService;
            _tokenService = tokenService;
            _emailService = emailService;
            _auditService = auditService;
        }

        /// <summary>
        /// Authenticate user and receive JWT tokens
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT access and refresh tokens</returns>
        /// <response code="200">Successfully authenticated</response>
        /// <response code="202">Two-factor authentication required</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="403">Account locked or email not verified</response>
        /// <response code="429">Too many login attempts</response>
        [HttpPost("login")]
        [AllowAnonymous]
        // SwaggerOperation attributes removed - use XML documentation comments instead
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TwoFactorChallengeResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.AuthenticateAsync(
                    request.Username,
                    request.Password,
                    ipAddress,
                    userAgent,
                    request.TwoFactorCode);

                if (result.RequiresTwoFactor)
                {
                    return Accepted(new TwoFactorChallengeResponse
                    {
                        RequiresTwoFactor = true,
                        Message = "Two-factor authentication required"
                    });
                }

                if (!result.Success)
                {
                    return result.ErrorCode switch
                    {
                        AuthenticationErrorCode.InvalidCredentials => Unauthorized(new ErrorResponse
                        {
                            Error = "invalid_credentials",
                            Message = result.Error
                        }),
                        AuthenticationErrorCode.AccountLocked => StatusCode(403, new ErrorResponse
                        {
                            Error = "account_locked",
                            Message = "Account is locked"
                        }),
                        AuthenticationErrorCode.EmailNotVerified => StatusCode(403, new ErrorResponse
                        {
                            Error = "email_not_verified",
                            Message = "Email address not verified"
                        }),
                        AuthenticationErrorCode.RateLimitExceeded => StatusCode(429, new ErrorResponse
                        {
                            Error = "rate_limit_exceeded",
                            Message = result.Error
                        }),
                        _ => BadRequest(new ErrorResponse
                        {
                            Error = "authentication_failed",
                            Message = result.Error
                        })
                    };
                }

                return Ok(new LoginResponse
                {
                    AccessToken = result.Tokens.AccessToken,
                    RefreshToken = result.Tokens.RefreshToken,
                    TokenType = result.Tokens.TokenType,
                    ExpiresIn = (int)(result.Tokens.AccessTokenExpiration - DateTime.UtcNow).TotalSeconds,
                    Username = result.Username,
                    Email = result.Email,
                    Roles = result.Roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user {Username}", request.Username);
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during authentication"
                });
            }
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Registration confirmation</returns>
        /// <response code="201">User successfully registered</response>
        /// <response code="400">Invalid registration data or user already exists</response>
        /// <response code="429">Too many registration attempts</response>
        [HttpPost("register")]
        [AllowAnonymous]
        // SwaggerOperation attributes removed - use XML documentation comments instead
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Error = "validation_failed",
                        Message = "Passwords do not match",
                        Errors = new Dictionary<string, string[]>
                        {
                            ["confirmPassword"] = new[] { "Passwords do not match" }
                        }
                    });
                }

                var result = await _authService.RegisterAsync(
                    request.Username,
                    request.Email,
                    request.Password,
                    request.AcceptTerms);

                if (!result.Success)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "registration_failed",
                        Message = result.Error
                    });
                }

                // Send verification email
                if (!string.IsNullOrEmpty(result.EmailVerificationToken))
                {
                    await _emailService.SendVerificationEmailAsync(
                        request.Email,
                        request.Username,
                        result.EmailVerificationToken);
                }

                return CreatedAtAction(
                    nameof(GetProfile),
                    new { },
                    new RegisterResponse
                    {
                        UserId = result.UserId,
                        Username = result.Username,
                        Email = result.Email,
                        Message = "Registration successful. Please check your email to verify your account.",
                        EmailVerificationRequired = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for user {Username}", request.Username);
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during registration"
                });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <returns>User profile information</returns>
        /// <response code="200">User profile retrieved</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("user/profile")]
        [Authorize]
        // SwaggerOperation attributes removed - use XML documentation comments instead
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetUserId();
                var profile = await _authService.GetUserProfileAsync(userId);

                if (profile == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        Error = "user_not_found",
                        Message = "User profile not found"
                    });
                }

                return Ok(new UserProfile
                {
                    UserId = profile.UserId,
                    Username = profile.Username,
                    Email = profile.Email,
                    EmailVerified = profile.EmailVerified,
                    TwoFactorEnabled = profile.TwoFactorEnabled,
                    Roles = profile.Roles,
                    CreatedAt = profile.CreatedAt,
                    LastLoginAt = profile.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred while retrieving profile"
                });
            }
        }

        /// <summary>
        /// Verify email address with token
        /// </summary>
        /// <param name="token">Email verification token</param>
        /// <returns>Verification result</returns>
        /// <response code="200">Email successfully verified</response>
        /// <response code="400">Invalid or expired token</response>
        [HttpGet("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "invalid_token",
                        Message = "Verification token is required"
                    });
                }

                var result = await _authService.VerifyEmailAsync(token);

                if (!result.Success)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "verification_failed",
                        Message = result.Error ?? "Email verification failed"
                    });
                }

                return Ok(new MessageResponse
                {
                    Message = "Email successfully verified. You can now log in."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during email verification"
                });
            }
        }

        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <param name="request">Email address for password reset</param>
        /// <returns>Confirmation message</returns>
        /// <response code="200">Password reset email sent if account exists</response>
        /// <response code="429">Too many requests</response>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                // Always return success to prevent email enumeration
                await _authService.InitiatePasswordResetAsync(request.Email);

                return Ok(new MessageResponse
                {
                    Message = "If an account exists with this email, a password reset link has been sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset request error");
                // Still return success to prevent enumeration
                return Ok(new MessageResponse
                {
                    Message = "If an account exists with this email, a password reset link has been sent."
                });
            }
        }

        /// <summary>
        /// Reset password with token
        /// </summary>
        /// <param name="request">New password and reset token</param>
        /// <returns>Password reset result</returns>
        /// <response code="200">Password successfully reset</response>
        /// <response code="400">Invalid token or weak password</response>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "password_mismatch",
                        Message = "Passwords do not match"
                    });
                }

                var result = await _authService.ResetPasswordAsync(
                    request.Token,
                    request.NewPassword);

                if (!result.Success)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "reset_failed",
                        Message = result.Error ?? "Password reset failed"
                    });
                }

                return Ok(new MessageResponse
                {
                    Message = "Password successfully reset. You can now log in with your new password."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during password reset"
                });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>New access and refresh tokens</returns>
        /// <response code="200">Tokens successfully refreshed</response>
        /// <response code="401">Invalid or expired refresh token</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _tokenService.RefreshTokenAsync(request.RefreshToken);

                if (!result.Success)
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Error = "invalid_refresh_token",
                        Message = "Invalid or expired refresh token"
                    });
                }

                return Ok(new RefreshTokenResponse
                {
                    AccessToken = result.AccessToken,
                    RefreshToken = result.RefreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = result.ExpiresIn
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during token refresh"
                });
            }
        }

        /// <summary>
        /// Logout and revoke tokens
        /// </summary>
        /// <returns>Logout confirmation</returns>
        /// <response code="200">Successfully logged out</response>
        /// <response code="401">Not authenticated</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetUserId();
                var authHeader = Request.Headers["Authorization"].ToString();
                var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

                await _authService.LogoutAsync(userId, token);

                return Ok(new MessageResponse
                {
                    Message = "Successfully logged out"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during logout"
                });
            }
        }

        /// <summary>
        /// Enable two-factor authentication
        /// </summary>
        /// <param name="request">MFA setup request</param>
        /// <returns>MFA setup information including QR code or phone number</returns>
        /// <response code="200">MFA successfully enabled</response>
        /// <response code="400">Invalid MFA type or setup failed</response>
        /// <response code="401">Not authenticated</response>
        [HttpPost("mfa/enable")]
        [Authorize]
        [ProducesResponseType(typeof(MfaSetupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _authService.SetupMfaAsync(userId, request.Type);

                if (!result.Success)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "mfa_setup_failed",
                        Message = result.Error ?? "Failed to enable MFA"
                    });
                }

                return Ok(new MfaSetupResponse
                {
                    Type = result.Type.ToString(),
                    Secret = result.Secret,
                    QrCodeUrl = result.QrCodeUrl,
                    BackupCodes = result.BackupCodes,
                    Message = "MFA has been enabled. Please save your backup codes in a safe place."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MFA setup error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during MFA setup"
                });
            }
        }

        /// <summary>
        /// Verify MFA setup
        /// </summary>
        /// <param name="request">MFA verification code</param>
        /// <returns>MFA verification result</returns>
        /// <response code="200">MFA successfully verified</response>
        /// <response code="400">Invalid verification code</response>
        /// <response code="401">Not authenticated</response>
        [HttpPost("mfa/verify")]
        [Authorize]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
        {
            try
            {
                var userId = GetUserId();
                var isValid = await _authService.ValidateMfaCodeAsync(userId, request.Code);

                if (!isValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "invalid_code",
                        Message = "Invalid verification code"
                    });
                }

                return Ok(new MessageResponse
                {
                    Message = "MFA successfully verified and activated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MFA verification error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred during MFA verification"
                });
            }
        }

        /// <summary>
        /// Disable two-factor authentication
        /// </summary>
        /// <param name="request">Current password for verification</param>
        /// <returns>MFA disable result</returns>
        /// <response code="200">MFA successfully disabled</response>
        /// <response code="400">Invalid password</response>
        /// <response code="401">Not authenticated</response>
        [HttpPost("mfa/disable")]
        [Authorize]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _authService.DisableMfaAsync(userId, request.Password);

                if (!result.Success)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "mfa_disable_failed",
                        Message = result.Error ?? "Failed to disable MFA"
                    });
                }

                return Ok(new MessageResponse
                {
                    Message = "MFA has been disabled"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MFA disable error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred while disabling MFA"
                });
            }
        }

        /// <summary>
        /// Generate new backup codes
        /// </summary>
        /// <returns>New backup codes</returns>
        /// <response code="200">Backup codes generated</response>
        /// <response code="401">Not authenticated</response>
        [HttpPost("mfa/backup-codes")]
        [Authorize]
        [ProducesResponseType(typeof(BackupCodesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateBackupCodes()
        {
            try
            {
                var userId = GetUserId();
                var codes = await _authService.GenerateBackupCodesAsync(userId);

                return Ok(new BackupCodesResponse
                {
                    BackupCodes = codes,
                    Message = "New backup codes generated. Please save them in a safe place. Old codes are now invalid."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generate backup codes error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "internal_error",
                    Message = "An error occurred while generating backup codes"
                });
            }
        }

        #region Helper Methods

        private string GetIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")
                ?? User.FindFirst("user_id");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID not found in token");
        }

        #endregion
    }

    #region Request/Response Models

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(12)]
        public string NewPassword { get; set; }

        [Required]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class EnableMfaRequest
    {
        [Required]
        public string Type { get; set; } // "totp", "sms", or "email"
    }

    public class VerifyMfaRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }
    }

    public class DisableMfaRequest
    {
        [Required]
        public string Password { get; set; }
    }

    public class MfaSetupResponse
    {
        public string Type { get; set; }
        public string Secret { get; set; }
        public string QrCodeUrl { get; set; }
        public string[] BackupCodes { get; set; }
        public string Message { get; set; }
    }

    public class BackupCodesResponse
    {
        public string[] BackupCodes { get; set; }
        public string Message { get; set; }
    }

    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public bool EmailVerificationRequired { get; set; }
    }

    public class TwoFactorChallengeResponse
    {
        public bool RequiresTwoFactor { get; set; }
        public string Message { get; set; }
    }

    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public List<string> Roles { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class MessageResponse
    {
        public string Message { get; set; }
    }


    public class ValidationErrorResponse : ErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; }
    }

    #endregion
}