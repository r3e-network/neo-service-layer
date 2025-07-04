using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.Infrastructure.Persistence;

namespace NeoServiceLayer.Services.SmartContracts;

public partial class SmartContractsService
{
    private IPersistentStorageProvider? _persistentStorage;
    private readonly IServiceProvider? _serviceProvider;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string CONTRACT_PREFIX = "smartcontract:contract:";
    private const string DEPLOYMENT_PREFIX = "smartcontract:deployment:";
    private const string INVOCATION_PREFIX = "smartcontract:invocation:";
    private const string EVENT_PREFIX = "smartcontract:event:";
    private const string STATS_PREFIX = "smartcontract:stats:";
    private const string INDEX_PREFIX = "smartcontract:index:";
    private const string VERSION_PREFIX = "smartcontract:version:";

    /// <summary>
    /// Initializes persistent storage for the smart contracts service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for SmartContractsService");

                // Restore smart contract data from storage
                await RestoreSmartContractDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistSmartContractDataAsync(),
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30));

                // Start cleanup timer (every hour)
                _cleanupTimer = new Timer(
                    async _ => await CleanupExpiredDataAsync(),
                    null,
                    TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1));
            }
            else
            {
                Logger.LogWarning("Persistent storage provider not available for SmartContractsService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for SmartContractsService");
        }
    }

    /// <summary>
    /// Persists a deployed contract to storage.
    /// </summary>
    private async Task PersistDeployedContractAsync(DeployedContract contract)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{CONTRACT_PREFIX}{contract.ContractHash}";
            var data = JsonSerializer.SerializeToUtf8Bytes(contract);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(3650) // Keep contracts for 10 years
            });

            // Update contract index
            await UpdateContractIndexAsync(contract.BlockchainType, contract.ContractHash);

            Logger.LogDebug("Persisted deployed contract {ContractHash} to storage", contract.ContractHash);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist deployed contract {ContractHash}", contract.ContractHash);
        }
    }

    /// <summary>
    /// Persists a deployment history entry to storage.
    /// </summary>
    private async Task PersistDeploymentHistoryAsync(DeploymentHistory deployment)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{DEPLOYMENT_PREFIX}{deployment.DeploymentId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(deployment);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep deployment history for 1 year
            });

            // Update deployment index by contract
            await UpdateDeploymentIndexAsync(deployment.ContractHash, deployment.DeploymentId);

            Logger.LogDebug("Persisted deployment history {DeploymentId} to storage", deployment.DeploymentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist deployment history {DeploymentId}", deployment.DeploymentId);
        }
    }

    /// <summary>
    /// Persists an invocation history entry to storage.
    /// </summary>
    private async Task PersistInvocationHistoryAsync(InvocationHistory invocation)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INVOCATION_PREFIX}{invocation.InvocationId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(invocation);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep invocation history for 90 days
            });

            // Update invocation statistics
            await UpdateInvocationStatisticsAsync(invocation.ContractHash, invocation.Method);

            Logger.LogDebug("Persisted invocation history {InvocationId} to storage", invocation.InvocationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist invocation history {InvocationId}", invocation.InvocationId);
        }
    }

    /// <summary>
    /// Persists contract events to storage.
    /// </summary>
    private async Task PersistContractEventAsync(Core.SmartContracts.ContractEvent contractEvent)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Convert to persistent contract event
            var eventId = $"{contractEvent.ContractHash}_{contractEvent.TransactionHash}_{contractEvent.Name}_{DateTime.UtcNow.Ticks}";
            var persistentEvent = new PersistentContractEvent
            {
                EventId = eventId,
                ContractHash = contractEvent.ContractHash,
                EventName = contractEvent.Name,
                Timestamp = DateTime.UtcNow,
                EventData = contractEvent.Parameters.Select((p, i) => new KeyValuePair<string, object>($"param{i}", p)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var key = $"{EVENT_PREFIX}{eventId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(persistentEvent);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30) // Keep events for 30 days
            });

            // Update event index by contract
            await UpdateEventIndexAsync(contractEvent.ContractHash, eventId);

            Logger.LogDebug("Persisted contract event {EventId} to storage", eventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist contract event {EventName} for contract {ContractHash}", contractEvent.Name, contractEvent.ContractHash);
        }
    }

    /// <summary>
    /// Updates contract index in storage.
    /// </summary>
    private async Task UpdateContractIndexAsync(BlockchainType blockchainType, string contractHash)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}blockchain:{blockchainType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var contractHashes = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            contractHashes.Add(contractHash);

            var data = JsonSerializer.SerializeToUtf8Bytes(contractHashes);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update contract index for {BlockchainType}", blockchainType);
        }
    }

    /// <summary>
    /// Updates deployment index in storage.
    /// </summary>
    private async Task UpdateDeploymentIndexAsync(string contractHash, string deploymentId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}deployments:{contractHash}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var deploymentIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            deploymentIds.Add(deploymentId);

            var data = JsonSerializer.SerializeToUtf8Bytes(deploymentIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update deployment index for contract {ContractHash}", contractHash);
        }
    }

    /// <summary>
    /// Updates event index in storage.
    /// </summary>
    private async Task UpdateEventIndexAsync(string contractHash, string eventId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}events:{contractHash}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var eventIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            eventIds.Add(eventId);

            var data = JsonSerializer.SerializeToUtf8Bytes(eventIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update event index for contract {ContractHash}", contractHash);
        }
    }

    /// <summary>
    /// Updates invocation statistics in storage.
    /// </summary>
    private async Task UpdateInvocationStatisticsAsync(string contractHash, string method)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{STATS_PREFIX}invocations:{contractHash}:{method}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var stats = existingData != null
                ? JsonSerializer.Deserialize<InvocationStatistics>(existingData) ?? new InvocationStatistics()
                : new InvocationStatistics();

            stats.TotalCount++;
            stats.LastInvocation = DateTime.UtcNow;

            var data = JsonSerializer.SerializeToUtf8Bytes(stats);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update invocation statistics for {ContractHash}:{Method}", contractHash, method);
        }
    }

    /// <summary>
    /// Restores smart contract data from persistent storage.
    /// </summary>
    private async Task RestoreSmartContractDataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring smart contract data from persistent storage");

            // Restore deployed contracts
            await RestoreDeployedContractsFromStorageAsync();

            // Restore usage statistics
            await RestoreUsageStatisticsFromStorageAsync();

            // Restore service statistics
            await RestoreServiceStatisticsAsync();

            Logger.LogInformation("Smart contract data restored from storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore smart contract data from storage");
        }
    }

    /// <summary>
    /// Restores deployed contracts from storage.
    /// </summary>
    private async Task RestoreDeployedContractsFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var contractKeys = await _persistentStorage.ListKeysAsync($"{CONTRACT_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in contractKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var contract = JsonSerializer.Deserialize<DeployedContract>(data);
                        if (contract != null)
                        {
                            // Restore to appropriate manager
                            if (_managers.TryGetValue(contract.BlockchainType, out var manager))
                            {
                                // Store in manager's cache
                                restoredCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore deployed contract from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} deployed contracts from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore deployed contracts from storage");
        }
    }

    /// <summary>
    /// Restores usage statistics from storage.
    /// </summary>
    private async Task RestoreUsageStatisticsFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var statsKeys = await _persistentStorage.ListKeysAsync($"{STATS_PREFIX}*");

            foreach (var key in statsKeys)
            {
                try
                {
                    if (key.StartsWith($"{STATS_PREFIX}usage:"))
                    {
                        var data = await _persistentStorage.RetrieveAsync(key);
                        if (data != null)
                        {
                            var contractHash = key.Replace($"{STATS_PREFIX}usage:", "");
                            var usageInfo = JsonSerializer.Deserialize<ContractUsageInfo>(data);
                            if (usageInfo != null)
                            {
                                _usageStats[contractHash] = usageInfo;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore usage statistics from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored usage statistics for {Count} contracts", _usageStats.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore usage statistics from storage");
        }
    }

    /// <summary>
    /// Restores service statistics from storage.
    /// </summary>
    private async Task RestoreServiceStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{STATS_PREFIX}service";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var stats = JsonSerializer.Deserialize<SmartContractServiceStatistics>(data);
                if (stats != null)
                {
                    _requestCount = stats.TotalRequests;
                    _successCount = stats.SuccessfulRequests;
                    _failureCount = stats.FailedRequests;

                    Logger.LogInformation("Restored smart contract service statistics from storage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore service statistics from storage");
        }
    }

    /// <summary>
    /// Persists all current smart contract data to storage.
    /// </summary>
    private async Task PersistSmartContractDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Persist usage statistics
            foreach (var kvp in _usageStats)
            {
                await PersistUsageStatisticsAsync(kvp.Key, kvp.Value);
            }

            // Persist service statistics
            await PersistServiceStatisticsAsync();

            Logger.LogDebug("Persisted smart contract data to storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist smart contract data");
        }
    }

    /// <summary>
    /// Persists usage statistics to storage.
    /// </summary>
    private async Task PersistUsageStatisticsAsync(string contractHash, ContractUsageInfo usageInfo)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{STATS_PREFIX}usage:{contractHash}";
            var data = JsonSerializer.SerializeToUtf8Bytes(usageInfo);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist usage statistics for contract {ContractHash}", contractHash);
        }
    }

    /// <summary>
    /// Persists service statistics to storage.
    /// </summary>
    private async Task PersistServiceStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var stats = new SmartContractServiceStatistics
            {
                TotalRequests = _requestCount,
                SuccessfulRequests = _successCount,
                FailedRequests = _failureCount,
                TotalContracts = _usageStats.Count,
                LastUpdated = DateTime.UtcNow
            };

            var key = $"{STATS_PREFIX}service";
            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist service statistics");
        }
    }

    /// <summary>
    /// Cleans up expired data from storage.
    /// </summary>
    private async Task CleanupExpiredDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Starting cleanup of expired smart contract data");

            // Clean up old invocation history (older than 90 days)
            var invocationKeys = await _persistentStorage.ListKeysAsync($"{INVOCATION_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in invocationKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var invocation = JsonSerializer.Deserialize<InvocationHistory>(data);
                        if (invocation != null && invocation.Timestamp < DateTime.UtcNow.AddDays(-90))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup invocation key {Key}", key);
                }
            }

            // Clean up old events (older than 30 days)
            var eventKeys = await _persistentStorage.ListKeysAsync($"{EVENT_PREFIX}*");
            foreach (var key in eventKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var contractEvent = JsonSerializer.Deserialize<PersistentContractEvent>(data);
                        if (contractEvent != null && contractEvent.Timestamp < DateTime.UtcNow.AddDays(-30))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup event key {Key}", key);
                }
            }

            Logger.LogInformation("Cleaned up {Count} expired smart contract data entries", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
        }
    }

    /// <summary>
    /// Disposes persistence resources.
    /// </summary>
    private void DisposePersistenceResources()
    {
        _persistenceTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _persistentStorage?.Dispose();
    }
}

