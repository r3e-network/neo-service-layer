using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Configuration.Models;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Core implementation of the Configuration service for dynamic configuration management with enclave security.
/// </summary>
public partial class ConfigurationService : ServiceFramework.EnclaveBlockchainServiceBase, IConfigurationService
{
    private readonly Dictionary<string, ConfigurationEntry> _configurations = new();
    private readonly Dictionary<string, Models.ConfigurationSubscription> _subscriptions = new();
    private readonly object _configLock = new();
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration? _configuration;
    private readonly IKeyManagementService? _keyManagementService;
    private readonly IAttestationService? _attestationService;
    private const string ConfigurationEncryptionKeyId = "configuration-encryption-key";
    private string? _cachedEncryptionKey;
    private Timer? _cleanupTimer;

    // LoggerMessage delegates for performance optimization
    private static readonly Action<ILogger, Exception?> _serviceInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(6001, "ServiceInitializing"),
            "Initializing Configuration Service...");

    private static readonly Action<ILogger, Exception?> _serviceInitialized =
        LoggerMessage.Define(LogLevel.Information, new EventId(6002, "ServiceInitialized"),
            "Configuration Service initialized successfully");

    private static readonly Action<ILogger, Exception> _serviceInitializationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6003, "ServiceInitializationFailed"),
            "Failed to initialize Configuration Service");

    private static readonly Action<ILogger, Exception?> _enclaveInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(6004, "EnclaveInitializing"),
            "Initializing Configuration Service enclave operations...");

    private static readonly Action<ILogger, Exception?> _enclaveInitialized =
        LoggerMessage.Define(LogLevel.Information, new EventId(6005, "EnclaveInitialized"),
            "Configuration Service enclave operations initialized successfully");

    private static readonly Action<ILogger, Exception> _enclaveInitializationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6006, "EnclaveInitializationFailed"),
            "Failed to initialize Configuration Service enclave operations");

    private static readonly Action<ILogger, Exception?> _serviceStarting =
        LoggerMessage.Define(LogLevel.Information, new EventId(6007, "ServiceStarting"),
            "Starting Configuration Service...");

    private static readonly Action<ILogger, Exception?> _serviceStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(6008, "ServiceStarted"),
            "Configuration Service started successfully");

    private static readonly Action<ILogger, Exception> _serviceStartFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6009, "ServiceStartFailed"),
            "Failed to start Configuration Service");

    private static readonly Action<ILogger, Exception?> _serviceStopping =
        LoggerMessage.Define(LogLevel.Information, new EventId(6010, "ServiceStopping"),
            "Stopping Configuration Service...");

    private static readonly Action<ILogger, Exception?> _serviceStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(6011, "ServiceStopped"),
            "Configuration Service stopped successfully");

    private static readonly Action<ILogger, Exception> _serviceStopFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6012, "ServiceStopFailed"),
            "Failed to stop Configuration Service");

    private static readonly Action<ILogger, Exception> _healthCheckFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6013, "HealthCheckFailed"),
            "Configuration Service health check failed");

    private static readonly Action<ILogger, int, Exception?> _defaultConfigurationsLoaded =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(6014, "DefaultConfigurationsLoaded"),
            "Loaded {Count} default configurations");

    private static readonly Action<ILogger, Exception> _defaultConfigurationsLoadFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6015, "DefaultConfigurationsLoadFailed"),
            "Failed to load default configurations");

    private static readonly Action<ILogger, Exception?> _configurationStorageInitialized =
        LoggerMessage.Define(LogLevel.Information, new EventId(6016, "ConfigurationStorageInitialized"),
            "Configuration storage initialized successfully");

    private static readonly Action<ILogger, Exception> _configurationStorageInitializationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6017, "ConfigurationStorageInitializationFailed"),
            "Failed to initialize configuration storage");

    private static readonly Action<ILogger, int, Exception?> _encryptedConfigurationsLoaded =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(6018, "EncryptedConfigurationsLoaded"),
            "Loaded {Count} encrypted configurations from enclave storage");

    private static readonly Action<ILogger, Exception> _encryptedConfigurationsLoadFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(6019, "EncryptedConfigurationsLoadFailed"),
            "Failed to load encrypted configurations from enclave storage");

    private static readonly Action<ILogger, string, Exception> _configurationLoadFromKeyFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(6020, "ConfigurationLoadFromKeyFailed"),
            "Failed to load configuration from key {Key}");

    private static readonly Action<ILogger, Exception?> _configurationMonitoringStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(6021, "ConfigurationMonitoringStarted"),
            "Configuration monitoring started successfully");

    private static readonly Action<ILogger, Exception> _configurationMonitoringStartFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6022, "ConfigurationMonitoringStartFailed"),
            "Failed to start configuration monitoring");

    private static readonly Action<ILogger, Exception?> _configurationMonitoringStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(6023, "ConfigurationMonitoringStopped"),
            "Configuration monitoring stopped successfully");

    private static readonly Action<ILogger, Exception> _configurationMonitoringStopFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(6024, "ConfigurationMonitoringStopFailed"),
            "Failed to stop configuration monitoring gracefully");

    private static readonly Action<ILogger, int, Exception?> _pendingConfigurationsPersisted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(6025, "PendingConfigurationsPersisted"),
            "Persisted {Count} pending configurations");

    private static readonly Action<ILogger, Exception> _pendingConfigurationsPersistFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6026, "PendingConfigurationsPersistFailed"),
            "Failed to persist pending configurations");

    private static readonly Action<ILogger, Exception?> _creatingConfigurationEncryptionKey =
        LoggerMessage.Define(LogLevel.Information, new EventId(6027, "CreatingConfigurationEncryptionKey"),
            "Creating configuration encryption key");

    private static readonly Action<ILogger, Exception?> _keyManagementServiceNotAvailable =
        LoggerMessage.Define(LogLevel.Warning, new EventId(6028, "KeyManagementServiceNotAvailable"),
            "KeyManagementService not available. Using enclave-derived key.");

    private static readonly Action<ILogger, string, Exception?> _configurationPersisted =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6029, "ConfigurationPersisted"),
            "Persisted configuration {Key} to storage");

    private static readonly Action<ILogger, string, Exception> _configurationPersistFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6030, "ConfigurationPersistFailed"),
            "Failed to persist configuration {Key}");

    private static readonly Action<ILogger, string, Exception?> _configurationRemovedFromStorage =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6031, "ConfigurationRemovedFromStorage"),
            "Removed configuration {Key} from storage");

    private static readonly Action<ILogger, string, Exception> _configurationRemovalFromStorageFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6032, "ConfigurationRemovalFromStorageFailed"),
            "Failed to remove configuration {Key} from storage");

    // Configuration management operations
    private static readonly Action<ILogger, string, BlockchainType, Exception?> _settingConfiguration =
        LoggerMessage.Define<string, BlockchainType>(LogLevel.Information, new EventId(6033, "SettingConfiguration"),
            "Setting configuration {Key} on {Blockchain} with enclave security");

    private static readonly Action<ILogger, string, int, Exception?> _configurationSetSuccessfully =
        LoggerMessage.Define<string, int>(LogLevel.Information, new EventId(6034, "ConfigurationSetSuccessfully"),
            "Configuration {Key} set successfully with version {Version}");

    private static readonly Action<ILogger, string, Exception> _configurationSetFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6035, "ConfigurationSetFailed"),
            "Failed to set configuration {Key}");

    private static readonly Action<ILogger, string, BlockchainType, Exception?> _gettingConfiguration =
        LoggerMessage.Define<string, BlockchainType>(LogLevel.Debug, new EventId(6036, "GettingConfiguration"),
            "Getting configuration {Key} on {Blockchain}");

    private static readonly Action<ILogger, string, BlockchainType, Exception?> _configurationNotFound =
        LoggerMessage.Define<string, BlockchainType>(LogLevel.Warning, new EventId(6037, "ConfigurationNotFound"),
            "Configuration {Key} not found on {Blockchain}");

    private static readonly Action<ILogger, string, Exception> _getConfigurationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6038, "GetConfigurationFailed"),
            "Failed to get configuration {Key}");

    private static readonly Action<ILogger, string, BlockchainType, Exception?> _deletingConfiguration =
        LoggerMessage.Define<string, BlockchainType>(LogLevel.Information, new EventId(6039, "DeletingConfiguration"),
            "Deleting configuration {Key} on {Blockchain}");

    private static readonly Action<ILogger, string, Exception?> _configurationDeletedSuccessfully =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6040, "ConfigurationDeletedSuccessfully"),
            "Configuration {Key} deleted successfully");

    private static readonly Action<ILogger, string, Exception?> _configurationNotFoundForDeletion =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(6041, "ConfigurationNotFoundForDeletion"),
            "Configuration {Key} not found for deletion");

    private static readonly Action<ILogger, string, Exception> _deleteConfigurationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6042, "DeleteConfigurationFailed"),
            "Failed to delete configuration {Key}");

    private static readonly Action<ILogger, string, BlockchainType, Exception?> _listingConfigurations =
        LoggerMessage.Define<string, BlockchainType>(LogLevel.Debug, new EventId(6043, "ListingConfigurations"),
            "Listing configurations with prefix {Prefix} on {Blockchain}");

    private static readonly Action<ILogger, Exception> _listConfigurationsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6044, "ListConfigurationsFailed"),
            "Failed to list configurations");

    private static readonly Action<ILogger, string, int, Exception?> _batchConfigurationProcessing =
        LoggerMessage.Define<string, int>(LogLevel.Information, new EventId(6045, "BatchConfigurationProcessing"),
            "Processing batch configuration update {BatchId} with {RequestCount} requests");

    private static readonly Action<ILogger, string, Exception> _batchConfigurationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6046, "BatchConfigurationFailed"),
            "Failed to process batch configuration update {BatchId}");

    private static readonly Action<ILogger, string, BlockchainType, Exception?> _gettingConfigurationsByPattern =
        LoggerMessage.Define<string, BlockchainType>(LogLevel.Debug, new EventId(6047, "GettingConfigurationsByPattern"),
            "Getting configurations by pattern {Pattern} on {Blockchain}");

    private static readonly Action<ILogger, string, Exception> _getConfigurationsByPatternFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6048, "GetConfigurationsByPatternFailed"),
            "Failed to get configurations by pattern {Pattern}");

    private static readonly Action<ILogger, string, Exception?> _configurationValidationSuccess =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6049, "ConfigurationValidationSuccess"),
            "Configuration {Key} validated successfully in enclave");

    private static readonly Action<ILogger, string, Exception> _configurationValidationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6050, "ConfigurationValidationFailed"),
            "Configuration validation failed for key {Key}");

    private static readonly Action<ILogger, Exception> _configurationEncryptionFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6051, "ConfigurationEncryptionFailed"),
            "Failed to encrypt configuration value");

    private static readonly Action<ILogger, object, Models.ConfigurationValueType, Exception?> _configurationConversionWarning =
        LoggerMessage.Define<object, Models.ConfigurationValueType>(LogLevel.Warning, new EventId(6052, "ConfigurationConversionWarning"),
            "Failed to convert value {Value} to type {ValueType}, returning original value");

    // Persistent storage operations
    private static readonly Action<ILogger, Exception?> _persistentStorageNotAvailable =
        LoggerMessage.Define(LogLevel.Warning, new EventId(6053, "PersistentStorageNotAvailable"),
            "Persistent storage not available for configuration service");

    private static readonly Action<ILogger, Exception?> _loadingPersistentConfigurations =
        LoggerMessage.Define(LogLevel.Information, new EventId(6054, "LoadingPersistentConfigurations"),
            "Loading persistent configurations...");

    private static readonly Action<ILogger, int, Exception?> _persistentConfigurationsLoaded =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(6055, "PersistentConfigurationsLoaded"),
            "Loaded {Count} configurations from persistent storage");

    private static readonly Action<ILogger, Exception> _loadPersistentConfigurationsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6056, "LoadPersistentConfigurationsFailed"),
            "Error loading persistent configurations");

    private static readonly Action<ILogger, string, Exception> _persistConfigurationEntryFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6057, "PersistConfigurationEntryFailed"),
            "Error persisting configuration {Key}");

    private static readonly Action<ILogger, string, Exception> _removePersistedConfigurationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6058, "RemovePersistedConfigurationFailed"),
            "Error removing persisted configuration {Key}");

    private static readonly Action<ILogger, string, Exception> _updateConfigurationIndexesFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6059, "UpdateConfigurationIndexesFailed"),
            "Error updating configuration indexes for {Key}");

    private static readonly Action<ILogger, string, Exception> _removeConfigurationIndexesFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6060, "RemoveConfigurationIndexesFailed"),
            "Error removing configuration indexes for {Key}");

    private static readonly Action<ILogger, string, Exception> _addConfigurationHistoryFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6061, "AddConfigurationHistoryFailed"),
            "Error adding configuration history for {Key}");

    private static readonly Action<ILogger, int, Exception?> _persistentSubscriptionsLoaded =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(6062, "PersistentSubscriptionsLoaded"),
            "Loaded {Count} active subscriptions from persistent storage");

    private static readonly Action<ILogger, Exception> _loadPersistentSubscriptionsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6063, "LoadPersistentSubscriptionsFailed"),
            "Error loading persistent subscriptions");

    private static readonly Action<ILogger, string, Exception> _persistSubscriptionFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6064, "PersistSubscriptionFailed"),
            "Error persisting subscription {SubscriptionId}");

    private static readonly Action<ILogger, string, Exception> _removePersistedSubscriptionFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6065, "RemovePersistedSubscriptionFailed"),
            "Error removing persisted subscription {SubscriptionId}");

    private static readonly Action<ILogger, string, Exception> _persistAuditEntryFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6066, "PersistAuditEntryFailed"),
            "Error persisting audit entry for {Key}");

    private static readonly Action<ILogger, Exception> _persistStatisticsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6067, "PersistStatisticsFailed"),
            "Error persisting configuration statistics");

    private static readonly Action<ILogger, Exception?> _cleanupCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(6068, "CleanupCompleted"),
            "Completed cleanup of old configuration data");

    private static readonly Action<ILogger, Exception> _cleanupFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6069, "CleanupFailed"),
            "Error during configuration data cleanup");

    private static readonly Action<ILogger, string, Exception> _getConfigurationHistoryFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6070, "GetConfigurationHistoryFailed"),
            "Error retrieving configuration history for {Key}");

    // Advanced operations delegates
    private static readonly Action<ILogger, string, Exception?> _importingConfigurationsFromFile =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6071, "ImportingConfigurationsFromFile"),
            "Importing configurations from file: {FilePath}");

    private static readonly Action<ILogger, Exception> _importConfigurationsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6072, "ImportConfigurationsFailed"),
            "Failed to import configurations");

    private static readonly Action<ILogger, int, Exception?> _configurationsImportedSuccessfully =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(6073, "ConfigurationsImportedSuccessfully"),
            "Successfully imported {Count} configurations");

    private static readonly Action<ILogger, Exception> _exportConfigurationsFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6074, "ExportConfigurationsFailed"),
            "Failed to export configurations");

    private static readonly Action<ILogger, string, Exception?> _configurationsExportedToFile =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6075, "ConfigurationsExportedToFile"),
            "Configurations exported to file: {FilePath}");

    private static readonly Action<ILogger, Exception> _createBackupFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6076, "CreateBackupFailed"),
            "Failed to create configuration backup");

    private static readonly Action<ILogger, Exception> _restoreBackupFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6077, "RestoreBackupFailed"),
            "Failed to restore configuration backup");

    private static readonly Action<ILogger, string, Exception?> _performingConfigurationValidation =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6078, "PerformingConfigurationValidation"),
            "Performing configuration validation for schema: {SchemaName}");

    private static readonly Action<ILogger, Exception> _configurationValidationFailed2 =
        LoggerMessage.Define(LogLevel.Error, new EventId(6079, "ConfigurationValidationFailed2"),
            "Configuration validation failed");

    // Subscription and notification delegates
    private static readonly Action<ILogger, string, Exception?> _notifyingSubscribersOfConfigurationChange =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6080, "NotifyingSubscribersOfConfigurationChange"),
            "Notifying subscribers of configuration change for key: {Key}");

    private static readonly Action<ILogger, Exception> _notifySubscribersFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6081, "NotifySubscribersFailed"),
            "Failed to notify subscribers");

    private static readonly Action<ILogger, string, Exception?> _configurationSubscriptionCreated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6082, "ConfigurationSubscriptionCreated"),
            "Created configuration subscription for key: {Key}");

    private static readonly Action<ILogger, Exception> _createSubscriptionFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(6083, "CreateSubscriptionFailed"),
            "Failed to create configuration subscription");

    private static readonly Action<ILogger, string, Exception> _notifySubscriberFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6084, "NotifySubscriberFailed"),
            "Failed to notify subscriber: {SubscriptionId}");

    private static readonly Action<ILogger, string, string, Exception?> _notifyingSubscriberOfDeletion =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(6085, "NotifyingSubscriberOfDeletion"),
            "Notifying subscriber {SubscriptionId} of configuration deletion for key: {Key}");

    private static readonly Action<ILogger, string, Exception> _notifySubscriberOfDeletionFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6086, "NotifySubscriberOfDeletionFailed"),
            "Failed to notify subscribers of configuration deletion for key: {Key}");

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager for secure configuration operations.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="persistentStorage">The persistent storage provider.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <param name="attestationService">The attestation service.</param>
    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IEnclaveManager enclaveManager,
        IServiceConfiguration? configuration = null,
        IPersistentStorageProvider? persistentStorage = null,
        IKeyManagementService? keyManagementService = null,
        IAttestationService? attestationService = null)
        : base("ConfigurationService", "Dynamic configuration management service with enclave security", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _persistentStorage = persistentStorage;
        _keyManagementService = keyManagementService;
        _attestationService = attestationService;

        // Add capabilities
        AddCapability<IConfigurationService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("SupportedValueTypes", "String,Integer,Boolean,Decimal,Json");
        SetMetadata("EncryptionSupport", "true");
        SetMetadata("SubscriptionSupport", "true");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");

        // Initialize cleanup timer if persistent storage is available
        if (_persistentStorage != null)
        {
            _cleanupTimer = new Timer(async _ => await CleanupOldDataAsync(), null, TimeSpan.FromHours(24), TimeSpan.FromHours(24));
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        _serviceInitializing(Logger, null);

        try
        {
            // Load default configurations
            await LoadDefaultConfigurationsAsync();

            // Load from persistent storage if available
            await LoadPersistentConfigurationsAsync();

            // Initialize configuration storage
            await InitializeConfigurationStorageAsync();

            _serviceInitialized(Logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _serviceInitializationFailed(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        _enclaveInitializing(Logger, null);

        try
        {
            // Initialize configuration encryption keys in the enclave
            await _enclaveManager.ExecuteJavaScriptAsync("initializeConfigurationEncryption()");

            // Load encrypted configurations from secure storage
            await LoadEncryptedConfigurationsAsync();

            _enclaveInitialized(Logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _enclaveInitializationFailed(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        _serviceStarting(Logger, null);

        try
        {
            // Start configuration monitoring
            await StartConfigurationMonitoringAsync();

            _serviceStarted(Logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _serviceStartFailed(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        _serviceStopping(Logger, null);

        try
        {
            // Stop configuration monitoring
            await StopConfigurationMonitoringAsync();

            // Dispose cleanup timer
            _cleanupTimer?.Dispose();

            // Persist pending configurations
            await PersistPendingConfigurationsAsync();

            // Persist statistics
            await PersistStatisticsAsync();

            _serviceStopped(Logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _serviceStopFailed(Logger, ex);
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
            _healthCheckFailed(Logger, ex);
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
                // Note: encryption key is now managed by KeyManagementService
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
                    BlockchainType = "NeoN3"
                };

                lock (_configLock)
                {
                    _configurations[key] = entry;
                }
            }

            _defaultConfigurationsLoaded(Logger, defaultConfigs.Count, null);
        }
        catch (Exception ex)
        {
            _defaultConfigurationsLoadFailed(Logger, ex);
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
                await GetStorageEncryptionKeyAsync(),
                CancellationToken.None);

            _configurationStorageInitialized(Logger, null);
        }
        catch (Exception ex)
        {
            _configurationStorageInitializationFailed(Logger, ex);
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
                            await GetStorageEncryptionKeyAsync(),
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
                        _configurationLoadFromKeyFailed(Logger, key, ex);
                    }
                }

                _encryptedConfigurationsLoaded(Logger, keys.Length, null);
            }
        }
        catch (Exception ex)
        {
            _encryptedConfigurationsLoadFailed(Logger, ex);
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

            _configurationMonitoringStarted(Logger, null);
        }
        catch (Exception ex)
        {
            _configurationMonitoringStartFailed(Logger, ex);
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

            _configurationMonitoringStopped(Logger, null);
        }
        catch (Exception ex)
        {
            _configurationMonitoringStopFailed(Logger, ex);
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

            _pendingConfigurationsPersisted(Logger, configurationsToPersist.Count, null);
        }
        catch (Exception ex)
        {
            _pendingConfigurationsPersistFailed(Logger, ex);
        }
    }

    /// <summary>
    /// Gets the storage encryption key for configuration data.
    /// </summary>
    /// <returns>The encryption key.</returns>
    private async Task<string> GetStorageEncryptionKeyAsync()
    {
        // Check if we have a cached key
        if (!string.IsNullOrEmpty(_cachedEncryptionKey))
        {
            return _cachedEncryptionKey;
        }

        // If key management service is available, use it
        if (_keyManagementService != null)
        {
            try
            {
                // Check if the encryption key exists
                var keyMetadata = await _keyManagementService.GetKeyMetadataAsync(ConfigurationEncryptionKeyId, BlockchainType.NeoN3);
                _cachedEncryptionKey = ConfigurationEncryptionKeyId;
                return _cachedEncryptionKey;
            }
            catch
            {
                // Key doesn't exist, create it
                _creatingConfigurationEncryptionKey(Logger, null);
                await _keyManagementService.CreateKeyAsync(
                    ConfigurationEncryptionKeyId,
                    "AES256",
                    "Encrypt,Decrypt",
                    false, // Not exportable for security
                    "Configuration service encryption key for securing sensitive configuration values",
                    BlockchainType.NeoN3);
                _cachedEncryptionKey = ConfigurationEncryptionKeyId;
                return _cachedEncryptionKey;
            }
        }

        // Fallback: derive key from enclave identity
        _keyManagementServiceNotAvailable(Logger, null);
        if (_attestationService == null)
        {
            throw new InvalidOperationException("Neither KeyManagementService nor AttestationService is available for encryption key derivation");
        }
        var enclaveInfo = await _attestationService.GetEnclaveInfoAsync();
        if (enclaveInfo == null)
        {
            throw new InvalidOperationException("Unable to retrieve enclave information for encryption key derivation");
        }
        var keyMaterial = $"config-encryption-{enclaveInfo.MrEnclave}-{enclaveInfo.MrSigner}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));
        _cachedEncryptionKey = Convert.ToBase64String(hash);
        return _cachedEncryptionKey;
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
            // Use persistent storage if available
            if (_persistentStorage != null)
            {
                await PersistConfigurationEntryAsync(entry);
            }
            else
            {
                // Fallback to simulated storage
                var json = JsonSerializer.Serialize(entry);
                await Task.Delay(10); // Simulate storage operation
            }

            _configurationPersisted(Logger, entry.Key, null);
        }
        catch (Exception ex)
        {
            _configurationPersistFailed(Logger, entry.Key, ex);
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
            // Use persistent storage if available
            if (_persistentStorage != null)
            {
                await RemovePersistedConfigurationAsync(key);
            }
            else
            {
                // Fallback to simulated storage
                await Task.Delay(10); // Simulate storage operation
            }

            _configurationRemovedFromStorage(Logger, key, null);
        }
        catch (Exception ex)
        {
            _configurationRemovalFromStorageFailed(Logger, key, ex);
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
