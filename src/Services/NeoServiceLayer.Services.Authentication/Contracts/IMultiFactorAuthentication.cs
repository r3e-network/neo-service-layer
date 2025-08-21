using System.Threading.Tasks;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Services.Authentication.Contracts
{
    /// <summary>
    /// Interface for multi-factor authentication operations
    /// </summary>
    public interface IMultiFactorAuthentication
    {
        /// <summary>
        /// Sets up MFA for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="type">MFA type</param>
        /// <returns>MFA setup result</returns>
        Task<MfaSetupResult> SetupMfaAsync(string userId, MfaType type);

        /// <summary>
        /// Validates an MFA code
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="code">MFA code</param>
        /// <returns>True if code is valid</returns>
        Task<bool> ValidateMfaCodeAsync(string userId, string code);

        /// <summary>
        /// Disables MFA for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verificationCode">Verification code</param>
        /// <returns>True if MFA was disabled</returns>
        Task<bool> DisableMfaAsync(string userId, string verificationCode);

        /// <summary>
        /// Generates backup codes for MFA
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Array of backup codes</returns>
        Task<string[]> GenerateBackupCodesAsync(string userId);
    }
}