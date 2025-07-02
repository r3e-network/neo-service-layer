using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// Implementation of the Secrets Management service.
/// </summary>
public partial class SecretsManagementService : EnclaveBlockchainServiceBase, ISecretsManagementService, ISecretsManager
{
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
            Logger.LogInformation("Initializing Secrets Management Service...");

            // Initialize external providers
            foreach (var provider in _externalProviders.Values)
            {
                await provider.InitializeAsync();
                Logger.LogInformation("Initialized external provider: {ProviderType}", provider.ProviderType);
            }

            // Load existing secrets metadata from the enclave
            await RefreshSecretCacheAsync();

            Logger.LogInformation("Secrets Management Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Secrets Management Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Secrets Management Service enclave...");
            await _enclaveManager.InitializeEnclaveAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Secrets Management Service enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Secrets Management Service...");
            await RefreshSecretCacheAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Secrets Management Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Secrets Management Service...");
            _secretCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Secrets Management Service");
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

            Logger.LogInformation("Storing secret {SecretId} securely within enclave", secretId);

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

            Logger.LogInformation("Successfully stored secret {SecretId} in enclave", secretId);

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

            Logger.LogDebug("Retrieving secret {SecretId} version {Version} from enclave", secretId, version?.ToString() ?? "latest");

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

            Logger.LogDebug("Successfully retrieved secret {SecretId}", secretId);

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
            Logger.LogError(ex, "Error retrieving secret {SecretId}", secretId);
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
            Logger.LogError(ex, "Error retrieving secret metadata for {SecretId}", secretId);
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
            Logger.LogError(ex, "Error listing secrets");
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

            Logger.LogInformation("Updating secret {SecretId} securely within enclave", secretId);

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

            Logger.LogInformation("Successfully updated secret {SecretId} in enclave", secretId);

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

            Logger.LogInformation("Deleting secret {SecretId} from enclave", secretId);

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

            Logger.LogInformation("Successfully deleted secret {SecretId} from enclave", secretId);

            return deleted;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error deleting secret {SecretId}", secretId);
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

            Logger.LogInformation("Rotating secret {SecretId} securely within enclave", secretId);

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

            Logger.LogInformation("Successfully rotated secret {SecretId} to version {Version} in enclave", secretId, rotatedMetadata.Version);

            return rotatedMetadata;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ConfigureExternalProviderAsync(ExternalSecretProviderType providerType, Dictionary<string, string> configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Configuring external provider {ProviderType}", providerType);

            if (!_externalProviders.TryGetValue(providerType, out var provider))
            {
                Logger.LogWarning("External provider {ProviderType} not registered", providerType);
                return false;
            }

            await provider.ConfigureAsync(configuration, cancellationToken);

            Logger.LogInformation("Successfully configured external provider {ProviderType}", providerType);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error configuring external provider {ProviderType}", providerType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> SynchronizeWithExternalProviderAsync(ExternalSecretProviderType providerType, IEnumerable<string>? secretIds = null, SyncDirection direction = SyncDirection.Pull, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Synchronizing with external provider {ProviderType}, direction: {Direction}", providerType, direction);

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

            Logger.LogInformation("Successfully synchronized {Count} secrets with external provider {ProviderType}", syncCount, providerType);
            return syncCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error synchronizing with external provider {ProviderType}", providerType);
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

            Logger.LogInformation("Secret cache refreshed. {SecretCount} secrets loaded.", _secretCache.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing secret cache");
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
            Logger.LogError(ex, "Error updating last accessed timestamp for secret {SecretId}", secretId);
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
                Logger.LogError(ex, "Error syncing secret {SecretId} from external provider", externalSecret.SecretId);
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
                Logger.LogError(ex, "Error syncing secret {SecretId} to external provider", localSecret.SecretId);
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
