using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Security;


namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// Implementation of the Secrets Management service.
/// </summary>
public partial class SecretsManagementService : ServiceFramework.EnclaveBlockchainServiceBase, ISecretsManagementService, ISecretsManager
{
    #region LoggerMessage Delegates

    private static readonly Action<ILogger, Exception?> _serviceinitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(5001, "ServiceInitializing"),
            "Initializing Secrets Management Service...");

    private static readonly Action<ILogger, ExternalSecretProviderType, Exception?> _externalProviderInitialized =
        LoggerMessage.Define<ExternalSecretProviderType>(LogLevel.Information, new EventId(5002, "ExternalProviderInitialized"),
            "Initialized external provider: {ProviderType}");

    private static readonly Action<ILogger, Exception?> _serviceInitialized =
        LoggerMessage.Define(LogLevel.Information, new EventId(5003, "ServiceInitialized"),
            "Secrets Management Service initialized successfully");

    private static readonly Action<ILogger, Exception> _initializationError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5004, "InitializationError"),
            "Error initializing Secrets Management Service");

    private static readonly Action<ILogger, Exception?> _enclaveInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(5005, "EnclaveInitializing"),
            "Initializing Secrets Management Service enclave...");

    private static readonly Action<ILogger, Exception> _enclaveInitializationError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5006, "EnclaveInitializationError"),
            "Error initializing Secrets Management Service enclave");

    private static readonly Action<ILogger, Exception?> _serviceStarting =
        LoggerMessage.Define(LogLevel.Information, new EventId(5007, "ServiceStarting"),
            "Starting Secrets Management Service...");

    private static readonly Action<ILogger, Exception> _serviceStartError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5008, "ServiceStartError"),
            "Error starting Secrets Management Service");

    private static readonly Action<ILogger, Exception?> _serviceStopping =
        LoggerMessage.Define(LogLevel.Information, new EventId(5009, "ServiceStopping"),
            "Stopping Secrets Management Service...");

    private static readonly Action<ILogger, Exception> _serviceStopError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5010, "ServiceStopError"),
            "Error stopping Secrets Management Service");

    private static readonly Action<ILogger, string, Exception?> _secretStoring =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5011, "SecretStoring"),
            "Storing secret {SecretId} securely within enclave");

    private static readonly Action<ILogger, string, Exception?> _secretStored =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5012, "SecretStored"),
            "Successfully stored secret {SecretId} in enclave");

    private static readonly Action<ILogger, string, string, Exception?> _secretRetrieving =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(5013, "SecretRetrieving"),
            "Retrieving secret {SecretId} version {Version} from enclave");

    private static readonly Action<ILogger, string, Exception?> _secretRetrieved =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5014, "SecretRetrieved"),
            "Successfully retrieved secret {SecretId}");

    private static readonly Action<ILogger, string, Exception> _secretRetrievalError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5015, "SecretRetrievalError"),
            "Error retrieving secret {SecretId}");

    private static readonly Action<ILogger, string, Exception> _secretMetadataRetrievalError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5016, "SecretMetadataRetrievalError"),
            "Error retrieving secret metadata for {SecretId}");

    private static readonly Action<ILogger, Exception> _secretListError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5017, "SecretListError"),
            "Error listing secrets");

    private static readonly Action<ILogger, string, Exception?> _secretUpdating =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5018, "SecretUpdating"),
            "Updating secret {SecretId} securely within enclave");

    private static readonly Action<ILogger, string, Exception?> _secretUpdated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5019, "SecretUpdated"),
            "Successfully updated secret {SecretId} in enclave");

    private static readonly Action<ILogger, string, Exception?> _secretDeleting =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5020, "SecretDeleting"),
            "Deleting secret {SecretId} from enclave");

    private static readonly Action<ILogger, string, Exception?> _secretDeleted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5021, "SecretDeleted"),
            "Successfully deleted secret {SecretId} from enclave");

    private static readonly Action<ILogger, string, Exception> _secretDeletionError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5022, "SecretDeletionError"),
            "Error deleting secret {SecretId}");

    private static readonly Action<ILogger, string, Exception?> _secretRotating =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(5023, "SecretRotating"),
            "Rotating secret {SecretId} securely within enclave");

    private static readonly Action<ILogger, string, int, Exception?> _secretRotated =
        LoggerMessage.Define<string, int>(LogLevel.Information, new EventId(5024, "SecretRotated"),
            "Successfully rotated secret {SecretId} to version {Version} in enclave");

    private static readonly Action<ILogger, ExternalSecretProviderType, Exception?> _externalProviderConfiguring =
        LoggerMessage.Define<ExternalSecretProviderType>(LogLevel.Information, new EventId(5025, "ExternalProviderConfiguring"),
            "Configuring external provider {ProviderType}");

    private static readonly Action<ILogger, ExternalSecretProviderType, Exception?> _externalProviderNotRegistered =
        LoggerMessage.Define<ExternalSecretProviderType>(LogLevel.Warning, new EventId(5026, "ExternalProviderNotRegistered"),
            "External provider {ProviderType} not registered");

    private static readonly Action<ILogger, ExternalSecretProviderType, Exception?> _externalProviderConfigured =
        LoggerMessage.Define<ExternalSecretProviderType>(LogLevel.Information, new EventId(5027, "ExternalProviderConfigured"),
            "Successfully configured external provider {ProviderType}");

    private static readonly Action<ILogger, ExternalSecretProviderType, Exception> _externalProviderConfigurationError =
        LoggerMessage.Define<ExternalSecretProviderType>(LogLevel.Error, new EventId(5028, "ExternalProviderConfigurationError"),
            "Error configuring external provider {ProviderType}");

    private static readonly Action<ILogger, ExternalSecretProviderType, SyncDirection, Exception?> _externalProviderSynchronizing =
        LoggerMessage.Define<ExternalSecretProviderType, SyncDirection>(LogLevel.Information, new EventId(5029, "ExternalProviderSynchronizing"),
            "Synchronizing with external provider {ProviderType}, direction: {Direction}");

    private static readonly Action<ILogger, int, ExternalSecretProviderType, Exception?> _externalProviderSynchronized =
        LoggerMessage.Define<int, ExternalSecretProviderType>(LogLevel.Information, new EventId(5030, "ExternalProviderSynchronized"),
            "Successfully synchronized {Count} secrets with external provider {ProviderType}");

    private static readonly Action<ILogger, ExternalSecretProviderType, Exception> _externalProviderSynchronizationError =
        LoggerMessage.Define<ExternalSecretProviderType>(LogLevel.Error, new EventId(5031, "ExternalProviderSynchronizationError"),
            "Error synchronizing with external provider {ProviderType}");

    private static readonly Action<ILogger, int, Exception?> _secretCacheRefreshed =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(5032, "SecretCacheRefreshed"),
            "Secret cache refreshed. {SecretCount} secrets loaded.");

    private static readonly Action<ILogger, Exception> _secretCacheRefreshError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5033, "SecretCacheRefreshError"),
            "Error refreshing secret cache");

    private static readonly Action<ILogger, string, Exception> _lastAccessedUpdateError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5034, "LastAccessedUpdateError"),
            "Error updating last accessed timestamp for secret {SecretId}");

    private static readonly Action<ILogger, string, Exception> _externalSecretSyncError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5035, "ExternalSecretSyncError"),
            "Error syncing secret {SecretId} from external provider");

    private static readonly Action<ILogger, string, Exception> _externalSecretPushError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5036, "ExternalSecretPushError"),
            "Error syncing secret {SecretId} to external provider");

    // Persistent storage LoggerMessage delegates
    private static readonly Action<ILogger, Exception?> _persistentStorageUnavailable =
        LoggerMessage.Define(LogLevel.Warning, new EventId(5037, "PersistentStorageUnavailable"),
            "Persistent storage not available for secrets management service");

    private static readonly Action<ILogger, Exception?> _loadingPersistentSecrets =
        LoggerMessage.Define(LogLevel.Information, new EventId(5038, "LoadingPersistentSecrets"),
            "Loading persistent secret metadata...");

    private static readonly Action<ILogger, int, Exception?> _persistentSecretsLoaded =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(5039, "PersistentSecretsLoaded"),
            "Loaded {Count} secret metadata entries from persistent storage");

    private static readonly Action<ILogger, Exception> _persistentSecretsLoadError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5040, "PersistentSecretsLoadError"),
            "Error loading persistent secret metadata");

    private static readonly Action<ILogger, string, Exception> _secretMetadataPersistError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5041, "SecretMetadataPersistError"),
            "Error persisting secret metadata for {SecretId}");

    private static readonly Action<ILogger, string, Exception> _secretMetadataRemovalError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5042, "SecretMetadataRemovalError"),
            "Error removing persisted secret metadata for {SecretId}");

    private static readonly Action<ILogger, string, Exception> _secretIndexUpdateError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5043, "SecretIndexUpdateError"),
            "Error updating secret indexes for {SecretId}");

    private static readonly Action<ILogger, string, Exception> _secretIndexRemovalError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5044, "SecretIndexRemovalError"),
            "Error removing secret indexes for {SecretId}");

    private static readonly Action<ILogger, string, Exception> _auditLogPersistError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5045, "AuditLogPersistError"),
            "Error persisting audit log for secret {SecretId}");

    private static readonly Action<ILogger, string, Exception> _auditHistoryRetrievalError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5046, "AuditHistoryRetrievalError"),
            "Error retrieving audit history for secret {SecretId}");

    private static readonly Action<ILogger, Exception> _statisticsPersistError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5047, "StatisticsPersistError"),
            "Error persisting secret statistics");

    private static readonly Action<ILogger, Exception?> _cleanupCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(5048, "CleanupCompleted"),
            "Completed cleanup of old secrets data");

    private static readonly Action<ILogger, Exception> _cleanupError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5049, "CleanupError"),
            "Error during secrets data cleanup");

    #endregion
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, SecretMetadata> _secretCache = new();
    private readonly Dictionary<ExternalSecretProviderType, IExternalSecretProvider> _externalProviders = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;
    private int _totalSecretsCreated;
    private int _totalSecretsAccessed;
    private int _totalSecretsRotated;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsManagementService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="externalProviders">External secret providers.</param>
    public SecretsManagementService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<SecretsManagementService> logger,
        IEnumerable<IExternalSecretProvider>? externalProviders = null)
        : base("SecretsManagement", "Trusted Secrets Management Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Register external providers
        if (externalProviders != null)
        {
            foreach (var provider in externalProviders)
            {
                _externalProviders[provider.ProviderType] = provider;
            }
        }

        // Add capabilities
        AddCapability<ISecretsManagementService>();
        AddCapability<ISecretsManager>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxSecretCount", _configuration.GetValue("SecretsManagement:MaxSecretCount", "10000"));
        SetMetadata("SupportedContentTypes", "Text,Json,Binary,ConnectionString,ApiKey,Certificate,PrivateKey");
        SetMetadata("EncryptionAlgorithm", "AES-256-GCM");
        SetMetadata("ExternalProviders", string.Join(",", _externalProviders.Keys));

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            _serviceinitializing(Logger, null);

            // Initialize external providers
            foreach (var provider in _externalProviders.Values)
            {
                await provider.InitializeAsync();
                _externalProviderInitialized(Logger, provider.ProviderType, null);
            }

            // Load existing secrets metadata from the enclave
            await RefreshSecretCacheAsync();

            _serviceInitialized(Logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _initializationError(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            _enclaveInitializing(Logger, null);
            await _enclaveManager.InitializeEnclaveAsync();
            return true;
        }
        catch (Exception ex)
        {
            _enclaveInitializationError(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            _serviceStarting(Logger, null);
            await RefreshSecretCacheAsync();
            return true;
        }
        catch (Exception ex)
        {
            _serviceStartError(Logger, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            _serviceStopping(Logger, null);
            _secretCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _serviceStopError(Logger, ex);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<SecretMetadata> StoreSecretAsync(string secretId, string name, SecureString value, StoreSecretOptions? options = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            _secretStoring(Logger, secretId, null);

            options ??= new StoreSecretOptions();

            // Check if secret already exists
            var existingSecret = await GetSecretMetadataAsync(secretId, null, blockchainType, cancellationToken);
            if (existingSecret != null && !options.Overwrite)
            {
                throw new InvalidOperationException($"Secret {secretId} already exists. Use Overwrite=true to replace it.");
            }

            // Create secret metadata
            var metadata = new SecretMetadata
            {
                SecretId = secretId,
                Name = name,
                Description = options.Description,
                Version = existingSecret?.Version + 1 ?? 1,
                Tags = options.Tags,
                CreatedAt = existingSecret?.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = options.ExpiresAt,
                ContentType = options.ContentType,
                AccessControl = options.AccessControl ?? new SecretAccessControl()
            };

            // Encrypt and store the secret in the enclave
            var secretData = new
            {
                metadata = metadata,
                value = SecureStringToString(value)
            };

            string jsonPayload = JsonSerializer.Serialize(secretData);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("storeSecret", jsonPayload, cancellationToken);

            // Parse the result to get updated metadata
            var storedMetadata = JsonSerializer.Deserialize<SecretMetadata>(result) ??
                throw new InvalidOperationException("Failed to deserialize stored secret metadata.");

            // Update the cache
            lock (_secretCache)
            {
                _secretCache[secretId] = storedMetadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            UpdateMetric("TotalSecretsStored", _secretCache.Count);

            _secretStored(Logger, secretId, null);

            return storedMetadata;
        });
    }

    /// <inheritdoc/>
    public async Task<Secret?> GetSecretAsync(string secretId, int? version = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            _secretRetrieving(Logger, secretId, version?.ToString() ?? "latest", null);

            // Prepare the request
            var request = new
            {
                secretId = secretId,
                version = version
            };

            string jsonPayload = JsonSerializer.Serialize(request);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("getSecret", jsonPayload, cancellationToken);

            if (string.IsNullOrEmpty(result) || result == "null")
            {
                return null;
            }

            // Parse the result
            var secretData = JsonSerializer.Deserialize<JsonElement>(result);

            var metadata = JsonSerializer.Deserialize<SecretMetadata>(secretData.GetProperty("metadata").GetRawText()) ??
                throw new InvalidOperationException("Failed to deserialize secret metadata.");

            var valueString = secretData.GetProperty("value").GetString() ?? string.Empty;
            var secureValue = StringToSecureString(valueString);

            // Update access timestamp
            metadata.LastAccessedAt = DateTime.UtcNow;
            await UpdateSecretLastAccessedAsync(secretId, cancellationToken);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            _secretRetrieved(Logger, secretId, null);

            return new Secret
            {
                Metadata = metadata,
                Value = secureValue
            };
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            _secretRetrievalError(Logger, secretId, ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SecretMetadata?> GetSecretMetadataAsync(string secretId, int? version = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check the cache first
            lock (_secretCache)
            {
                if (version == null && _secretCache.TryGetValue(secretId, out var cachedMetadata))
                {
                    _successCount++;
                    UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                    return cachedMetadata;
                }
            }

            // Get metadata from the enclave
            var request = new
            {
                secretId = secretId,
                version = version
            };

            string jsonPayload = JsonSerializer.Serialize(request);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("getSecretMetadata", jsonPayload, cancellationToken);

            if (string.IsNullOrEmpty(result) || result == "null")
            {
                return null;
            }

            var metadata = JsonSerializer.Deserialize<SecretMetadata>(result) ??
                throw new InvalidOperationException("Failed to deserialize secret metadata.");

            // Update the cache
            lock (_secretCache)
            {
                _secretCache[secretId] = metadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return metadata;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            _secretMetadataRetrievalError(Logger, secretId, ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SecretMetadata>> ListSecretsAsync(GetSecretsOptions? options = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            options ??= new GetSecretsOptions();

            string jsonPayload = JsonSerializer.Serialize(options);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("listSecrets", jsonPayload, cancellationToken);

            var secretList = JsonSerializer.Deserialize<List<SecretMetadata>>(result) ??
                throw new InvalidOperationException("Failed to deserialize secret list.");

            // Update the cache
            lock (_secretCache)
            {
                foreach (var secret in secretList)
                {
                    _secretCache[secret.SecretId] = secret;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return secretList;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            _secretListError(Logger, ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SecretMetadata> UpdateSecretAsync(string secretId, SecureString value, string? description = null, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            _secretUpdating(Logger, secretId, null);

            // Get existing secret metadata
            var existingMetadata = await GetSecretMetadataAsync(secretId, null, blockchainType, cancellationToken);
            if (existingMetadata == null)
            {
                throw new InvalidOperationException($"Secret {secretId} not found.");
            }

            // Update metadata
            existingMetadata.UpdatedAt = DateTime.UtcNow;
            existingMetadata.Version++;
            if (!string.IsNullOrEmpty(description))
            {
                existingMetadata.Description = description;
            }

            // Update the secret in the enclave
            var secretData = new
            {
                metadata = existingMetadata,
                value = SecureStringToString(value)
            };

            string jsonPayload = JsonSerializer.Serialize(secretData);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("updateSecret", jsonPayload, cancellationToken);

            var updatedMetadata = JsonSerializer.Deserialize<SecretMetadata>(result) ??
                throw new InvalidOperationException("Failed to deserialize updated secret metadata.");

            // Update the cache
            lock (_secretCache)
            {
                _secretCache[secretId] = updatedMetadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            _secretUpdated(Logger, secretId, null);

            return updatedMetadata;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSecretAsync(string secretId, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            _secretDeleting(Logger, secretId, null);

            var request = new
            {
                secretId = secretId
            };

            string jsonPayload = JsonSerializer.Serialize(request);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("deleteSecret", jsonPayload, cancellationToken);

            bool deleted = bool.Parse(result);

            // Remove from cache if successful
            if (deleted)
            {
                lock (_secretCache)
                {
                    _secretCache.Remove(secretId);
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            _secretDeleted(Logger, secretId, null);

            return deleted;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            _secretDeletionError(Logger, secretId, ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SecretMetadata> RotateSecretAsync(string secretId, SecureString newValue, bool disableOldVersion = true, BlockchainType blockchainType = BlockchainType.NeoN3, CancellationToken cancellationToken = default)
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

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            _secretRotating(Logger, secretId, null);

            // Get existing secret metadata
            var existingMetadata = await GetSecretMetadataAsync(secretId, null, blockchainType, cancellationToken);
            if (existingMetadata == null)
            {
                throw new InvalidOperationException($"Secret {secretId} not found.");
            }

            // Create new version
            var newMetadata = new SecretMetadata
            {
                SecretId = secretId,
                Name = existingMetadata.Name,
                Description = $"Rotated version from {existingMetadata.Version}",
                Version = existingMetadata.Version + 1,
                Tags = existingMetadata.Tags,
                CreatedAt = existingMetadata.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = existingMetadata.ExpiresAt,
                ContentType = existingMetadata.ContentType,
                AccessControl = existingMetadata.AccessControl
            };

            // Rotate the secret in the enclave
            var rotateData = new
            {
                secretId = secretId,
                newMetadata = newMetadata,
                newValue = SecureStringToString(newValue),
                disableOldVersion = disableOldVersion
            };

            string jsonPayload = JsonSerializer.Serialize(rotateData);
            string result = await _enclaveManager.CallEnclaveFunctionAsync("rotateSecret", jsonPayload, cancellationToken);

            var rotatedMetadata = JsonSerializer.Deserialize<SecretMetadata>(result) ??
                throw new InvalidOperationException("Failed to deserialize rotated secret metadata.");

            // Update the cache
            lock (_secretCache)
            {
                _secretCache[secretId] = rotatedMetadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            UpdateMetric("TotalSecretsRotated", (_successCount).ToString());

            _secretRotated(Logger, secretId, rotatedMetadata.Version, null);

            return rotatedMetadata;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ConfigureExternalProviderAsync(ExternalSecretProviderType providerType, Dictionary<string, string> configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _externalProviderConfiguring(Logger, providerType, null);

            if (!_externalProviders.TryGetValue(providerType, out var provider))
            {
                _externalProviderNotRegistered(Logger, providerType, null);
                return false;
            }

            await provider.ConfigureAsync(configuration, cancellationToken);

            _externalProviderConfigured(Logger, providerType, null);
            return true;
        }
        catch (Exception ex)
        {
            _externalProviderConfigurationError(Logger, providerType, ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> SynchronizeWithExternalProviderAsync(ExternalSecretProviderType providerType, IEnumerable<string>? secretIds = null, SyncDirection direction = SyncDirection.Pull, CancellationToken cancellationToken = default)
    {
        try
        {
            _externalProviderSynchronizing(Logger, providerType, direction, null);

            if (!_externalProviders.TryGetValue(providerType, out var provider))
            {
                throw new InvalidOperationException($"External provider {providerType} not registered");
            }

            int syncCount = 0;

            if (direction == SyncDirection.Pull || direction == SyncDirection.Bidirectional)
            {
                syncCount += await SyncFromExternalProvider(provider, secretIds, cancellationToken);
            }

            if (direction == SyncDirection.Push || direction == SyncDirection.Bidirectional)
            {
                syncCount += await SyncToExternalProvider(provider, secretIds, cancellationToken);
            }

            _externalProviderSynchronized(Logger, syncCount, providerType, null);
            return syncCount;
        }
        catch (Exception ex)
        {
            _externalProviderSynchronizationError(Logger, providerType, ex);
            throw;
        }
    }

    #region ISecretsManager Implementation

    async Task<SecretMetadata> ISecretsManager.StoreSecretAsync(string secretId, string name, SecureString value, StoreSecretOptions? options, CancellationToken cancellationToken)
    {
        return await StoreSecretAsync(secretId, name, value, options, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<Secret?> ISecretsManager.GetSecretAsync(string secretId, int? version, CancellationToken cancellationToken)
    {
        return await GetSecretAsync(secretId, version, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<SecretMetadata?> ISecretsManager.GetSecretMetadataAsync(string secretId, int? version, CancellationToken cancellationToken)
    {
        return await GetSecretMetadataAsync(secretId, version, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<IEnumerable<SecretMetadata>> ISecretsManager.ListSecretsAsync(GetSecretsOptions? options, CancellationToken cancellationToken)
    {
        return await ListSecretsAsync(options, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<SecretMetadata> ISecretsManager.UpdateSecretAsync(string secretId, SecureString value, string? description, CancellationToken cancellationToken)
    {
        return await UpdateSecretAsync(secretId, value, description, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<bool> ISecretsManager.DeleteSecretAsync(string secretId, CancellationToken cancellationToken)
    {
        return await DeleteSecretAsync(secretId, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<SecretMetadata> ISecretsManager.CreateSecretVersionAsync(string secretId, SecureString value, string? description, CancellationToken cancellationToken)
    {
        return await RotateSecretAsync(secretId, value, false, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<IEnumerable<SecretMetadata>> ISecretsManager.ListSecretVersionsAsync(string secretId, CancellationToken cancellationToken)
    {
        var request = new { secretId = secretId };
        string jsonPayload = JsonSerializer.Serialize(request);
        string result = await _enclaveManager.CallEnclaveFunctionAsync("listSecretVersions", jsonPayload, cancellationToken);

        return JsonSerializer.Deserialize<List<SecretMetadata>>(result) ?? new List<SecretMetadata>();
    }

    async Task<SecretMetadata> ISecretsManager.RotateSecretAsync(string secretId, SecureString newValue, bool disableOldVersion, CancellationToken cancellationToken)
    {
        return await RotateSecretAsync(secretId, newValue, disableOldVersion, BlockchainType.NeoN3, cancellationToken);
    }

    async Task<byte[]> ISecretsManager.ExportSecretsAsync(IEnumerable<string>? secretIds, SecureString? encryptionKey, CancellationToken cancellationToken)
    {
        var request = new
        {
            secretIds = secretIds?.ToArray(),
            encryptionKey = encryptionKey != null ? SecureStringToString(encryptionKey) : null
        };

        string jsonPayload = JsonSerializer.Serialize(request);
        string result = await _enclaveManager.CallEnclaveFunctionAsync("exportSecrets", jsonPayload, cancellationToken);

        return Convert.FromBase64String(result);
    }

    async Task<int> ISecretsManager.ImportSecretsAsync(byte[] backupData, SecureString decryptionKey, bool overwriteExisting, CancellationToken cancellationToken)
    {
        var request = new
        {
            backupData = Convert.ToBase64String(backupData),
            decryptionKey = SecureStringToString(decryptionKey),
            overwriteExisting = overwriteExisting
        };

        string jsonPayload = JsonSerializer.Serialize(request);
        string result = await _enclaveManager.CallEnclaveFunctionAsync("importSecrets", jsonPayload, cancellationToken);

        return int.Parse(result);
    }

    #endregion

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var health = IsEnclaveInitialized && IsRunning
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
        UpdateMetric("SecretCount", _secretCache.Count);
        UpdateMetric("ExternalProviderCount", _externalProviders.Count);

        return Task.CompletedTask;
    }

    private async Task RefreshSecretCacheAsync()
    {
        try
        {
            var secrets = await ListSecretsAsync(new GetSecretsOptions { Limit = int.MaxValue }, BlockchainType.NeoN3);

            lock (_secretCache)
            {
                _secretCache.Clear();
                foreach (var secret in secrets)
                {
                    _secretCache[secret.SecretId] = secret;
                }
            }

            _secretCacheRefreshed(Logger, _secretCache.Count, null);
        }
        catch (Exception ex)
        {
            _secretCacheRefreshError(Logger, ex);
        }
    }

    private async Task UpdateSecretLastAccessedAsync(string secretId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new
            {
                secretId = secretId,
                lastAccessedAt = DateTime.UtcNow
            };

            string jsonPayload = JsonSerializer.Serialize(request);
            await _enclaveManager.CallEnclaveFunctionAsync("updateSecretLastAccessed", jsonPayload, cancellationToken);

            // Update cache
            lock (_secretCache)
            {
                if (_secretCache.TryGetValue(secretId, out var metadata))
                {
                    metadata.LastAccessedAt = DateTime.UtcNow;
                }
            }
        }
        catch (Exception ex)
        {
            _lastAccessedUpdateError(Logger, secretId, ex);
        }
    }

    private async Task<int> SyncFromExternalProvider(IExternalSecretProvider provider, IEnumerable<string>? secretIds, CancellationToken cancellationToken)
    {
        int count = 0;
        var secrets = await provider.ListSecretsAsync(secretIds, cancellationToken);

        foreach (var externalSecret in secrets)
        {
            try
            {
                var value = await provider.GetSecretAsync(externalSecret.SecretId, cancellationToken);
                if (value != null)
                {
                    await StoreSecretAsync(
                        externalSecret.SecretId,
                        externalSecret.Name,
                        value,
                        new StoreSecretOptions
                        {
                            Description = $"Synced from {provider.ProviderType}",
                            Tags = externalSecret.Tags,
                            ContentType = externalSecret.ContentType,
                            Overwrite = true
                        },
                        BlockchainType.NeoN3,
                        cancellationToken);

                    count++;
                }
            }
            catch (Exception ex)
            {
                _externalSecretSyncError(Logger, externalSecret.SecretId, ex);
            }
        }

        return count;
    }

    private async Task<int> SyncToExternalProvider(IExternalSecretProvider provider, IEnumerable<string>? secretIds, CancellationToken cancellationToken)
    {
        int count = 0;
        var localSecrets = await ListSecretsAsync(
            new GetSecretsOptions { Limit = int.MaxValue },
            BlockchainType.NeoN3,
            cancellationToken);

        var secretsToSync = secretIds != null
            ? localSecrets.Where(s => secretIds.Contains(s.SecretId))
            : localSecrets;

        foreach (var localSecret in secretsToSync)
        {
            try
            {
                var secret = await GetSecretAsync(localSecret.SecretId, null, BlockchainType.NeoN3, cancellationToken);
                if (secret?.Value != null)
                {
                    await provider.StoreSecretAsync(localSecret.SecretId, localSecret.Name, secret.Value, cancellationToken);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _externalSecretPushError(Logger, localSecret.SecretId, ex);
            }
        }

        return count;
    }

    private static string SecureStringToString(SecureString secureString)
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr) ?? string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }

    private static SecureString StringToSecureString(string str)
    {
        var secureString = new SecureString();
        foreach (char c in str)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();
        return secureString;
    }
}
