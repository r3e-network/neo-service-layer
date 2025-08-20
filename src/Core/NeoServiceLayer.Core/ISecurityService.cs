using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Interface for security service operations.
    /// </summary>
    public interface ISecurityService : IService
    {
        /// <summary>
        /// Validates security credentials.
        /// </summary>
        Task<bool> ValidateCredentialsAsync(string credentials);

        /// <summary>
        /// Encrypts data.
        /// </summary>
        Task<byte[]> EncryptAsync(byte[] data);

        /// <summary>
        /// Decrypts data.
        /// </summary>
        Task<byte[]> DecryptAsync(byte[] encryptedData);

        /// <summary>
        /// Generates a secure token.
        /// </summary>
        Task<string> GenerateSecureTokenAsync();

        /// <summary>
        /// Validates a security token.
        /// </summary>
        Task<bool> ValidateTokenAsync(string token);
    }
}