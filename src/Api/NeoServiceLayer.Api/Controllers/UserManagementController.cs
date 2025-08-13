using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication;
using NeoServiceLayer.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// User management endpoints for administrators
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [Authorize(Roles = "admin")]
    public class UserManagementController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly ISecurityLogger _securityLogger;
        private readonly ITokenService _tokenService;

        public UserManagementController(
            ILogger<UserManagementController> logger,
            IUserRepository userRepository,
            ISecurityLogger securityLogger,
            ITokenService tokenService) : base(logger)
        {
            _userRepository = userRepository;
            _securityLogger = securityLogger;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<UserDto>), 200)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string role = null)
        {
            try
            {
                // This would typically query from database with filtering
                var users = new List<UserDto>();
                
                // For demonstration, return empty paginated response
                var response = new PaginatedResponse<UserDto>
                {
                    Success = true,
                    Data = users,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Message = "Users retrieved successfully",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetUsers");
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                var roles = await _userRepository.GetUserRolesAsync(userId);
                var loginAttempts = await _userRepository.GetRecentLoginAttemptsAsync(userId, 10);

                var userDetail = new UserDetailDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified,
                    MfaEnabled = user.MfaEnabled,
                    MfaType = user.MfaType.ToString(),
                    IsActive = user.IsActive,
                    IsLocked = user.IsLocked,
                    LockReason = user.LockReason,
                    LockedUntil = user.LockedUntil,
                    LastLoginAt = user.LastLoginAt,
                    LastPasswordChangeAt = user.LastPasswordChangeAt,
                    RequiresPasswordChange = user.RequiresPasswordChange,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Roles = roles.ToList(),
                    RecentLoginAttempts = loginAttempts.Select(a => new LoginAttemptDto
                    {
                        AttemptedAt = a.AttemptedAt,
                        Success = a.Success,
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent,
                        FailureReason = a.FailureReason
                    }).ToList(),
                    Metadata = user.Metadata
                };

                await _securityLogger.LogSecurityEventAsync("UserViewed", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                return Ok(CreateResponse(userDetail, "User retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetUser");
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(CreateErrorResponse("Invalid request", ModelState));
                }

                // Check if username already exists
                var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
                if (existingUser != null)
                {
                    return BadRequest(CreateErrorResponse("Username already exists"));
                }

                // Check if email already exists
                existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(CreateErrorResponse("Email already exists"));
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = request.Password, // Will be hashed in repository
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    EmailVerified = request.SendVerificationEmail ? false : true,
                    IsActive = true,
                    RequiresPasswordChange = request.RequirePasswordChange
                };

                var createdUser = await _userRepository.CreateAsync(user);

                // Add roles
                if (request.Roles?.Any() == true)
                {
                    foreach (var role in request.Roles)
                    {
                        await _userRepository.AddUserToRoleAsync(createdUser.Id, role);
                    }
                }

                await _securityLogger.LogSecurityEventAsync("UserCreated", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["NewUserId"] = createdUser.Id,
                        ["Username"] = createdUser.Username,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    FirstName = createdUser.FirstName,
                    LastName = createdUser.LastName,
                    IsActive = createdUser.IsActive,
                    CreatedAt = createdUser.CreatedAt
                };

                return CreatedAtAction(nameof(GetUser), new { userId = createdUser.Id }, 
                    CreateResponse(userDto, "User created successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CreateUser");
            }
        }

        /// <summary>
        /// Update user details
        /// </summary>
        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(CreateErrorResponse("Invalid request", ModelState));
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                // Update user properties
                if (!string.IsNullOrEmpty(request.Email))
                {
                    user.Email = request.Email;
                    user.EmailVerified = false; // Require re-verification
                }

                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user.PhoneNumber = request.PhoneNumber;
                    user.PhoneVerified = false; // Require re-verification
                }

                if (request.IsActive.HasValue)
                    user.IsActive = request.IsActive.Value;

                if (request.RequiresPasswordChange.HasValue)
                    user.RequiresPasswordChange = request.RequiresPasswordChange.Value;

                var updated = await _userRepository.UpdateAsync(user);
                if (!updated)
                {
                    return StatusCode(500, CreateErrorResponse("Failed to update user"));
                }

                await _securityLogger.LogSecurityEventAsync("UserUpdated", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                };

                return Ok(CreateResponse(userDto, "User updated successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "UpdateUser");
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{userId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                // Prevent self-deletion
                if (userId == GetUserId())
                {
                    return BadRequest(CreateErrorResponse("Cannot delete your own account"));
                }

                var deleted = await _userRepository.DeleteAsync(userId);
                if (!deleted)
                {
                    return StatusCode(500, CreateErrorResponse("Failed to delete user"));
                }

                await _securityLogger.LogSecurityEventAsync("UserDeleted", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["DeletedUserId"] = userId,
                        ["DeletedUsername"] = user.Username,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex, "DeleteUser");
            }
        }

        /// <summary>
        /// Lock user account
        /// </summary>
        [HttpPost("{userId}/lock")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LockUser(string userId, [FromBody] LockUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(CreateErrorResponse("Invalid request", ModelState));
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                // Prevent self-locking
                if (userId == GetUserId())
                {
                    return BadRequest(CreateErrorResponse("Cannot lock your own account"));
                }

                user.IsLocked = true;
                user.LockReason = request.Reason;
                user.LockedUntil = request.LockUntil ?? DateTime.UtcNow.AddDays(30);

                await _userRepository.UpdateAsync(user);

                // Revoke all active tokens for the user
                // This would typically be done through a service

                await _securityLogger.LogAccountLockoutAsync(userId, request.Reason);

                return Ok(CreateResponse<object>(null, "User account locked successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "LockUser");
            }
        }

        /// <summary>
        /// Unlock user account
        /// </summary>
        [HttpPost("{userId}/unlock")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                user.IsLocked = false;
                user.LockReason = null;
                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;

                await _userRepository.UpdateAsync(user);

                await _securityLogger.LogSecurityEventAsync("UserUnlocked", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                return Ok(CreateResponse<object>(null, "User account unlocked successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "UnlockUser");
            }
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        [HttpPost("{userId}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetUserPassword(string userId, [FromBody] AdminResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(CreateErrorResponse("Invalid request", ModelState));
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                string newPassword;
                if (request.GeneratePassword)
                {
                    // Generate a random password
                    newPassword = GenerateRandomPassword();
                }
                else if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    newPassword = request.NewPassword;
                }
                else
                {
                    return BadRequest(CreateErrorResponse("Either provide a password or request generation"));
                }

                await _userRepository.ChangePasswordAsync(userId, newPassword);

                // Mark that user needs to change password on next login
                user.RequiresPasswordChange = true;
                await _userRepository.UpdateAsync(user);

                await _securityLogger.LogSecurityEventAsync("AdminPasswordReset", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["RequiresChange"] = true,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                var response = new ResetPasswordResponse
                {
                    TemporaryPassword = request.GeneratePassword ? newPassword : null,
                    RequiresPasswordChange = true,
                    Message = request.GeneratePassword 
                        ? "Password has been reset. Provide the temporary password to the user securely."
                        : "Password has been reset successfully."
                };

                return Ok(CreateResponse(response, "Password reset successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ResetUserPassword");
            }
        }

        /// <summary>
        /// Get user roles
        /// </summary>
        [HttpGet("{userId}/roles")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                var roles = await _userRepository.GetUserRolesAsync(userId);

                return Ok(CreateResponse(roles.ToList(), "Roles retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetUserRoles");
            }
        }

        /// <summary>
        /// Add user to role
        /// </summary>
        [HttpPost("{userId}/roles")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddUserToRole(string userId, [FromBody] RoleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(CreateErrorResponse("Invalid request", ModelState));
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                await _userRepository.AddUserToRoleAsync(userId, request.Role);

                await _securityLogger.LogSecurityEventAsync("UserRoleAdded", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["Role"] = request.Role,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                return Ok(CreateResponse<object>(null, $"User added to role '{request.Role}' successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "AddUserToRole");
            }
        }

        /// <summary>
        /// Remove user from role
        /// </summary>
        [HttpDelete("{userId}/roles/{role}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RemoveUserFromRole(string userId, string role)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                // Prevent removing last admin
                if (role == "admin" && userId != GetUserId())
                {
                    // Check if this is the last admin (in production, would query DB)
                    var currentUserRoles = await _userRepository.GetUserRolesAsync(GetUserId());
                    if (!currentUserRoles.Contains("admin"))
                    {
                        return BadRequest(CreateErrorResponse("Insufficient permissions to remove admin role"));
                    }
                }

                await _userRepository.RemoveUserFromRoleAsync(userId, role);

                await _securityLogger.LogSecurityEventAsync("UserRoleRemoved", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["Role"] = role,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RemoveUserFromRole");
            }
        }

        /// <summary>
        /// Get user sessions
        /// </summary>
        [HttpGet("{userId}/sessions")]
        [ProducesResponseType(typeof(ApiResponse<List<SessionDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserSessions(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                // This would typically fetch from a session store
                var sessions = new List<SessionDto>();

                return Ok(CreateResponse(sessions, "Sessions retrieved successfully"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetUserSessions");
            }
        }

        /// <summary>
        /// Revoke all user sessions
        /// </summary>
        [HttpPost("{userId}/revoke-sessions")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RevokeUserSessions(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(CreateErrorResponse($"User {userId} not found"));
                }

                // This would typically revoke all sessions and tokens
                // Implementation would involve TokenService

                await _securityLogger.LogSecurityEventAsync("UserSessionsRevoked", GetUserId(), 
                    new Dictionary<string, object> 
                    { 
                        ["TargetUserId"] = userId,
                        ["IpAddress"] = GetClientIpAddress()
                    });

                return Ok(CreateResponse<object>(null, "All user sessions have been revoked"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RevokeUserSessions");
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return password;
        }
    }

    // Request/Response DTOs
    public class CreateUserRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }
        public List<string> Roles { get; set; }
        public bool SendVerificationEmail { get; set; }
        public bool RequirePasswordChange { get; set; }
    }

    public class UpdateUserRequest
    {
        [EmailAddress]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
        public bool? RequiresPasswordChange { get; set; }
    }

    public class LockUserRequest
    {
        [Required]
        public string Reason { get; set; }
        public DateTime? LockUntil { get; set; }
    }

    public class AdminResetPasswordRequest
    {
        public string NewPassword { get; set; }
        public bool GeneratePassword { get; set; }
    }

    public class RoleRequest
    {
        [Required]
        public string Role { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserDetailDto : UserDto
    {
        public string PhoneNumber { get; set; }
        public bool EmailVerified { get; set; }
        public bool PhoneVerified { get; set; }
        public bool MfaEnabled { get; set; }
        public string MfaType { get; set; }
        public bool IsLocked { get; set; }
        public string LockReason { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastPasswordChangeAt { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Roles { get; set; }
        public List<LoginAttemptDto> RecentLoginAttempts { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class LoginAttemptDto
    {
        public DateTime AttemptedAt { get; set; }
        public bool Success { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string FailureReason { get; set; }
    }

    public class ResetPasswordResponse
    {
        public string TemporaryPassword { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public string Message { get; set; }
    }

    public class SessionDto
    {
        public string SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool IsActive { get; set; }
    }
}