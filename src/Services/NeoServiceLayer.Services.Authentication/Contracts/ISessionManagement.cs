using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for session management operations
    /// </summary>
    public interface ISessionManagement
    {
        /// <summary>
        /// Creates a new session
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>Session info</returns>
        Task<SessionInfo> CreateSessionAsync(string userId, string deviceId);

        /// <summary>
        /// Gets active sessions for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Array of active sessions</returns>
        Task<SessionInfo[]> GetActiveSessionsAsync(string userId);

        /// <summary>
        /// Revokes a specific session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>Task</returns>
        Task RevokeSessionAsync(string sessionId);

        /// <summary>
        /// Revokes all sessions for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Task</returns>
        Task RevokeAllSessionsAsync(string userId);
    }
}