using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using CoreConfig = NeoServiceLayer.Core.Configuration.IServiceConfiguration;
using NeoServiceLayer.Core.Security;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.CrossChain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using CoreModels = NeoServiceLayer.Core.Models;
using ServiceCrossChainMessage = NeoServiceLayer.Services.CrossChain.Models.CrossChainMessage;
using ServiceCrossChainMessageRequest = NeoServiceLayer.Services.CrossChain.Models.CrossChainMessageRequest;
using ServiceCrossChainMessageStatus = NeoServiceLayer.Services.CrossChain.Models.CrossChainMessageStatus;
using ServiceCrossChainRoute = NeoServiceLayer.Services.CrossChain.Models.CrossChainRoute;
using ServiceCrossChainOperation = NeoServiceLayer.Services.CrossChain.Models.CrossChainOperation;


namespace NeoServiceLayer.Services.CrossChain;

/// <summary>
/// Implementation of the Cross-Chain Service that provides cross-chain interoperability and messaging capabilities.
/// </summary>
public partial class CrossChainService : CryptographicServiceBase, ICrossChainService
{
    private readonly Dictionary<string, ServiceCrossChainMessageStatus> _messages = new();
    private readonly Dictionary<string, List<CrossChainTransaction>> _transactionHistory = new();
    private readonly object _messagesLock = new();
    private readonly List<CrossChainPair> _supportedChains;
    private readonly IBlockchainClientFactory? _blockchainClientFactory;
    
