using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework.SGX;

/// <summary>
/// Standard interface for services that need SGX computing and storage capabilities.
/// Provides unified methods for JavaScript execution and secure storage operations.
/// </summary>
public interface ISGXComputingService
{
    /// <summary>
    /// Executes JavaScript code securely within the SGX enclave.
    /// </summary>
    /// <param name="context">The execution context containing JavaScript and parameters.</param>
    /// <param name="blockchainType">The blockchain type for storage operations.</param>
    /// <returns>The execution result.</returns>
    Task<SGXExecutionResult> ExecuteSecureComputingAsync(SGXExecutionContext context, BlockchainType blockchainType);

    /// <summary>
    /// Stores data securely in SGX enclave with automatic encryption.
    /// </summary>
    /// <param name="storageContext">The storage context containing key, data, and metadata.</param>
    /// <param name="blockchainType">The blockchain type for storage partitioning.</param>
    /// <returns>The storage result.</returns>
    Task<SGXStorageResult> StoreSecureDataAsync(SGXStorageContext storageContext, BlockchainType blockchainType);

    /// <summary>
    /// Retrieves data securely from SGX enclave with automatic decryption.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="blockchainType">The blockchain type for storage partitioning.</param>
    /// <returns>The retrieved data result.</returns>
    Task<SGXRetrievalResult> RetrieveSecureDataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Deletes data securely from SGX enclave with secure shredding.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="blockchainType">The blockchain type for storage partitioning.</param>
    /// <returns>The deletion result.</returns>
    Task<SGXDeletionResult> DeleteSecureDataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Executes a privacy-preserving computation with automatic storage integration.
    /// </summary>
    /// <param name="computationContext">The computation context with JavaScript, input data, and storage keys.</param>
    /// <param name="blockchainType">The blockchain type for operations.</param>
    /// <returns>The computation result.</returns>
    Task<SGXComputationResult> ExecutePrivacyComputationAsync(SGXComputationContext computationContext, BlockchainType blockchainType);

    /// <summary>
    /// Lists all storage keys accessible by the current service.
    /// </summary>
    /// <param name="prefix">Optional key prefix filter.</param>
    /// <param name="blockchainType">The blockchain type for storage partitioning.</param>
    /// <returns>List of accessible storage keys.</returns>
    Task<SGXKeyListResult> ListStorageKeysAsync(string? prefix, BlockchainType blockchainType);

    /// <summary>
    /// Gets metadata about stored data without retrieving the actual content.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="blockchainType">The blockchain type for storage partitioning.</param>
    /// <returns>The metadata result.</returns>
    Task<SGXMetadataResult> GetStorageMetadataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Executes a batch of operations atomically within the enclave.
    /// </summary>
    /// <param name="batchContext">The batch context containing multiple operations.</param>
    /// <param name="blockchainType">The blockchain type for operations.</param>
    /// <returns>The batch execution result.</returns>
    Task<SGXBatchResult> ExecuteBatchOperationsAsync(SGXBatchContext batchContext, BlockchainType blockchainType);

    /// <summary>
    /// Creates a secure session for multiple related operations.
    /// </summary>
    /// <param name="sessionContext">The session context with configuration and permissions.</param>
    /// <param name="blockchainType">The blockchain type for session operations.</param>
    /// <returns>The session creation result with session ID.</returns>
    Task<SGXSessionResult> CreateSecureSessionAsync(SGXSessionContext sessionContext, BlockchainType blockchainType);

    /// <summary>
    /// Executes operations within an established secure session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="operationContext">The operation context for session execution.</param>
    /// <param name="blockchainType">The blockchain type for operations.</param>
    /// <returns>The session operation result.</returns>
    Task<SGXExecutionResult> ExecuteInSessionAsync(string sessionId, SGXOperationContext operationContext, BlockchainType blockchainType);

    /// <summary>
    /// Closes and cleans up a secure session.
    /// </summary>
    /// <param name="sessionId">The session ID to close.</param>
    /// <param name="blockchainType">The blockchain type for session operations.</param>
    /// <returns>The session closure result.</returns>
    Task<SGXSessionResult> CloseSecureSessionAsync(string sessionId, BlockchainType blockchainType);
}

#region Context Classes

