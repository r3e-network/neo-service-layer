using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Threading;
using NeoServiceLayer.Services.Authentication.Models;


namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// User repository for authentication and user management
    /// </summary>
    public interface IUserRepository
    {
        Task<Models.User> GetByIdAsync(string userId);
        Task<Models.User> GetByUsernameAsync(string username);
        Task<Models.User> GetByEmailAsync(string email);
        Task<Models.User> CreateAsync(Models.User user);
        Task<bool> UpdateAsync(Models.User user);
        Task<bool> DeleteAsync(string userId);
        Task<bool> ValidatePasswordAsync(string userId, string password);
        Task<bool> ChangePasswordAsync(string userId, string newPassword);
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        Task<bool> AddUserToRoleAsync(string userId, string role);
        Task<bool> RemoveUserFromRoleAsync(string userId, string role);
        
        // Additional methods for AuthenticationService compatibility
        Task<bool> UpdatePasswordAsync(string userId, string passwordHash, string salt);
        Task<MfaSettings> GetMfaSettingsAsync(string userId);
        Task<bool> UpdateMfaSettingsAsync(Guid userId, MfaSettings settings);
        Task RecordLoginAttemptAsync(string userId, bool success, string ipAddress, string userAgent);
        Task<IEnumerable<LoginAttempt>> GetRecentLoginAttemptsAsync(string userId, int count);
    }

    /// <summary>
    /// In-memory user repository implementation (for demonstration)
    /// Note: In production, this would use a proper database
    /// </summary>
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ILogger<InMemoryUserRepository> _logger;
        private readonly IDistributedCache _cache;
        private readonly Dictionary<string, Models.User> _users;
        private readonly Dictionary<string, List<string>> _userRoles;
        private readonly Dictionary<string, List<LoginAttempt>> _loginAttempts;

        public InMemoryUserRepository(
            ILogger<InMemoryUserRepository> logger,
            IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
            _users = new Dictionary<string, Models.User>();
            _userRoles = new Dictionary<string, List<string>>();
            _loginAttempts = new Dictionary<string, List<LoginAttempt>>();

            // Seed with test users
            _ = Task.Run(async () => await SeedTestUsersAsync().ConfigureAwait(false));
        }

        public async Task<Models.User> GetByIdAsync(string userId)
        {
            await Task.CompletedTask;
            return _users.TryGetValue(userId, out var user) ? user : null;
        }

        public async Task<Models.User> GetByUsernameAsync(string username)
        {
            await Task.CompletedTask;
            return _users.Values.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Models.User> GetByEmailAsync(string email)
        {
            await Task.CompletedTask;
            return _users.Values.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Models.User> CreateAsync(Models.User user)
        {
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }

            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Hash password if provided
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = HashPassword(user.PasswordHash);
            }

            _users[user.Id.ToString()] = user;
            _userRoles[user.Id.ToString()] = new List<string> { "user" }; // Default role

            _logger.LogInformation("User {UserId} created", user.Id);

            await Task.CompletedTask;
            return user;
        }

        public async Task<bool> UpdateAsync(Models.User user)
        {
            if (!_users.ContainsKey(user.Id.ToString()))
            {
                return false;
            }

            user.UpdatedAt = DateTime.UtcNow;
            _users[user.Id.ToString()] = user;

            _logger.LogInformation("User {UserId} updated", user.Id);

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> DeleteAsync(string userId)
        {
            var removed = _users.Remove(userId);
            if (removed)
            {
                _userRoles.Remove(userId);
                _loginAttempts.Remove(userId);
                _logger.LogInformation("User {UserId} deleted", userId);
            }

            await Task.CompletedTask;
            return removed;
        }

        public async Task<bool> ValidatePasswordAsync(string userId, string password)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return VerifyPassword(password, user.PasswordHash);
        }

        public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.LastPasswordChangeAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return true;
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            await Task.CompletedTask;
            return _userRoles.TryGetValue(userId, out var roles) ? roles : new List<string>();
        }

        public async Task<bool> AddUserToRoleAsync(string userId, string role)
        {
            if (!_userRoles.ContainsKey(userId))
            {
                _userRoles[userId] = new List<string>();
            }

            if (!_userRoles[userId].Contains(role))
            {
                _userRoles[userId].Add(role);
                _logger.LogInformation("User {UserId} added to role {Role}", userId, role);
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> RemoveUserFromRoleAsync(string userId, string role)
        {
            if (_userRoles.TryGetValue(userId, out var roles))
            {
                roles.Remove(role);
                _logger.LogInformation("User {UserId} removed from role {Role}", userId, role);
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task RecordLoginAttemptAsync(string userId, bool success, string ipAddress, string userAgent)
        {
            if (!_loginAttempts.ContainsKey(userId))
            {
                _loginAttempts[userId] = new List<LoginAttempt>();
            }

            var attempt = new LoginAttempt
            {
                AttemptedAt = DateTime.UtcNow,
                Success = success,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                FailureReason = success ? null : "Invalid credentials"
            };

            _loginAttempts[userId].Insert(0, attempt);

            // Keep only last 100 attempts
            if (_loginAttempts[userId].Count > 100)
            {
                _loginAttempts[userId] = _loginAttempts[userId].Take(100).ToList();
            }

            await Task.CompletedTask;
        }

        public async Task<IEnumerable<LoginAttempt>> GetRecentLoginAttemptsAsync(string userId, int count)
        {
            await Task.CompletedTask;

            if (_loginAttempts.TryGetValue(userId, out var attempts))
            {
                return attempts.Take(count);
            }

            return new List<LoginAttempt>();
        }

        private string HashPassword(string password)
        {
            // Use PBKDF2 with random salt
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            // Combine salt and hash for storage
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            var parts = hashedPassword.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var hash = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hash == hashed;
        }

        private async Task SeedTestUsersAsync()
        {
            // Create admin user
            var adminUser = new Models.User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "admin",
                Email = "admin@neoservicelayer.com",
                PasswordHash = "AdminPass123!",
                PasswordSalt = "salt123",
                EmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await CreateAsync(adminUser).ConfigureAwait(false);
            await AddUserToRoleAsync(adminUser.Id.ToString(), "admin").ConfigureAwait(false);
            await AddUserToRoleAsync(adminUser.Id.ToString(), "user").ConfigureAwait(false);

            // Create test user
            var testUser = new Models.User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Username = "testuser",
                Email = "test@neoservicelayer.com",
                PasswordHash = "TestPass123!",
                PasswordSalt = "salt456",
                EmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await CreateAsync(testUser).ConfigureAwait(false);

            _logger.LogInformation("Test users seeded successfully");
        }

        public async Task<bool> UpdatePasswordAsync(string userId, string passwordHash, string salt)
        {
            await Task.CompletedTask;
            if (_users.TryGetValue(userId, out var user))
            {
                user.PasswordHash = passwordHash;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        public async Task<MfaSettings> GetMfaSettingsAsync(string userId)
        {
            await Task.CompletedTask;
            if (_users.TryGetValue(userId, out var user))
            {
                return new MfaSettings
                {
                    Enabled = user.MfaEnabled,
                    Type = Enum.TryParse<MfaType>(user.MfaType, out var mfaType) ? mfaType : MfaType.Totp,
                    Secret = user.MfaSecret
                };
            }
            return new MfaSettings();
        }

        public async Task<bool> UpdateMfaSettingsAsync(Guid userId, MfaSettings settings)
        {
            await Task.CompletedTask;
            var userIdStr = userId.ToString();
            if (_users.TryGetValue(userIdStr, out var user))
            {
                user.MfaEnabled = settings.Enabled;
                user.MfaType = settings.Type.ToString();
                user.MfaSecret = settings.Secret;
                return true;
            }
            return false;
        }

        // RecordLoginAttemptAsync and GetRecentLoginAttemptsAsync already defined above
    }

    /// <summary>
    /// User model
    /// </summary>
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailVerified { get; set; }
        public bool PhoneVerified { get; set; }
        public bool MfaEnabled { get; set; }
        public MfaType MfaType { get; set; }
        public string MfaSecret { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public string LockReason { get; set; }
        public DateTime? LockedUntil { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastPasswordChangeAt { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}