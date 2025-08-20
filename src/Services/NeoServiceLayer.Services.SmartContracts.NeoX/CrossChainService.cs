using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.SmartContracts.NeoX.Models;
using NeoServiceLayer.Tee.Host.Services;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Numerics;


namespace NeoServiceLayer.Services.SmartContracts.NeoX;

/// <summary>
/// Cross-chain service for Neo X bridging operations.
/// </summary>
public class CrossChainService : ServiceFramework.EnclaveBlockchainServiceBase
{
    private readonly ISmartContractManager _neoXManager;
    private readonly ISmartContractManager _neoN3Manager;
    private readonly IServiceConfiguration _configuration;
    private readonly IEnclaveManager _enclaveManager;
    private readonly Dictionary<string, BridgeConfiguration> _bridgeConfigs = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    public CrossChainService(
        ISmartContractManager neoXManager,
        ISmartContractManager neoN3Manager,
        IServiceConfiguration configuration,
        IEnclaveManager enclaveManager,
        ILogger<CrossChainService> logger)
        : base("CrossChainBridge", "Neo N3 to Neo X Cross-Chain Bridge Service", "1.0.0", logger,
               new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _neoXManager = neoXManager;
        _neoN3Manager = neoN3Manager;
        _configuration = configuration;
        _enclaveManager = enclaveManager;

        InitializeBridgeConfigurations();

        // Add capabilities
        AddCapability<CrossChainService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("SupportedChains", "NeoN3,NeoX");
        SetMetadata("BridgeType", "Bi-directional");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <summary>
    /// Executes a cross-chain transaction.
    /// </summary>
    public async Task<CrossChainTransactionResult> ExecuteCrossChainTransactionAsync(
        CrossChainTransactionRequest request,
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

            Logger.LogInformation("Executing cross-chain transaction from {Source} to {Target}",
                request.SourceBlockchain, request.TargetBlockchain);

            try
            {
                var bridgeKey = $"{request.SourceBlockchain}-{request.TargetBlockchain}";
                if (!_bridgeConfigs.TryGetValue(bridgeKey, out var bridgeConfig))
                {
                    throw new InvalidOperationException($"No bridge configuration found for {bridgeKey}");
                }

                var result = new CrossChainTransactionResult();

                // Step 1: Lock assets on source chain
                var lockResult = await LockAssetsOnSourceChainAsync(request, bridgeConfig, cancellationToken).ConfigureAwait(false);
                result.SourceTransactionHash = lockResult.TransactionHash;
                result.SourceBlockNumber = lockResult.BlockNumber;
                result.SourceGasConsumed = lockResult.GasConsumed;

                if (!lockResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = lockResult.ErrorMessage;
                    return result;
                }

                // Step 2: Wait for confirmations
                await WaitForConfirmationsAsync(request.SourceBlockchain, lockResult.TransactionHash,
                    bridgeConfig.MinConfirmations, cancellationToken);

                // Step 3: Mint/unlock assets on target chain
                var mintResult = await MintAssetsOnTargetChainAsync(request, bridgeConfig, lockResult, cancellationToken).ConfigureAwait(false);
                result.TargetTransactionHash = mintResult.TransactionHash;
                result.TargetBlockNumber = mintResult.BlockNumber;
                result.TargetGasConsumed = mintResult.GasConsumed;
                result.ReturnValue = mintResult.ReturnValue;

                result.IsSuccess = mintResult.IsSuccess;
                result.ErrorMessage = mintResult.ErrorMessage;
                result.BridgeFee = CalculateBridgeFee(request.Value ?? 0, bridgeConfig.FeePercentage);
                result.CompletedAt = DateTime.UtcNow;

                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                UpdateMetric("TotalCrossChainTransactions", _successCount);

                Logger.LogInformation("Successfully executed cross-chain transaction: {SourceTx} -> {TargetTx}",
                    result.SourceTransactionHash, result.TargetTransactionHash);

                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error executing cross-chain transaction");

                return new CrossChainTransactionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <summary>
    /// Gets the bridge configuration for a chain pair.
    /// </summary>
    public BridgeConfiguration? GetBridgeConfiguration(string sourceChain, string targetChain)
    {
        var bridgeKey = $"{sourceChain}-{targetChain}";
        return _bridgeConfigs.TryGetValue(bridgeKey, out var config) ? config : null;
    }

    /// <summary>
    /// Updates bridge configuration.
    /// </summary>
    public async Task<bool> UpdateBridgeConfigurationAsync(
        string sourceChain,
        string targetChain,
        BridgeConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                var bridgeKey = $"{sourceChain}-{targetChain}";
                _bridgeConfigs[bridgeKey] = configuration;

                // Store in enclave
                var configData = JsonSerializer.Serialize(configuration);
                await _enclaveManager.CallEnclaveFunctionAsync("storeBridgeConfiguration",
                    JsonSerializer.Serialize(new { bridgeKey, configuration = configData }));

                Logger.LogInformation("Updated bridge configuration for {BridgeKey}", bridgeKey);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating bridge configuration for {Source}-{Target}",
                    sourceChain, targetChain);
                return false;
            }
        });
    }

    /// <summary>
    /// Gets cross-chain transaction status.
    /// </summary>
    public async Task<CrossChainTransactionResult?> GetTransactionStatusAsync(
        string sourceTransactionHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusData = await _enclaveManager.CallEnclaveFunctionAsync("getCrossChainTransactionStatus",
                sourceTransactionHash, cancellationToken);

            if (!string.IsNullOrEmpty(statusData) && statusData != "null")
            {
                return JsonSerializer.Deserialize<CrossChainTransactionResult>(statusData);
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting cross-chain transaction status for {TxHash}",
                sourceTransactionHash);
            return null;
        }
    }

    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Cross-Chain Service...");

            // Load bridge configurations from enclave
            await LoadBridgeConfigurationsAsync().ConfigureAwait(false);

            Logger.LogInformation("Cross-Chain Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Cross-Chain Service");
            return false;
        }
    }

    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing enclave for Cross-Chain Service...");
            return await _enclaveManager.InitializeEnclaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing enclave for Cross-Chain Service");
            return false;
        }
    }

    protected override Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Cross-Chain Service...");
        return Task.FromResult(true);
    }

    protected override Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Cross-Chain Service...");
        return Task.FromResult(true);
    }

    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var health = IsRunning && _bridgeConfigs.Count > 0
            ? ServiceHealth.Healthy
            : ServiceHealth.Unhealthy;

        return Task.FromResult(health);
    }

    protected override Task OnUpdateMetricsAsync()
    {
        UpdateMetric("RequestCount", _requestCount);
        UpdateMetric("SuccessCount", _successCount);
        UpdateMetric("FailureCount", _failureCount);
        UpdateMetric("SuccessRate", _requestCount > 0 ? (double)_successCount / _requestCount : 0);
        UpdateMetric("LastRequestTime", _lastRequestTime);
        UpdateMetric("BridgeCount", _bridgeConfigs.Count);

        return Task.CompletedTask;
    }

    #region Private Methods

    private void InitializeBridgeConfigurations()
    {
        // Initialize default bridge configurations
        _bridgeConfigs["NeoN3-NeoX"] = new BridgeConfiguration
        {
            SourceBridgeAddress = _configuration.GetValue("Bridge:NeoN3:ContractHash", ""),
            TargetBridgeAddress = _configuration.GetValue("Bridge:NeoX:ContractAddress", ""),
            MinConfirmations = _configuration.GetValue("Bridge:NeoN3:MinConfirmations", 6),
            SignatureThreshold = _configuration.GetValue("Bridge:SignatureThreshold", 2),
            FeePercentage = _configuration.GetValue("Bridge:FeePercentage", 0.001m),
            Operators = _configuration.GetValue("Bridge:Operators", "").Split(',').ToList()
        };

        _bridgeConfigs["NeoX-NeoN3"] = new BridgeConfiguration
        {
            SourceBridgeAddress = _configuration.GetValue("Bridge:NeoX:ContractAddress", ""),
            TargetBridgeAddress = _configuration.GetValue("Bridge:NeoN3:ContractHash", ""),
            MinConfirmations = _configuration.GetValue("Bridge:NeoX:MinConfirmations", 12),
            SignatureThreshold = _configuration.GetValue("Bridge:SignatureThreshold", 2),
            FeePercentage = _configuration.GetValue("Bridge:FeePercentage", 0.001m),
            Operators = _configuration.GetValue("Bridge:Operators", "").Split(',').ToList()
        };
    }

    private async Task LoadBridgeConfigurationsAsync()
    {
        try
        {
            var configData = await _enclaveManager.CallEnclaveFunctionAsync("loadBridgeConfigurations", "").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(configData) && configData != "null")
            {
                var configs = JsonSerializer.Deserialize<Dictionary<string, BridgeConfiguration>>(configData);
                if (configs != null)
                {
                    foreach (var kvp in configs)
                    {
                        _bridgeConfigs[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error loading bridge configurations, using defaults");
        }
    }

    private async Task<ContractInvocationResult> LockAssetsOnSourceChainAsync(
        CrossChainTransactionRequest request,
        BridgeConfiguration bridgeConfig,
        CancellationToken cancellationToken)
    {
        var manager = GetManagerForBlockchain(request.SourceBlockchain);

        var parameters = new object[]
        {
            request.TargetBlockchain,
            request.TargetContract,
            request.Method,
            JsonSerializer.Serialize(request.Parameters ?? Array.Empty<object>()),
            request.Value ?? 0
        };

        var options = new ContractInvocationOptions
        {
            GasLimit = request.GasLimit,
            Value = new BigInteger(request.Value ?? 0),
            WaitForConfirmation = true
        };

        return await manager.InvokeContractAsync(
            bridgeConfig.SourceBridgeAddress,
            "lockAssets",
            parameters,
            options,
            cancellationToken);
    }

    private async Task<ContractInvocationResult> MintAssetsOnTargetChainAsync(
        CrossChainTransactionRequest request,
        BridgeConfiguration bridgeConfig,
        ContractInvocationResult lockResult,
        CancellationToken cancellationToken)
    {
        var manager = GetManagerForBlockchain(request.TargetBlockchain);

        var parameters = new object[]
        {
            request.SourceBlockchain,
            lockResult.TransactionHash,
            request.TargetContract,
            request.Method,
            JsonSerializer.Serialize(request.Parameters ?? Array.Empty<object>()),
            request.Value ?? 0
        };

        var options = new ContractInvocationOptions
        {
            GasLimit = request.GasLimit,
            WaitForConfirmation = true
        };

        return await manager.InvokeContractAsync(
            bridgeConfig.TargetBridgeAddress,
            "mintAssets",
            parameters,
            options,
            cancellationToken);
    }

    private async Task WaitForConfirmationsAsync(
        string blockchain,
        string transactionHash,
        int minConfirmations,
        CancellationToken cancellationToken)
    {
        const int maxWaitTime = 300; // 5 minutes
        const int pollInterval = 10; // 10 seconds

        var manager = GetManagerForBlockchain(blockchain);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(maxWaitTime))
        {
            try
            {
                // This would need to be implemented in the contract managers
                // For now, we'll just wait a bit
                await Task.Delay(pollInterval * 1000, cancellationToken).ConfigureAwait(false);

                Logger.LogDebug("Waiting for confirmations: {TxHash}", transactionHash);
                break; // Assume confirmed for now
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error checking confirmations for {TxHash}", transactionHash);
                await Task.Delay(pollInterval * 1000, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private ISmartContractManager GetManagerForBlockchain(string blockchain)
    {
        return blockchain.ToUpperInvariant() switch
        {
            "NEON3" => _neoN3Manager,
            "NEOX" => _neoXManager,
            _ => throw new ArgumentException($"Unsupported blockchain: {blockchain}")
        };
    }

    private decimal CalculateBridgeFee(decimal value, decimal feePercentage)
    {
        return value * feePercentage;
    }

    #endregion
}
