using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.AbstractAccount.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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


