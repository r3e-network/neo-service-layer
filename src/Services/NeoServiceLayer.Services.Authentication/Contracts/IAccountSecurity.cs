using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for account security operations
    /// </summary>
    public interface IAccountSecurity
    {
        /// <summary>
        /// Locks a user account
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Reason for locking</param>
        /// <returns>True if account was locked</returns>
        Task<bool> LockAccountAsync(string userId, string reason);

        /// <summary>
        /// Unlocks a user account
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if account was unlocked</returns>
        Task<bool> UnlockAccountAsync(string userId);

        /// <summary>
        /// Checks rate limit for an identifier and action
        /// </summary>
        /// <param name="identifier">Identifier to check</param>
        /// <param name="action">Action being performed</param>
        /// <returns>True if within rate limit</returns>
        Task<bool> CheckRateLimitAsync(string identifier, string action);

        /// <summary>
        /// Records a failed attempt
        /// </summary>
        /// <param name="identifier">Identifier</param>
        /// <returns>Task</returns>
        Task RecordFailedAttemptAsync(string identifier);

        /// <summary>
        /// Resets failed attempts counter
        /// </summary>
        /// <param name="identifier">Identifier</param>
        /// <returns>Task</returns>
        Task ResetFailedAttemptsAsync(string identifier);
    }
}