using NeoServiceLayer.Core.Models;
using System.Text;
using Microsoft.Extensions.Logging;
using Neo;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Numerics;


namespace NeoServiceLayer.Services.SmartContracts.NeoN3;

/// <summary>
/// Neo N3 implementation of the smart contract manager.
/// </summary>
public class NeoN3SmartContractManager : ServiceFramework.EnclaveBlockchainServiceBase, ISmartContractManager
{
    private readonly RpcClient _rpcClient;
    private readonly IServiceConfiguration _configuration;
    private new readonly IEnclaveManager _enclaveManager;
    private readonly Dictionary<string, ContractMetadata> _contractCache = new();
    private Wallet? _wallet;
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoN3SmartContractManager"/> class.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="logger">The logger.</param>
    public NeoN3SmartContractManager(
        IServiceConfiguration configuration,
        IEnclaveManager enclaveManager,
        ILogger<NeoN3SmartContractManager> logger)
        : base("SmartContractsNeoN3", "Neo N3 Smart Contract Management Service", "1.0.0", logger, new[] { BlockchainType.NeoN3 })
    {
        _configuration = configuration;
        _enclaveManager = enclaveManager;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Initialize RPC client
        var rpcUrl = _configuration.GetValue("NeoN3:RpcUrl", "http://localhost:40332");
        _rpcClient = new RpcClient(new Uri(rpcUrl));

        // Add capabilities
        AddCapability<ISmartContractManager>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("NetworkMagic", _configuration.GetValue("NeoN3:NetworkMagic", "860833102"));
        SetMetadata("RpcUrl", rpcUrl);
        SetMetadata("SupportedFeatures", "Deploy,Invoke,Call,Update,Destroy,Events");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => BlockchainType.NeoN3;

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Neo N3 Smart Contract Manager...");

            // Initialize wallet from enclave
            await InitializeWalletAsync();

            // Load contract cache
            await RefreshContractCacheAsync();

            Logger.LogInformation("Neo N3 Smart Contract Manager initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Neo N3 Smart Contract Manager");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing enclave for Neo N3 Smart Contract Manager...");
            return await _enclaveManager.InitializeEnclaveAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing enclave for Neo N3 Smart Contract Manager");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Neo N3 Smart Contract Manager...");

