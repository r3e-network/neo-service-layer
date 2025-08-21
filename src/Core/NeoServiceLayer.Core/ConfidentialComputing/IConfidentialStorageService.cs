using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Interface for SGX-based confidential storage operations
    /// Provides secure, encrypted storage with SGX sealing and unsealing capabilities
    /// </summary>
    public interface IConfidentialStorageService
    {
        /// <summary>
        /// Stores data securely using SGX sealing
        /// </summary>
        /// <typeparam name="T">Type of data to store</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="data">Data to store</param>
        /// <param name="storageOptions">Storage configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage result</returns>
        Task<ConfidentialStorageResult> StoreAsync<T>(
            string key,
            T data,
            ConfidentialStorageOptions? storageOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves and unseals data from secure storage
        /// </summary>
        /// <typeparam name="T">Type of data to retrieve</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Retrieved data</returns>
        Task<ConfidentialRetrievalResult<T>> RetrieveAsync<T>(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if data exists in secure storage
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if data exists</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes data from secure storage with secure wiping
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deletion result</returns>
        Task<ConfidentialDeletionResult> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all keys matching a pattern
        /// </summary>
        /// <param name="keyPattern">Key pattern (supports wildcards)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching keys with metadata</returns>
        Task<ConfidentialKeyListResult> ListKeysAsync(
            string keyPattern = "*",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a backup of sealed data
        /// </summary>
        /// <param name="backupRequest">Backup configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Backup result</returns>
        Task<ConfidentialBackupResult> CreateBackupAsync(
            ConfidentialBackupRequest backupRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores data from a backup
        /// </summary>
        /// <param name="restoreRequest">Restore configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Restore result</returns>
        Task<ConfidentialRestoreResult> RestoreBackupAsync(
            ConfidentialRestoreRequest restoreRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage statistics and health information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage statistics</returns>
        Task<ConfidentialStorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs integrity check on stored data
        /// </summary>
        /// <param name="key">Storage key (optional, checks all if null)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Integrity check result</returns>
        Task<ConfidentialIntegrityResult> CheckIntegrityAsync(
            string? key = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a secure storage transaction for multiple operations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transaction handle</returns>
        Task<IConfidentialStorageTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a confidential storage transaction
    /// </summary>
    public interface IConfidentialStorageTransaction : IDisposable
    {
        /// <summary>
        /// Transaction identifier
        /// </summary>
        string TransactionId { get; }

        /// <summary>
        /// Whether the transaction is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Stores data within the transaction
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="data">Data to store</param>
        /// <param name="storageOptions">Storage options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage result</returns>
        Task<ConfidentialStorageResult> StoreAsync<T>(
            string key,
            T data,
            ConfidentialStorageOptions? storageOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes data within the transaction
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deletion result</returns>
        Task<ConfidentialDeletionResult> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all operations in the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Commit result</returns>
        Task<ConfidentialTransactionResult> CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all operations in the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rollback result</returns>
        Task<ConfidentialTransactionResult> RollbackAsync(CancellationToken cancellationToken = default);
    }
}