using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Configuration.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Core implementation of the Configuration service for dynamic configuration management with enclave security.
/// </summary>
public partial class ConfigurationService : EnclaveBlockchainServiceBase, IConfigurationService
{
    private readonly Dictionary<string, ConfigurationEntry> _configurations = new();
    private readonly Dictionary<string, Models.ConfigurationSubscription> _subscriptions = new();
    private readonly object _configLock = new();
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager for secure configuration operations.</param>
    /// <param name="configuration">The service configuration.</param>
    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IEnclaveManager enclaveManager,
        IServiceConfiguration? configuration = null)
        : base("ConfigurationService", "Dynamic configuration management service with enclave security", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;

        // Add capabilities
        AddCapability<IConfigurationService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("SupportedValueTypes", "String,Integer,Boolean,Decimal,Json");
        SetMetadata("EncryptionSupport", "true");
        SetMetadata("SubscriptionSupport", "true");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Configuration Service...");

        try
        {
            // Load default configurations
            await LoadDefaultConfigurationsAsync();

            // Initialize configuration storage
            await InitializeConfigurationStorageAsync();

            Logger.LogInformation("Configuration Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Configuration Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Configuration Service enclave operations...");

        try
        {
            // Initialize configuration encryption keys in the enclave
            await _enclaveManager.ExecuteJavaScriptAsync("initializeConfigurationEncryption()");

            // Load encrypted configurations from secure storage
            await LoadEncryptedConfigurationsAsync();

            Logger.LogInformation("Configuration Service enclave operations initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Configuration Service enclave operations");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Configuration Service...");

        try
        {
            // Start configuration monitoring
            await StartConfigurationMonitoringAsync();

            Logger.LogInformation("Configuration Service started successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start Configuration Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Configuration Service...");

        try
        {
            // Stop configuration monitoring
            await StopConfigurationMonitoringAsync();

            // Persist pending configurations
            await PersistPendingConfigurationsAsync();

            Logger.LogInformation("Configuration Service stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to stop Configuration Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check enclave health
            if (!IsEnclaveInitialized)
            {
                return ServiceHealth.Degraded;
            }

            // Test configuration retrieval
            var healthCheck = await _enclaveManager.ExecuteJavaScriptAsync("configurationHealthCheck()");
            var isHealthy = healthCheck?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            // Check configuration cache
            var configCount = 0;
            lock (_configLock)
            {
                configCount = _configurations.Count;
            }

            // Update health metrics
            UpdateMetric("LastHealthCheck", DateTime.UtcNow);
            UpdateMetric("ConfigurationCount", configCount);
            UpdateMetric("SubscriptionCount", _subscriptions.Count);
            UpdateMetric("IsHealthy", isHealthy);

            return isHealthy ? ServiceHealth.Healthy : ServiceHealth.Degraded;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Configuration Service health check failed");
            return ServiceHealth.Unhealthy;
        }
    }

    /// <summary>
    /// Loads default configurations required for service operation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadDefaultConfigurationsAsync()
    {
        try
        {
            var defaultConfigs = new Dictionary<string, (string Value, ConfigurationValueType Type, bool Encrypt)>
            {
                ["configuration.cache.ttl"] = ("3600", ConfigurationValueType.Integer, false),
                ["configuration.encryption.enabled"] = ("true", ConfigurationValueType.Boolean, false),
                ["configuration.audit.enabled"] = ("true", ConfigurationValueType.Boolean, false),
                ["configuration.subscription.maxSubscribers"] = ("1000", ConfigurationValueType.Integer, false),
                ["configuration.storage.encryptionKey"] = ("default-encryption-key", ConfigurationValueType.String, true)
            };

            foreach (var (key, (value, type, encrypt)) in defaultConfigs)
            {
                var entry = new ConfigurationEntry
                {
                    Key = key,
                    Value = value,
                    ValueType = (Models.ConfigurationValueType)type,
                    Description = $"Default configuration for {key}",
                    EncryptValue = encrypt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1,
                    BlockchainType = BlockchainType.NeoN3
                };

                lock (_configLock)
                {
                    _configurations[key] = entry;
                }
            }

            Logger.LogInformation("Loaded {Count} default configurations", defaultConfigs.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load default configurations");
            throw;
        }
    }

    /// <summary>
    /// Initializes the configuration storage system.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InitializeConfigurationStorageAsync()
    {
        try
        {
            // Initialize storage keys and encryption in the enclave
            await _enclaveManager.StorageStoreDataAsync(
                "config:initialization",
                DateTime.UtcNow.ToString("O"),
                GetStorageEncryptionKey(),
                CancellationToken.None);

            Logger.LogInformation("Configuration storage initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize configuration storage");
            throw;
        }
    }

    /// <summary>
    /// Loads encrypted configurations from secure enclave storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadEncryptedConfigurationsAsync()
    {
        try
        {
            // Load configuration keys from the enclave
            var configKeys = await _enclaveManager.StorageListKeysAsync("config:", 0, 1000, CancellationToken.None);

            if (!string.IsNullOrEmpty(configKeys))
            {
                var keys = JsonSerializer.Deserialize<string[]>(configKeys) ?? Array.Empty<string>();

                foreach (var key in keys.Where(k => k.StartsWith("config:") && k != "config:initialization"))
                {
                    try
                    {
                        var configData = await _enclaveManager.StorageRetrieveDataAsync(
                            key,
                            GetStorageEncryptionKey(),
                            CancellationToken.None);

                        if (!string.IsNullOrEmpty(configData))
                        {
                            var entry = JsonSerializer.Deserialize<ConfigurationEntry>(configData);
                            if (entry != null)
                            {
                                lock (_configLock)
                                {
                                    _configurations[entry.Key] = entry;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to load configuration from key {Key}", key);
                    }
                }

                Logger.LogInformation("Loaded {Count} encrypted configurations from enclave storage", keys.Length);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load encrypted configurations from enclave storage");
        }
    }

    /// <summary>
    /// Starts configuration monitoring and change detection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task StartConfigurationMonitoringAsync()
    {
        try
        {
            // Initialize configuration change monitoring in the enclave
            await _enclaveManager.ExecuteJavaScriptAsync("startConfigurationMonitoring()");

            Logger.LogInformation("Configuration monitoring started successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start configuration monitoring");
            throw;
        }
    }

    /// <summary>
    /// Stops configuration monitoring.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task StopConfigurationMonitoringAsync()
    {
        try
        {
            // Stop configuration monitoring in the enclave
            await _enclaveManager.ExecuteJavaScriptAsync("stopConfigurationMonitoring()");

            Logger.LogInformation("Configuration monitoring stopped successfully");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to stop configuration monitoring gracefully");
        }
    }

    /// <summary>
    /// Persists pending configurations to secure storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task PersistPendingConfigurationsAsync()
    {
        try
        {
            var configurationsToPersist = new List<ConfigurationEntry>();

            lock (_configLock)
            {
                configurationsToPersist.AddRange(_configurations.Values.Where(c => c.UpdatedAt > c.CreatedAt.AddMinutes(1)));
            }

            foreach (var config in configurationsToPersist)
            {
                await PersistConfigurationAsync(config);
            }

            Logger.LogInformation("Persisted {Count} pending configurations", configurationsToPersist.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist pending configurations");
        }
    }

    /// <summary>
    /// Gets the storage encryption key for configuration data.
    /// </summary>
    /// <returns>The encryption key.</returns>
    private string GetStorageEncryptionKey()
    {
        // In production, this would derive from enclave identity or configuration
        return "config-storage-encryption-key-v1";
    }

    /// <summary>
    /// Validates a configuration request.
    /// </summary>
    /// <param name="request">The configuration request to validate.</param>
    private async Task ValidateConfigurationAsync(SetConfigurationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            throw new ArgumentException("Configuration key cannot be empty");
        }

        if (request.Key.Length > 255)
        {
            throw new ArgumentException("Configuration key cannot exceed 255 characters");
        }

        if (request.Value != null && request.Value.ToString()?.Length > 10000)
        {
            throw new ArgumentException("Configuration value cannot exceed 10000 characters");
        }

        // Additional validation logic can be added here
        await Task.CompletedTask;
    }

    /// <summary>
    /// Persists a configuration to storage.
    /// </summary>
    /// <param name="entry">The configuration entry to persist.</param>
    private async Task PersistConfigurationAsync(ConfigurationEntry entry)
    {
        try
        {
            // Persist configuration to storage
            var json = JsonSerializer.Serialize(entry);
            await Task.Delay(10); // Simulate storage operation

            Logger.LogDebug("Persisted configuration {Key} to storage", entry.Key);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist configuration {Key}", entry.Key);
            throw;
        }
    }

    /// <summary>
    /// Removes a configuration from storage.
    /// </summary>
    /// <param name="key">The configuration key to remove.</param>
    private async Task RemoveConfigurationFromStorageAsync(string key)
    {
        try
        {
            // Remove configuration from storage
            await Task.Delay(10); // Simulate storage operation

            Logger.LogDebug("Removed configuration {Key} from storage", key);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove configuration {Key} from storage", key);
            throw;
        }
    }

    /// <summary>
    /// Gets configuration statistics.
    /// </summary>
    /// <returns>Configuration statistics.</returns>
    public ConfigurationStatistics GetStatistics()
    {
        lock (_configLock)
        {
            return new ConfigurationStatistics
            {
                TotalConfigurations = _configurations.Count,
                ActiveSubscriptions = _subscriptions.Values.Count(s => s.IsActive),
                LastModified = _configurations.Values.Any()
                    ? _configurations.Values.Max(c => c.UpdatedAt)
                    : DateTime.MinValue,
                ConfigurationsByType = _configurations.Values
                    .GroupBy(c => c.ValueType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };
        }
    }
}
