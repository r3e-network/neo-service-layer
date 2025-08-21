using System;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for password management operations
    /// </summary>
    public interface IPasswordManagement
    {
        /// <summary>
        /// Changes a user's password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if password was changed successfully</returns>
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        /// <summary>
        /// Initiates password reset process
        /// </summary>
        /// <param name="email">User's email</param>
        /// <returns>Reset token</returns>
        Task<string> InitiatePasswordResetAsync(string email);

        /// <summary>
        /// Completes password reset
        /// </summary>
        /// <param name="token">Reset token</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if reset was successful</returns>
        Task<bool> CompletePasswordResetAsync(string token, string newPassword);

        /// <summary>
        /// Finds user ID by reset token
        /// </summary>
        /// <param name="resetToken">Reset token</param>
        /// <returns>User ID if found</returns>
        Task<Guid?> FindUserIdByResetTokenAsync(string resetToken);
    }
}