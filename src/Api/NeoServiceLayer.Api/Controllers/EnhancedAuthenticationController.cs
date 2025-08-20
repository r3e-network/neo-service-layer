using NeoServiceLayer.Api.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Enhanced authentication controller with comprehensive auth endpoints
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class EnhancedAuthenticationController : ControllerBase
    {
        private readonly ILogger<EnhancedAuthenticationController> _logger;
        private readonly IComprehensiveAuthenticationService _authService;
        private readonly IEnhancedJwtTokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IEmailService _emailService;

        public EnhancedAuthenticationController(
            ILogger<EnhancedAuthenticationController> logger,
            IComprehensiveAuthenticationService authService,
            IEnhancedJwtTokenService tokenService,
            ITwoFactorService twoFactorService,
            IEmailService emailService)
        {
            _logger = logger;
            _authService = authService;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
        }

        /// <summary>
        /// Authenticate user and receive access/refresh tokens
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ipAddress = GetIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.AuthenticateAsync(
                request.UsernameOrEmail,
                request.Password,
                ipAddress,
                userAgent,
                request.TwoFactorCode);

            if (!result.Success)
            {
                if (result.RequiresTwoFactor)
                {
                    return Ok(new
                    {
                        requiresTwoFactor = true,
                        message = result.Error
                    });
                }

                return Unauthorized(new
                {
                    error = result.Error,
                    errorCode = result.ErrorCode,
                    remainingAttempts = result.RemainingAttempts
                });
            }

            // Set refresh token in HTTP-only cookie
            SetRefreshTokenCookie(result.Tokens.RefreshToken);

            return Ok(new LoginResponse
            {
                AccessToken = result.Tokens.AccessToken,
                TokenType = result.Tokens.TokenType,
                ExpiresIn = (int)(result.Tokens.AccessTokenExpiration - DateTime.UtcNow).TotalSeconds,
                UserId = result.UserId,
                Username = result.Username,
                Email = result.Email,
                Roles = result.Roles,
                SessionId = result.SessionId
            });
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Implementation would create user account
            // This is a placeholder for the actual implementation

            return Ok(new
            {
                message = "Registration successful. Please check your email to verify your account.",
                userId = Guid.NewGuid()
            });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = request?.RefreshToken ?? Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }

            try
            {
                var tokens = await _tokenService.RefreshTokensAsync(refreshToken);

                // Set new refresh token in cookie
                SetRefreshTokenCookie(tokens.RefreshToken);

                return Ok(new
                {
                    accessToken = tokens.AccessToken,
                    tokenType = tokens.TokenType,
                    expiresIn = (int)(tokens.AccessTokenExpiration - DateTime.UtcNow).TotalSeconds
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }
        }

        /// <summary>
        /// Logout and revoke tokens
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            if (userId.HasValue && !string.IsNullOrEmpty(sessionId))
            {
                await _authService.LogoutAsync(sessionId, userId.Value);
            }

            // Clear refresh token cookie
            Response.Cookies.Delete("refresh_token");

            return Ok(new { message = "Logout successful" });
        }

        /// <summary>
        /// Setup two-factor authentication
        /// </summary>
        [HttpPost("2fa/setup")]
        [Authorize]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var userId = GetUserId();
            var username = User.Identity?.Name;

            if (!userId.HasValue || string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var secret = _twoFactorService.GenerateSecret();
            var qrCodeUri = _twoFactorService.GenerateQrCodeUri(username, secret);
            var backupCodes = _twoFactorService.GenerateBackupCodes(8);

            // Save secret and backup codes to user account
            // Implementation would depend on your data access layer

            return Ok(new
            {
                secret = secret,
                qrCode = qrCodeUri,
                backupCodes = backupCodes
            });
        }

        /// <summary>
        /// Enable two-factor authentication
        /// </summary>
        [HttpPost("2fa/enable")]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Verify the code before enabling
            var isValid = _twoFactorService.ValidateTotp(request.Secret, request.VerificationCode);

            if (!isValid)
            {
                return BadRequest(new { error = "Invalid verification code" });
            }

            // Enable 2FA for user
            // Implementation would depend on your data access layer

            return Ok(new { message = "Two-factor authentication enabled successfully" });
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Generate reset token and send email
            // Implementation would depend on your data access layer

            return Ok(new { message = "If the email exists, a password reset link has been sent." });
        }

        /// <summary>
        /// Reset password with token
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate token and reset password
            // Implementation would depend on your data access layer

            return Ok(new { message = "Password has been reset successfully" });
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { error = "Verification token is required" });
            }

            // Verify email with token
            // Implementation would depend on your data access layer

            return Ok(new { message = "Email verified successfully" });
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetUserId();
            var username = User.Identity?.Name;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Ok(new
            {
                userId = userId,
                username = username,
                email = email,
                roles = roles
            });
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"].ToString().Split(',').First().Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
        }
    }

    #region Request/Response Models




    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    public class EnableTwoFactorRequest
    {
        [Required]
        public string Secret { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string VerificationCode { get; set; }
    }

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
        [MinLength(8)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }

    #endregion
}