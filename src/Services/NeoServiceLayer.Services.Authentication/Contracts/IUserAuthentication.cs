using System;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for user authentication operations
    /// </summary>
    public interface IUserAuthentication
    {
        /// <summary>
        /// Authenticates a user with username and password
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <returns>Authentication result</returns>
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Authenticates a user with MFA
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <param name="mfaCode">The MFA code</param>
        /// <returns>Authentication result</returns>
        Task<AuthenticationResult> AuthenticateWithMfaAsync(string username, string password, string mfaCode);

        /// <summary>
        /// Validates if a user exists
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="email">The email</param>
        /// <returns>True if user exists</returns>
        Task<bool> UserExistsAsync(string username, string email);

        /// <summary>
        /// Finds a user ID by username or email
        /// </summary>
        /// <param name="usernameOrEmail">Username or email</param>
        /// <returns>User ID if found</returns>
        Task<Guid?> FindUserIdByUsernameOrEmailAsync(string usernameOrEmail);

        /// <summary>
        /// Finds a user ID by email
        /// </summary>
        /// <param name="email">The email</param>
        /// <returns>User ID if found</returns>
        Task<Guid?> FindUserIdByEmailAsync(string email);

        /// <summary>
        /// Gets account security status
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Security status</returns>
        Task<AccountSecurityStatus> GetAccountSecurityStatusAsync(string userId);

        /// <summary>
        /// Gets recent login attempts
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="count">Number of attempts to retrieve</param>
        /// <returns>Login attempts</returns>
        Task<LoginAttempt[]> GetRecentLoginAttemptsAsync(string userId, int count = 10);
    }
}