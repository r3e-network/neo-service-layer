namespace NeoServiceLayer.Services.AbstractAccount.Models;

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
    /// Gets or sets the account address on the blockchain.
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
    /// Gets or sets the list of guardians.
    /// </summary>
    public GuardianInfo[] Guardians { get; set; } = Array.Empty<GuardianInfo>();

    /// <summary>
    /// Gets or sets the recovery threshold.
    /// </summary>
    public int RecoveryThreshold { get; set; }

    /// <summary>
    /// Gets or sets the active session keys.
    /// </summary>
    public SessionKeyInfo[] SessionKeys { get; set; } = Array.Empty<SessionKeyInfo>();

    /// <summary>
    /// Gets or sets whether gasless transactions are enabled.
    /// </summary>
    public bool GaslessTransactionsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the account creation timestamp.
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
    /// Gets or sets the session key public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key permissions.
    /// </summary>
    public SessionKeyPermissions Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the session key status.
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
/// Request model for transaction history.
/// </summary>
public class TransactionHistoryRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date for the history query.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the history query.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of transactions to return.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
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
    public TransactionHistoryItem[] Transactions { get; set; } = Array.Empty<TransactionHistoryItem>();

    /// <summary>
    /// Gets or sets the total number of transactions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether there are more transactions available.
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
    /// Gets or sets the target address.
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
/// Transaction status enumeration.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending.
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
    Reverted
}
