using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Storage.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Core implementation of the Storage service.
/// </summary>
public partial class StorageService : EnclaveBlockchainServiceBase, IStorageService, IDisposable
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, StorageMetadata> _metadataCache = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="persistentStorage">The persistent storage provider.</param>
    public StorageService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<StorageService> logger,
        IPersistentStorageProvider? persistentStorage = null)
        : base("Storage", "Privacy-Preserving Data Storage Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
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

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Storage Service...");

            // Initialize service-specific components
            await RefreshMetadataCacheAsync();

            // Initialize persistent storage if available
            await InitializePersistentMetadataAsync();

            Logger.LogInformation("Storage Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Storage Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Storage Service enclave...");
            await _enclaveManager.InitializeAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Storage Service enclave.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Storage Service...");

            // Load existing storage metadata from the enclave
            await RefreshMetadataCacheAsync();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Storage Service.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Storage Service...");
            _metadataCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Storage Service.");
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

            Logger.LogDebug("Refreshed metadata cache with {Count} items", _metadataCache.Count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to refresh metadata cache");
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
            Logger.LogError(ex, "Health check failed");
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
            Logger.LogDebug("Listed {Count} keys with prefix {Prefix}", metadataList.Count, prefix);
            return metadataList;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Failed to list keys with prefix {Prefix}", prefix);
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
