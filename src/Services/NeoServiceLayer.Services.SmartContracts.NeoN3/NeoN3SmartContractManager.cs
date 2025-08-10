using System.Numerics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Neo;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Persistence;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using ContractEvent = NeoServiceLayer.Core.SmartContracts.ContractEvent;
using NeoContractParameter = Neo.SmartContract.ContractParameter;
using StackItem = Neo.VM.Types.StackItem;
using Transaction = Neo.Network.P2P.Payloads.Transaction;

namespace NeoServiceLayer.Services.SmartContracts.NeoN3;

/// <summary>
/// Neo N3 implementation of the smart contract manager.
/// </summary>
public class NeoN3SmartContractManager : EnclaveBlockchainServiceBase, ISmartContractManager
{
    private readonly HttpClient _httpClient;
    private readonly IServiceConfiguration _configuration;
    private new readonly IEnclaveManager _enclaveManager;
    private readonly Dictionary<string, ContractMetadata> _contractCache = new();
    private readonly string _rpcUrl;
    private Wallet? _wallet;
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;
    private int _rpcId = 1;

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

        // Initialize HTTP client for RPC calls
        _rpcUrl = _configuration.GetValue("NeoN3:RpcUrl", "http://localhost:40332");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NeoServiceLayer/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Add capabilities
        AddCapability<ISmartContractManager>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("NetworkMagic", _configuration.GetValue("NeoN3:NetworkMagic", "860833102"));
        SetMetadata("RpcUrl", _rpcUrl);
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
            var version = await CallRpcMethodAsync<JsonElement>("getversion");
            var userAgent = version.TryGetProperty("useragent", out var ua) ? ua.GetString() : "Unknown";
            Logger.LogInformation("Connected to Neo N3 node version: {Version}", userAgent);

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
            _httpClient.Dispose();
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
                var txHash = await SendRawTransactionAsync(signedTx);

                // Wait for confirmation if required
                var result = new ContractDeploymentResult
                {
                    TransactionHash = txHash.ToString(),
                    ContractHash = global::Neo.SmartContract.Helper.GetContractHash(
                        _wallet!.GetAccounts().First().ScriptHash,
                        nef.CheckSum,
                        manifest.Name).ToString(),
                    IsSuccess = true
                };