/// <summary>
/// Context for SGX JavaScript execution.
/// </summary>
public class SGXExecutionContext
{
    /// <summary>
    /// The JavaScript code to execute.
    /// </summary>
    public string JavaScriptCode { get; set; } = string.Empty;

    /// <summary>
    /// Input parameters for the JavaScript execution.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Function name to execute (optional, defaults to main function).
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Execution timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Whether to enable debug mode for execution.
    /// </summary>
    public bool EnableDebug { get; set; } = false;

    /// <summary>
    /// Additional execution options.
    /// </summary>
    public Dictionary<string, object> ExecutionOptions { get; set; } = new();

    /// <summary>
    /// Required permissions for the execution.
    /// </summary>
    public List<string> RequiredPermissions { get; set; } = new();
}

/// <summary>
/// Context for SGX secure storage operations.
/// </summary>
public class SGXStorageContext
{
    /// <summary>
    /// The storage key (will be prefixed with service name).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The data to store (will be automatically encrypted).
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Additional metadata for the stored data.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Storage policy for the data.
    /// </summary>
    public SGXStoragePolicy Policy { get; set; } = new();

    /// <summary>
    /// Content type of the stored data.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Compression type to apply to the data.
    /// </summary>
    public SGXCompressionType Compression { get; set; } = SGXCompressionType.None;

    /// <summary>
    /// Tags for categorizing the stored data.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Context for privacy-preserving computations.
/// </summary>
public class SGXComputationContext
{
    /// <summary>
    /// The computation JavaScript code.
    /// </summary>
    public string ComputationCode { get; set; } = string.Empty;

    /// <summary>
    /// Input data storage keys.
    /// </summary>
    public List<string> InputKeys { get; set; } = new();

    /// <summary>
    /// Output data storage keys.
    /// </summary>
    public List<string> OutputKeys { get; set; } = new();

    /// <summary>
    /// Computation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Whether to preserve intermediate results.
    /// </summary>
    public bool PreserveIntermediateResults { get; set; } = false;

    /// <summary>
    /// Maximum computation time in milliseconds.
    /// </summary>
    public int MaxComputationTimeMs { get; set; } = 300000; // 5 minutes

    /// <summary>
    /// Privacy level for the computation.
    /// </summary>
    public SGXPrivacyLevel PrivacyLevel { get; set; } = SGXPrivacyLevel.High;
}

/// <summary>
/// Context for batch operations.
/// </summary>
public class SGXBatchContext
{
    /// <summary>
    /// List of operations to execute in the batch.
    /// </summary>
    public List<SGXBatchOperation> Operations { get; set; } = new();

    /// <summary>
    /// Whether the batch should be atomic (all succeed or all fail).
    /// </summary>
    public bool IsAtomic { get; set; } = true;

    /// <summary>
    /// Maximum batch execution time in milliseconds.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 600000; // 10 minutes

    /// <summary>
    /// Batch metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Context for secure session creation.
/// </summary>
public class SGXSessionContext
{
    /// <summary>
    /// Session name or identifier.
    /// </summary>
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// Session timeout in minutes.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Allowed operations for the session.
    /// </summary>
    public List<SGXOperationType> AllowedOperations { get; set; } = new();

    /// <summary>
    /// Session-specific storage keys that can be accessed.
    /// </summary>
    public List<string> AccessibleKeys { get; set; } = new();

    /// <summary>
    /// Session configuration options.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Context for operations within a session.
/// </summary>
public class SGXOperationContext
{
    /// <summary>
    /// The operation type.
    /// </summary>
    public SGXOperationType OperationType { get; set; }

    /// <summary>
    /// Operation-specific parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// JavaScript code for computation operations.
    /// </summary>
    public string? JavaScriptCode { get; set; }

    /// <summary>
    /// Storage key for storage operations.
    /// </summary>
    public string? StorageKey { get; set; }

    /// <summary>
    /// Data for storage operations.
    /// </summary>
    public byte[]? Data { get; set; }
}

#endregion

#region Result Classes

/// <summary>
/// Result of SGX JavaScript execution.
/// </summary>
public class SGXExecutionResult
{
    /// <summary>
    /// Whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The execution result data.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution metrics.
    /// </summary>
    public SGXExecutionMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Debug information (if debug mode was enabled).
    /// </summary>
    public Dictionary<string, object> DebugInfo { get; set; } = new();