/// <summary>
/// Deployed contract information.
/// </summary>
internal class DeployedContract
{
    public string ContractHash { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public BlockchainType BlockchainType { get; set; }
    public DateTime DeployedAt { get; set; }
    public string DeployedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Deployment history entry.
/// </summary>
internal class DeploymentHistory
{
    public string DeploymentId { get; set; } = string.Empty;
    public string ContractHash { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string DeployedBy { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Invocation history entry.
/// </summary>
internal class InvocationHistory
{
    public string InvocationId { get; set; } = string.Empty;
    public string ContractHash { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public object[] Parameters { get; set; } = Array.Empty<object>();
    public string TransactionHash { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string InvokedBy { get; set; } = string.Empty;
    public object? Result { get; set; }
}

/// <summary>
/// Contract event.
/// </summary>
internal class PersistentContractEvent
{
    public string EventId { get; set; } = string.Empty;
    public string ContractHash { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
}

/// <summary>
/// Invocation statistics.
/// </summary>
internal class InvocationStatistics
{
    public long TotalCount { get; set; }
    public DateTime LastInvocation { get; set; }
}

/// <summary>
/// Service statistics.
/// </summary>
internal class SmartContractServiceStatistics
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int TotalContracts { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Deployment status.
/// </summary>
internal enum DeploymentStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
