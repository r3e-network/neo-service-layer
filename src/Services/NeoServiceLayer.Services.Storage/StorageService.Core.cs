using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Storage.Models;
using ServiceFrameworkConfig = NeoServiceLayer.ServiceFramework.IServiceConfiguration;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Core implementation of the Storage service.
/// </summary>
public partial class StorageService : ServiceFramework.EnclaveBlockchainServiceBase, IStorageService, IDisposable
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly ServiceFrameworkConfig _configuration;
    private readonly Dictionary<string, StorageMetadata> _metadataCache = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;
    private bool _isRunning;

    /// <summary>
    /// Gets a value indicating whether the service is running.
    /// </summary>
    public bool IsRunning => _isRunning;

    // LoggerMessage delegates for performance optimization
    private static readonly Action<ILogger, Exception?> _persistentMetadataStorageNotAvailable =
        LoggerMessage.Define(LogLevel.Warning, new EventId(9001, "PersistentMetadataStorageNotAvailable"),
            "Persistent metadata storage not available, using in-memory cache only");

    private static readonly Action<ILogger, Exception?> _initializingPersistentMetadataStorage =
        LoggerMessage.Define(LogLevel.Information, new EventId(9002, "InitializingPersistentMetadataStorage"),
            "Initializing persistent metadata storage...");

    private static readonly Action<ILogger, Exception?> _persistentMetadataStorageInitialized =
        LoggerMessage.Define(LogLevel.Information, new EventId(9003, "PersistentMetadataStorageInitialized"),
            "Persistent metadata storage initialized successfully");

    private static readonly Action<ILogger, Exception> _persistentMetadataStorageInitializationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9004, "PersistentMetadataStorageInitializationFailed"),
            "Error initializing persistent metadata storage");

    private static readonly Action<ILogger, Exception?> _loadingMetadataFromPersistentStorage =
        LoggerMessage.Define(LogLevel.Information, new EventId(9005, "LoadingMetadataFromPersistentStorage"),
            "Loading metadata from persistent storage...");

    private static readonly Action<ILogger, int, Exception?> _metadataEntriesLoadedFromPersistentStorage =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(9006, "MetadataEntriesLoadedFromPersistentStorage"),
            "Loaded {Count} metadata entries from persistent storage");

    private static readonly Action<ILogger, Exception> _loadMetadataFromPersistentStorageFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9007, "LoadMetadataFromPersistentStorageFailed"),
            "Error loading metadata from persistent storage");

    private static readonly Action<ILogger, string, Exception> _persistMetadataFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9008, "PersistMetadataFailed"),
            "Error persisting metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception> _removePersistedMetadataFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9009, "RemovePersistedMetadataFailed"),
            "Error removing persisted metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception> _updateMetadataIndexesFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9010, "UpdateMetadataIndexesFailed"),
            "Error updating metadata indexes for key {Key}");

    private static readonly Action<ILogger, string, Exception> _removeMetadataIndexesFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9011, "RemoveMetadataIndexesFailed"),
            "Error removing metadata indexes for key {Key}");

    private static readonly Action<ILogger, string, Exception> _queryMetadataByOwnerFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9012, "QueryMetadataByOwnerFailed"),
            "Error querying metadata by owner {Owner}");

    private static readonly Action<ILogger, Exception> _persistStorageStatisticsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9013, "PersistStorageStatisticsFailed"),
            "Error persisting storage statistics");

    private static readonly Action<ILogger, Exception?> _performingMetadataMaintenance =
        LoggerMessage.Define(LogLevel.Information, new EventId(9014, "PerformingMetadataMaintenance"),
            "Performing metadata maintenance...");

    private static readonly Action<ILogger, string, Exception?> _metadataStorageValidationFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9015, "MetadataStorageValidationFailed"),
            "Metadata storage validation failed: {Errors}");

    private static readonly Action<ILogger, Exception?> _metadataMaintenanceCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(9016, "MetadataMaintenanceCompleted"),
            "Metadata maintenance completed successfully");

    private static readonly Action<ILogger, Exception> _metadataMaintenanceFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9017, "MetadataMaintenanceFailed"),
            "Error during metadata maintenance");

    private static readonly Action<ILogger, Exception?> _storageServiceInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(9018, "StorageServiceInitializing"),
            "Initializing Storage Service...");

    private static readonly Action<ILogger, Exception?> _storageServiceInitialized =
        LoggerMessage.Define(LogLevel.Information, new EventId(9019, "StorageServiceInitialized"),
            "Storage Service initialized successfully");

    private static readonly Action<ILogger, Exception> _storageServiceInitializationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9020, "StorageServiceInitializationFailed"),
            "Error initializing Storage Service");

    private static readonly Action<ILogger, Exception?> _storageServiceEnclaveInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(9021, "StorageServiceEnclaveInitializing"),
            "Initializing Storage Service enclave...");

    private static readonly Action<ILogger, Exception> _storageServiceEnclaveInitializationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9022, "StorageServiceEnclaveInitializationFailed"),
            "Error initializing Storage Service enclave.");

    private static readonly Action<ILogger, Exception?> _storageServiceStarting =
        LoggerMessage.Define(LogLevel.Information, new EventId(9023, "StorageServiceStarting"),
            "Starting Storage Service...");

    private static readonly Action<ILogger, Exception> _storageServiceStartFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9024, "StorageServiceStartFailed"),
            "Error starting Storage Service.");

    private static readonly Action<ILogger, Exception?> _storageServiceStopping =
        LoggerMessage.Define(LogLevel.Information, new EventId(9025, "StorageServiceStopping"),
            "Stopping Storage Service...");

    private static readonly Action<ILogger, Exception> _storageServiceStopFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9026, "StorageServiceStopFailed"),
            "Error stopping Storage Service.");

    private static readonly Action<ILogger, int, Exception?> _metadataCacheRefreshed =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(9027, "MetadataCacheRefreshed"),
            "Refreshed metadata cache with {Count} items");

    private static readonly Action<ILogger, Exception> _metadataCacheRefreshFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(9028, "MetadataCacheRefreshFailed"),
            "Failed to refresh metadata cache");

    private static readonly Action<ILogger, Exception> _healthCheckFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(9029, "HealthCheckFailed"),
            "Health check failed");

    private static readonly Action<ILogger, int, string, Exception?> _keysListedWithPrefix =
        LoggerMessage.Define<int, string>(LogLevel.Debug, new EventId(9030, "KeysListedWithPrefix"),
            "Listed {Count} keys with prefix {Prefix}");

    private static readonly Action<ILogger, string, Exception> _listKeysWithPrefixFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9031, "ListKeysWithPrefixFailed"),
            "Failed to list keys with prefix {Prefix}");

    // Data operations delegates
    private static readonly Action<ILogger, string, long, Exception> _dataStorageFailed =
        LoggerMessage.Define<string, long>(LogLevel.Error, new EventId(9032, "DataStorageFailed"),
            "Failed to store data for key {Key} (size: {Size} bytes)");

    private static readonly Action<ILogger, string, Exception?> _dataNotFoundWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9033, "DataNotFoundWarning"),
            "Data not found for key {Key}");

    private static readonly Action<ILogger, string, long, Exception?> _dataStoredSuccessfully =
        LoggerMessage.Define<string, long>(LogLevel.Information, new EventId(9034, "DataStoredSuccessfully"),
            "Stored data for key {Key} (size: {Size} bytes)");

    private static readonly Action<ILogger, string, Exception?> _dataNotFoundForRetrieval =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9035, "DataNotFoundForRetrieval"),
            "Data not found for key {Key} during retrieval");

    private static readonly Action<ILogger, string, Exception?> _dataRetrievedSuccessfully =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9036, "DataRetrievedSuccessfully"),
            "Retrieved data for key {Key}");

    private static readonly Action<ILogger, string, Exception> _dataRetrievalFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9037, "DataRetrievalFailed"),
            "Failed to retrieve data for key {Key}");

    // Metadata operations delegates
    private static readonly Action<ILogger, string, Exception> _getMetadataFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9038, "GetMetadataFailed"),
            "Failed to get metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception> _updateMetadataFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9039, "UpdateMetadataFailed"),
            "Failed to update metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception> _deleteMetadataFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9040, "DeleteMetadataFailed"),
            "Failed to delete metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception?> _metadataNotFoundWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9041, "MetadataNotFoundWarning"),
            "Metadata not found for key {Key}");

    private static readonly Action<ILogger, string, Exception?> _gettingMetadataForKey =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9042, "GettingMetadataForKey"),
            "Getting metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception> _getMetadataForKeyFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9043, "GetMetadataForKeyFailed"),
            "Failed to get metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception?> _updatingMetadataForKey =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(9044, "UpdatingMetadataForKey"),
            "Updating metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception> _updateMetadataForKeyFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9045, "UpdateMetadataForKeyFailed"),
            "Failed to update metadata for key {Key}");

    private static readonly Action<ILogger, string, Exception?> _metadataValidationWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9046, "MetadataValidationWarning"),
            "Metadata validation failed for key {Key}");

    private static readonly Action<ILogger, string, Exception> _metadataExceptionWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9047, "MetadataExceptionWarning"),
            "Exception occurred while processing metadata for key {Key}");

    // Transaction operations delegates
    private static readonly Action<ILogger, string, Exception?> _atomicTransactionStarted =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9048, "AtomicTransactionStarted"),
            "Starting atomic transaction with ID {TransactionId}");

    private static readonly Action<ILogger, string, Exception> _atomicTransactionFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9049, "AtomicTransactionFailed"),
            "Failed to execute atomic transaction with ID {TransactionId}");

    private static readonly Action<ILogger, string, Exception?> _operationInTransactionExecuted =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9050, "OperationInTransactionExecuted"),
            "Executed operation in transaction {TransactionId}");

    private static readonly Action<ILogger, string, Exception?> _operationInTransactionFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9051, "OperationInTransactionFailed"),
            "Failed to execute operation in transaction {TransactionId}");

    private static readonly Action<ILogger, string, Exception> _transactionExecutionFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9052, "TransactionExecutionFailed"),
            "Failed to execute transaction {TransactionId}");

    private static readonly Action<ILogger, string, Exception?> _batchTransactionOperationExecuted =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9053, "BatchTransactionOperationExecuted"),
            "Executed batch operation in transaction {TransactionId}");

    private static readonly Action<ILogger, string, Exception?> _batchTransactionOperationFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9054, "BatchTransactionOperationFailed"),
            "Failed to execute batch operation in transaction {TransactionId}");

    private static readonly Action<ILogger, string, Exception> _batchTransactionFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(9055, "BatchTransactionFailed"),
            "Failed to execute batch transaction {TransactionId}");

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="persistentStorage">The persistent storage provider.</param>
    public StorageService(
        IEnclaveManager enclaveManager,
        ServiceFrameworkConfig configuration,
        ILogger<StorageService> logger,
        IPersistentStorageProvider? persistentStorage = null)
        : base("Storage", "Privacy-Preserving Data Storage Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _persistentMetadataStorage = persistentStorage;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IStorageService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxStorageItemCount", _configuration.GetValue("Storage:MaxStorageItemCount", "10000"));
        SetMetadata("MaxStorageItemSizeBytes", _configuration.GetValue("Storage:MaxStorageItemSizeBytes", "10485760")); // 10 MB
        SetMetadata("DefaultChunkSizeBytes", _configuration.GetValue("Storage:DefaultChunkSizeBytes", "1048576")); // 1 MB
        SetMetadata("SupportedStorageClasses", "Standard,Archive,ColdStorage");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <summary>
    /// Initializes the Storage service.
    /// </summary>
    /// <returns>True if initialization was successful.</returns>
    public new async Task<bool> InitializeAsync()
    {
        // Call the base class InitializeAsync which handles enclave initialization
        return await base.InitializeAsync();
    }

    /// <summary>
    /// Starts the Storage service.
    /// </summary>
    /// <returns>True if the service started successfully.</returns>
    public async Task<bool> StartAsync()
    {
        var result = await OnStartAsync();
        if (result)
        {
            _isRunning = true;
        }
        return result;
    }

    /// <summary>
    /// Stops the Storage service.
    /// </summary>
    /// <returns>True if the service stopped successfully.</returns>
    public async Task<bool> StopAsync()
    {
        var result = await OnStopAsync();
        if (result)
        {
            _isRunning = false;
        }
        return result;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            _storageServiceInitializing(Logger, null);

            // Initialize service-specific components
            await RefreshMetadataCacheAsync();

            // Initialize persistent storage if available
            await InitializePersistentMetadataAsync();

            _storageServiceInitialized(Logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _storageServiceInitializationFailed(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            _storageServiceEnclaveInitializing(Logger, null);
            await _enclaveManager.InitializeAsync();
            return true;
        }
        catch (Exception ex)
        {
            _storageServiceEnclaveInitializationFailed(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            _storageServiceStarting(Logger, null);

            // Load existing storage metadata from the enclave
            await RefreshMetadataCacheAsync();

            return true;
        }
        catch (Exception ex)
        {
            _storageServiceStartFailed(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            _storageServiceStopping(Logger, null);
            _metadataCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _storageServiceStopFailed(Logger, ex);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Refreshes the metadata cache from the enclave.
    /// </summary>
    private async Task RefreshMetadataCacheAsync()
    {
        try
        {
            // Get all metadata from the enclave
            string metadataListJson = await _enclaveManager.ExecuteJavaScriptAsync("getAllMetadata()");

            if (!string.IsNullOrEmpty(metadataListJson))
            {
                var metadataList = JsonSerializer.Deserialize<Dictionary<string, StorageMetadata>>(metadataListJson);
                if (metadataList != null)
                {
                    lock (_metadataCache)
                    {
                        _metadataCache.Clear();
                        foreach (var kvp in metadataList)
                        {
                            _metadataCache[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            _metadataCacheRefreshed(Logger, _metadataCache.Count, null);
        }
        catch (Exception ex)
        {
            _metadataCacheRefreshFailed(Logger, ex);
        }
    }

    /// <summary>
    /// Updates a metric value.
    /// </summary>
    /// <param name="metricName">The metric name.</param>
    /// <param name="value">The metric value.</param>
    public new void UpdateMetric(string metricName, object value)
    {
        SetMetadata(metricName, value.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Gets storage statistics.
    /// </summary>
    /// <returns>Storage statistics.</returns>
    public Models.StorageStatistics GetStatistics()
    {
        lock (_metadataCache)
        {
            return new Models.StorageStatistics
            {
                TotalItems = _metadataCache.Count,
                TotalSizeBytes = _metadataCache.Values.Sum(m => m.SizeBytes),
                RequestCount = _requestCount,
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                LastRequestTime = _lastRequestTime,
                CacheHitRate = _requestCount > 0 ? (double)_successCount / _requestCount : 0.0
            };
        }
    }

    /// <summary>
    /// Checks if the service supports the specified blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if supported, false otherwise.</returns>
    public new bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType == BlockchainType.NeoN3 || blockchainType == BlockchainType.NeoX;
    }

    /// <summary>
    /// Validates storage operation parameters.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    private void ValidateStorageOperation(string key, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }
    }

    /// <summary>
    /// Increments request counters.
    /// </summary>
    private void IncrementRequestCounters()
    {
        _requestCount++;
        _lastRequestTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Records successful operation.
    /// </summary>
    private void RecordSuccess()
    {
        _successCount++;
        UpdateMetric("LastSuccessTime", DateTime.UtcNow);
    }

    /// <summary>
    /// Records failed operation.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    private void RecordFailure(Exception ex)
    {
        _failureCount++;
        UpdateMetric("LastFailureTime", DateTime.UtcNow);
        UpdateMetric("LastErrorMessage", ex.Message);
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check enclave health
            if (!IsEnclaveInitialized)
                return ServiceHealth.Degraded;

            // Check if we can execute basic operations
            var healthCheck = await _enclaveManager.ExecuteJavaScriptAsync("healthCheck()");
            var isHealthy = healthCheck?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            // Update health metrics
            UpdateMetric("LastHealthCheck", DateTime.UtcNow);
            UpdateMetric("IsHealthy", isHealthy);

            return isHealthy ? ServiceHealth.Healthy : ServiceHealth.Degraded;
        }
        catch (Exception ex)
        {
            _healthCheckFailed(Logger, ex);
            return ServiceHealth.Unhealthy;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageMetadata>> ListKeysAsync(string prefix, int skip, int take, BlockchainType blockchainType)
    {
        ValidateStorageOperation(prefix ?? "", blockchainType);
        IncrementRequestCounters();

        try
        {
            var request = new
            {
                Prefix = prefix,
                Skip = skip,
                Take = take,
                BlockchainType = blockchainType.ToString()
            };

            var result = await _enclaveManager.ExecuteJavaScriptAsync($"listKeys('{prefix}', {skip}, {take})");
            var keys = JsonSerializer.Deserialize<string[]>(result?.ToString() ?? "[]") ?? Array.Empty<string>();

            // Get metadata for each key
            var metadataList = new List<StorageMetadata>();
            foreach (var key in keys)
            {
                if (_metadataCache.TryGetValue(key, out var metadata))
                {
                    metadataList.Add(metadata);
                }
                else
                {
                    // Create basic metadata if not in cache
                    metadataList.Add(new StorageMetadata
                    {
                        Key = key,
                        CreatedAt = DateTime.UtcNow,
                        SizeBytes = 0,
                        IsEncrypted = false,
                        IsCompressed = false,
                        ChunkCount = 1
                    });
                }
            }

            RecordSuccess();
            _keysListedWithPrefix(Logger, metadataList.Count, prefix, null);
            return metadataList;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            _listKeysWithPrefixFailed(Logger, prefix, ex);
            throw;
        }
    }

    /// <summary>
    /// Disposes the storage service.
    /// </summary>
    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the storage service.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _metadataCache.Clear();
        }
        base.Dispose(disposing);
    }

}
