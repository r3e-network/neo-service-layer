using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for token management operations
    /// </summary>
    public interface ITokenManagement
    {
        /// <summary>
        /// Validates a token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>True if token is valid</returns>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Revokes a token
        /// </summary>
        /// <param name="token">Token to revoke</param>
        /// <returns>Task</returns>
        Task RevokeTokenAsync(string token);

        /// <summary>
        /// Generates a token pair for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roles">User roles</param>
        /// <returns>Token pair</returns>
        Task<TokenPair> GenerateTokenPairAsync(string userId, string[] roles);

        /// <summary>
        /// Refreshes an access token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>New token pair</returns>
        Task<TokenPair> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Checks if a token is blacklisted
        /// </summary>
        /// <param name="token">Token to check</param>
        /// <returns>True if blacklisted</returns>
        Task<bool> IsTokenBlacklistedAsync(string token);
    }
}