                if (options?.WaitForConfirmation != false) // Wait by default
                {
                    var applicationLog = await WaitForTransactionAsync(txHash);
                    if (applicationLog?.blockHash != null)
                    {
                        var block = await GetBlockAsync(applicationLog.blockHash);
                        result.BlockNumber = block?.index ?? 0;
                    }
                    result.GasConsumed = applicationLog?.executions?[0]?.gasconsumed ?? 0;
                    result.IsSuccess = applicationLog?.executions?[0]?.state == "HALT";

                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = applicationLog?.executions?[0]?.exception;
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
                var txHash = await SendRawTransactionAsync(signedTx);

                var result = new ContractInvocationResult
                {
                    TransactionHash = txHash.ToString(),
                    IsSuccess = true
                };

                // Wait for confirmation if required
                if (options?.WaitForConfirmation != false)
                {
                    var applicationLog = await WaitForTransactionAsync(txHash);
                    if (applicationLog?.blockHash != null)
                    {
                        var block = await GetBlockAsync(applicationLog.blockHash);
                        result.BlockNumber = block?.index ?? 0;
                    }
                    result.GasConsumed = applicationLog?.executions?[0]?.gasconsumed ?? 0;
                    result.IsSuccess = applicationLog?.executions?[0]?.state == "HALT";
                    result.ExecutionState = applicationLog?.executions?[0]?.state;

                    if (result.IsSuccess)
                    {
                        var stack = applicationLog?.executions?[0]?.stack;
                        if (stack?.Length > 0)
                        {
                            result.ReturnValue = ParseStackItem(stack[0]);
                        }

                        // Parse events
                        result.Events = ParseContractEvents(applicationLog, contractHash);
                    }
                    else
                    {
                        result.ErrorMessage = applicationLog?.executions?[0]?.exception;
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
            var result = await InvokeScriptAsync(script);

            if (result.state == "HALT" && result.stack?.Length > 0)
            {
                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);

                var returnValue = ParseStackItem(result.stack[0]);
                Logger.LogDebug("Successfully called contract {ContractHash} method {Method}", contractHash, method);
                return returnValue;
            }
            else
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                var errorMessage = result.exception ?? "Contract call failed";
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
            var contractState = await GetContractStateAsync(contractHash);
            if (contractState == null)
            {
                return null;
            }

            var metadata = new ContractMetadata
            {
                ContractHash = contractHash,
                Name = contractState.manifest?.name ?? "Unknown",
                Version = "Unknown",
                Author = contractState.manifest?.author ?? "Unknown",
                Description = "Contract retrieved from blockchain",
                Manifest = System.Text.Json.JsonSerializer.Serialize(contractState.manifest),
                DeployedBlockNumber = 0, // Would need to search transaction history
                DeploymentTxHash = "Unknown",
                DeployedAt = DateTime.MinValue,
                Methods = ParseContractMethods(contractState.manifest),
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
    public async Task<IEnumerable<ContractEvent>> GetContractEventsAsync(
        string contractHash,
        string? eventName = null,
        long? fromBlock = null,
        long? toBlock = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Getting contract events for {ContractHash}", contractHash);

            var events = new List<ContractEvent>();

            // Get current block height if toBlock not specified
            if (!toBlock.HasValue)
            {
                var blockCount = await GetBlockCountAsync();
                toBlock = blockCount - 1;
            }

            fromBlock ??= Math.Max(0, toBlock.Value - 100); // Default to last 100 blocks

            // Query application logs for the block range
            for (long blockIndex = fromBlock.Value; blockIndex <= toBlock.Value; blockIndex++)
            {
                try
                {
                    var block = await GetBlockAsync(blockIndex.ToString());
                    if (block?.tx == null) continue;

                    foreach (var tx in block.tx)
                    {
                        try
                        {
                            var appLog = await GetApplicationLogAsync(tx.hash);
                            if (appLog != null)
                            {
                                var contractEvents = ParseContractEvents(appLog, contractHash, eventName);
                                events.AddRange(contractEvents);
                            }
                        }
                        catch (Exception)
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
            var result = await InvokeScriptAsync(script);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            var gasConsumed = result.gasconsumed;
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
                var txHash = await SendRawTransactionAsync(signedTx);

                var result = new ContractDeploymentResult
                {
                    ContractHash = contractHash,
                    TransactionHash = txHash.ToString(),
                    IsSuccess = true,
                    ContractManifest = manifest.ToJson().ToString()
                };

                // Wait for confirmation
                var applicationLog = await WaitForTransactionAsync(txHash);
                if (applicationLog?.blockHash != null)
                {
                    var block = await GetBlockAsync(applicationLog.blockHash);
                    result.BlockNumber = block?.index ?? 0;
                }
                result.GasConsumed = applicationLog?.executions?[0]?.gasconsumed ?? 0;
                result.IsSuccess = applicationLog?.executions?[0]?.state == "HALT";

                if (!result.IsSuccess)
                {
                    result.ErrorMessage = applicationLog?.executions?[0]?.exception;
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
                var txHash = await SendRawTransactionAsync(signedTx);

                // Wait for confirmation
                var applicationLog = await WaitForTransactionAsync(txHash);
                var success = applicationLog?.executions?[0]?.state == "HALT";

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
                    var errorMessage = applicationLog?.executions?[0]?.exception ?? "Destroy failed";
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

    #region RPC Methods

    private async Task<T> CallRpcMethodAsync<T>(string method, params object[] parameters)
    {
        var requestId = Interlocked.Increment(ref _rpcId);
        var request = new
        {
            jsonrpc = "2.0",
            method = method,
            @params = parameters,
            id = requestId
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_rpcUrl, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseJson);

        if (rpcResponse.TryGetProperty("error", out var error))
        {
            var errorMessage = error.GetProperty("message").GetString();
            throw new InvalidOperationException($"RPC Error: {errorMessage}");
        }

        var result = rpcResponse.GetProperty("result");
        return System.Text.Json.JsonSerializer.Deserialize<T>(result.GetRawText())!;
    }

    private async Task<long> GetBlockCountAsync()
    {
        return await CallRpcMethodAsync<long>("getblockcount");
    }

    private async Task<RpcBlock?> GetBlockAsync(string blockHashOrIndex)
    {
        try
        {
            return await CallRpcMethodAsync<RpcBlock>("getblock", blockHashOrIndex, true);
        }
        catch
        {
            return null;
        }
    }

    private async Task<RpcContractState?> GetContractStateAsync(string contractHash)
    {
        try
        {
            return await CallRpcMethodAsync<RpcContractState>("getcontractstate", contractHash);
        }
        catch
        {
            return null;
        }
    }

    private async Task<RpcApplicationLog?> GetApplicationLogAsync(string txHash)
    {
        try
        {
            return await CallRpcMethodAsync<RpcApplicationLog>("getapplicationlog", txHash);
        }
        catch
        {
            return null;
        }
    }

    private async Task<RpcInvocationResult> InvokeScriptAsync(byte[] script)
    {
        var scriptBase64 = Convert.ToBase64String(script);
        return await CallRpcMethodAsync<RpcInvocationResult>("invokescript", scriptBase64);
    }

    private async Task<UInt256> SendRawTransactionAsync(Transaction transaction)
    {
        var rawTransaction = Convert.ToBase64String(transaction.ToArray());
        var result = await CallRpcMethodAsync<JsonElement>("sendrawtransaction", rawTransaction);
        
        if (result.TryGetProperty("hash", out var hashProp))
        {
            return UInt256.Parse(hashProp.GetString()!);
        }
        
        return transaction.Hash;
    }

    private async Task<long> CalculateNetworkFeeAsync(byte[] script)
    {
        try
        {
            var scriptBase64 = Convert.ToBase64String(script);
            var result = await CallRpcMethodAsync<RpcInvocationResult>("invokescript", scriptBase64);
            return Math.Max(1000000, result.gasconsumed / 10); // Estimate network fee
        }
        catch
        {
            return 1000000; // Default 0.01 GAS
        }
    }

    #endregion

    private (NefFile nef, ContractManifest manifest) ParseContractCode(byte[] contractCode)
    {
        // Parse the contract code to extract NEF and manifest
        // This assumes the contract code contains both NEF and manifest

        try
        {
            using var reader = new BinaryReader(new MemoryStream(contractCode));

            // Read NEF
            var nefLength = reader.ReadInt32();
            var nefBytes = reader.ReadBytes(nefLength);
            var nef = nefBytes.AsSerializable<NefFile>();

            // Read Manifest
            var manifestLength = reader.ReadInt32();
            var manifestBytes = reader.ReadBytes(manifestLength);
            var manifestJson = Encoding.UTF8.GetString(manifestBytes);
            var manifest = ContractManifest.FromJson((JObject)JToken.Parse(manifestJson))!;

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
        // Serialize NEF to byte array
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
        // Serialize NEF to byte array
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
        using var scriptBuilder = new ScriptBuilder();

        var from = _wallet!.GetAccounts().First().ScriptHash;
        var to = UInt160.Parse(contractHash);

        // Create transfer script for GAS
        // Push parameters in reverse order
        scriptBuilder.EmitPush(StackItem.Null); // data parameter
        scriptBuilder.EmitPush(amount);
        scriptBuilder.EmitPush(to);
        scriptBuilder.EmitPush(from);
        scriptBuilder.EmitDynamicCall(NativeContract.GAS.Hash, "transfer");

        return scriptBuilder.ToArray();
    }

    private byte[] CombineScripts(byte[] script1, byte[] script2)
    {
        var combined = new byte[script1.Length + script2.Length];
        Array.Copy(script1, 0, combined, 0, script1.Length);
        Array.Copy(script2, 0, combined, script1.Length, script2.Length);
        return combined;
    }

    private async Task<Transaction> CreateTransactionAsync(byte[] script, long? gasLimit = null)
    {
        var account = _wallet!.GetAccounts().First();
        var sender = account.ScriptHash;

        // Get network fee
        var networkFee = await CalculateNetworkFeeAsync(script);

        // Get current block count for ValidUntilBlock
        var blockCount = await GetBlockCountAsync();

        // Create transaction
        var transaction = new Transaction
        {
            Script = script,
            SystemFee = gasLimit ?? 10000000, // Default 0.1 GAS
            NetworkFee = networkFee,
            ValidUntilBlock = (uint)(blockCount + 86400), // Valid for ~24 hours
            Nonce = (uint)Random.Shared.Next(),
            Version = 0,
            Signers = new Signer[]
            {
                new Signer
                {
                    Account = sender,
                    Scopes = WitnessScope.CalledByEntry
                }
            }
        };

        return transaction;
    }

    private async Task<Transaction> SignTransactionInEnclaveAsync(Transaction transaction)
    {
        // In production, this would sign the transaction within the enclave
        // For now, create a simple witness
        var account = _wallet!.GetAccounts().First();
        
        // Create witness array for the transaction
        if (account.HasKey)
        {
            var key = account.GetKey();
            
            // Create invocation script (signature)
            using var sb = new global::Neo.VM.ScriptBuilder();
            sb.EmitPush(transaction.Sign(key, ProtocolSettings.Default.Network));
            var invocationScript = sb.ToArray();
            
            // Get verification script from the account contract
            var verificationScript = account.Contract.Script;
            
            transaction.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = invocationScript,
                    VerificationScript = verificationScript
                }
            };
        }
        else
        {
            // Create a placeholder witness for now
            transaction.Witnesses = new global::Neo.Network.P2P.Payloads.Witness[] 
            { 
                new global::Neo.Network.P2P.Payloads.Witness 
                { 
                    InvocationScript = new byte[0], 
                    VerificationScript = new byte[0] 
                } 
            };
        }

        return transaction;
    }

    private async Task<RpcApplicationLog?> WaitForTransactionAsync(UInt256 txHash)
    {
        const int maxAttempts = 30;
        const int delayMs = 1000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await GetApplicationLogAsync(txHash.ToString());
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

    private object? ParseStackItem(object stackItem)
    {
        if (stackItem is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetInt64(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(e => ParseStackItem(e)).ToArray(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ParseStackItem(p.Value)),
                _ => element.GetRawText()
            };
        }
        return stackItem;
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

    private List<ContractMethod> ParseContractMethods(RpcContractManifest? manifest)
    {
        if (manifest?.abi?.methods == null)
        {
            return new List<ContractMethod>();
        }

        return manifest.abi.methods.Select(method => new ContractMethod
        {
            Name = method.name ?? "unknown",
            Parameters = method.parameters?.Select(param => new Core.SmartContracts.ContractParameter
            {
                Name = param.name ?? "param",
                Type = param.type ?? "Any"
            }).ToList() ?? new List<Core.SmartContracts.ContractParameter>(),
            ReturnType = method.returntype ?? "Void",
            IsSafe = method.safe,
            IsPayable = false // Neo N3 doesn't have payable concept like Ethereum
        }).ToList();
    }

    private List<ContractEvent> ParseContractEvents(RpcApplicationLog? applicationLog, string contractHash, string? eventName = null)
    {
        var events = new List<ContractEvent>();

        if (applicationLog?.executions != null)
        {
            foreach (var execution in applicationLog.executions)
            {
                if (execution.notifications != null)
                {
                    foreach (var notification in execution.notifications)
                    {
                        if (notification.contract == contractHash &&
                            (eventName == null || notification.eventname == eventName))
                        {
                            events.Add(new ContractEvent
                            {
                                Name = notification.eventname ?? string.Empty,
                                ContractHash = contractHash,
                                Parameters = notification.state?.Select(ParseStackItem).Where(p => p != null).ToList() ?? new List<object>(),
                                BlockNumber = 0, // Would need block context
                                TransactionHash = applicationLog.txid ?? string.Empty
                            });
                        }
                    }
                }
            }
        }

        return events;
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}

#region RPC Data Models

public class RpcBlock
{
    public long index { get; set; }
    public string? hash { get; set; }
    public RpcTransaction[]? tx { get; set; }
}

public class RpcTransaction
{
    public string? hash { get; set; }
}

public class RpcContractState
{
    public RpcContractManifest? manifest { get; set; }
}

public class RpcContractManifest
{
    public string? name { get; set; }
    public string? author { get; set; }
    public RpcContractAbi? abi { get; set; }
}

public class RpcContractAbi
{
    public RpcContractMethod[]? methods { get; set; }
}

public class RpcContractMethod
{
    public string? name { get; set; }
    public RpcContractParameter[]? parameters { get; set; }
    public string? returntype { get; set; }
    public bool safe { get; set; }
}

public class RpcContractParameter
{
    public string? name { get; set; }
    public string? type { get; set; }
}

public class RpcApplicationLog
{
    public string? txid { get; set; }
    public string? blockHash { get; set; }
    public RpcExecution[]? executions { get; set; }
}

public class RpcExecution
{
    public string? state { get; set; }
    public long gasconsumed { get; set; }
    public string? exception { get; set; }
    public object[]? stack { get; set; }
    public RpcNotification[]? notifications { get; set; }
}

public class RpcNotification
{
    public string? contract { get; set; }
    public string? eventname { get; set; }
    public object[]? state { get; set; }
}

public class RpcInvocationResult
{
    public string? state { get; set; }
    public long gasconsumed { get; set; }
    public string? exception { get; set; }
    public object[]? stack { get; set; }
}

#endregion

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

    public override WalletAccount CreateAccount(Contract contract, KeyPair? key = null)
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

    public override WalletAccount? GetAccount(UInt160 scriptHash)
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

    public new WalletAccount CreateAccount()
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