using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for the key management service.
    /// </summary>
    public interface IKeyManagementService
    {
        /// <summary>
        /// Creates a new account.
        /// </summary>
        /// <param name="account">The account to create.</param>
        /// <returns>The created account.</returns>
        Task<TeeAccount> CreateAccountAsync(TeeAccount account);

        /// <summary>
        /// Gets an account by ID.
        /// </summary>
        /// <param name="accountId">The ID of the account to get.</param>
        /// <returns>The account with the specified ID.</returns>
        Task<TeeAccount> GetAccountAsync(string accountId);

        /// <summary>
        /// Gets all accounts for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="type">Optional type filter.</param>
        /// <returns>A list of accounts for the user.</returns>
        Task<IEnumerable<TeeAccount>> GetAccountsAsync(string userId, AccountType? type = null);

        /// <summary>
        /// Signs data using an account.
        /// </summary>
        /// <param name="accountId">The ID of the account to use for signing.</param>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use.</param>
        /// <returns>The signature.</returns>
        Task<string> SignAsync(string accountId, byte[] data, string hashAlgorithm = "SHA256");

        /// <summary>
        /// Verifies a signature.
        /// </summary>
        /// <param name="accountId">The ID of the account to use for verification.</param>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> VerifyAsync(string accountId, byte[] data, string signature, string hashAlgorithm = "SHA256");

        /// <summary>
        /// Encrypts data using an account.
        /// </summary>
        /// <param name="accountId">The ID of the account to use for encryption.</param>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        Task<byte[]> EncryptAsync(string accountId, byte[] data);

        /// <summary>
        /// Decrypts data using an account.
        /// </summary>
        /// <param name="accountId">The ID of the account to use for decryption.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        Task<byte[]> DecryptAsync(string accountId, byte[] encryptedData);

        /// <summary>
        /// Exports an account.
        /// </summary>
        /// <param name="accountId">The ID of the account to export.</param>
        /// <param name="password">The password to protect the exported account.</param>
        /// <returns>The exported account data.</returns>
        Task<byte[]> ExportAccountAsync(string accountId, string password);

        /// <summary>
        /// Imports an account.
        /// </summary>
        /// <param name="accountData">The account data to import.</param>
        /// <param name="password">The password to unlock the imported account.</param>
        /// <param name="userId">The ID of the user who will own the account.</param>
        /// <returns>The imported account.</returns>
        Task<TeeAccount> ImportAccountAsync(byte[] accountData, string password, string userId);
    }
}