    /// <summary>
    /// Console output from the execution.
    /// </summary>
    public List<string> ConsoleOutput { get; set; } = new();
}

/// <summary>
/// Result of SGX storage operations.
/// </summary>
public class SGXStorageResult
{
    /// <summary>
    /// Whether the storage operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The storage identifier.
    /// </summary>
    public string? StorageId { get; set; }

    /// <summary>
    /// Error message if storage failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Storage fingerprint for integrity verification.
    /// </summary>
    public string? Fingerprint { get; set; }

    /// <summary>
    /// Size of the stored data in bytes.
    /// </summary>
    public long StoredSize { get; set; }

    /// <summary>
    /// Timestamp of the storage operation.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of SGX data retrieval operations.
/// </summary>
public class SGXRetrievalResult
{
    /// <summary>
    /// Whether the retrieval was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The retrieved data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Error message if retrieval failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Metadata associated with the data.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// When the data was last accessed.
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Whether the data was sealed in the enclave.
    /// </summary>
    public bool WasSealed { get; set; }
}

/// <summary>
/// Result of SGX data deletion operations.
/// </summary>
public class SGXDeletionResult
{
    /// <summary>
    /// Whether the deletion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the data was securely shredded.
    /// </summary>
    public bool Shredded { get; set; }

    /// <summary>
    /// Error message if deletion failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp of the deletion.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of privacy-preserving computations.
/// </summary>
public class SGXComputationResult
{
    /// <summary>
    /// Whether the computation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The computation result.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Output data storage keys.
    /// </summary>
    public List<string> OutputKeys { get; set; } = new();

    /// <summary>
    /// Error message if computation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Computation metrics.
    /// </summary>
    public SGXComputationMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Privacy computation attestation.
    /// </summary>
    public string? PrivacyAttestation { get; set; }
}

/// <summary>
/// Result of storage key listing operations.
/// </summary>
public class SGXKeyListResult
{
    /// <summary>
    /// Whether the listing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of storage keys.
    /// </summary>
    public List<string> Keys { get; set; } = new();

    /// <summary>
    /// Error message if listing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total count of keys.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Whether there are more keys available.
    /// </summary>
    public bool HasMore { get; set; }
}

/// <summary>
/// Result of storage metadata operations.
/// </summary>
public class SGXMetadataResult
{
    /// <summary>
    /// Whether the metadata retrieval was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Error message if metadata retrieval failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Size of the data in bytes.
    /// </summary>
    public long DataSize { get; set; }

    /// <summary>
    /// Content type of the stored data.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// When the data was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the data was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// Result of batch operations.
/// </summary>
public class SGXBatchResult
{
    /// <summary>
    /// Whether the entire batch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Results of individual operations.
    /// </summary>
    public List<SGXBatchOperationResult> OperationResults { get; set; } = new();

    /// <summary>
    /// Error message if batch failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Batch execution metrics.
    /// </summary>
    public SGXBatchMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Result of session operations.
/// </summary>
public class SGXSessionResult
{
    /// <summary>
    /// Whether the session operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The session ID.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Error message if session operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Session metadata.
    /// </summary>
    public Dictionary<string, object> SessionData { get; set; } = new();

    /// <summary>
    /// Session expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

#endregion

#region Supporting Types

/// <summary>
/// Storage policy for SGX data.
/// </summary>
public class SGXStoragePolicy
{
    /// <summary>
    /// Sealing policy type.
    /// </summary>
    public SGXSealingPolicyType SealingType { get; set; } = SGXSealingPolicyType.MrSigner;

    /// <summary>
    /// Data expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the data can be shared between services.
    /// </summary>
    public bool AllowSharing { get; set; } = false;

    /// <summary>
    /// Replication policy.
    /// </summary>
    public SGXReplicationPolicy Replication { get; set; } = new();
}

/// <summary>
/// Execution metrics for SGX operations.
/// </summary>
public class SGXExecutionMetrics
{
    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Memory used in bytes.
    /// </summary>
    public long MemoryUsedBytes { get; set; }

    /// <summary>
    /// Number of instructions executed.
    /// </summary>
    public long InstructionCount { get; set; }

