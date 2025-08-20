using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.TestUtilities.MockServices
{
    /// <summary>
    /// Mock authentication service for testing authentication workflows.
    /// </summary>
    public class MockAuthenticationService : MockServiceBase
    {
        private readonly Dictionary<string, TestUserData> _users;
        private readonly Dictionary<string, TestAuthToken> _tokens;
        private readonly Dictionary<string, AuthenticationSession> _sessions;

        public MockAuthenticationService(ILogger<MockAuthenticationService> logger, Dictionary<string, object>? configuration = null) 
            : base(logger, configuration)
        {
            _users = new Dictionary<string, TestUserData>();
            _tokens = new Dictionary<string, TestAuthToken>();
            _sessions = new Dictionary<string, AuthenticationSession>();
            
            // Add default test users
            SeedTestUsers();
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        public async Task<UserRegistrationResult> RegisterUserAsync(string username, string password, object? userInfo = null)
        {
            await SimulateDelay();
            SimulateFailure(nameof(RegisterUserAsync));

            try
            {
                if (_users.ContainsKey(username))
                {
                    var existsResult = new UserRegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "User already exists",
                        UserId = null
                    };
                    
                    RecordCall(nameof(RegisterUserAsync), new { username, userInfo }, existsResult);
                    return existsResult;
                }

                var userId = Guid.NewGuid().ToString();
                var user = new TestUserData
                {
                    UserId = userId,
                    Username = username,
                    Email = $"{username}@test.com",
                    Password = password,
                    PasswordHash = GeneratePasswordHash(password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Profile = userInfo as Dictionary<string, object> ?? new Dictionary<string, object>()
                };

                _users[username] = user;

                var result = new UserRegistrationResult
                {
                    Success = true,
                    UserId = userId,
                    Username = username,
                    CreatedAt = user.CreatedAt
                };

                RecordCall(nameof(RegisterUserAsync), new { username, userInfo }, result);
                _logger.LogInformation("User {Username} registered successfully with ID {UserId}", username, userId);

                return result;
            }
            catch (Exception ex)
            {
                var errorResult = new UserRegistrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                RecordCall(nameof(RegisterUserAsync), new { username, userInfo }, null, ex);
                throw;
            }
        }

        /// <summary>
        /// Initiates a login process with challenge-response.
        /// </summary>
        public async Task<LoginChallengeResult> InitiateLoginAsync(string username, string? challenge = null)
        {
            await SimulateDelay();
            SimulateFailure(nameof(InitiateLoginAsync));

            try
            {
                if (!_users.TryGetValue(username, out var user) || !user.IsActive)
                {
                    var notFoundResult = new LoginChallengeResult
                    {
                        Success = false,
                        ErrorMessage = "User not found or inactive"
                    };

                    RecordCall(nameof(InitiateLoginAsync), new { username, challenge }, notFoundResult);
                    return notFoundResult;
                }

                var challengeId = Guid.NewGuid().ToString();
                var challengeData = challenge ?? GenerateRandomChallenge();

                var session = new AuthenticationSession
                {
                    SessionId = challengeId,
                    UserId = user.UserId,
                    Username = username,
                    Challenge = challengeData,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    Status = "challenge_sent"
                };

                _sessions[challengeId] = session;

                var result = new LoginChallengeResult
                {
                    Success = true,
                    ChallengeId = challengeId,
                    Challenge = challengeData,
                    ExpiresAt = session.ExpiresAt
                };

                RecordCall(nameof(InitiateLoginAsync), new { username, challenge }, result);
                _logger.LogDebug("Login challenge initiated for user {Username}", username);

                return result;
            }
            catch (Exception ex)
            {
                RecordCall(nameof(InitiateLoginAsync), new { username, challenge }, null, ex);
                throw;
            }
        }

        /// <summary>
        /// Completes login with challenge response.
        /// </summary>
        public async Task<LoginResult> CompleteLoginAsync(string challengeId, string signature)
        {
            await SimulateDelay();
            SimulateFailure(nameof(CompleteLoginAsync));

            try
            {
                if (!_sessions.TryGetValue(challengeId, out var session))
                {
                    var invalidResult = new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid challenge ID"
                    };

                    RecordCall(nameof(CompleteLoginAsync), new { challengeId, signature }, invalidResult);
                    return invalidResult;
                }

                if (session.ExpiresAt < DateTime.UtcNow)
                {
                    var expiredResult = new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Challenge expired"
                    };

                    RecordCall(nameof(CompleteLoginAsync), new { challengeId, signature }, expiredResult);
                    return expiredResult;
                }

                if (!ValidateSignature(session.Challenge, signature))
                {
                    var invalidSigResult = new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid signature"
                    };

                    RecordCall(nameof(CompleteLoginAsync), new { challengeId, signature }, invalidSigResult);
                    return invalidSigResult;
                }

                // Generate auth token
                var token = GenerateAuthToken(session.UserId);
                _tokens[token.AccessToken] = token;

                // Update session
                session.Status = "authenticated";
                session.AccessToken = token.AccessToken;

                var result = new LoginResult
                {
                    Success = true,
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                    ExpiresAt = token.ExpiresAt,
                    UserId = session.UserId,
                    Username = session.Username
                };

                RecordCall(nameof(CompleteLoginAsync), new { challengeId, signature }, result);
                _logger.LogInformation("User {Username} logged in successfully", session.Username);

                return result;
            }
            catch (Exception ex)
            {
                RecordCall(nameof(CompleteLoginAsync), new { challengeId, signature }, null, ex);
                throw;
            }
        }

        /// <summary>
        /// Validates an authentication token.
        /// </summary>
        public async Task<TokenValidationResult> ValidateTokenAsync(string accessToken)
        {
            await SimulateDelay();
            SimulateFailure(nameof(ValidateTokenAsync));

            try
            {
                if (!_tokens.TryGetValue(accessToken, out var token))
                {
                    var notFoundResult = new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Token not found"
                    };

                    RecordCall(nameof(ValidateTokenAsync), new { accessToken }, notFoundResult);
                    return notFoundResult;
                }

                if (token.ExpiresAt < DateTime.UtcNow)
                {
                    var expiredResult = new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Token expired"
                    };

                    RecordCall(nameof(ValidateTokenAsync), new { accessToken }, expiredResult);
                    return expiredResult;
                }

                var result = new TokenValidationResult
                {
                    IsValid = true,
                    UserId = token.UserId,
                    Claims = token.Claims,
                    ExpiresAt = token.ExpiresAt
                };

                RecordCall(nameof(ValidateTokenAsync), new { accessToken }, result);
                return result;
            }
            catch (Exception ex)
            {
                RecordCall(nameof(ValidateTokenAsync), new { accessToken }, null, ex);
                throw;
            }
        }

        /// <summary>
        /// Logs out a user by invalidating their token.
        /// </summary>
        public async Task<LogoutResult> LogoutAsync(string accessToken)
        {
            await SimulateDelay();
            SimulateFailure(nameof(LogoutAsync));

            try
            {
                var tokenRemoved = _tokens.Remove(accessToken);
                
                // Remove associated sessions
                var sessionsToRemove = new List<string>();
                foreach (var kvp in _sessions)
                {
                    if (kvp.Value.AccessToken == accessToken)
                    {
                        sessionsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var sessionId in sessionsToRemove)
                {
                    _sessions.Remove(sessionId);
                }

                var result = new LogoutResult
                {
                    Success = true,
                    TokenInvalidated = tokenRemoved
                };

                RecordCall(nameof(LogoutAsync), new { accessToken }, result);
                _logger.LogDebug("User logged out, token invalidated: {TokenInvalidated}", tokenRemoved);

                return result;
            }
            catch (Exception ex)
            {
                RecordCall(nameof(LogoutAsync), new { accessToken }, null, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets user information by user ID.
        /// </summary>
        public async Task<TestUserData?> GetUserAsync(string userId)
        {
            await SimulateDelay();
            SimulateFailure(nameof(GetUserAsync));

            try
            {
                var user = _users.Values.FirstOrDefault(u => u.UserId == userId);
                
                RecordCall(nameof(GetUserAsync), new { userId }, user);
                return user;
            }
            catch (Exception ex)
            {
                RecordCall(nameof(GetUserAsync), new { userId }, null, ex);
                throw;
            }
        }

        #region Private Helper Methods

        private void SeedTestUsers()
        {
            var defaultUsers = new[]
            {
                new TestUserData
                {
                    UserId = "test-user-1",
                    Username = "testuser1",
                    Email = "testuser1@example.com",
                    Password = "TestPassword123!",
                    PasswordHash = GeneratePasswordHash("TestPassword123!"),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    Roles = new List<string> { "User" }
                },
                new TestUserData
                {
                    UserId = "test-admin-1",
                    Username = "testadmin",
                    Email = "testadmin@example.com",
                    Password = "AdminPassword123!",
                    PasswordHash = GeneratePasswordHash("AdminPassword123!"),
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    IsActive = true,
                    Roles = new List<string> { "User", "Admin" }
                }
            };

            foreach (var user in defaultUsers)
            {
                _users[user.Username] = user;
            }

            _logger.LogDebug("Seeded {UserCount} default test users", defaultUsers.Length);
        }

        private string GeneratePasswordHash(string password)
        {
            // Simple hash for testing - in real implementation use proper hashing
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"hashed_{password}"));
        }

        private string GenerateRandomChallenge()
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"challenge_{Guid.NewGuid()}"));
        }

        private bool ValidateSignature(string challenge, string signature)
        {
            // Simple validation for testing - in real implementation use cryptographic validation
            return !string.IsNullOrEmpty(signature) && signature.Length > 10;
        }

        private TestAuthToken GenerateAuthToken(string userId)
        {
            return new TestAuthToken
            {
                TokenId = Guid.NewGuid().ToString(),
                UserId = userId,
                AccessToken = Guid.NewGuid().ToString(),
                RefreshToken = Guid.NewGuid().ToString(),
                TokenType = "Bearer",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IssuedAt = DateTime.UtcNow,
                Scope = "read write",
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = userId,
                    ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["exp"] = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
                }
            };
        }

        #endregion
    }

    #region Result Classes

    public class UserRegistrationResult
    {
        public bool Success { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LoginChallengeResult
    {
        public bool Success { get; set; }
        public string? ChallengeId { get; set; }
        public string? Challenge { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? UserId { get; set; }
        public Dictionary<string, object>? Claims { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LogoutResult
    {
        public bool Success { get; set; }
        public bool TokenInvalidated { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthenticationSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Challenge { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
    }

    #endregion
}