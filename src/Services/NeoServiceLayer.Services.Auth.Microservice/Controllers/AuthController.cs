using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NeoServiceLayer.Services.Auth.Microservice.Models;
using NeoServiceLayer.Services.Auth.Microservice.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace NeoServiceLayer.Services.Auth.Microservice.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AuthApi")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ITokenService _tokenService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        ITokenService tokenService,
        IRateLimitingService rateLimitingService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with username/password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT tokens and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Rate limiting check
        if (!await _rateLimitingService.CheckRateLimitAsync(ipAddress, "login"))
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
            return StatusCode(429, new ProblemDetails
            {
                Title = "Rate Limit Exceeded",
                Detail = "Too many login attempts. Please try again later.",
                Status = 429
            });
        }

        try
        {
            var result = await _authService.AuthenticateAsync(
                request.Username, 
                request.Password, 
                request.MfaCode,
                ipAddress,
                userAgent,
                request.DeviceId);

            if (!result.Success)
            {
                await _rateLimitingService.RecordFailedAttemptAsync(ipAddress);
                
                _logger.LogWarning("Failed login attempt for user: {Username} from IP: {IpAddress}. Reason: {Reason}",
                    request.Username, ipAddress, result.ErrorCode);

                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = result.ErrorMessage ?? "Invalid credentials",
                    Status = 401,
                    Extensions = { ["errorCode"] = result.ErrorCode?.ToString() }
                });
            }

            // Reset rate limiting on successful login
            await _rateLimitingService.ResetFailedAttemptsAsync(ipAddress);

            _logger.LogInformation("Successful login for user: {Username} from IP: {IpAddress}",
                request.Username, ipAddress);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during authentication",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Register new user account
    /// </summary>
    /// <param name="request">User registration information</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Rate limiting for registration
        if (!await _rateLimitingService.CheckRateLimitAsync(ipAddress, "register"))
        {
            return StatusCode(429, new ProblemDetails
            {
                Title = "Rate Limit Exceeded",
                Detail = "Too many registration attempts. Please try again later.",
                Status = 429
            });
        }

        try
        {
            var result = await _authService.RegisterAsync(request, ipAddress);

            if (!result.Success)
            {
                if (result.Message?.Contains("already exists") == true)
                {
                    return Conflict(new ProblemDetails
                    {
                        Title = "User Already Exists",
                        Detail = result.Message,
                        Status = 409
                    });
                }

                return BadRequest(new ValidationProblemDetails
                {
                    Title = "Registration Failed",
                    Detail = result.Message ?? "Registration failed",
                    Status = 400,
                    Errors = result.Errors?.ToDictionary(e => "general", e => new[] { e }) 
                            ?? new Dictionary<string, string[]>()
                });
            }

            _logger.LogInformation("New user registered: {Username} from IP: {IpAddress}",
                request.Username, ipAddress);

            return CreatedAtAction(nameof(GetUserInfo), new { userId = result.UserId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during registration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New token pair</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _tokenService.RefreshTokenAsync(request.RefreshToken);

            if (result == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Invalid Refresh Token",
                    Detail = "The refresh token is invalid or expired",
                    Status = 401
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during token refresh",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        var sessionToken = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        try
        {
            await _authService.LogoutAsync(userId, sessionToken);
            
            _logger.LogInformation("User logged out: {UserId}", userId);
            
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during logout",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <returns>Change password result</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

            if (!result)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Password Change Failed",
                    Detail = "Current password is incorrect or new password doesn't meet requirements",
                    Status = 400
                });
            }

            _logger.LogInformation("Password changed for user: {UserId}", userId);
            
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while changing password",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Setup Multi-Factor Authentication
    /// </summary>
    /// <param name="request">MFA setup parameters</param>
    /// <returns>MFA setup information including QR code</returns>
    [HttpPost("mfa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(MfaSetupResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> SetupMfa([FromBody] MfaSetupRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _authService.SetupMfaAsync(userId, request.Type, request.PhoneNumber);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "MFA Setup Failed",
                    Detail = result.Message ?? "Failed to setup MFA",
                    Status = 400
                });
            }

            _logger.LogInformation("MFA setup initiated for user: {UserId}, type: {MfaType}", userId, request.Type);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up MFA for user: {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during MFA setup",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Verify MFA code during setup or login
    /// </summary>
    /// <param name="request">MFA verification code</param>
    /// <returns>Verification result</returns>
    [HttpPost("mfa/verify")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _authService.ValidateMfaCodeAsync(userId, request.Code, request.IsBackupCode);

            if (!result)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid MFA Code",
                    Detail = "The provided MFA code is invalid or expired",
                    Status = 400
                });
            }

            _logger.LogInformation("MFA verification successful for user: {UserId}", userId);
            
            return Ok(new { message = "MFA verification successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying MFA for user: {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during MFA verification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = GetCurrentUserId();

        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }

            var userInfo = new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                emailVerified = user.EmailVerified,
                mfaEnabled = user.MfaEnabled,
                roles = user.Roles,
                createdAt = user.CreatedAt
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info for user: {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving user information",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get active user sessions
    /// </summary>
    /// <returns>List of active sessions</returns>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetCurrentUserId();

        try
        {
            var sessions = await _authService.GetActiveSessionsAsync(userId);
            
            var sessionInfo = sessions.Select(s => new
            {
                id = s.Id,
                deviceId = s.DeviceId,
                ipAddress = s.IpAddress,
                userAgent = s.UserAgent,
                createdAt = s.CreatedAt,
                lastActivityAt = s.LastActivityAt,
                isCurrent = s.SessionToken == HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "")
            });

            return Ok(sessionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user: {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving sessions",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    /// <param name="sessionId">Session ID to revoke</param>
    /// <returns>Revocation confirmation</returns>
    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid sessionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _authService.RevokeSessionAsync(userId, sessionId);
            
            if (!result)
            {
                return NotFound();
            }

            _logger.LogInformation("Session revoked: {SessionId} for user: {UserId}", sessionId, userId);
            
            return Ok(new { message = "Session revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId} for user: {UserId}", sessionId, userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while revoking session",
                Status = 500
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}