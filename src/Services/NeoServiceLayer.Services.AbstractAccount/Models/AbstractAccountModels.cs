namespace NeoServiceLayer.Services.AbstractAccount.Models;

/// <summary>
/// Request model for batch transaction execution.
/// </summary>
public class BatchTransactionRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of transactions to execute.
    /// </summary>
    public ExecuteTransactionRequest[] Transactions { get; set; } = Array.Empty<ExecuteTransactionRequest>();

    /// <summary>
    /// Gets or sets whether to stop on first failure.
    /// </summary>
    public bool StopOnFailure { get; set; } = true;

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
    /// Gets or sets the individual transaction results.
    /// </summary>
    public TransactionResult[] Results { get; set; } = Array.Empty<TransactionResult>();

    /// <summary>
    /// Gets or sets whether all transactions were successful.
    /// </summary>
    public bool AllSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the total gas used.
    /// </summary>
    public long TotalGasUsed { get; set; }

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
/// Request model for adding a guardian.
/// </summary>
public class AddGuardianRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian's public key or address.
    /// </summary>
    public string GuardianAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian's name or identifier.
    /// </summary>
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
    /// Gets or sets the transaction hash for the guardian operation.
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
    /// Gets or sets the account identifier to recover.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new owner's public key.
    /// </summary>
    public string NewOwnerPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recovery reason.
    /// </summary>
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
    public string RecoveryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guardian signatures.
    /// </summary>
    public GuardianSignature[] GuardianSignatures { get; set; } = Array.Empty<GuardianSignature>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Guardian signature for recovery operations.
/// </summary>
public class GuardianSignature
{
    /// <summary>
    /// Gets or sets the guardian address.
    /// </summary>
    public string GuardianAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature timestamp.
    /// </summary>
    public DateTime SignedAt { get; set; }
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
    /// Gets or sets whether the recovery was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the recovery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash for the recovery.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the recovery status.
    /// </summary>
    public RecoveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the recovery timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
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
/// Request model for creating a session key.
/// </summary>
public class CreateSessionKeyRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key permissions.
    /// </summary>
    public SessionKeyPermissions Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the session key expiration time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the session key name or description.
    /// </summary>
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
    /// Gets or sets the maximum transaction value allowed.
    /// </summary>
    public decimal MaxTransactionValue { get; set; }

    /// <summary>
    /// Gets or sets the allowed contract addresses.
    /// </summary>
    public string[] AllowedContracts { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the allowed function selectors.
    /// </summary>
    public string[] AllowedFunctions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum number of transactions per day.
    /// </summary>
    public int MaxTransactionsPerDay { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether gasless transactions are allowed.
    /// </summary>
    public bool AllowGaslessTransactions { get; set; } = true;
}

/// <summary>
/// Request model for revoking a session key.
/// </summary>
public class RevokeSessionKeyRequest
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key identifier to revoke.
    /// </summary>
    public string SessionKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revocation reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
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
    /// Gets or sets the expiration time.
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
