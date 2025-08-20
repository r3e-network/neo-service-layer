using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Services.AbstractAccount.Models;

/// <summary>
/// Request model for creating an abstract account.
/// </summary>
public class CreateAccountRequest
{
    /// <summary>
    /// Gets or sets the account owner's public key.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string OwnerPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial guardians for social recovery.
    /// </summary>
    public string[] InitialGuardians { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the recovery threshold (number of guardians needed for recovery).
    /// </summary>
    [Range(1, 10)]
    public int RecoveryThreshold { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether gasless transactions are enabled.
    /// </summary>
    public bool EnableGaslessTransactions { get; set; } = true;

    /// <summary>
    /// Gets or sets the account name or description.
    /// </summary>
    [StringLength(100)]
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets the initial account balance to fund.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; } = 0;

    /// <summary>
    /// Gets or sets the account metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result model for abstract account creation.
/// </summary>
public class AbstractAccountResult
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account address on the blockchain.
    /// </summary>
    public string AccountAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the master public key.
    /// </summary>
    public string MasterPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash for account creation.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the initial balance set for the account.
    /// </summary>
    public decimal InitialBalance { get; set; }

    /// <summary>
    /// Gets or sets the gas used for account creation.
    /// </summary>
    public long GasUsed { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for executing a transaction.
/// </summary>
public class ExecuteTransactionRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target contract address.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the transaction data/payload (hex encoded).
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    [Range(21000, 8000000)]
    public long GasLimit { get; set; } = 21000;

    /// <summary>
    /// Gets or sets the gas price (optional, defaults to network gas price).
    /// </summary>
    [Range(0, long.MaxValue)]
    public long? GasPrice { get; set; }

    /// <summary>
    /// Gets or sets whether to use a session key for signing.
    /// </summary>
    public bool UseSessionKey { get; set; }

    /// <summary>
    /// Gets or sets the session key identifier if using session key.
    /// </summary>
    [StringLength(64)]
    public string? SessionKeyId { get; set; }

    /// <summary>
    /// Gets or sets the transaction nonce (optional, auto-calculated if not provided).
    /// </summary>
    public long? Nonce { get; set; }

    /// <summary>
    /// Gets or sets the transaction deadline (optional).
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result model for transaction execution.
/// </summary>
public class TransactionResult
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the transaction was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the transaction failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the gas used.
    /// </summary>
    public long GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the effective gas price used.
    /// </summary>
    public long EffectiveGasPrice { get; set; }

    /// <summary>
    /// Gets or sets the transaction fee.
    /// </summary>
    public decimal TransactionFee { get; set; }

    /// <summary>
    /// Gets or sets the block number where the transaction was included.
    /// </summary>
    public long? BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the transaction index in the block.
    /// </summary>
    public int? TransactionIndex { get; set; }

    /// <summary>
    /// Gets or sets the transaction receipt (raw receipt data).
    /// </summary>
    public string? Receipt { get; set; }

    /// <summary>
    /// Gets or sets the transaction logs/events.
    /// </summary>
    public List<TransactionLog> Logs { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction status on the blockchain.
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the confirmation timestamp.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of confirmations.
    /// </summary>
    public int Confirmations { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a transaction log/event.
/// </summary>
public class TransactionLog
{
    /// <summary>
    /// Gets or sets the contract address that emitted the log.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log topics.
    /// </summary>
    public List<string> Topics { get; set; } = new();

    /// <summary>
    /// Gets or sets the log data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log index.
    /// </summary>
    public int LogIndex { get; set; }

    /// <summary>
    /// Gets or sets whether the log was removed (due to chain reorganization).
    /// </summary>
    public bool Removed { get; set; }
}

/// <summary>
/// Transaction status enumeration.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending confirmation.
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction was successful.
    /// </summary>
    Success,

    /// <summary>
    /// Transaction failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction was reverted.
    /// </summary>
    Reverted,

    /// <summary>
    /// Transaction timed out.
    /// </summary>
    Timeout
}

/// <summary>
/// Request model for batch transaction execution.
/// </summary>
public class BatchTransactionRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of transactions to execute.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(10)]
    public List<ExecuteTransactionRequest> Transactions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to stop execution on first failure.
    /// </summary>
    public bool StopOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum gas limit for the entire batch.
    /// </summary>
    [Range(21000, 80000000)]
    public long MaxGasLimit { get; set; } = 8000000;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result model for batch transaction execution.
/// </summary>
public class BatchTransactionResult
{
    /// <summary>
    /// Gets or sets the batch identifier.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of individual transaction results.
    /// </summary>
    public List<TransactionResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets whether all transactions were successful.
    /// </summary>
    public bool AllSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the number of successful transactions.
    /// </summary>
    public int SuccessfulCount { get; set; }

    /// <summary>
    /// Gets or sets the total gas used for all transactions.
    /// </summary>
    public long TotalGasUsed { get; set; }

    /// <summary>
    /// Gets or sets the batch execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for adding a guardian.
/// </summary>
public class AddGuardianRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian's address.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string GuardianAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian's name.
    /// </summary>
    [StringLength(100)]
    public string GuardianName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result model for guardian operations.
/// </summary>
public class GuardianResult
{
    /// <summary>
    /// Gets or sets the guardian identifier.
    /// </summary>
    public string GuardianId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for initiating account recovery.
/// </summary>
public class InitiateRecoveryRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new owner's public key.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string NewOwnerPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for recovery.
    /// </summary>
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for completing account recovery.
/// </summary>
public class CompleteRecoveryRequest
{
    /// <summary>
    /// Gets or sets the recovery identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string RecoveryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian signatures.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<GuardianSignature> GuardianSignatures { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a guardian signature for recovery.
/// </summary>
public class GuardianSignature
{
    /// <summary>
    /// Gets or sets the guardian's address.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string GuardianAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature timestamp.
    /// </summary>
    public DateTime SignedAt { get; set; }
}

/// <summary>
/// Recovery status enumeration.
/// </summary>
public enum RecoveryStatus
{
    /// <summary>
    /// Recovery has been initiated.
    /// </summary>
    Initiated,

    /// <summary>
    /// Waiting for guardian signatures.
    /// </summary>
    PendingSignatures,

    /// <summary>
    /// Recovery has been completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Recovery has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Recovery has failed.
    /// </summary>
    Failed
}

/// <summary>
/// Result model for recovery operations.
/// </summary>
public class RecoveryResult
{
    /// <summary>
    /// Gets or sets the recovery identifier.
    /// </summary>
    public string RecoveryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the recovery status.
    /// </summary>
    public RecoveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for creating session keys.
/// </summary>
public class CreateSessionKeyRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key permissions.
    /// </summary>
    [Required]
    public SessionKeyPermissions Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the session key expiration time.
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the session key name.
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Session key permissions.
/// </summary>
public class SessionKeyPermissions
{
    /// <summary>
    /// Gets or sets the maximum transaction value.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal MaxTransactionValue { get; set; }

    /// <summary>
    /// Gets or sets the allowed contracts.
    /// </summary>
    public List<string> AllowedContracts { get; set; } = new();

    /// <summary>
    /// Gets or sets the allowed functions.
    /// </summary>
    public List<string> AllowedFunctions { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum transactions per day.
    /// </summary>
    [Range(1, 1000)]
    public int MaxTransactionsPerDay { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether gasless transactions are allowed.
    /// </summary>
    public bool AllowGaslessTransactions { get; set; } = true;
}

/// <summary>
/// Request model for revoking session keys.
/// </summary>
public class RevokeSessionKeyRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string SessionKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for revocation.
    /// </summary>
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Session key status enumeration.
/// </summary>
public enum SessionKeyStatus
{
    /// <summary>
    /// Session key is active and can be used.
    /// </summary>
    Active,

    /// <summary>
    /// Session key has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Session key has been revoked.
    /// </summary>
    Revoked,

    /// <summary>
    /// Session key is suspended.
    /// </summary>
    Suspended
}

/// <summary>
/// Result model for session key operations.
/// </summary>
public class SessionKeyResult
{
    /// <summary>
    /// Gets or sets the session key identifier.
    /// </summary>
    public string SessionKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the session key status.
    /// </summary>
    public SessionKeyStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the session key expiration time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Abstract account information.
/// </summary>
public class AbstractAccountInfo
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account address.
    /// </summary>
    public string AccountAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the master public key.
    /// </summary>
    public string MasterPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account status.
    /// </summary>
    public AccountStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the account balance.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets or sets the guardians.
    /// </summary>
    public List<GuardianInfo> Guardians { get; set; } = new();

    /// <summary>
    /// Gets or sets the recovery threshold.
    /// </summary>
    public int RecoveryThreshold { get; set; }

    /// <summary>
    /// Gets or sets the session keys.
    /// </summary>
    public List<SessionKeyInfo> SessionKeys { get; set; } = new();

    /// <summary>
    /// Gets or sets whether gasless transactions are enabled.
    /// </summary>
    public bool GaslessTransactionsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Account status enumeration.
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// Account is active and can be used.
    /// </summary>
    Active,

    /// <summary>
    /// Account is suspended.
    /// </summary>
    Suspended,

    /// <summary>
    /// Account is in recovery mode.
    /// </summary>
    InRecovery,

    /// <summary>
    /// Account is frozen.
    /// </summary>
    Frozen,

    /// <summary>
    /// Account has been closed.
    /// </summary>
    Closed
}

/// <summary>
/// Guardian information.
/// </summary>
public class GuardianInfo
{
    /// <summary>
    /// Gets or sets the guardian identifier.
    /// </summary>
    public string GuardianId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian address.
    /// </summary>
    public string GuardianAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian name.
    /// </summary>
    public string GuardianName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian status.
    /// </summary>
    public GuardianStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the guardian was added.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Guardian status enumeration.
/// </summary>
public enum GuardianStatus
{
    /// <summary>
    /// Guardian is active.
    /// </summary>
    Active,

    /// <summary>
    /// Guardian is pending activation.
    /// </summary>
    Pending,

    /// <summary>
    /// Guardian has been removed.
    /// </summary>
    Removed,

    /// <summary>
    /// Guardian is suspended.
    /// </summary>
    Suspended
}

/// <summary>
/// Session key information.
/// </summary>
public class SessionKeyInfo
{
    /// <summary>
    /// Gets or sets the session key identifier.
    /// </summary>
    public string SessionKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permissions.
    /// </summary>
    public SessionKeyPermissions Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public SessionKeyStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the last used timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the usage count.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for getting transaction history.
/// </summary>
public class TransactionHistoryRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of transactions to return.
    /// </summary>
    [Range(1, 1000)]
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of transactions to skip.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Gets or sets the transaction type filter.
    /// </summary>
    public TransactionType? TransactionType { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Transaction type enumeration.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Regular transaction.
    /// </summary>
    Regular,

    /// <summary>
    /// Batch transaction.
    /// </summary>
    Batch,

    /// <summary>
    /// Guardian operation.
    /// </summary>
    Guardian,

    /// <summary>
    /// Recovery operation.
    /// </summary>
    Recovery,

    /// <summary>
    /// Session key operation.
    /// </summary>
    SessionKey
}

/// <summary>
/// Result model for transaction history.
/// </summary>
public class TransactionHistoryResult
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of transactions.
    /// </summary>
    public List<TransactionHistoryItem> Transactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of transactions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether there are more transactions.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Transaction history item.
/// </summary>
public class TransactionHistoryItem
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the gas used.
    /// </summary>
    public long GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
