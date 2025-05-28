using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;
using System.Security.Cryptography;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for cryptographic services with key management capabilities.
/// </summary>
public abstract class CryptographicServiceBase : EnclaveBlockchainServiceBase
{
    private readonly Dictionary<string, CryptoKeyInfo> _managedKeys = new();
    private readonly object _keysLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptographicServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="enclaveManager">The enclave manager (optional).</param>
    protected CryptographicServiceBase(string name, string description, string version, ILogger logger, IServiceConfiguration? configuration = null, IEnclaveManager? enclaveManager = null)
        : base(name, description, version, logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        Configuration = configuration;
        AddCapability<ICryptographicService>();
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected IServiceConfiguration? Configuration { get; }

    /// <summary>
    /// Gets the managed cryptographic keys.
    /// </summary>
    protected IEnumerable<CryptoKeyInfo> ManagedKeys
    {
        get
        {
            lock (_keysLock)
            {
                return _managedKeys.Values.ToList();
            }
        }
    }

    /// <summary>
    /// Generates a new cryptographic key within the enclave.
    /// </summary>
    /// <param name="keyType">The type of key to generate.</param>
    /// <param name="keySize">The key size in bits.</param>
    /// <param name="keyUsage">The intended usage of the key.</param>
    /// <returns>The key ID.</returns>
    protected virtual async Task<string> GenerateKeyAsync(CryptoKeyType keyType, int keySize, CryptoKeyUsage keyUsage)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var keyId = Guid.NewGuid().ToString();
            var keyInfo = new CryptoKeyInfo
            {
                Id = keyId,
                Type = keyType,
                Size = keySize,
                Usage = keyUsage,
                CreatedAt = DateTime.UtcNow,
                IsHardwareBacked = true
            };

            // Generate the actual key within the enclave
            await GenerateKeyInEnclaveAsync(keyInfo);

            lock (_keysLock)
            {
                _managedKeys[keyId] = keyInfo;
            }

            Logger.LogInformation("Generated {KeyType} key {KeyId} with size {KeySize} for service {ServiceName}",
                keyType, keyId, keySize, Name);

            return keyId;
        });
    }

    /// <summary>
    /// Signs data using a managed key within the enclave.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="data">The data to sign.</param>
    /// <param name="algorithm">The signing algorithm.</param>
    /// <returns>The signature.</returns>
    protected virtual async Task<byte[]> SignDataAsync(string keyId, byte[] data, string algorithm = "SHA256withECDSA")
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);
        ArgumentNullException.ThrowIfNull(data);

        return await ExecuteInEnclaveAsync(async () =>
        {
            var keyInfo = GetKeyInfo(keyId);
            if (keyInfo == null)
            {
                throw new ArgumentException($"Key {keyId} not found", nameof(keyId));
            }

            if (!keyInfo.Usage.HasFlag(CryptoKeyUsage.Signing))
            {
                throw new InvalidOperationException($"Key {keyId} is not configured for signing");
            }

            // Perform signing within the enclave
            var signature = await SignDataInEnclaveAsync(keyId, data, algorithm);

            // Update key usage statistics
            keyInfo.LastUsed = DateTime.UtcNow;
            keyInfo.UsageCount++;

            Logger.LogDebug("Signed data with key {KeyId} using algorithm {Algorithm} for service {ServiceName}",
                keyId, algorithm, Name);

            return signature;
        });
    }

    /// <summary>
    /// Verifies a signature using a managed key within the enclave.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="data">The original data.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="algorithm">The signing algorithm.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    protected virtual async Task<bool> VerifySignatureAsync(string keyId, byte[] data, byte[] signature, string algorithm = "SHA256withECDSA")
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);

        return await ExecuteInEnclaveAsync(async () =>
        {
            var keyInfo = GetKeyInfo(keyId);
            if (keyInfo == null)
            {
                throw new ArgumentException($"Key {keyId} not found", nameof(keyId));
            }

            if (!keyInfo.Usage.HasFlag(CryptoKeyUsage.Verification))
            {
                throw new InvalidOperationException($"Key {keyId} is not configured for verification");
            }

            // Perform verification within the enclave
            var isValid = await VerifySignatureInEnclaveAsync(keyId, data, signature, algorithm);

            // Update key usage statistics
            keyInfo.LastUsed = DateTime.UtcNow;
            keyInfo.UsageCount++;

            Logger.LogDebug("Verified signature with key {KeyId} using algorithm {Algorithm} for service {ServiceName}. Result: {IsValid}",
                keyId, algorithm, Name, isValid);

            return isValid;
        });
    }

    /// <summary>
    /// Encrypts data using a managed key within the enclave.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="algorithm">The encryption algorithm.</param>
    /// <returns>The encrypted data.</returns>
    protected virtual async Task<byte[]> EncryptDataAsync(string keyId, byte[] data, string algorithm = "AES-256-GCM")
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);
        ArgumentNullException.ThrowIfNull(data);

        return await ExecuteInEnclaveAsync(async () =>
        {
            var keyInfo = GetKeyInfo(keyId);
            if (keyInfo == null)
            {
                throw new ArgumentException($"Key {keyId} not found", nameof(keyId));
            }

            if (!keyInfo.Usage.HasFlag(CryptoKeyUsage.Encryption))
            {
                throw new InvalidOperationException($"Key {keyId} is not configured for encryption");
            }

            // Perform encryption within the enclave
            var encryptedData = await EncryptDataInEnclaveAsync(keyId, data, algorithm);

            // Update key usage statistics
            keyInfo.LastUsed = DateTime.UtcNow;
            keyInfo.UsageCount++;

            Logger.LogDebug("Encrypted data with key {KeyId} using algorithm {Algorithm} for service {ServiceName}",
                keyId, algorithm, Name);

            return encryptedData;
        });
    }

    /// <summary>
    /// Decrypts data using a managed key within the enclave.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <param name="encryptedData">The encrypted data.</param>
    /// <param name="algorithm">The encryption algorithm.</param>
    /// <returns>The decrypted data.</returns>
    protected virtual async Task<byte[]> DecryptDataAsync(string keyId, byte[] encryptedData, string algorithm = "AES-256-GCM")
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);
        ArgumentNullException.ThrowIfNull(encryptedData);

        return await ExecuteInEnclaveAsync(async () =>
        {
            var keyInfo = GetKeyInfo(keyId);
            if (keyInfo == null)
            {
                throw new ArgumentException($"Key {keyId} not found", nameof(keyId));
            }

            if (!keyInfo.Usage.HasFlag(CryptoKeyUsage.Decryption))
            {
                throw new InvalidOperationException($"Key {keyId} is not configured for decryption");
            }

            // Perform decryption within the enclave
            var decryptedData = await DecryptDataInEnclaveAsync(keyId, encryptedData, algorithm);

            // Update key usage statistics
            keyInfo.LastUsed = DateTime.UtcNow;
            keyInfo.UsageCount++;

            Logger.LogDebug("Decrypted data with key {KeyId} using algorithm {Algorithm} for service {ServiceName}",
                keyId, algorithm, Name);

            return decryptedData;
        });
    }

    /// <summary>
    /// Gets key information by ID.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <returns>The key information, or null if not found.</returns>
    protected virtual CryptoKeyInfo? GetKeyInfo(string keyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);

        lock (_keysLock)
        {
            return _managedKeys.TryGetValue(keyId, out var keyInfo) ? keyInfo : null;
        }
    }

    /// <summary>
    /// Deletes a managed key.
    /// </summary>
    /// <param name="keyId">The key ID.</param>
    /// <returns>True if the key was deleted, false otherwise.</returns>
    protected virtual async Task<bool> DeleteKeyAsync(string keyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);

        return await ExecuteInEnclaveAsync(async () =>
        {
            lock (_keysLock)
            {
                if (!_managedKeys.Remove(keyId))
                {
                    Logger.LogWarning("Key {KeyId} not found for deletion in service {ServiceName}", keyId, Name);
                    return false;
                }
            }

            // Delete the key from the enclave
            await DeleteKeyInEnclaveAsync(keyId);

            Logger.LogInformation("Deleted key {KeyId} from service {ServiceName}", keyId, Name);
            return true;
        });
    }

    // Abstract methods to be implemented by derived classes for enclave operations
    protected abstract Task GenerateKeyInEnclaveAsync(CryptoKeyInfo keyInfo);
    protected abstract Task<byte[]> SignDataInEnclaveAsync(string keyId, byte[] data, string algorithm);
    protected abstract Task<bool> VerifySignatureInEnclaveAsync(string keyId, byte[] data, byte[] signature, string algorithm);
    protected abstract Task<byte[]> EncryptDataInEnclaveAsync(string keyId, byte[] data, string algorithm);
    protected abstract Task<byte[]> DecryptDataInEnclaveAsync(string keyId, byte[] encryptedData, string algorithm);
    protected abstract Task DeleteKeyInEnclaveAsync(string keyId);

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing cryptographic service {ServiceName}", Name);

        // Initialize cryptographic service functionality

        // Initialize cryptographic subsystem
        await InitializeCryptographicSubsystemAsync();

        Logger.LogInformation("Cryptographic service {ServiceName} initialized successfully", Name);
        return true;
    }

    /// <summary>
    /// Initializes the cryptographic subsystem.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task InitializeCryptographicSubsystemAsync()
    {
        // Override in derived classes for specific initialization
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check cryptographic-specific health

        // Check if cryptographic subsystem is functioning
        try
        {
            // Perform a simple cryptographic operation to verify health
            using var rng = RandomNumberGenerator.Create();
            var testData = new byte[32];
            rng.GetBytes(testData);

            return Task.FromResult(ServiceHealth.Healthy);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cryptographic health check failed for service {ServiceName}", Name);
            return Task.FromResult(ServiceHealth.Unhealthy);
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting cryptographic service {ServiceName}", Name);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping cryptographic service {ServiceName}", Name);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Disposes the cryptographic service and cleans up resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clear managed keys
            lock (_keysLock)
            {
                _managedKeys.Clear();
            }
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Interface marker for cryptographic services.
/// </summary>
public interface ICryptographicService
{
}

/// <summary>
/// Cryptographic key information.
/// </summary>
public class CryptoKeyInfo
{
    public string Id { get; set; } = string.Empty;
    public CryptoKeyType Type { get; set; }
    public int Size { get; set; }
    public CryptoKeyUsage Usage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsed { get; set; }
    public long UsageCount { get; set; }
    public bool IsHardwareBacked { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Cryptographic key types.
/// </summary>
public enum CryptoKeyType
{
    RSA,
    ECDSA,
    ECDH,
    AES,
    ChaCha20,
    Ed25519
}

/// <summary>
/// Cryptographic key usage flags.
/// </summary>
[Flags]
public enum CryptoKeyUsage
{
    None = 0,
    Signing = 1,
    Verification = 2,
    Encryption = 4,
    Decryption = 8,
    KeyAgreement = 16,
    All = Signing | Verification | Encryption | Decryption | KeyAgreement
}
