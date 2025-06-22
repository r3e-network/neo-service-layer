using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.AbstractAccount.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.AbstractAccount;

/// <summary>
/// Implementation of the Abstract Account Service that provides account abstraction functionality.
/// </summary>
public partial class AbstractAccountService : EnclaveBlockchainServiceBase, IAbstractAccountService
{
    private readonly Dictionary<string, AbstractAccountInfo> _accounts = new();
    private readonly Dictionary<string, List<TransactionHistoryItem>> _transactionHistory = new();
    private readonly object _accountsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractAccountService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    public AbstractAccountService(
        ILogger<AbstractAccountService> logger,
        IEnclaveManager enclaveManager)
        : base("AbstractAccountService", "Account abstraction and smart wallet functionality", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        AddCapability<IAbstractAccountService>();
        AddDependency(new ServiceDependency("KeyManagementService", true, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", false, "1.0.0"));
    }

    /// <inheritdoc/>
    public async Task<AbstractAccountResult> CreateAccountAsync(CreateAccountRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var accountId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Creating abstract account {AccountId} for {Blockchain}", accountId, blockchainType);

                // Create account in the enclave
                var accountResult = await CreateAccountInEnclaveAsync(accountId, request);

                var accountInfo = new AbstractAccountInfo
                {
                    AccountId = accountId,
                    AccountAddress = accountResult.AccountAddress,
                    MasterPublicKey = accountResult.MasterPublicKey,
                    Status = AccountStatus.Active,
                    Balance = 0,
                    Guardians = request.InitialGuardians.Select((g, i) => new GuardianInfo
                    {
                        GuardianId = Guid.NewGuid().ToString(),
                        GuardianAddress = g,
                        GuardianName = $"Guardian {i + 1}",
                        Status = GuardianStatus.Active,
                        AddedAt = DateTime.UtcNow
                    }).ToList(),
                    RecoveryThreshold = request.RecoveryThreshold,
                    SessionKeys = new List<SessionKeyInfo>(),
                    GaslessTransactionsEnabled = request.EnableGaslessTransactions,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };

                lock (_accountsLock)
                {
                    _accounts[accountId] = accountInfo;
                    _transactionHistory[accountId] = new List<TransactionHistoryItem>();
                }

                Logger.LogInformation("Created abstract account {AccountId} at address {Address} on {Blockchain}",
                    accountId, accountResult.AccountAddress, blockchainType);

                return new AbstractAccountResult
                {
                    AccountId = accountId,
                    AccountAddress = accountResult.AccountAddress,
                    MasterPublicKey = accountResult.MasterPublicKey,
                    Success = true,
                    TransactionHash = accountResult.TransactionHash,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create abstract account {AccountId}", accountId);

                return new AbstractAccountResult
                {
                    AccountId = accountId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<TransactionResult> ExecuteTransactionAsync(ExecuteTransactionRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var transactionId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Executing transaction {TransactionId} for account {AccountId}",
                    transactionId, request.AccountId);

                // Validate account exists
                var account = GetAccount(request.AccountId);

                // Execute transaction in the enclave
                var txResult = await ExecuteTransactionInEnclaveAsync(request);

                // Record transaction history
                var historyItem = new TransactionHistoryItem
                {
                    TransactionHash = txResult.TransactionHash,
                    Type = TransactionType.Regular,
                    ToAddress = request.ToAddress,
                    Value = request.Value,
                    GasUsed = txResult.GasUsed,
                    Status = txResult.Success ? TransactionStatus.Success : TransactionStatus.Failed,
                    ExecutedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };

                lock (_accountsLock)
                {
                    _transactionHistory[request.AccountId].Add(historyItem);
                    account.LastActivityAt = DateTime.UtcNow;
                }

                Logger.LogInformation("Executed transaction {TransactionHash} for account {AccountId} on {Blockchain}",
                    txResult.TransactionHash, request.AccountId, blockchainType);

                return txResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute transaction {TransactionId} for account {AccountId}",
                    transactionId, request.AccountId);

                return new TransactionResult
                {
                    TransactionHash = transactionId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<BatchTransactionResult> ExecuteBatchTransactionAsync(BatchTransactionRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var batchId = Guid.NewGuid().ToString();
            var results = new List<TransactionResult>();
            long totalGasUsed = 0;
            bool allSuccessful = true;

            try
            {
                Logger.LogDebug("Executing batch transaction {BatchId} with {Count} transactions for account {AccountId}",
                    batchId, request.Transactions.Count, request.AccountId);

                foreach (var transaction in request.Transactions)
                {
                    var result = await ExecuteTransactionAsync(transaction, blockchainType);
                    results.Add(result);
                    totalGasUsed += result.GasUsed;

                    if (!result.Success)
                    {
                        allSuccessful = false;
                        if (request.StopOnFailure)
                        {
                            break;
                        }
                    }
                }

                Logger.LogInformation("Executed batch transaction {BatchId}: {Successful}/{Total} successful on {Blockchain}",
                    batchId, results.Count(r => r.Success), results.Count, blockchainType);

                return new BatchTransactionResult
                {
                    BatchId = batchId,
                    Results = results,
                    AllSuccessful = allSuccessful,
                    TotalGasUsed = totalGasUsed,
                    ExecutedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute batch transaction {BatchId}", batchId);

                return new BatchTransactionResult
                {
                    BatchId = batchId,
                    Results = results,
                    AllSuccessful = false,
                    TotalGasUsed = totalGasUsed,
                    ExecutedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <summary>
    /// Gets an account by ID.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <returns>The account information.</returns>
    private AbstractAccountInfo GetAccount(string accountId)
    {
        lock (_accountsLock)
        {
            if (_accounts.TryGetValue(accountId, out var account))
            {
                return account;
            }
        }

        throw new ArgumentException($"Account {accountId} not found", nameof(accountId));
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Abstract Account Service");

        // Initialize service-specific components
        await Task.CompletedTask; // Placeholder for actual initialization

        Logger.LogInformation("Abstract Account Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Abstract Account Service enclave operations");

        try
        {
            // Initialize account abstraction algorithms in the enclave
            if (_enclaveManager != null)
            {
                var initResult = await _enclaveManager.ExecuteJavaScriptAsync("initializeAccountAbstraction()");
                Logger.LogDebug("Enclave account abstraction initialized: {Result}", initResult);
            }

            Logger.LogInformation("Abstract Account Service enclave operations initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Abstract Account Service enclave operations");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<GuardianResult> AddGuardianAsync(AddGuardianRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Adding guardian {GuardianAddress} to account {AccountId}",
                    request.GuardianAddress, request.AccountId);

                var account = GetAccount(request.AccountId);
                var result = await AddGuardianInEnclaveAsync(request);

                if (result.Success)
                {
                    var guardianInfo = new GuardianInfo
                    {
                        GuardianId = result.GuardianId,
                        GuardianAddress = request.GuardianAddress,
                        GuardianName = request.GuardianName,
                        Status = GuardianStatus.Active,
                        AddedAt = DateTime.UtcNow,
                        Metadata = request.Metadata
                    };

                    lock (_accountsLock)
                    {
                        var guardiansList = account.Guardians.ToList();
                        guardiansList.Add(guardianInfo);
                        account.Guardians = guardiansList;
                        account.LastActivityAt = DateTime.UtcNow;
                    }

                    Logger.LogInformation("Added guardian {GuardianId} to account {AccountId}",
                        result.GuardianId, request.AccountId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to add guardian to account {AccountId}", request.AccountId);

                return new GuardianResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<RecoveryResult> InitiateRecoveryAsync(InitiateRecoveryRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Initiating recovery for account {AccountId}", request.AccountId);

                var account = GetAccount(request.AccountId);
                var result = await InitiateRecoveryInEnclaveAsync(request);

                if (result.Success)
                {
                    lock (_accountsLock)
                    {
                        account.Status = AccountStatus.InRecovery;
                        account.LastActivityAt = DateTime.UtcNow;
                    }

                    Logger.LogInformation("Initiated recovery {RecoveryId} for account {AccountId}",
                        result.RecoveryId, request.AccountId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initiate recovery for account {AccountId}", request.AccountId);

                return new RecoveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = RecoveryStatus.Failed,
                    Timestamp = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<RecoveryResult> CompleteRecoveryAsync(CompleteRecoveryRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Completing recovery {RecoveryId}", request.RecoveryId);

                var result = await CompleteRecoveryInEnclaveAsync(request);

                Logger.LogInformation("Completed recovery {RecoveryId}: {Success}",
                    request.RecoveryId, result.Success);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to complete recovery {RecoveryId}", request.RecoveryId);

                return new RecoveryResult
                {
                    RecoveryId = request.RecoveryId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = RecoveryStatus.Failed,
                    Timestamp = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<SessionKeyResult> CreateSessionKeyAsync(CreateSessionKeyRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Creating session key for account {AccountId}", request.AccountId);

                var account = GetAccount(request.AccountId);
                var result = await CreateSessionKeyInEnclaveAsync(request);

                if (result.Success)
                {
                    var sessionKeyInfo = new SessionKeyInfo
                    {
                        SessionKeyId = result.SessionKeyId,
                        PublicKey = result.PublicKey,
                        Name = request.Name,
                        Permissions = request.Permissions,
                        Status = SessionKeyStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = request.ExpiresAt,
                        UsageCount = 0,
                        Metadata = request.Metadata
                    };

                    lock (_accountsLock)
                    {
                        var sessionKeysList = account.SessionKeys.ToList();
                        sessionKeysList.Add(sessionKeyInfo);
                        account.SessionKeys = sessionKeysList;
                        account.LastActivityAt = DateTime.UtcNow;
                    }

                    Logger.LogInformation("Created session key {SessionKeyId} for account {AccountId}",
                        result.SessionKeyId, request.AccountId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create session key for account {AccountId}", request.AccountId);

                return new SessionKeyResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = SessionKeyStatus.Suspended,
                    Timestamp = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<SessionKeyResult> RevokeSessionKeyAsync(RevokeSessionKeyRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Revoking session key {SessionKeyId} for account {AccountId}",
                    request.SessionKeyId, request.AccountId);

                await Task.CompletedTask; // Placeholder for async operation
                var account = GetAccount(request.AccountId);

                lock (_accountsLock)
                {
                    var sessionKey = account.SessionKeys.FirstOrDefault(sk => sk.SessionKeyId == request.SessionKeyId);
                    if (sessionKey != null)
                    {
                        sessionKey.Status = SessionKeyStatus.Revoked;
                        account.LastActivityAt = DateTime.UtcNow;
                    }
                }

                Logger.LogInformation("Revoked session key {SessionKeyId} for account {AccountId}",
                    request.SessionKeyId, request.AccountId);

                return new SessionKeyResult
                {
                    SessionKeyId = request.SessionKeyId,
                    Success = true,
                    Status = SessionKeyStatus.Revoked,
                    Timestamp = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to revoke session key {SessionKeyId} for account {AccountId}",
                    request.SessionKeyId, request.AccountId);

                return new SessionKeyResult
                {
                    SessionKeyId = request.SessionKeyId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = SessionKeyStatus.Active,
                    Timestamp = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<AbstractAccountInfo> GetAccountInfoAsync(string accountId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            await Task.CompletedTask; // Placeholder for async operation
            return GetAccount(accountId);
        });
    }

    /// <inheritdoc/>
    public async Task<TransactionHistoryResult> GetTransactionHistoryAsync(TransactionHistoryRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            await Task.CompletedTask; // Placeholder for async operation

            lock (_accountsLock)
            {
                if (!_transactionHistory.TryGetValue(request.AccountId, out var history))
                {
                    throw new ArgumentException($"Account {request.AccountId} not found", nameof(request));
                }

                var filteredHistory = history.AsEnumerable();

                if (request.StartDate.HasValue)
                {
                    filteredHistory = filteredHistory.Where(h => h.ExecutedAt >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    filteredHistory = filteredHistory.Where(h => h.ExecutedAt <= request.EndDate.Value);
                }

                if (request.TransactionType.HasValue)
                {
                    filteredHistory = filteredHistory.Where(h => h.Type == request.TransactionType.Value);
                }

                var totalCount = filteredHistory.Count();
                var transactions = filteredHistory
                    .OrderByDescending(h => h.ExecutedAt)
                    .Skip(request.Offset)
                    .Take(request.Limit)
                    .ToList();

                return new TransactionHistoryResult
                {
                    AccountId = request.AccountId,
                    Transactions = transactions,
                    TotalCount = totalCount,
                    HasMore = request.Offset + request.Limit < totalCount,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Abstract Account Service");

        try
        {
            // Start any background services or timers here
            await Task.CompletedTask;
            Logger.LogInformation("Abstract Account Service started successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start Abstract Account Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Abstract Account Service");

        try
        {
            // Stop any background services or timers here
            await Task.CompletedTask;
            Logger.LogInformation("Abstract Account Service stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to stop Abstract Account Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var accountsCount = _accounts.Count;
        var activeAccounts = _accounts.Values.Count(a => a.Status == AccountStatus.Active);

        Logger.LogDebug("Abstract Account service health check: {AccountsCount} accounts, {ActiveAccounts} active",
            accountsCount, activeAccounts);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <summary>
    /// Checks if the service supports the specified blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if supported, false otherwise.</returns>
    private new static bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType == BlockchainType.NeoN3 || blockchainType == BlockchainType.NeoX;
    }
}
