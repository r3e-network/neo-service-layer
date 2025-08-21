using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Core interface for SGX-based confidential computing operations
    /// Provides easy-to-use abstractions for secure computation within trusted execution environments
    /// </summary>
    public interface IConfidentialComputingService
    {
        /// <summary>
        /// Executes confidential computation within SGX enclave
        /// </summary>
        /// <typeparam name="TInput">Input data type</typeparam>
        /// <typeparam name="TOutput">Output data type</typeparam>
        /// <param name="computation">Computation definition</param>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Computation result</returns>
        Task<ConfidentialComputationResult<TOutput>> ExecuteAsync<TInput, TOutput>(
            ConfidentialComputation<TInput, TOutput> computation,
            TInput input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes JavaScript code within SGX enclave for privacy-preserving operations
        /// </summary>
        /// <param name="scriptTemplate">JavaScript template from PrivacyComputingJavaScriptTemplates</param>
        /// <param name="parameters">Parameters for the script</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Script execution result</returns>
        Task<ConfidentialScriptResult> ExecuteJavaScriptAsync(
            string scriptTemplate,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a secure computation session for multiple operations
        /// </summary>
        /// <param name="sessionOptions">Session configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Secure session handle</returns>
        Task<IConfidentialSession> CreateSessionAsync(
            ConfidentialSessionOptions sessionOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates cryptographically secure random data using SGX hardware
        /// </summary>
        /// <param name="length">Number of random bytes to generate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cryptographically secure random bytes</returns>
        Task<byte[]> GenerateSecureRandomAsync(int length, CancellationToken cancellationToken = default);

        /// <summary>
        /// Encrypts data using SGX-protected keys
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="keyId">Key identifier for encryption</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Encrypted data</returns>
        Task<ConfidentialEncryptionResult> EncryptAsync(
            byte[] data,
            string keyId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypts data using SGX-protected keys
        /// </summary>
        /// <param name="encryptedData">Encrypted data</param>
        /// <param name="keyId">Key identifier for decryption</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Decrypted data</returns>
        Task<byte[]> DecryptAsync(
            byte[] encryptedData,
            string keyId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates SGX attestation for remote verification
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Attestation report</returns>
        Task<AttestationReport> GetAttestationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the health status of the confidential computing service
        /// </summary>
        /// <returns>Service health status</returns>
        Task<ConfidentialComputingHealth> GetHealthAsync();
    }

    /// <summary>
    /// Represents a secure computation session within SGX enclave
    /// </summary>
    public interface IConfidentialSession : IDisposable
    {
        /// <summary>
        /// Unique session identifier
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// Session creation timestamp
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Whether the session is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Executes computation within the session context
        /// </summary>
        /// <typeparam name="TInput">Input type</typeparam>
        /// <typeparam name="TOutput">Output type</typeparam>
        /// <param name="computation">Computation to execute</param>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Computation result</returns>
        Task<ConfidentialComputationResult<TOutput>> ExecuteAsync<TInput, TOutput>(
            ConfidentialComputation<TInput, TOutput> computation,
            TInput input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores data securely within the session
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="data">Data to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success indicator</returns>
        Task<bool> StoreAsync(string key, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves data from secure session storage
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Retrieved data</returns>
        Task<byte[]?> RetrieveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Terminates the session and clears all data
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task completion</returns>
        Task TerminateAsync(CancellationToken cancellationToken = default);
    }
}