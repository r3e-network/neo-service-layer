using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.AbstractAccount.Models;

namespace NeoServiceLayer.Services.AbstractAccount;

/// <summary>
/// Interface for the Abstract Account Service that provides account abstraction functionality.
/// </summary>
public interface IAbstractAccountService : IService
{
    /// <summary>
    /// Creates a new abstract account with the specified configuration.
    /// </summary>
    /// <param name="request">The account creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The created account information.</returns>
    Task<AbstractAccountResult> CreateAccountAsync(CreateAccountRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Executes a transaction using the abstract account.
    /// </summary>
    /// <param name="request">The transaction execution request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The transaction execution result.</returns>
    Task<TransactionResult> ExecuteTransactionAsync(ExecuteTransactionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Executes multiple transactions in a batch.
    /// </summary>
    /// <param name="request">The batch transaction request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The batch execution result.</returns>
    Task<BatchTransactionResult> ExecuteBatchTransactionAsync(BatchTransactionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Adds a guardian for social recovery.
    /// </summary>
    /// <param name="request">The add guardian request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The operation result.</returns>
    Task<GuardianResult> AddGuardianAsync(AddGuardianRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Initiates account recovery using guardians.
    /// </summary>
    /// <param name="request">The recovery request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The recovery result.</returns>
    Task<RecoveryResult> InitiateRecoveryAsync(InitiateRecoveryRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Completes account recovery with guardian signatures.
    /// </summary>
    /// <param name="request">The complete recovery request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The recovery completion result.</returns>
    Task<RecoveryResult> CompleteRecoveryAsync(CompleteRecoveryRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a session key for limited-time operations.
    /// </summary>
    /// <param name="request">The session key creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The session key result.</returns>
    Task<SessionKeyResult> CreateSessionKeyAsync(CreateSessionKeyRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Revokes a session key.
    /// </summary>
    /// <param name="request">The revoke session key request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The revocation result.</returns>
    Task<SessionKeyResult> RevokeSessionKeyAsync(RevokeSessionKeyRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets account information and status.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The account information.</returns>
    Task<AbstractAccountInfo> GetAccountInfoAsync(string accountId, BlockchainType blockchainType);

    /// <summary>
    /// Gets transaction history for an account.
    /// </summary>
    /// <param name="request">The transaction history request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The transaction history.</returns>
    Task<TransactionHistoryResult> GetTransactionHistoryAsync(TransactionHistoryRequest request, BlockchainType blockchainType);
}

/// <summary>
/// Request model for creating an abstract account.
/// </summary>
public class CreateAccountRequest
{
    /// <summary>
    /// Gets or sets the account owner's public key.
    /// </summary>
    public string OwnerPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial guardians for social recovery.
    /// </summary>
    public string[] InitialGuardians { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the recovery threshold (number of guardians needed for recovery).
    /// </summary>
    public int RecoveryThreshold { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether gasless transactions are enabled.
    /// </summary>
    public bool EnableGaslessTransactions { get; set; } = true;

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
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target contract address.
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the transaction data/payload.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public long GasLimit { get; set; }

    /// <summary>
    /// Gets or sets whether to use a session key for signing.
    /// </summary>
    public bool UseSessionKey { get; set; }

    /// <summary>
    /// Gets or sets the session key identifier if using session key.
    /// </summary>
    public string? SessionKeyId { get; set; }

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
    /// Gets or sets the transaction receipt.
    /// </summary>
    public string? Receipt { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