    /// <summary>
    /// CPU time used in milliseconds.
    /// </summary>
    public long CpuTimeMs { get; set; }
}

/// <summary>
/// Computation metrics for SGX privacy operations.
/// </summary>
public class SGXComputationMetrics
{
    /// <summary>
    /// Total computation time in milliseconds.
    /// </summary>
    public long ComputationTimeMs { get; set; }

    /// <summary>
    /// Input data size in bytes.
    /// </summary>
    public long InputDataSize { get; set; }

    /// <summary>
    /// Output data size in bytes.
    /// </summary>
    public long OutputDataSize { get; set; }

    /// <summary>
    /// Privacy level achieved.
    /// </summary>
    public SGXPrivacyLevel PrivacyLevel { get; set; }
}

/// <summary>
/// Batch operation definition.
/// </summary>
public class SGXBatchOperation
{
    /// <summary>
    /// Operation type.
    /// </summary>
    public SGXOperationType OperationType { get; set; }

    /// <summary>
    /// Operation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// JavaScript code for computation operations.
    /// </summary>
    public string? JavaScriptCode { get; set; }

    /// <summary>
    /// Storage context for storage operations.
    /// </summary>
    public SGXStorageContext? StorageContext { get; set; }
}

/// <summary>
/// Result of individual batch operations.
/// </summary>
public class SGXBatchOperationResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The operation result.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Operation execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Batch execution metrics.
/// </summary>
public class SGXBatchMetrics
{
    /// <summary>
    /// Total batch execution time in milliseconds.
    /// </summary>
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// Number of successful operations.
    /// </summary>
    public int SuccessfulOperations { get; set; }

    /// <summary>
    /// Number of failed operations.
    /// </summary>
    public int FailedOperations { get; set; }

    /// <summary>
    /// Average operation execution time in milliseconds.
    /// </summary>
    public double AverageOperationTimeMs { get; set; }
}

/// <summary>
/// Replication policy for SGX data.
/// </summary>
public class SGXReplicationPolicy
{
    /// <summary>
    /// Whether to replicate the data.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Number of replicas to maintain.
    /// </summary>
    public int ReplicaCount { get; set; } = 1;

    /// <summary>
    /// Replication strategy.
    /// </summary>
    public SGXReplicationStrategy Strategy { get; set; } = SGXReplicationStrategy.Simple;
}

#endregion

#region Enums

/// <summary>
/// SGX sealing policy types.
/// </summary>
public enum SGXSealingPolicyType
{
    /// <summary>Seal to the signer identity.</summary>
    MrSigner,
    /// <summary>Seal to the enclave identity.</summary>
    MrEnclave,
    /// <summary>Seal to both signer and enclave.</summary>
    Both
}

/// <summary>
/// SGX operation types.
/// </summary>
public enum SGXOperationType
{
    /// <summary>Execute JavaScript computation.</summary>
    Computation,
    /// <summary>Store data securely.</summary>
    Storage,
    /// <summary>Retrieve stored data.</summary>
    Retrieval,
    /// <summary>Delete stored data.</summary>
    Deletion,
    /// <summary>List storage keys.</summary>
    KeyListing,
    /// <summary>Get storage metadata.</summary>
    MetadataRetrieval
}

/// <summary>
/// SGX privacy levels.
/// </summary>
public enum SGXPrivacyLevel
{
    /// <summary>Basic privacy protection.</summary>
    Low,
    /// <summary>Standard privacy protection.</summary>
    Medium,
    /// <summary>High privacy protection.</summary>
    High,
    /// <summary>Maximum privacy protection.</summary>
    Maximum
}

/// <summary>
/// SGX compression types.
/// </summary>
public enum SGXCompressionType
{
    /// <summary>No compression.</summary>
    None,
    /// <summary>GZIP compression.</summary>
    GZip,
    /// <summary>LZ4 compression.</summary>
    LZ4,
    /// <summary>Brotli compression.</summary>
    Brotli
}

/// <summary>
/// SGX replication strategies.
/// </summary>
public enum SGXReplicationStrategy
{
    /// <summary>Simple replication strategy.</summary>
    Simple,
    /// <summary>Distributed replication strategy.</summary>
    Distributed,
    /// <summary>Consensus-based replication strategy.</summary>
    Consensus
}

#endregion