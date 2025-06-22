using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.CrossChain.Models;
using CoreModels = NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.CrossChain;

/// <summary>
/// Implementation of the Cross-Chain Service that provides cross-chain interoperability and messaging capabilities.
/// </summary>
public partial class CrossChainService : CryptographicServiceBase, ICrossChainService
{
    private readonly Dictionary<string, CoreModels.CrossChainMessageStatus> _messages = new();
    private readonly Dictionary<string, List<CrossChainTransaction>> _transactionHistory = new();
    private readonly object _messagesLock = new();
    private readonly List<CrossChainPair> _supportedChains;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChainService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    public CrossChainService(ILogger<CrossChainService> logger, IServiceConfiguration? configuration = null)
        : base("CrossChainService", "Cross-chain interoperability and messaging service", "1.0.0", logger, configuration)
    {
        Configuration = configuration;

        // Initialize supported blockchain pairs
        _supportedChains = new List<CrossChainPair>
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

        AddCapability<ICrossChainService>();
        AddDependency(new ServiceDependency("KeyManagementService", true, "1.0.0"));
        AddDependency(new ServiceDependency("EventSubscriptionService", true, "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected new IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    public async Task<string> SendMessageAsync(CoreModels.CrossChainMessageRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
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
            var messageStatus = new CoreModels.CrossChainMessageStatus
            {
                MessageId = messageId,
                Status = CoreModels.MessageStatus.Pending,
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
    public async Task<CoreModels.CrossChainMessageStatus> GetMessageStatusAsync(string messageId, BlockchainType blockchainType)
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
                FromAddress = request.Sender,
                ToAddress = request.Receiver,
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
                if (!_transactionHistory.ContainsKey(request.Sender))
                {
                    _transactionHistory[request.Sender] = new List<CrossChainTransaction>();
                }
                _transactionHistory[request.Sender].Add(transaction);
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

                // Simulate contract execution
                await Task.Delay(1000); // Simulate execution time

                var result = new CrossChainExecutionResult
                {
                    ExecutionId = executionId,
                    Success = true,
                    Result = $"Contract {request.TargetContract}.{request.Method} executed successfully",
                    TransactionHash = Guid.NewGuid().ToString(),
                    GasUsed = Random.Shared.Next(21000, 100000),
                    ExecutedAt = DateTime.UtcNow
                };

                Logger.LogInformation("Cross-chain contract call {ExecutionId} completed successfully", executionId);
                return result;
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
    public async Task<bool> VerifyMessageProofAsync(CoreModels.CrossChainMessageProof proof, BlockchainType blockchainType)
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
    public async Task<IEnumerable<CoreModels.SupportedChain>> GetSupportedChainsAsync()
    {
        var supportedChains = _supportedChains.Select(pair => new CoreModels.SupportedChain
        {
            ChainId = pair.SourceChain.ToString(),
            Name = pair.SourceChain.ToString(),
            ChainType = pair.SourceChain,
            IsActive = pair.IsEnabled,
            SupportedTokens = new[] { "GAS", "NEO", "USDT" }
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
    public async Task<IEnumerable<CoreModels.CrossChainMessage>> GetPendingMessagesAsync(BlockchainType destinationChain)
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
                    .Where(m => m.Status == CoreModels.MessageStatus.Pending)
                    .Select(m => new CoreModels.CrossChainMessage
                    {
                        MessageId = m.MessageId,
                        DestinationChain = destinationChain,
                        Status = CoreModels.MessageStatus.Pending,
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

        // Simulate message verification
        await Task.Delay(500);
        return true;
    }

    /// <inheritdoc/>
    public async Task<CoreModels.CrossChainRoute> GetOptimalRouteAsync(BlockchainType source, BlockchainType destination)
    {
        if (!SupportsBlockchain(source) || !SupportsBlockchain(destination))
        {
            throw new NotSupportedException($"Blockchain pair {source} -> {destination} is not supported");
        }

        return await Task.FromResult(new CoreModels.CrossChainRoute
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
    public async Task<decimal> EstimateFeesAsync(CoreModels.CrossChainOperation operation, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        // Simulate fee estimation
        await Task.Delay(100);
        return 0.001m;
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
}
