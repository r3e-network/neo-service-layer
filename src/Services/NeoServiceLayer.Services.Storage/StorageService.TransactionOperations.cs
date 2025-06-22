using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Transaction operations for the Storage service.
/// </summary>
public partial class StorageService
{
    private readonly Dictionary<string, StorageTransaction> _activeTransactions = new();
    private readonly object _transactionLock = new();

    /// <inheritdoc/>
    public async Task<string> BeginTransactionAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        IncrementRequestCounters();

        try
        {
            var transactionId = Guid.NewGuid().ToString();
            var transaction = new StorageTransaction
            {
                TransactionId = transactionId,
                BlockchainType = blockchainType,
                StartedAt = DateTime.UtcNow,
                Status = TransactionStatus.Active,
                Operations = new List<StorageOperation>()
            };

            lock (_transactionLock)
            {
                _activeTransactions[transactionId] = transaction;
            }

            // Begin transaction in the enclave
            var transactionData = new
            {
                transactionId,
                blockchainType = blockchainType.ToString(),
                startedAt = transaction.StartedAt
            };

            var result = await _enclaveManager.ExecuteJavaScriptAsync($"beginTransaction('{JsonSerializer.Serialize(transactionData)}')");

            RecordSuccess();
            Logger.LogDebug("Started storage transaction {TransactionId} for {BlockchainType}", transactionId, blockchainType);
            return transactionId;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Failed to begin storage transaction for {BlockchainType}", blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CommitTransactionAsync(string transactionId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(transactionId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        IncrementRequestCounters();

        try
        {
            StorageTransaction? transaction;
            lock (_transactionLock)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out transaction))
                {
                    throw new ArgumentException($"Transaction {transactionId} not found", nameof(transactionId));
                }

                if (transaction.Status != TransactionStatus.Active)
                {
                    throw new InvalidOperationException($"Transaction {transactionId} is not active");
                }

                transaction.Status = TransactionStatus.Committing;
            }

            // Commit transaction in the enclave
            var commitData = new
            {
                transactionId,
                blockchainType = blockchainType.ToString(),
                operations = transaction.Operations
            };

            var result = await _enclaveManager.ExecuteJavaScriptAsync($"commitTransaction('{JsonSerializer.Serialize(commitData)}')");
            var success = JsonSerializer.Deserialize<JsonElement>(result).GetProperty("success").GetBoolean();

            lock (_transactionLock)
            {
                transaction.Status = success ? TransactionStatus.Committed : TransactionStatus.Failed;
                transaction.CompletedAt = DateTime.UtcNow;
                if (success)
                {
                    _activeTransactions.Remove(transactionId);
                }
            }

            if (success)
            {
                RecordSuccess();
                Logger.LogDebug("Committed storage transaction {TransactionId}", transactionId);
            }
            else
            {
                Logger.LogWarning("Failed to commit storage transaction {TransactionId}", transactionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Failed to commit storage transaction {TransactionId}", transactionId);

            // Mark transaction as failed
            lock (_transactionLock)
            {
                if (_activeTransactions.TryGetValue(transactionId, out var transaction))
                {
                    transaction.Status = TransactionStatus.Failed;
                    transaction.CompletedAt = DateTime.UtcNow;
                }
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RollbackTransactionAsync(string transactionId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(transactionId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        IncrementRequestCounters();

        try
        {
            StorageTransaction? transaction;
            lock (_transactionLock)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out transaction))
                {
                    throw new ArgumentException($"Transaction {transactionId} not found", nameof(transactionId));
                }

                if (transaction.Status != TransactionStatus.Active)
                {
                    throw new InvalidOperationException($"Transaction {transactionId} is not active");
                }

                transaction.Status = TransactionStatus.RollingBack;
            }

            // Rollback transaction in the enclave
            var rollbackData = new
            {
                transactionId,
                blockchainType = blockchainType.ToString()
            };

            var result = await _enclaveManager.ExecuteJavaScriptAsync($"rollbackTransaction('{JsonSerializer.Serialize(rollbackData)}')");
            var success = JsonSerializer.Deserialize<JsonElement>(result).GetProperty("success").GetBoolean();

            lock (_transactionLock)
            {
                transaction.Status = success ? TransactionStatus.RolledBack : TransactionStatus.Failed;
                transaction.CompletedAt = DateTime.UtcNow;
                _activeTransactions.Remove(transactionId);
            }

            if (success)
            {
                RecordSuccess();
                Logger.LogDebug("Rolled back storage transaction {TransactionId}", transactionId);
            }
            else
            {
                Logger.LogWarning("Failed to rollback storage transaction {TransactionId}", transactionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Failed to rollback storage transaction {TransactionId}", transactionId);

            // Mark transaction as failed
            lock (_transactionLock)
            {
                if (_activeTransactions.TryGetValue(transactionId, out var transaction))
                {
                    transaction.Status = TransactionStatus.Failed;
                    transaction.CompletedAt = DateTime.UtcNow;
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Gets active transactions.
    /// </summary>
    /// <returns>The list of active transactions.</returns>
    public IEnumerable<StorageTransaction> GetActiveTransactions()
    {
        lock (_transactionLock)
        {
            return _activeTransactions.Values.ToList();
        }
    }
}

/// <summary>
/// Represents a storage transaction.
/// </summary>
public class StorageTransaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the transaction started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the transaction completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the operations in this transaction.
    /// </summary>
    public List<StorageOperation> Operations { get; set; } = new();
}

/// <summary>
/// Represents a storage operation.
/// </summary>
public class StorageOperation
{
    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Transaction status enumeration.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is active.
    /// </summary>
    Active,

    /// <summary>
    /// Transaction is being committed.
    /// </summary>
    Committing,

    /// <summary>
    /// Transaction has been committed.
    /// </summary>
    Committed,

    /// <summary>
    /// Transaction is being rolled back.
    /// </summary>
    RollingBack,

    /// <summary>
    /// Transaction has been rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Transaction has failed.
    /// </summary>
    Failed
}