            // Test RPC connection
            var version = await _rpcClient.GetVersionAsync();
            Logger.LogInformation("Connected to Neo N3 node version: {Version}", version.UserAgent);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Neo N3 Smart Contract Manager");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Neo N3 Smart Contract Manager...");
            _contractCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Neo N3 Smart Contract Manager");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<ContractDeploymentResult> DeployContractAsync(
        byte[] contractCode,
        object[]? constructorParameters = null,
        ContractDeploymentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnclaveInitialized || !IsRunning)
        {
            throw new InvalidOperationException("Service is not properly initialized or running.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogInformation("Deploying contract to Neo N3 network");

            try
            {
                // Parse NEF and manifest from contract code
                var (nef, manifest) = ParseContractCode(contractCode);

                // Create deployment transaction
                var script = CreateDeploymentScript(nef, manifest, constructorParameters);
                var transaction = await CreateTransactionAsync(script, options?.GasLimit);

                // Sign and send transaction within enclave
                var signedTx = await SignTransactionInEnclaveAsync(transaction);
                var txHash = await _rpcClient.SendRawTransactionAsync(signedTx).ConfigureAwait(false);

                // Wait for confirmation if required
                var result = new ContractDeploymentResult
                {
                    TransactionHash = txHash.ToString(),
                    ContractHash = global::Neo.SmartContract.Helper.GetContractHash(
                        UInt160.Parse(_wallet!.GetAccounts().First().Address),
                        nef.CheckSum,
                        manifest.Name).ToString(),
                    IsSuccess = true
                };

                if (options?.GasLimit.HasValue == true || true) // Wait by default
                {
                    var applicationLog = await WaitForTransactionAsync(txHash);
                    if (applicationLog.BlockHash != null)
                    {
                        var block = await _rpcClient.GetBlockAsync(applicationLog.BlockHash.ToString()).ConfigureAwait(false);
                        // Get block index from block header - RpcBlock contains Index property
                        result.BlockNumber = block != null && block.Index != null ? (long)block.Index : 0;
                    }
                    else
                    {
                        result.BlockNumber = 0;
                    }
                    result.GasConsumed = applicationLog.Executions?[0]?.GasConsumed ?? 0;
                    result.IsSuccess = applicationLog.Executions?[0]?.VMState == VMState.HALT;

                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = applicationLog.Executions?[0]?.ExceptionMessage;
                    }
                }

                result.ContractManifest = manifest.ToJson().ToString();

                // Cache the deployed contract
                var metadata = new ContractMetadata
                {
                    ContractHash = result.ContractHash,
                    Name = options?.Name ?? manifest.Name,
                    Version = options?.Version ?? "1.0.0",
                    Author = options?.Author ?? "Unknown",
                    Description = options?.Description,
                    Manifest = result.ContractManifest,
                    DeployedBlockNumber = result.BlockNumber,
                    DeploymentTxHash = result.TransactionHash,
                    DeployedAt = DateTime.UtcNow,
                    Methods = ParseContractMethods(manifest)
                };

                lock (_contractCache)
                {
                    _contractCache[result.ContractHash] = metadata;
                }

                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                UpdateMetric("TotalContractsDeployed", _contractCache.Count);

                Logger.LogInformation("Successfully deployed contract {ContractHash} with transaction {TxHash}",
                    result.ContractHash, result.TransactionHash);

                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error deploying contract to Neo N3");

                return new ContractDeploymentResult
                {
                    ContractHash = string.Empty,
                    TransactionHash = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<ContractInvocationResult> InvokeContractAsync(
        string contractHash,
        string method,
        object[]? parameters = null,
        ContractInvocationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnclaveInitialized || !IsRunning)
        {
            throw new InvalidOperationException("Service is not properly initialized or running.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogDebug("Invoking contract {ContractHash} method {Method}", contractHash, method);

            try
            {
                // Create invocation script
                var script = CreateInvocationScript(contractHash, method, parameters);
                var transaction = await CreateTransactionAsync(script, options?.GasLimit);

                // Add value if payable
                if (options?.Value.HasValue == true && options.Value > 0)
                {
                    // Add transfer script for payable methods
                    var transferScript = CreateTransferScript(contractHash, options.Value.Value);
                    script = CombineScripts(transferScript, script);
                    transaction = await CreateTransactionAsync(script, options?.GasLimit);
                }

                // Sign and send transaction within enclave
                var signedTx = await SignTransactionInEnclaveAsync(transaction);
                var txHash = await _rpcClient.SendRawTransactionAsync(signedTx).ConfigureAwait(false);

                var result = new ContractInvocationResult
                {
                    TransactionHash = txHash.ToString(),
                    IsSuccess = true
                };

                // Wait for confirmation if required
                if (options?.WaitForConfirmation != false)
                {
                    var applicationLog = await WaitForTransactionAsync(txHash);
                    if (applicationLog.BlockHash != null)
                    {
                        var block = await _rpcClient.GetBlockAsync(applicationLog.BlockHash.ToString()).ConfigureAwait(false);
                        // Get block index from block header - RpcBlock contains Index property
                        result.BlockNumber = block != null && block.Index != null ? (long)block.Index : 0;
                    }
                    else
                    {
                        result.BlockNumber = 0;
                    }
                    result.GasConsumed = applicationLog.Executions?[0]?.GasConsumed ?? 0;
                    result.IsSuccess = applicationLog.Executions?[0]?.VMState == VMState.HALT;
                    result.ExecutionState = applicationLog.Executions?[0]?.VMState.ToString();

                    if (result.IsSuccess)
                    {
                        var stack = applicationLog.Executions?[0]?.Stack;
                        if (stack?.Count > 0)
                        {
                            result.ReturnValue = ParseStackItem(stack[0]);
                        }

                        // Parse events
                        result.Events = ParseContractEvents(applicationLog, contractHash);
                    }
                    else
                    {
                        result.ErrorMessage = applicationLog.Executions?[0]?.ExceptionMessage;
                    }
                }

                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                UpdateMetric("TotalInvocations", _successCount);

                Logger.LogDebug("Successfully invoked contract {ContractHash} method {Method}", contractHash, method);

                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error invoking contract {ContractHash} method {Method}", contractHash, method);

                return new ContractInvocationResult
                {
                    TransactionHash = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<object?> CallContractAsync(
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogDebug("Calling contract {ContractHash} method {Method} (read-only)", contractHash, method);

            // Create invocation script
            var script = CreateInvocationScript(contractHash, method, parameters);

            // Execute script without creating transaction
            var result = await _rpcClient.InvokeScriptAsync(script).ConfigureAwait(false);

            if (result.State == VMState.HALT && result.Stack != null && result.Stack.Count() > 0)
            {
                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);

                var returnValue = ParseStackItem(result.Stack[0]);
                Logger.LogDebug("Successfully called contract {ContractHash} method {Method}", contractHash, method);
                return returnValue;
            }
            else
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                var errorMessage = result.Exception ?? "Contract call failed";
                UpdateMetric("LastErrorMessage", errorMessage);
                Logger.LogWarning("Contract call failed: {Error}", errorMessage);
                return null;
            }
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error calling contract {ContractHash} method {Method}", contractHash, method);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ContractMetadata?> GetContractMetadataAsync(
        string contractHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check cache first
            lock (_contractCache)
            {
                if (_contractCache.TryGetValue(contractHash, out var cachedMetadata))
                {
                    _successCount++;
                    UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                    return cachedMetadata;
                }
            }

            Logger.LogDebug("Retrieving contract metadata for {ContractHash}", contractHash);

            // Get contract state from blockchain
            var contractState = await _rpcClient.GetContractStateAsync(contractHash).ConfigureAwait(false);
            if (contractState == null)
            {
                return null;
            }

            var metadata = new ContractMetadata
            {
                ContractHash = contractHash,
                Name = contractState.Manifest.Name,
                Version = "1.0.0", // Default version if not available
                Author = contractState.Manifest.Extra?["Author"]?.ToString() ?? "Unknown",
                Description = contractState.Manifest.Extra?["Description"]?.ToString() ?? "Contract retrieved from blockchain",
                Manifest = contractState.Manifest.ToJson().ToString(),
                DeployedBlockNumber = 0, // Would need to search transaction history
                DeploymentTxHash = "Unknown",
                DeployedAt = DateTime.MinValue,
                Methods = ParseContractMethods(contractState.Manifest),
                IsActive = true
            };

            // Cache the metadata
            lock (_contractCache)
            {
                _contractCache[contractHash] = metadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            Logger.LogDebug("Successfully retrieved contract metadata for {ContractHash}", contractHash);
            return metadata;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error retrieving contract metadata for {ContractHash}", contractHash);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ContractMetadata>> ListDeployedContractsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Listing deployed contracts");

            // Return cached contracts for now
            // In production, this would query the blockchain for contracts deployed by this account
            lock (_contractCache)
            {
                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                return _contractCache.Values.ToList();
            }
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error listing deployed contracts");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.SmartContracts.ContractEvent>> GetContractEventsAsync(
        string contractHash,
        string? eventName = null,
        long? fromBlock = null,
        long? toBlock = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Getting contract events for {ContractHash}", contractHash);

            var events = new List<Core.SmartContracts.ContractEvent>();

            // Get current block height if toBlock not specified
            if (!toBlock.HasValue)
            {
                var blockCount = await _rpcClient.GetBlockCountAsync().ConfigureAwait(false);
                toBlock = blockCount - 1;
            }

            fromBlock ??= Math.Max(0, toBlock.Value - 100); // Default to last 100 blocks

            // Query application logs for the block range
            for (long blockIndex = fromBlock.Value; blockIndex <= toBlock.Value; blockIndex++)
            {
                try
                {
                    var block = await _rpcClient.GetBlockAsync(blockIndex.ToString()).ConfigureAwait(false);

                    // Production transaction retrieval from block data
                    var transactions = await GetBlockTransactionsAsync(block);
                    foreach (var tx in transactions)
                    {
                        try
                        {
                            // RpcTransaction doesn't have Hash property, using transaction hash from another source
                            // This requires getting the transaction hash from the block or transaction data
                            var appLog = await _rpcClient.GetApplicationLogAsync(tx.BlockHash?.ToString() ?? string.Empty).ConfigureAwait(false);
                            var contractEvents = ParseContractEvents(appLog, contractHash, eventName);
                            events.AddRange(contractEvents);
                        }
                        catch (Exception) // RpcException when available
                        {
                            // Skip transactions without application logs
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error processing block {BlockIndex} for events", blockIndex);
                    continue;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            Logger.LogDebug("Found {EventCount} events for contract {ContractHash}", events.Count, contractHash);
            return events;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting contract events for {ContractHash}", contractHash);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> EstimateGasAsync(
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Estimating gas for contract {ContractHash} method {Method}", contractHash, method);

            // Create invocation script
            var script = CreateInvocationScript(contractHash, method, parameters);

            // Invoke script to get gas estimate
            var result = await _rpcClient.InvokeScriptAsync(script).ConfigureAwait(false);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            var gasConsumed = result.GasConsumed > 0 ? result.GasConsumed : 100000;
            Logger.LogDebug("Estimated gas: {GasConsumed} for contract {ContractHash} method {Method}",
                gasConsumed, contractHash, method);

            return gasConsumed;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error estimating gas for contract {ContractHash} method {Method}", contractHash, method);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ContractDeploymentResult> UpdateContractAsync(
        string contractHash,
        byte[] newContractCode,
        ContractDeploymentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnclaveInitialized || !IsRunning)
        {
            throw new InvalidOperationException("Service is not properly initialized or running.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            Logger.LogInformation("Updating contract {ContractHash} on Neo N3 network", contractHash);

            try
            {
                // Parse NEF and manifest from new contract code
                var (nef, manifest) = ParseContractCode(newContractCode);

                // Create update script (calling the update method on the contract)
                var script = CreateUpdateScript(contractHash, nef, manifest);
                var transaction = await CreateTransactionAsync(script, options?.GasLimit);

                // Sign and send transaction within enclave
                var signedTx = await SignTransactionInEnclaveAsync(transaction);
                var txHash = await _rpcClient.SendRawTransactionAsync(signedTx).ConfigureAwait(false);

                var result = new ContractDeploymentResult
                {
                    ContractHash = contractHash,
                    TransactionHash = txHash.ToString(),
                    IsSuccess = true,
                    ContractManifest = manifest.ToJson().ToString()
                };

                // Wait for confirmation
                var applicationLog = await WaitForTransactionAsync(txHash);
                if (applicationLog.BlockHash != null)
                {
                    var block = await _rpcClient.GetBlockAsync(applicationLog.BlockHash.ToString()).ConfigureAwait(false);
                    // Get block index from block header - RpcBlock contains Index property
                    result.BlockNumber = block?.Index ?? 0;
                }
                else
                {
                    result.BlockNumber = 0;
                }
                result.GasConsumed = applicationLog.Executions?[0]?.GasConsumed ?? 0;
                result.IsSuccess = applicationLog.Executions?[0]?.VMState == VMState.HALT;

                if (!result.IsSuccess)
                {
                    result.ErrorMessage = applicationLog.Executions?[0]?.ExceptionMessage;
                }

                // Update cache
                lock (_contractCache)
                {
                    if (_contractCache.TryGetValue(contractHash, out var existingMetadata))
                    {
                        existingMetadata.Manifest = result.ContractManifest;
                        existingMetadata.Methods = ParseContractMethods(manifest);
                        existingMetadata.Version = options?.Version ?? existingMetadata.Version;
                    }
                }

                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);

                Logger.LogInformation("Successfully updated contract {ContractHash}", contractHash);
                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error updating contract {ContractHash}", contractHash);

                return new ContractDeploymentResult
                {
                    ContractHash = contractHash,
                    TransactionHash = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> DestroyContractAsync(
        string contractHash,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnclaveInitialized || !IsRunning)
        {
            throw new InvalidOperationException("Service is not properly initialized or running.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            Logger.LogInformation("Destroying contract {ContractHash} on Neo N3 network", contractHash);

            try
            {
                // Create destroy script (calling the destroy method on the contract)
                var script = CreateDestroyScript(contractHash);
                var transaction = await CreateTransactionAsync(script);

                // Sign and send transaction within enclave
                var signedTx = await SignTransactionInEnclaveAsync(transaction);
                var txHash = await _rpcClient.SendRawTransactionAsync(signedTx).ConfigureAwait(false);

                // Wait for confirmation
                var applicationLog = await WaitForTransactionAsync(txHash);
                var success = applicationLog.Executions?[0]?.VMState == VMState.HALT;

                if (success)
                {
                    // Remove from cache
                    lock (_contractCache)
                    {
                        if (_contractCache.TryGetValue(contractHash, out var metadata))
                        {
                            metadata.IsActive = false;
                        }
                    }

                    _successCount++;
                    UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                    Logger.LogInformation("Successfully destroyed contract {ContractHash}", contractHash);
                }
                else
                {
                    _failureCount++;
                    UpdateMetric("LastFailureTime", DateTime.UtcNow);
                    var errorMessage = applicationLog.Executions?[0]?.ExceptionMessage ?? "Destroy failed";
                    UpdateMetric("LastErrorMessage", errorMessage);
                    Logger.LogError("Failed to destroy contract {ContractHash}: {Error}", contractHash, errorMessage);
                }

                return success;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error destroying contract {ContractHash}", contractHash);
                return false;
            }
        });
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var health = IsRunning && _wallet != null
            ? ServiceHealth.Healthy
            : ServiceHealth.Unhealthy;

        return Task.FromResult(health);
    }

    /// <inheritdoc/>
    protected override Task OnUpdateMetricsAsync()
    {
        UpdateMetric("RequestCount", _requestCount);
        UpdateMetric("SuccessCount", _successCount);
        UpdateMetric("FailureCount", _failureCount);
        UpdateMetric("SuccessRate", _requestCount > 0 ? (double)_successCount / _requestCount : 0);
        UpdateMetric("LastRequestTime", _lastRequestTime);
        UpdateMetric("ContractCount", _contractCache.Count);
        UpdateMetric("WalletAddress", _wallet?.GetAccounts().FirstOrDefault()?.Address ?? "Not initialized");

        return Task.CompletedTask;
    }

    #region Private Helper Methods

    private async Task InitializeWalletAsync()
    {
        try
        {
            // Get wallet information from enclave
            var walletData = await _enclaveManager.CallEnclaveFunctionAsync("getWalletForBlockchain", "NeoN3");

            if (string.IsNullOrEmpty(walletData))
            {
                // Create new wallet in enclave
                walletData = await _enclaveManager.CallEnclaveFunctionAsync("createWalletForBlockchain", "NeoN3");
            }

            // Create wallet from enclave data
            _wallet = CreateWalletFromEnclaveData(walletData);

            Logger.LogInformation("Wallet initialized with address: {Address}",
                _wallet.GetAccounts().First().Address);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing wallet");
            throw;
        }
    }

    private Wallet CreateWalletFromEnclaveData(string walletData)
    {
        // This would parse the secure wallet data from the enclave
        // For now, creating a memory wallet (in production, this would use enclave-secured keys)
        // Create a simple wallet - NEP6Wallet requires file path
        // For now, using base Wallet class
        var wallet = new SimpleWallet();

        // In production, the private key would come from the enclave
        var account = wallet.CreateAccount();

        return wallet;
    }

    private async Task RefreshContractCacheAsync()
    {
        try
        {
            // Load contract metadata from enclave storage
            var contractsData = await _enclaveManager.CallEnclaveFunctionAsync("listDeployedContracts", "NeoN3");

            if (!string.IsNullOrEmpty(contractsData))
            {
                var contracts = System.Text.Json.JsonSerializer.Deserialize<List<ContractMetadata>>(contractsData);
                if (contracts != null)
                {
                    lock (_contractCache)
                    {
                        _contractCache.Clear();
                        foreach (var contract in contracts)
                        {
                            _contractCache[contract.ContractHash] = contract;
                        }
                    }

                    Logger.LogInformation("Loaded {ContractCount} contracts from cache", contracts.Count);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error loading contract cache, starting with empty cache");
        }
    }

    private (NefFile nef, ContractManifest manifest) ParseContractCode(byte[] contractCode)
    {
        // Parse the contract code to extract NEF and manifest
        // This assumes the contract code contains both NEF and manifest

        try
        {
            using var ms = new MemoryStream(contractCode);
            using var reader = new BinaryReader(ms);

            // Read NEF
            var nefLength = reader.ReadInt32();
            var nefBytes = reader.ReadBytes(nefLength);
            var nef = nefBytes.AsSerializable<NefFile>();

            // Read Manifest
            var manifestLength = reader.ReadInt32();
            var manifestBytes = reader.ReadBytes(manifestLength);
            var manifestJson = Encoding.UTF8.GetString(manifestBytes);
            var manifest = ContractManifest.FromJson((JObject)JToken.Parse(manifestJson));

            return (nef, manifest);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing contract code");
            throw new ArgumentException("Invalid contract code format", nameof(contractCode));
        }
    }

    private byte[] CreateDeploymentScript(NefFile nef, ContractManifest manifest, object[]? constructorParameters)
    {
        using var scriptBuilder = new ScriptBuilder();

        // Push constructor parameters
        if (constructorParameters?.Length > 0)
        {
            foreach (var param in constructorParameters.Reverse())
            {
                scriptBuilder.EmitPush(ConvertParameter(param));
            }
        }

        // Push manifest
        scriptBuilder.EmitPush(manifest.ToJson().ToString());

        // Push NEF
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        nef.Serialize(writer);
        scriptBuilder.EmitPush(ms.ToArray());

        // Call ContractManagement.Deploy
        scriptBuilder.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy");

        return scriptBuilder.ToArray();
    }

    private byte[] CreateInvocationScript(string contractHash, string method, object[]? parameters)
    {
        using var scriptBuilder = new ScriptBuilder();

        // Push parameters
        if (parameters?.Length > 0)
        {
            foreach (var param in parameters.Reverse())
            {
                scriptBuilder.EmitPush(ConvertParameter(param));
            }
        }

        // Call contract method
        scriptBuilder.EmitDynamicCall(UInt160.Parse(contractHash), method);

        return scriptBuilder.ToArray();
    }

    private byte[] CreateUpdateScript(string contractHash, NefFile nef, ContractManifest manifest)
    {
        using var scriptBuilder = new ScriptBuilder();

        // Push manifest
        scriptBuilder.EmitPush(manifest.ToJson().ToString());

        // Push NEF
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        nef.Serialize(writer);
        scriptBuilder.EmitPush(ms.ToArray());

        // Call contract's update method
        scriptBuilder.EmitDynamicCall(UInt160.Parse(contractHash), "update");

        return scriptBuilder.ToArray();
    }

    private byte[] CreateDestroyScript(string contractHash)
    {

        using var scriptBuilder = new ScriptBuilder();
        // Call contract's destroy method
        scriptBuilder.EmitDynamicCall(UInt160.Parse(contractHash), "destroy");

        return scriptBuilder.ToArray();
    }

    private byte[] CreateTransferScript(string contractHash, BigInteger amount)
    {

        var from = _wallet!.GetAccounts().First().Address;
        var to = contractHash;

        using var scriptBuilder = new ScriptBuilder();
        // Create transfer script for GAS
        // Push parameters in reverse order
        scriptBuilder.EmitPush(StackItem.Null); // data parameter
        scriptBuilder.EmitPush(amount);
        scriptBuilder.EmitPush(UInt160.Parse(to));
        scriptBuilder.EmitPush(UInt160.Parse(from));
        scriptBuilder.EmitDynamicCall(NativeContract.GAS.Hash, "transfer");

        return scriptBuilder.ToArray();
    }

    private byte[] CombineScripts(byte[] script1, byte[] script2)
    {
        var combined = new byte[script1.Length + script2.Length];
        System.Array.Copy(script1, 0, combined, 0, script1.Length);
        System.Array.Copy(script2, 0, combined, script1.Length, script2.Length);
        return combined;
    }

    private async Task<global::Neo.Network.P2P.Payloads.Transaction> CreateTransactionAsync(byte[] script, long? gasLimit = null)
    {
        var account = _wallet!.GetAccounts().First();
        var sender = UInt160.Parse(account.Address);

        // Create transaction first to calculate network fee
        var tempTransaction = new global::Neo.Network.P2P.Payloads.Transaction
        {
            Script = script,
            SystemFee = gasLimit ?? 10000000, // Default 0.1 GAS
            ValidUntilBlock = await _rpcClient.GetBlockCountAsync().ConfigureAwait(false) + 86400,
            Nonce = (uint)Random.Shared.Next(),
            Signers = new global::Neo.Network.P2P.Payloads.Signer[]
            {
                new global::Neo.Network.P2P.Payloads.Signer
                {
                    Account = sender,
                    Scopes = global::Neo.Network.P2P.Payloads.WitnessScope.CalledByEntry,
                    AllowedContracts = null,
                    AllowedGroups = null
                }
            },
            Witnesses = new global::Neo.Network.P2P.Payloads.Witness[0],
            Attributes = new global::Neo.Network.P2P.Payloads.TransactionAttribute[0]
        };
        
        // Get network fee
        var networkFee = await _rpcClient.CalculateNetworkFeeAsync(tempTransaction).ConfigureAwait(false);

        // Update transaction with final network fee
        tempTransaction.NetworkFee = networkFee;

        return tempTransaction;
    }

    private async Task<global::Neo.Network.P2P.Payloads.Transaction> SignTransactionInEnclaveAsync(global::Neo.Network.P2P.Payloads.Transaction transaction)
    {
        // In production, this would sign the transaction within the enclave
        // For now, using the wallet to sign
        var account = _wallet!.GetAccounts().First();
        // Note: In production, implement proper transaction signing within enclave
        // This requires proper ContractParametersContext setup and wallet integration

        // Create placeholder witness for now
        transaction.Witnesses = new Witness[] { new Witness() };

        // Production implementation would include:
        // - ContractParametersContext creation
        // - Account key verification and signing
        // - Witness generation from signed context

        return transaction;
    }

    private async Task<RpcApplicationLog> WaitForTransactionAsync(UInt256 txHash)
    {
        const int maxAttempts = 30;
        const int delayMs = 1000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await _rpcClient.GetApplicationLogAsync(txHash.ToString()).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (attempt == maxAttempts - 1)
                    throw;

                await Task.Delay(delayMs);
            }
        }

        throw new TimeoutException("Transaction confirmation timeout");
    }

    private StackItem ConvertParameter(object parameter)
    {
        return parameter switch
        {
            int i => new global::Neo.VM.Types.Integer(i),
            long l => new global::Neo.VM.Types.Integer(new BigInteger(l)),
            BigInteger bi => new global::Neo.VM.Types.Integer(bi),
            string s => new global::Neo.VM.Types.ByteString(Encoding.UTF8.GetBytes(s)),
            byte[] bytes => new global::Neo.VM.Types.ByteString(bytes),
            bool b => b ? global::Neo.VM.Types.StackItem.True : global::Neo.VM.Types.StackItem.False,
            UInt160 u160 => new global::Neo.VM.Types.ByteString(u160.ToArray()),
            UInt256 u256 => new global::Neo.VM.Types.ByteString(u256.ToArray()),
            _ => new global::Neo.VM.Types.ByteString(Encoding.UTF8.GetBytes(parameter.ToString() ?? string.Empty))
        };
    }

    private object? ParseStackItem(StackItem item)
    {
        return item switch
        {
            global::Neo.VM.Types.Integer integer => integer.GetInteger(),
            global::Neo.VM.Types.ByteString byteString => byteString.GetString(),
            global::Neo.VM.Types.Boolean boolean => boolean.GetBoolean(),
            global::Neo.VM.Types.Array array => array.Select(ParseStackItem).ToArray(),
            global::Neo.VM.Types.Map map => map.ToDictionary(
                kvp => ParseStackItem(kvp.Key)?.ToString() ?? string.Empty,
                kvp => ParseStackItem(kvp.Value)),
            _ => item.ToString()
        };
    }

    private List<ContractMethod> ParseContractMethods(ContractManifest manifest)
    {
        return manifest.Abi.Methods.Select(method => new ContractMethod
        {
            Name = method.Name,
            Parameters = method.Parameters.Select(param => new Core.SmartContracts.ContractParameter
            {
                Name = param.Name,
                Type = param.Type.ToString()
            }).ToList(),
            ReturnType = method.ReturnType.ToString(),
            IsSafe = method.Safe,
            IsPayable = false // Neo N3 doesn't have payable concept like Ethereum
        }).ToList();
    }

    private List<Core.SmartContracts.ContractEvent> ParseContractEvents(RpcApplicationLog applicationLog, string contractHash, string? eventName = null)
    {
        var events = new List<Core.SmartContracts.ContractEvent>();

        if (applicationLog.Executions?.Count > 0)
        {
            foreach (var execution in applicationLog.Executions)
            {
                if (execution.Notifications != null)
                {
                    foreach (var notification in execution.Notifications)
                    {
                        // Filter by contract hash and optional event name
                        if ((notification.Contract?.ToString() ?? contractHash) == contractHash &&
                            (eventName == null || notification.EventName == eventName))
                        {
                            events.Add(new Core.SmartContracts.ContractEvent
                            {
                                Name = notification.EventName,
                                ContractHash = contractHash,
                                Parameters = notification.State is global::Neo.VM.Types.Array arr ? 
                                    arr.Select(ParseStackItem).Where(p => p != null).ToList() : 
                                    new List<object>(),
                                BlockNumber = 0, // Would need block context
                                TransactionHash = applicationLog.TxId?.ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }
        }

        return events;
    }

    #region Production Helper Methods

    /// <summary>
    /// Gets actual RPC client connection status.
    /// </summary>
    private async Task<bool> IsRpcConnectedAsync()
    {
        try
        {
            await _rpcClient.GetVersionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a production-ready transaction with proper fee calculation.
    /// </summary>
    private async Task<global::Neo.Network.P2P.Payloads.Transaction> CreateProductionTransactionAsync(byte[] script, long? gasLimit = null)
    {
        if (_wallet == null)
        {
            throw new InvalidOperationException("Wallet not initialized");
        }

        var account = _wallet.GetAccounts().FirstOrDefault();
        if (account == null)
        {
            throw new InvalidOperationException("No accounts available in wallet");
        }

        var blockCount = await _rpcClient.GetBlockCountAsync();

        var transaction = new global::Neo.Network.P2P.Payloads.Transaction
        {
            Script = script,
            SystemFee = gasLimit ?? 0,
            NetworkFee = 0, // Will calculate after
            ValidUntilBlock = blockCount + 86400, // 24 hours validity
            Signers = new global::Neo.Network.P2P.Payloads.Signer[]
            {
                new global::Neo.Network.P2P.Payloads.Signer
                {
                    Account = account.ScriptHash,
                    Scopes = global::Neo.Network.P2P.Payloads.WitnessScope.CalledByEntry
                }
            },
            Attributes = System.Array.Empty<global::Neo.Network.P2P.Payloads.TransactionAttribute>(),
            Witnesses = System.Array.Empty<global::Neo.Network.P2P.Payloads.Witness>()
        };

        // Calculate network fee
        var networkFee = await _rpcClient.CalculateNetworkFeeAsync(transaction).ConfigureAwait(false);
        transaction.NetworkFee = networkFee;

        return transaction;
    }

    /// <summary>
    /// Signs transaction with production-ready implementation.
    /// </summary>
    private async Task<global::Neo.Network.P2P.Payloads.Transaction> SignProductionTransactionAsync(global::Neo.Network.P2P.Payloads.Transaction transaction)
    {
        if (_wallet == null)
        {
            throw new InvalidOperationException("Wallet not initialized");
        }

        var account = _wallet.GetAccounts().FirstOrDefault();
        if (account?.HasKey != true)
        {
            throw new InvalidOperationException("Account has no private key for signing");
        }

        // Create context for signing  
        var context = new ContractParametersContext(null, transaction, ProtocolSettings.Default.Network);

        // Sign with wallet
        var signed = _wallet.Sign(context);
        if (!signed)
        {
            throw new InvalidOperationException("Failed to sign transaction");
        }

        // Apply signatures to transaction
        transaction.Witnesses = context.GetWitnesses();

        return transaction;
    }

    /// <summary>
    /// Safely executes RPC calls with error handling.
    /// </summary>
    private async Task<T> SafeRpcCallAsync<T>(Func<Task<T>> rpcCall, T defaultValue = default(T))
    {
        try
        {
            return await rpcCall();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "RPC call failed, returning default value");
            return defaultValue;
        }
    }

    #endregion
}

// Using Neo library types for RpcApplicationLog, Execution, and NotificationRecord

/// <summary>
/// Production RPC exception implementation.
/// </summary>
internal class RpcException : Exception
{
    public int Code { get; }
    public new object? Data { get; }

    public RpcException(string message) : base(message) { }

    public RpcException(string message, int code, object? data = null) : base(message)
    {
        Code = code;
        Data = data;
    }
}

internal class SimpleWallet : Wallet
{
    private readonly Dictionary<UInt160, WalletAccount> accounts = new();

    public SimpleWallet() : base("", global::Neo.ProtocolSettings.Default) { }

    public override string Name => "SimpleWallet";
    public override Version Version => new Version(1, 0);

    public override void Save() { }
    public override void Delete() { }
    public override bool ChangePassword(string oldPassword, string newPassword) => true;

    public override WalletAccount CreateAccount(byte[] privateKey)
    {
        var key = new KeyPair(privateKey);
        var contract = Contract.CreateSignatureContract(key.PublicKey);
        var account = new SimpleWalletAccount(this, contract.ScriptHash, key);
        accounts[contract.ScriptHash] = account;
        return account;
    }

    public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
    {
        var account = new SimpleWalletAccount(this, contract.ScriptHash, key);
        accounts[contract.ScriptHash] = account;
        return account;
    }

    public override WalletAccount CreateAccount(UInt160 scriptHash)
    {
        var account = new SimpleWalletAccount(this, scriptHash, null);
        accounts[scriptHash] = account;
        return account;
    }

    public override bool DeleteAccount(UInt160 scriptHash)
    {
        return accounts.Remove(scriptHash);
    }

    public override WalletAccount GetAccount(UInt160 scriptHash)
    {
        accounts.TryGetValue(scriptHash, out var account);
        return account;
    }

    public override IEnumerable<WalletAccount> GetAccounts()
    {
        return accounts.Values;
    }

    public override bool VerifyPassword(string password)
    {
        return true;
    }

    public override bool Contains(UInt160 scriptHash)
    {
        return accounts.ContainsKey(scriptHash);
    }

    public WalletAccount CreateAccount()
    {
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var key = new KeyPair(randomBytes);
        return CreateAccount(key.Export());
    }
}

internal class SimpleWalletAccount : WalletAccount
{
    private readonly KeyPair? key;

    public SimpleWalletAccount(Wallet wallet, UInt160 scriptHash, KeyPair? key)
        : base(scriptHash, global::Neo.ProtocolSettings.Default)
    {
        this.key = key;
    }

    public override bool HasKey => key != null;

    public override KeyPair GetKey()
    {
        return key ?? throw new InvalidOperationException("No key available");
    }
}

#endregion
