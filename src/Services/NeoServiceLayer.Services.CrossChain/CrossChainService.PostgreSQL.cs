using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.Services.CrossChain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NeoServiceLayer.Services.CrossChain;

public partial class CrossChainService
{
    private ICrossChainRepository? _crossChainRepository;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes PostgreSQL storage for the cross-chain service.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    public void InitializePostgreSQLStorage(IServiceProvider serviceProvider)
    {
        try
        {
            _serviceProvider = serviceProvider;
            _crossChainRepository = serviceProvider.GetService<ICrossChainRepository>();

            if (_crossChainRepository != null)
            {
                Logger.LogInformation("PostgreSQL storage initialized for CrossChainService");
                
                // Load persisted data from PostgreSQL
                _ = Task.Run(async () => await LoadPersistedDataFromPostgreSQLAsync());
            }
            else
            {
                Logger.LogWarning("PostgreSQL repository not available for CrossChainService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PostgreSQL storage for CrossChainService");
        }
    }

    /// <summary>
    /// Loads persisted cross-chain data from PostgreSQL storage.
    /// </summary>
    private async Task LoadPersistedDataFromPostgreSQLAsync()
    {
        if (_crossChainRepository == null) return;

        try
        {
            Logger.LogInformation("Loading cross-chain data from PostgreSQL storage");

            // Load recent cross-chain operations
            await LoadCrossChainOperationsFromPostgreSQLAsync();

            // Load supported chain configurations
            await LoadChainConfigurationsFromPostgreSQLAsync();

            Logger.LogInformation("Cross-chain data loaded from PostgreSQL storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load cross-chain data from PostgreSQL storage");
        }
    }

    /// <summary>
    /// Loads cross-chain operations from PostgreSQL.
    /// </summary>
    private async Task LoadCrossChainOperationsFromPostgreSQLAsync()
    {
        if (_crossChainRepository == null) return;

        try
        {
            // Load recent operations (last 24 hours)
            var recentOperations = await _crossChainRepository.GetRecentAsync(TimeSpan.FromHours(24));

            foreach (var operation in recentOperations)
            {
                try
                {
                    // Convert PostgreSQL entity to service model
                    var messageStatus = new CrossChainMessageStatus
                    {
                        MessageId = operation.OperationId,
                        Status = Enum.TryParse<MessageStatus>(operation.Status, out var status) ? status : MessageStatus.Pending,
                        CreatedAt = operation.CreatedAt
                    };

                    lock (_messagesLock)
                    {
                        _messages[operation.OperationId] = messageStatus;
                    }

                    // If operation has transaction data, add to transaction history
                    if (!string.IsNullOrEmpty(operation.Metadata))
                    {
                        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(operation.Metadata) ?? new Dictionary<string, object>();
                        
                        if (metadata.TryGetValue("transaction_data", out var transactionDataObj) && transactionDataObj is JsonElement transactionElement)
                        {
                            var transaction = JsonSerializer.Deserialize<CrossChainTransaction>(transactionElement.GetRawText());
                            if (transaction != null)
                            {
                                var fromAddress = transaction.FromAddress ?? "unknown";
                                lock (_messagesLock)
                                {
                                    if (!_transactionHistory.ContainsKey(fromAddress))
                                    {
                                        _transactionHistory[fromAddress] = new List<CrossChainTransaction>();
                                    }
                                    _transactionHistory[fromAddress].Add(transaction);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to load cross-chain operation {OperationId}", operation.Id);
                }
            }

            Logger.LogInformation("Loaded {OperationCount} cross-chain operations from PostgreSQL", recentOperations.Count());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load cross-chain operations from PostgreSQL");
            throw;
        }
    }

    /// <summary>
    /// Loads chain configurations from PostgreSQL.
    /// </summary>
    private async Task LoadChainConfigurationsFromPostgreSQLAsync()
    {
        if (_crossChainRepository == null) return;

        try
        {
            // In a real implementation, chain configurations might be stored separately
            // For now, we'll check if any operations exist for supported chains
            var operations = await _crossChainRepository.GetAllAsync();
            var usedChains = operations
                .SelectMany(o => new[] { o.SourceChain, o.TargetChain })
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            foreach (var chainName in usedChains)
            {
                if (Enum.TryParse<Infrastructure.Blockchain.BlockchainType>(chainName, out var chainType))
                {
                    // Ensure the chain is in our supported chains list
                    if (!_supportedChains.Any(c => c.SourceChain == chainType || c.TargetChain == chainType))
                    {
                        Logger.LogInformation("Found historical usage of chain {ChainType}, but not in current configuration", chainType);
                    }
                }
            }

            Logger.LogInformation("Loaded chain configuration data from PostgreSQL");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load chain configurations from PostgreSQL");
            throw;
        }
    }

    /// <summary>
    /// Persists cross-chain operation to PostgreSQL.
    /// </summary>
    private async Task PersistCrossChainOperationAsync(string operationId, string operationType, Infrastructure.Blockchain.BlockchainType sourceChain, Infrastructure.Blockchain.BlockchainType targetChain, string status, object? additionalData = null)
    {
        if (_crossChainRepository == null) return;

        try
        {
            var metadata = new Dictionary<string, object>();
            if (additionalData != null)
            {
                metadata["operation_data"] = additionalData;
            }

            var operation = new Infrastructure.Persistence.PostgreSQL.Entities.CrossChainEntities.CrossChainOperation
            {
                Id = Guid.NewGuid(),
                OperationId = operationId,
                OperationType = operationType,
                SourceChain = sourceChain.ToString(),
                TargetChain = targetChain.ToString(),
                Status = status,
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _crossChainRepository.CreateAsync(operation);

            Logger.LogDebug("Persisted cross-chain operation {OperationId} to PostgreSQL", operationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist cross-chain operation {OperationId} to PostgreSQL", operationId);
        }
    }

    /// <summary>
    /// Updates cross-chain operation status in PostgreSQL.
    /// </summary>
    private async Task UpdateCrossChainOperationStatusAsync(string operationId, string status)
    {
        if (_crossChainRepository == null) return;

        try
        {
            var operations = await _crossChainRepository.GetByOperationIdAsync(operationId);
            var operation = operations.FirstOrDefault();

            if (operation != null)
            {
                operation.Status = status;
                operation.UpdatedAt = DateTime.UtcNow;

                await _crossChainRepository.UpdateAsync(operation);

                Logger.LogDebug("Updated cross-chain operation {OperationId} status to {Status} in PostgreSQL", operationId, status);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update cross-chain operation {OperationId} status in PostgreSQL", operationId);
        }
    }

    /// <summary>
    /// Persists cross-chain transaction to PostgreSQL.
    /// </summary>
    private async Task PersistCrossChainTransactionAsync(CrossChainTransaction transaction)
    {
        if (_crossChainRepository == null) return;

        try
        {
            var metadata = new Dictionary<string, object>
            {
                ["transaction_data"] = transaction
            };

            await PersistCrossChainOperationAsync(
                transaction.Id,
                "TokenTransfer",
                transaction.SourceChain,
                transaction.TargetChain,
                transaction.Status.ToString(),
                metadata
            );

            Logger.LogDebug("Persisted cross-chain transaction {TransactionId} to PostgreSQL", transaction.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist cross-chain transaction {TransactionId} to PostgreSQL", transaction.Id);
        }
    }

    /// <summary>
    /// Gets cross-chain operation history from PostgreSQL.
    /// </summary>
    public async Task<IEnumerable<Infrastructure.Persistence.PostgreSQL.Entities.CrossChainEntities.CrossChainOperation>> GetOperationHistoryAsync(string? operationType = null, Infrastructure.Blockchain.BlockchainType? sourceChain = null, Infrastructure.Blockchain.BlockchainType? targetChain = null, int limit = 100)
    {
        if (_crossChainRepository == null)
        {
            return Enumerable.Empty<Infrastructure.Persistence.PostgreSQL.Entities.CrossChainEntities.CrossChainOperation>();
        }

        try
        {
            var operations = await _crossChainRepository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(operationType))
            {
                operations = operations.Where(o => o.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase));
            }

            if (sourceChain.HasValue)
            {
                operations = operations.Where(o => o.SourceChain.Equals(sourceChain.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (targetChain.HasValue)
            {
                operations = operations.Where(o => o.TargetChain.Equals(targetChain.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            return operations.OrderByDescending(o => o.CreatedAt).Take(limit);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get operation history from PostgreSQL");
            return Enumerable.Empty<Infrastructure.Persistence.PostgreSQL.Entities.CrossChainEntities.CrossChainOperation>();
        }
    }

    /// <summary>
    /// Gets cross-chain operation statistics from PostgreSQL.
    /// </summary>
    public async Task<Dictionary<string, object>> GetOperationStatisticsAsync(TimeSpan? timeRange = null)
    {
        if (_crossChainRepository == null)
        {
            return new Dictionary<string, object>();
        }

        try
        {
            var cutoffTime = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.UtcNow.AddDays(-30);
            var recentOperations = await _crossChainRepository.GetRecentAsync(timeRange ?? TimeSpan.FromDays(30));

            var statistics = new Dictionary<string, object>
            {
                ["total_operations"] = recentOperations.Count(),
                ["operations_by_type"] = recentOperations.GroupBy(o => o.OperationType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["operations_by_status"] = recentOperations.GroupBy(o => o.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["operations_by_source_chain"] = recentOperations.GroupBy(o => o.SourceChain)
                    .ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
                ["operations_by_target_chain"] = recentOperations.GroupBy(o => o.TargetChain)
                    .ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
                ["time_range"] = timeRange?.ToString() ?? "30 days",
                ["generated_at"] = DateTime.UtcNow
            };

            return statistics;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get operation statistics from PostgreSQL");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["generated_at"] = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Enhanced SendMessageAsync with PostgreSQL persistence.
    /// </summary>
    public async Task<string> SendMessageWithPersistenceAsync(ServiceCrossChainMessageRequest request, Infrastructure.Blockchain.BlockchainType sourceBlockchain, Infrastructure.Blockchain.BlockchainType targetBlockchain)
    {
        var messageId = await SendMessageAsync(request, sourceBlockchain, targetBlockchain);

        // Persist to PostgreSQL
        await PersistCrossChainOperationAsync(
            messageId,
            "Message",
            sourceBlockchain,
            targetBlockchain,
            "Pending",
            new
            {
                request_data = request,
                message_type = "CrossChainMessage"
            }
        );

        return messageId;
    }

    /// <summary>
    /// Enhanced TransferTokensAsync with PostgreSQL persistence.
    /// </summary>
    public async Task<string> TransferTokensWithPersistenceAsync(Core.Models.CrossChainTransferRequest request, Infrastructure.Blockchain.BlockchainType sourceBlockchain, Infrastructure.Blockchain.BlockchainType targetBlockchain)
    {
        var transferId = await TransferTokensAsync(request, sourceBlockchain, targetBlockchain);

        // Create transaction record
        var transaction = new CrossChainTransaction
        {
            Id = transferId,
            FromAddress = "sender", // Would come from authenticated user context
            ToAddress = request.DestinationAddress,
            SourceChain = sourceBlockchain,
            TargetChain = targetBlockchain,
            Type = CrossChainTransactionType.TokenTransfer,
            Amount = request.Amount,
            TokenContract = request.TokenAddress,
            Status = CrossChainMessageState.Created,
            CreatedAt = DateTime.UtcNow
        };

        // Persist to PostgreSQL
        await PersistCrossChainTransactionAsync(transaction);

        return transferId;
    }

    /// <summary>
    /// Updates message status in both memory and PostgreSQL.
    /// </summary>
    private async Task UpdateMessageStatusWithPersistenceAsync(string messageId, MessageStatus status)
    {
        // Update in memory
        lock (_messagesLock)
        {
            if (_messages.TryGetValue(messageId, out var messageStatus))
            {
                messageStatus.Status = status;
            }
        }

        // Update in PostgreSQL
        await UpdateCrossChainOperationStatusAsync(messageId, status.ToString());
    }

    /// <summary>
    /// Enhanced ProcessMessageAsync with PostgreSQL updates.
    /// </summary>
    private async Task ProcessMessageWithPersistenceAsync(string messageId, ServiceCrossChainMessageRequest request, Infrastructure.Blockchain.BlockchainType sourceBlockchain, Infrastructure.Blockchain.BlockchainType targetBlockchain)
    {
        try
        {
            await UpdateMessageStatusWithPersistenceAsync(messageId, MessageStatus.Processing);

            // Simulate message processing
            await Task.Delay(2000);

            // Simulate success/failure
            var success = Random.Shared.Next(100) < 90; // 90% success rate
            var finalStatus = success ? MessageStatus.Completed : MessageStatus.Failed;

            await UpdateMessageStatusWithPersistenceAsync(messageId, finalStatus);

            Logger.LogInformation("Cross-chain message {MessageId} processed with status {Status}", messageId, finalStatus);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing cross-chain message {MessageId}", messageId);
            await UpdateMessageStatusWithPersistenceAsync(messageId, MessageStatus.Failed);
        }
    }

    /// <summary>
    /// Cleanup old cross-chain operations from PostgreSQL.
    /// </summary>
    public async Task CleanupOldOperationsAsync(TimeSpan maxAge)
    {
        if (_crossChainRepository == null) return;

        try
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            await _crossChainRepository.DeleteOlderThanAsync(cutoffDate);

            Logger.LogInformation("Cleaned up cross-chain operations older than {CutoffDate}", cutoffDate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup old cross-chain operations from PostgreSQL");
        }
    }
}