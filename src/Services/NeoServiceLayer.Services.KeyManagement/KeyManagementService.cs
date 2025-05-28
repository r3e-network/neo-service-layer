using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using System.Text.Json;

namespace NeoServiceLayer.Services.KeyManagement;

/// <summary>
/// Implementation of the Key Management service.
/// </summary>
public class KeyManagementService : EnclaveBlockchainServiceBase, IKeyManagementService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, KeyMetadata> _keyCache = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyManagementService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    public KeyManagementService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<KeyManagementService> logger)
        : base("KeyManagement", "Trusted Key Management Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IKeyManagementService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxKeyCount", _configuration.GetValue("KeyManagement:MaxKeyCount", "1000"));
        SetMetadata("SupportedKeyTypes", "Secp256k1,Ed25519,RSA");
        SetMetadata("SupportedSigningAlgorithms", "ECDSA,EdDSA,RSA-PSS");
        SetMetadata("SupportedEncryptionAlgorithms", "ECIES,RSA-OAEP");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Key Management Service...");

            // Initialize service-specific components
            await RefreshKeyCacheAsync();

            Logger.LogInformation("Key Management Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Key Management Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Key Management Service enclave...");
            await _enclaveManager.InitializeEnclaveAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Key Management Service enclave.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Key Management Service...");

            // Load existing keys from the enclave
            await RefreshKeyCacheAsync();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Key Management Service.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Key Management Service...");
            _keyCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Key Management Service.");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<KeyMetadata> GenerateKeyAsync(string keyId, string keyType, string keyUsage, bool exportable, string description, BlockchainType blockchainType)
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

            // Generate key in the enclave
            string result = await _enclaveManager.KmsGenerateKeyAsync(keyId, keyType, keyUsage, exportable, description);

            // Parse the result
            var keyMetadata = JsonSerializer.Deserialize<KeyMetadata>(result) ??
                throw new InvalidOperationException("Failed to deserialize key metadata.");

            // Update the cache
            lock (_keyCache)
            {
                _keyCache[keyId] = keyMetadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return keyMetadata;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error generating key {KeyId} of type {KeyType} for blockchain {BlockchainType}",
                keyId, keyType, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<KeyMetadata> GetKeyMetadataAsync(string keyId, BlockchainType blockchainType)
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
            lock (_keyCache)
            {
                if (_keyCache.TryGetValue(keyId, out var cachedMetadata))
                {
                    _successCount++;
                    UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                    return cachedMetadata;
                }
            }

            // Get key metadata from the enclave
            string result = await _enclaveManager.KmsGetKeyMetadataAsync(keyId);

            // Parse the result
            var keyMetadata = JsonSerializer.Deserialize<KeyMetadata>(result) ??
                throw new InvalidOperationException("Failed to deserialize key metadata.");

            // Update the cache
            lock (_keyCache)
            {
                _keyCache[keyId] = keyMetadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return keyMetadata;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting key metadata for key {KeyId} for blockchain {BlockchainType}",
                keyId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<KeyMetadata>> ListKeysAsync(int skip, int take, BlockchainType blockchainType)
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

            // List keys from the enclave
            string result = await _enclaveManager.KmsListKeysAsync(skip, take);

            // Parse the result
            var keyList = JsonSerializer.Deserialize<List<KeyMetadata>>(result) ??
                throw new InvalidOperationException("Failed to deserialize key list.");

            // Update the cache
            lock (_keyCache)
            {
                foreach (var key in keyList)
                {
                    _keyCache[key.KeyId] = key;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return keyList;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error listing keys for blockchain {BlockchainType}", blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> SignDataAsync(string keyId, string dataHex, string signingAlgorithm, BlockchainType blockchainType)
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

            // Sign data in the enclave
            string result = await _enclaveManager.KmsSignDataAsync(keyId, dataHex, signingAlgorithm);

            // Update the key's last used timestamp
            await UpdateKeyLastUsedAsync(keyId);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error signing data with key {KeyId} using algorithm {SigningAlgorithm} for blockchain {BlockchainType}",
                keyId, signingAlgorithm, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifySignatureAsync(string keyIdOrPublicKeyHex, string dataHex, string signatureHex, string signingAlgorithm, BlockchainType blockchainType)
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

            // Verify signature in the enclave
            bool result = await _enclaveManager.KmsVerifySignatureAsync(keyIdOrPublicKeyHex, dataHex, signatureHex, signingAlgorithm);

            // If a key ID was used, update the key's last used timestamp
            if (!keyIdOrPublicKeyHex.StartsWith("0x") && keyIdOrPublicKeyHex.Length < 64)
            {
                await UpdateKeyLastUsedAsync(keyIdOrPublicKeyHex);
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error verifying signature with key {KeyIdOrPublicKeyHex} using algorithm {SigningAlgorithm} for blockchain {BlockchainType}",
                keyIdOrPublicKeyHex, signingAlgorithm, blockchainType);
            throw;
        }
    }
    /// <inheritdoc/>
    public async Task<string> EncryptDataAsync(string keyIdOrPublicKeyHex, string dataHex, string encryptionAlgorithm, BlockchainType blockchainType)
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

            // Encrypt data in the enclave
            string result = await _enclaveManager.KmsEncryptDataAsync(keyIdOrPublicKeyHex, dataHex, encryptionAlgorithm);

            // If a key ID was used, update the key's last used timestamp
            if (!keyIdOrPublicKeyHex.StartsWith("0x") && keyIdOrPublicKeyHex.Length < 64)
            {
                await UpdateKeyLastUsedAsync(keyIdOrPublicKeyHex);
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error encrypting data with key {KeyIdOrPublicKeyHex} using algorithm {EncryptionAlgorithm} for blockchain {BlockchainType}",
                keyIdOrPublicKeyHex, encryptionAlgorithm, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> DecryptDataAsync(string keyId, string encryptedDataHex, string encryptionAlgorithm, BlockchainType blockchainType)
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

            // Decrypt data in the enclave
            string result = await _enclaveManager.KmsDecryptDataAsync(keyId, encryptedDataHex, encryptionAlgorithm);

            // Update the key's last used timestamp
            await UpdateKeyLastUsedAsync(keyId);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error decrypting data with key {KeyId} using algorithm {EncryptionAlgorithm} for blockchain {BlockchainType}",
                keyId, encryptionAlgorithm, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteKeyAsync(string keyId, BlockchainType blockchainType)
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

            // Delete key in the enclave
            bool result = await _enclaveManager.KmsDeleteKeyAsync(keyId);

            // Remove from cache if successful
            if (result)
            {
                lock (_keyCache)
                {
                    _keyCache.Remove(keyId);
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error deleting key {KeyId} for blockchain {BlockchainType}",
                keyId, blockchainType);
            throw;
        }
    }

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
        // Update service metrics
        UpdateMetric("RequestCount", _requestCount);
        UpdateMetric("SuccessCount", _successCount);
        UpdateMetric("FailureCount", _failureCount);
        UpdateMetric("SuccessRate", _requestCount > 0 ? (double)_successCount / _requestCount : 0);
        UpdateMetric("LastRequestTime", _lastRequestTime);
        UpdateMetric("KeyCount", _keyCache.Count);

        return Task.CompletedTask;
    }

    private async Task RefreshKeyCacheAsync()
    {
        try
        {
            // Get all keys from the enclave
            var keys = await ListKeysAsync(0, int.MaxValue, BlockchainType.NeoN3);

            // Update the cache
            lock (_keyCache)
            {
                _keyCache.Clear();
                foreach (var key in keys)
                {
                    _keyCache[key.KeyId] = key;
                }
            }

            Logger.LogInformation("Key cache refreshed. {KeyCount} keys loaded.", _keyCache.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing key cache.");
        }
    }

    private async Task UpdateKeyLastUsedAsync(string keyId)
    {
        try
        {
            // Get the key metadata
            var metadata = await GetKeyMetadataAsync(keyId, BlockchainType.NeoN3);

            // Update the last used timestamp
            metadata.LastUsedAt = DateTime.UtcNow;

            // Update the cache
            lock (_keyCache)
            {
                _keyCache[keyId] = metadata;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating last used timestamp for key {KeyId}.", keyId);
        }
    }
}