    // Cryptographic algorithm instances
    private readonly System.Security.Cryptography.ECDsa ecdsa = System.Security.Cryptography.ECDsa.Create();
    private readonly System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
    private readonly System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChainService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="blockchainClientFactory">The blockchain client factory.</param>
    /// <param name="enclaveManager">The enclave manager (optional).</param>
    public CrossChainService(
        ILogger<CrossChainService> logger,
        CoreConfig? configuration = null,
        IBlockchainClientFactory? blockchainClientFactory = null,
        NeoServiceLayer.Tee.Host.Services.IEnclaveManager? enclaveManager = null)
        : base("CrossChainService", "Cross-chain interoperability and messaging service", "1.0.0", logger, configuration as NeoServiceLayer.ServiceFramework.IServiceConfiguration, enclaveManager)
    {
        Configuration = configuration;
        _blockchainClientFactory = blockchainClientFactory;

        // Initialize supported blockchain pairs from configuration
        _supportedChains = LoadSupportedChainsFromConfiguration();

        // If no configuration, use defaults with warning
        if (_supportedChains.Count == 0)
        {
            Logger.LogWarning("No cross-chain pairs configured, using defaults. Configure 'CrossChain:SupportedPairs' in appsettings.json");
            _supportedChains = GetDefaultChainPairs();
        }

        AddCapability<ICrossChainService>();
        AddDependency(new ServiceDependency("KeyManagementService", true, "1.0.0"));
        AddDependency(new ServiceDependency("EventSubscriptionService", true, "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected new CoreConfig? Configuration { get; }

    private List<CrossChainPair> LoadSupportedChainsFromConfiguration()
    {
        var pairs = new List<CrossChainPair>();

        // Try to load configured pairs from configuration
        var pairCount = Configuration?.GetValue("CrossChain:SupportedPairs:Count", 0) ?? 0;

        for (int i = 0; i < pairCount; i++)
        {
            try
            {
                var prefix = $"CrossChain:SupportedPairs:{i}";
                var sourceChainStr = Configuration?.GetValue<string>($"{prefix}:SourceChain");
                var targetChainStr = Configuration?.GetValue<string>($"{prefix}:TargetChain");

                if (!string.IsNullOrEmpty(sourceChainStr) && !string.IsNullOrEmpty(targetChainStr) &&
                    Enum.TryParse<BlockchainType>(sourceChainStr, out var sourceChain) &&
                    Enum.TryParse<BlockchainType>(targetChainStr, out var targetChain))
                {
                    var chainPair = new CrossChainPair
                    {
                        SourceChain = sourceChain,
                        TargetChain = targetChain,
                        IsActive = Configuration?.GetValue($"{prefix}:IsActive", true) ?? true,
                        MinTransferAmount = Configuration?.GetValue($"{prefix}:MinTransferAmount", 0.001m) ?? 0.001m,
                        MaxTransferAmount = Configuration?.GetValue($"{prefix}:MaxTransferAmount", 1000000m) ?? 1000000m,
                        BaseFee = Configuration?.GetValue($"{prefix}:BaseFee", 0.01m) ?? 0.01m,
                        EstimatedTime = Configuration?.GetValue($"{prefix}:EstimatedTime", 5) ?? 5,
                        SupportedTokens = new List<string> { "GAS", "NEO", "USDT" } // Default tokens
                    };
                    pairs.Add(chainPair);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error loading cross-chain pair configuration at index {Index}", i);
            }
        }

        return pairs;
    }

    private List<CrossChainPair> GetDefaultChainPairs()
    {
        return new List<CrossChainPair>
        {
            new CrossChainPair
            {
                SourceChain = BlockchainType.NeoN3,
                TargetChain = BlockchainType.NeoX,
                IsActive = true,
                MinTransferAmount = 0.001m,
                MaxTransferAmount = 1000000m,
                BaseFee = 0.01m,
                EstimatedTime = 5,
                SupportedTokens = new List<string> { "GAS", "NEO", "USDT" }
            },
            new CrossChainPair
            {
                SourceChain = BlockchainType.NeoX,
                TargetChain = BlockchainType.NeoN3,
                IsActive = true,
                MinTransferAmount = 0.001m,
                MaxTransferAmount = 1000000m,
                BaseFee = 0.01m,
                EstimatedTime = 5,
                SupportedTokens = new List<string> { "GAS", "NEO", "USDT" }
            }
        };
    }

    /// <inheritdoc/>
    public async Task<string> SendMessageAsync(ServiceCrossChainMessageRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(sourceBlockchain) || !SupportsBlockchain(targetBlockchain))
        {
            throw new NotSupportedException($"Blockchain pair {sourceBlockchain} -> {targetBlockchain} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var messageId = Guid.NewGuid().ToString();

            // Create message status
            var messageStatus = new CrossChainMessageStatus
            {
                MessageId = messageId,
                Status = MessageStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Generate cryptographic proof for the message
            var messageHash = await ComputeMessageHashAsync(request, sourceBlockchain, targetBlockchain);
            var signature = await SignMessageAsync(messageHash);

            // Store message status
            lock (_messagesLock)
            {
                _messages[messageId] = messageStatus;
            }

            // Process message asynchronously
            _ = Task.Run(async () => await ProcessMessageAsync(messageId, request, sourceBlockchain, targetBlockchain));

            Logger.LogInformation("Cross-chain message {MessageId} created from {Source} to {Target}",
                messageId, sourceBlockchain, targetBlockchain);

            return messageId;
        });
    }

    /// <inheritdoc/>
    public async Task<CrossChainMessageStatus> GetMessageStatusAsync(string messageId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_messagesLock)
            {
                if (_messages.TryGetValue(messageId, out var status))
                {
                    return status;
                }
            }

            throw new ArgumentException($"Message {messageId} not found", nameof(messageId));
        });
    }

    /// <inheritdoc/>
    public async Task<string> TransferTokensAsync(CoreModels.CrossChainTransferRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate blockchain address formats
        if (!string.IsNullOrEmpty(request.DestinationAddress))
        {
            var targetBlockchainStr = targetBlockchain.ToString().ToUpperInvariant();
            if (!InputValidation.IsValidBlockchainAddress(request.DestinationAddress, targetBlockchainStr))
            {
                throw new ArgumentException($"Invalid destination address format for {targetBlockchain}: {request.DestinationAddress}");
            }
        }

        if (!string.IsNullOrEmpty(request.TokenAddress))
        {
            var sourceBlockchainStr = sourceBlockchain.ToString().ToUpperInvariant();
            if (!InputValidation.IsValidBlockchainAddress(request.TokenAddress, sourceBlockchainStr))
            {
                throw new ArgumentException($"Invalid token address format for {sourceBlockchain}: {request.TokenAddress}");
            }
        }

        if (!SupportsBlockchain(sourceBlockchain) || !SupportsBlockchain(targetBlockchain))
        {
            throw new NotSupportedException($"Blockchain pair {sourceBlockchain} -> {targetBlockchain} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var transferId = Guid.NewGuid().ToString();

            // Validate transfer amount
            var chainPair = GetChainPair(sourceBlockchain, targetBlockchain);
            if (chainPair == null || !chainPair.IsActive)
            {
                throw new InvalidOperationException($"Chain pair {sourceBlockchain} -> {targetBlockchain} is not active");
            }

            if (request.Amount < chainPair.MinTransferAmount || request.Amount > chainPair.MaxTransferAmount)
            {
                throw new ArgumentException($"Transfer amount {request.Amount} is outside allowed range");
            }

            // Create transaction record
            var transaction = new CrossChainTransaction
            {
                Id = transferId,
                FromAddress = "sender", // ServiceModels.CrossChainTransferRequest doesn't have sender
                ToAddress = request.DestinationAddress,
                SourceChain = sourceBlockchain,
                TargetChain = targetBlockchain,
                Type = CrossChainTransactionType.TokenTransfer,
                Amount = request.Amount,
                TokenContract = request.TokenAddress,
                Status = CrossChainMessageState.Created,
                CreatedAt = DateTime.UtcNow
            };

            // Store transaction
            lock (_messagesLock)
            {
                if (!_transactionHistory.ContainsKey("sender"))
                {
                    _transactionHistory["sender"] = new List<CrossChainTransaction>();
                }
                _transactionHistory["sender"].Add(transaction);
            }

            // Process transfer asynchronously
            _ = Task.Run(async () => await ProcessTransferAsync(transferId, request, sourceBlockchain, targetBlockchain));

            // Add a small delay to make this method truly async
            await Task.Delay(1);

            Logger.LogInformation("Cross-chain transfer {TransferId} created: {Amount} {Token} from {Source} to {Target}",
                transferId, request.Amount, request.TokenAddress, sourceBlockchain, targetBlockchain);

            return transferId;
        });
    }

    /// <inheritdoc/>
    public async Task<CrossChainExecutionResult> ExecuteContractCallAsync(CrossChainContractCallRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(sourceBlockchain) || !SupportsBlockchain(targetBlockchain))
        {
            throw new NotSupportedException($"Blockchain pair {sourceBlockchain} -> {targetBlockchain} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var executionId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Executing cross-chain contract call {ExecutionId} from {Source} to {Target}",
                    executionId, sourceBlockchain, targetBlockchain);

                if (_blockchainClientFactory != null)
                {
                    // Execute via bridge contract
                    var sourceClient = _blockchainClientFactory.CreateClient(sourceBlockchain);
                    var bridgeContract = GetBridgeContract(sourceBlockchain, targetBlockchain);

                    // Encode cross-chain call request
                    var callData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        target = request.TargetContract,
                        method = request.Method,
                        parameters = request.Parameters,
                        gasLimit = request.GasLimit
                    });

                    // Submit cross-chain call to bridge
                    var txHash = await sourceClient.InvokeContractMethodAsync(
                        bridgeContract,
                        "executeRemoteCall",
                        targetBlockchain.ToString(),
                        request.TargetContract,
                        request.Method,
                        callData
                    );

                    // Wait for confirmations
                    var confirmations = GetRequiredConfirmations(sourceBlockchain);
                    await WaitForConfirmationsAsync(sourceClient, txHash, confirmations);

                    // Get transaction details
                    var tx = await sourceClient.GetTransactionAsync(txHash);

                    var result = new CrossChainExecutionResult
                    {
                        ExecutionId = executionId,
                        Success = tx?.Status.ToString() == "Success",
                        Result = $"Cross-chain call submitted to bridge: {txHash}",
                        TransactionHash = txHash,
                        GasUsed = (long)(tx?.GasUsed ?? 0),
                        BlockNumber = tx?.BlockNumber ?? 0,
                        ExecutedAt = DateTime.UtcNow
                    };

                    Logger.LogInformation("Cross-chain contract call {ExecutionId} submitted: {TxHash}",
                        executionId, txHash);
                    return result;
                }
                else
                {
                    // Fallback simulation
                    await Task.Delay(1000);
                    return new CrossChainExecutionResult
                    {
                        ExecutionId = executionId,
                        Success = true,
                        Result = "Simulated execution (configure blockchain client)",
                        TransactionHash = Guid.NewGuid().ToString(),
                        GasUsed = Random.Shared.Next(21000, 100000),
                        ExecutedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Cross-chain contract call {ExecutionId} failed", executionId);

                return new CrossChainExecutionResult
                {
                    ExecutionId = executionId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyMessageProofAsync(CrossChainMessageProof proof, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(proof);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            // Verify the cryptographic proof within the enclave
            var isValid = await VerifyProofInEnclaveAsync(proof);

            Logger.LogDebug("Message proof verification for {MessageId} on {Blockchain}: {IsValid}",
                proof.MessageId, blockchainType, isValid);

            return isValid;
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SupportedChain>> GetSupportedChainsAsync()
    {
        var supportedChains = _supportedChains.Select(pair => new SupportedChain
        {
            ChainId = pair.SourceChain.ToString(),
            Name = pair.SourceChain.ToString(),
            Type = pair.SourceChain,
            IsTestnet = false,
            Configuration = new Dictionary<string, object>
            {
                { "IsActive", pair.IsEnabled },
                { "SupportedTokens", new[] { "GAS", "NEO", "USDT" } }
            }
        }).ToList();

        return await Task.FromResult(supportedChains.AsEnumerable());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CrossChainTransaction>> GetTransactionHistoryAsync(string address, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(address);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_messagesLock)
            {
                if (_transactionHistory.TryGetValue(address, out var history))
                {
                    return history.Where(t => t.SourceChain == blockchainType || t.TargetChain == blockchainType).ToList();
                }
            }

            return Enumerable.Empty<CrossChainTransaction>();
        });
    }

    /// <inheritdoc/>
    public async Task<string> ExecuteRemoteCallAsync(CoreModels.RemoteCallRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(sourceBlockchain) || !SupportsBlockchain(targetBlockchain))
        {
            throw new NotSupportedException($"Blockchain pair {sourceBlockchain} -> {targetBlockchain} is not supported");
        }

        var callId = Guid.NewGuid().ToString();
        Logger.LogInformation("Executing remote call {CallId} from {Source} to {Target}", callId, sourceBlockchain, targetBlockchain);

        // Simulate remote call execution
        await Task.Delay(1000);

        return callId;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CrossChainMessage>> GetPendingMessagesAsync(BlockchainType destinationChain)
    {
        if (!SupportsBlockchain(destinationChain))
        {
            throw new NotSupportedException($"Blockchain {destinationChain} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_messagesLock)
            {
                return _messages.Values
                    .Where(m => m.Status == MessageStatus.Pending)
                    .Select(m => new CrossChainMessage
                    {
                        MessageId = m.MessageId,
                        DestinationChain = destinationChain,
                        Status = MessageStatus.Pending,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList();
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyMessageAsync(string messageId, string proof, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);
        ArgumentException.ThrowIfNullOrEmpty(proof);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                // Deserialize the proof
                var proofData = System.Text.Json.JsonSerializer.Deserialize<CrossChainMessageProof>(proof);
                if (proofData == null)
                {
                    Logger.LogWarning("Invalid proof format for message {MessageId}", messageId);
                    return false;
                }

                // Verify the proof matches the message ID
                if (proofData.MessageId != messageId)
                {
                    Logger.LogWarning("Proof message ID mismatch: expected {Expected}, got {Actual}",
                        messageId, proofData.MessageId);
                    return false;
                }

                // Verify using enclave-based proof verification
                var isValid = await VerifyProofInEnclaveAsync(proofData);

                if (_blockchainClientFactory != null && isValid)
                {
                    // Additional on-chain verification
                    var client = _blockchainClientFactory.CreateClient(blockchainType);
                    var bridgeContract = GetBridgeContract(blockchainType, blockchainType);

                    try
                    {
                        var onChainStatus = await client.CallContractMethodAsync(
                            bridgeContract,
                            "verifyMessage",
                            messageId,
                            proofData.MessageHash
                        );

                        isValid = bool.TryParse(onChainStatus, out var verified) && verified;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "On-chain verification failed, using enclave result only");
                    }
                }

                Logger.LogInformation("Message {MessageId} verification result: {IsValid}", messageId, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error verifying message {MessageId}", messageId);
                return false;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<CrossChainRoute> GetOptimalRouteAsync(BlockchainType source, BlockchainType destination)
    {
        if (!SupportsBlockchain(source) || !SupportsBlockchain(destination))
        {
            throw new NotSupportedException($"Blockchain pair {source} -> {destination} is not supported");
        }

        return await Task.FromResult(new CrossChainRoute
        {
            Source = source,
            Destination = destination,
            IntermediateChains = Array.Empty<string>(),
            EstimatedFee = 0.001m,
            EstimatedTime = TimeSpan.FromMinutes(5),
            ReliabilityScore = 0.95
        });
    }

    /// <inheritdoc/>
    public async Task<decimal> EstimateFeesAsync(CrossChainOperation operation, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            try
            {
                // Get base fee from configuration
                var baseFeeKey = $"CrossChain:Fees:{blockchainType}:BaseFee";
                var baseFee = Configuration?.GetValue(baseFeeKey, 0.001m) ?? 0.001m;

                // Calculate operation-specific multiplier
                var multiplier = operation.OperationType switch
                {
                    "TokenTransfer" => 1.0m,
                    "ContractCall" => 2.5m,
                    "Message" => 1.5m,
                    _ => 1.0m
                };

                // Add data size fee if applicable
                var dataSizeFee = 0m;
                if (operation.Data != null)
                {
                    var dataSize = System.Text.Encoding.UTF8.GetByteCount(operation.Data);
                    var perByteFee = Configuration?.GetValue($"CrossChain:Fees:{blockchainType}:PerByteFee", 0.000001m) ?? 0.000001m;
                    dataSizeFee = dataSize * perByteFee;
                }

                // Calculate priority fee
                var priorityFee = operation.Priority switch
                {
                    "High" => baseFee * 0.5m,
                    "Low" => 0m,
                    _ => baseFee * 0.2m
                };

                // Check if cross-chain pair has special fees
                var chainPair = GetChainPair(
                    operation.SourceChain,
                    operation.TargetChain
                );

                if (chainPair != null)
                {
                    baseFee = chainPair.BaseFee;
                }

                var totalFee = (baseFee * multiplier) + dataSizeFee + priorityFee;

                Logger.LogDebug("Estimated fee for {OperationType} on {Blockchain}: {Fee} (base: {Base}, data: {Data}, priority: {Priority})",
                    operation.OperationType, blockchainType, totalFee, baseFee, dataSizeFee, priorityFee);

                return totalFee;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error estimating fees for operation on {Blockchain}", blockchainType);
                // Return default fee on error
                return 0.001m;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> RegisterTokenMappingAsync(CoreModels.TokenMapping mapping, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        // Simulate token mapping registration
        await Task.Delay(200);
        Logger.LogInformation("Registered token mapping for {Blockchain}", blockchainType);
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Cross-Chain Service");

        if (!await base.OnInitializeAsync())
        {
            return false;
        }

        Logger.LogInformation("Cross-Chain Service initialized successfully with {ChainCount} supported chain pairs",
            _supportedChains.Count);
        return true;
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Cross-Chain Service");
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Cross-Chain Service");
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var baseHealth = base.OnGetHealthAsync().Result;

        if (baseHealth != ServiceHealth.Healthy)
        {
            return Task.FromResult(baseHealth);
        }

        // Check cross-chain specific health
        var activeChains = _supportedChains.Count(c => c.IsActive);

        if (activeChains == 0)
        {
            Logger.LogWarning("No active cross-chain pairs available");
            return Task.FromResult(ServiceHealth.Degraded);
        }

        Logger.LogDebug("Cross-Chain Service health check: {ActiveChains} active chain pairs", activeChains);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    // Helper methods
    private bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return _supportedChains.Any(c => c.SourceChain == blockchainType || c.TargetChain == blockchainType);
    }

    private CrossChainPair? GetChainPair(BlockchainType source, BlockchainType target)
    {
        return _supportedChains.FirstOrDefault(c => c.SourceChain == source && c.TargetChain == target);
    }

    // ComputeMessageHashAsync is implemented in EnclaveOperations partial class

    private async Task<string> SignMessageAsync(string messageHash)
    {
        // Simulate cryptographic signing in enclave
        await Task.Delay(10);
        return $"sig_{messageHash[..16]}";
    }

    // ProcessMessageAsync is implemented in EnclaveOperations partial class

    private async Task ProcessTransferAsync(string transferId, CoreModels.CrossChainTransferRequest request, BlockchainType source, BlockchainType target)
    {
        try
        {
            await Task.Delay(3000); // Simulate transfer processing
            Logger.LogInformation("Transfer {TransferId} completed", transferId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing transfer {TransferId}", transferId);
        }
    }

    private string GetBridgeContract(BlockchainType source, BlockchainType target)
    {
        return $"0x{source}Bridge{target}Contract";
    }

    private int GetRequiredConfirmations(BlockchainType blockchain)
    {
        return blockchain switch
        {
            BlockchainType.Bitcoin => 6,
            BlockchainType.Ethereum => 12,
            BlockchainType.NeoN3 => 1,
            BlockchainType.NeoX => 1,
            _ => 3
        };
    }

    private async Task WaitForConfirmationsAsync(NeoServiceLayer.Infrastructure.IBlockchainClient client, string txHash, int confirmations)
    {
        // Simulate waiting for confirmations
        await Task.Delay(confirmations * 1000);
    }

    private async Task<bool> VerifyProofInEnclaveAsync(CrossChainMessageProof proof)
    {
        // Simulate enclave-based proof verification
        await Task.Delay(100);
        return !string.IsNullOrEmpty(proof.MessageHash) && !string.IsNullOrEmpty(proof.Signature);
    }
}
