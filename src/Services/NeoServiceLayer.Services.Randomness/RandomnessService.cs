using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Tee.Host.Services;

// Use Infrastructure namespace for IBlockchainClientFactory
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;

namespace NeoServiceLayer.Services.Randomness;

/// <summary>
/// Exception thrown when randomness operations fail.
/// </summary>
public class RandomnessException : Exception
{
    public RandomnessException(string message) : base(message) { }
    public RandomnessException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Represents a verifiable random result.
/// </summary>
public class VerifiableRandomResult
{
    /// <summary>
    /// Gets or sets the random value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the seed used for the random number generation.
    /// </summary>
    public string Seed { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof of the random number generation.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the random number generation.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the block height at which the randomness was generated.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the block hash at which the randomness was generated.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Implementation of the Randomness service.
/// </summary>
public partial class RandomnessService : EnclaveBlockchainServiceBase, IRandomnessService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IBlockchainClientFactory _blockchainClientFactory;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, VerifiableRandomResult> _results = new();
    private readonly IServiceProvider? _serviceProvider;
    private IKeyManagementService? _keyManagementService;
    private const string RandomnessSigningKeyId = "randomness-signing-key";
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomnessService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="blockchainClientFactory">The blockchain client factory.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public RandomnessService(
        IEnclaveManager enclaveManager,
        IBlockchainClientFactory blockchainClientFactory,
        IServiceConfiguration configuration,
        ILogger<RandomnessService> logger,
        IServiceProvider? serviceProvider = null)
        : base("Randomness", "Provably Fair Randomness Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
        _blockchainClientFactory = blockchainClientFactory;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IRandomnessService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxRandomNumberRange", _configuration.GetValue("Randomness:MaxRandomNumberRange", "1000000"));
        SetMetadata("MaxRandomBytesLength", _configuration.GetValue("Randomness:MaxRandomBytesLength", "1024"));
        SetMetadata("MaxRandomStringLength", _configuration.GetValue("Randomness:MaxRandomStringLength", "1000"));
        SetMetadata("DefaultCharset", _configuration.GetValue("Randomness:DefaultCharset", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"));

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    public async Task<int> GenerateRandomNumberAsync(int min, int max, BlockchainType blockchainType)
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

        if (min > max)
        {
            throw new ArgumentException("Minimum value must be less than or equal to maximum value.", nameof(min));
        }

        var maxRange = int.Parse(_configuration.GetValue("Randomness:MaxRandomNumberRange", "1000000"));
        if (max - min > maxRange)
        {
            throw new ArgumentException($"Range between min and max must not exceed {maxRange}.", nameof(max));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Generate random number in the enclave
            var result = await _enclaveManager.GenerateRandomAsync(min, max);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (CryptographicException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Cryptographic error during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to cryptographic error", ex);
        }
        catch (InvalidOperationException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Invalid operation during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to invalid operation", ex);
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error generating random number between {Min} and {Max} for blockchain {BlockchainType}",
                min, max, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> GenerateRandomBytesAsync(int length, BlockchainType blockchainType)
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

        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        }

        var maxLength = int.Parse(_configuration.GetValue("Randomness:MaxRandomBytesLength", "1024"));
        if (length > maxLength)
        {
            throw new ArgumentException($"Length must not exceed {maxLength}.", nameof(length));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Generate random bytes efficiently in the enclave using batch generation
            byte[] result = await _enclaveManager.GenerateRandomBytesAsync(length);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (CryptographicException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Cryptographic error during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to cryptographic error", ex);
        }
        catch (InvalidOperationException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Invalid operation during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to invalid operation", ex);
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error generating {Length} random bytes for blockchain {BlockchainType}",
                length, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateRandomStringAsync(int length, string? charset, BlockchainType blockchainType)
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

        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        }

        var maxLength = int.Parse(_configuration.GetValue("Randomness:MaxRandomStringLength", "1000"));
        if (length > maxLength)
        {
            throw new ArgumentException($"Length must not exceed {maxLength}.", nameof(length));
        }

        // Use default charset if none provided
        charset ??= _configuration.GetValue("Randomness:DefaultCharset", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
        if (string.IsNullOrEmpty(charset))
        {
            throw new ArgumentException("Character set cannot be empty.", nameof(charset));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Generate random string efficiently using batch random bytes
            byte[] randomBytes = await _enclaveManager.GenerateRandomBytesAsync(length);
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                int randomIndex = randomBytes[i] % charset.Length;
                result[i] = charset[randomIndex];
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return new string(result);
        }
        catch (CryptographicException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Cryptographic error during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to cryptographic error", ex);
        }
        catch (InvalidOperationException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Invalid operation during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to invalid operation", ex);
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error generating random string of length {Length} for blockchain {BlockchainType}",
                length, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VerifiableRandomResult> GenerateVerifiableRandomNumberAsync(int min, int max, string seed, BlockchainType blockchainType)
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

        if (min > max)
        {
            throw new ArgumentException("Minimum value must be less than or equal to maximum value.", nameof(min));
        }

        var maxRange = int.Parse(_configuration.GetValue("Randomness:MaxRandomNumberRange", "1000000"));
        if (max - min > maxRange)
        {
            throw new ArgumentException($"Range between min and max must not exceed {maxRange}.", nameof(max));
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Generate a random number using the enclave's secure random number generator
            int randomValue = await _enclaveManager.GenerateRandomAsync(min, max);

            // Get the latest block hash from the blockchain to use as additional entropy
            var blockchainClient = _blockchainClientFactory.CreateClient(blockchainType);
            long blockHeight = await blockchainClient.GetBlockHeightAsync();
            var blockHash = await blockchainClient.GetBlockHashAsync(blockHeight);

            // Create a proof by signing the seed, block hash, and random value
            string dataToSign = $"{seed}:{blockHash}:{randomValue}";
            string signatureHex;

            if (_keyManagementService != null)
            {
                // Use key management service for secure key handling
                var dataBytes = Encoding.UTF8.GetBytes(dataToSign);
                var dataHex = Convert.ToHexString(dataBytes);
                signatureHex = await _keyManagementService.SignDataAsync(
                    RandomnessSigningKeyId,
                    dataHex,
                    "ECDSA_SHA256",
                    blockchainType);
            }
            else
            {
                // Fallback: use enclave manager with a generated key
                // This should only be used in development/testing
                Logger.LogWarning("Using fallback key management. This is not secure for production!");
                string privateKeyHex = GenerateDevelopmentKey();
                signatureHex = await _enclaveManager.SignDataAsync(dataToSign, privateKeyHex);
            }

            byte[] signature = Convert.FromHexString(signatureHex);

            var requestId = Guid.NewGuid().ToString();
            var result = new VerifiableRandomResult
            {
                RequestId = requestId,
                Value = randomValue,
                Seed = seed,
                Proof = Convert.ToBase64String(signature),
                Timestamp = DateTime.UtcNow,
                BlockchainType = blockchainType,
                BlockHeight = blockHeight,
                BlockHash = blockHash
            };

            // Store the result for verification
            _results[requestId] = result;

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            UpdateMetric("StoredResultCount", _results.Count);
            return result;
        }
        catch (CryptographicException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Cryptographic error during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to cryptographic error", ex);
        }
        catch (InvalidOperationException ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            Logger.LogError(ex, "Invalid operation during random number generation for blockchain {BlockchainType}", blockchainType);
            throw new RandomnessException("Random number generation failed due to invalid operation", ex);
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error generating verifiable random number between {Min} and {Max} for blockchain {BlockchainType}",
                min, max, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyRandomNumberAsync(VerifiableRandomResult result)
    {
        if (!SupportsBlockchain(result.BlockchainType))
        {
            throw new NotSupportedException($"Blockchain type {result.BlockchainType} is not supported.");
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
            // Verify the proof
            string dataToVerify = $"{result.Seed}:{result.BlockHash}:{result.Value}";
            byte[] signature = Convert.FromBase64String(result.Proof);
            bool isValid;

            if (_keyManagementService != null)
            {
                // Use key management service for verification
                var dataBytes = Encoding.UTF8.GetBytes(dataToVerify);
                var dataHex = Convert.ToHexString(dataBytes);
                var signatureHex = Convert.ToHexString(signature);

                // Get the public key for verification
                var keyMetadata = await _keyManagementService.GetKeyMetadataAsync(RandomnessSigningKeyId, result.BlockchainType);
                if (keyMetadata?.PublicKeyHex == null)
                {
                    throw new InvalidOperationException("Public key not available for verification");
                }

                isValid = await _keyManagementService.VerifySignatureAsync(
                    keyMetadata.PublicKeyHex,
                    dataHex,
                    signatureHex,
                    "ECDSA_SHA256",
                    result.BlockchainType);
            }
            else
            {
                // Fallback: use enclave manager
                Logger.LogWarning("Using fallback key verification. This is not secure for production!");
                string publicKeyHex = GenerateDevelopmentPublicKey();
                isValid = await _enclaveManager.VerifySignatureAsync(dataToVerify, result.Proof, publicKeyHex);
            }

            return isValid;
        }
        catch (CryptographicException ex)
        {
            Logger.LogError(ex, "Cryptographic error verifying random number for blockchain {BlockchainType}",
                result.BlockchainType);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation verifying random number for blockchain {BlockchainType}",
                result.BlockchainType);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error verifying random number for blockchain {BlockchainType}",
                result.BlockchainType);
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        // Initialize the service
        Logger.LogInformation("Initializing Randomness service...");

        // Initialize persistent storage
        await InitializePersistentStorageAsync();

        // Load configuration
        var maxRandomNumberRange = _configuration.GetValue("Randomness:MaxRandomNumberRange", "1000000");
        var maxRandomBytesLength = _configuration.GetValue("Randomness:MaxRandomBytesLength", "1024");
        var maxRandomStringLength = _configuration.GetValue("Randomness:MaxRandomStringLength", "1000");

        Logger.LogInformation("Randomness service configuration: MaxRandomNumberRange={MaxRandomNumberRange}, MaxRandomBytesLength={MaxRandomBytesLength}, MaxRandomStringLength={MaxRandomStringLength}",
            maxRandomNumberRange, maxRandomBytesLength, maxRandomStringLength);

        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        // Initialize the enclave
        Logger.LogInformation("Initializing enclave for Randomness service...");
        return await _enclaveManager.InitializeEnclaveAsync();
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        // Start the service
        Logger.LogInformation("Starting Randomness service...");

        // Get key management service from service provider
        if (_serviceProvider != null)
        {
            _keyManagementService = _serviceProvider.GetService(typeof(IKeyManagementService)) as IKeyManagementService;
            if (_keyManagementService != null)
            {
                // Ensure the signing key exists
                await EnsureSigningKeyExistsAsync();
            }
            else
            {
                Logger.LogWarning("KeyManagementService not available. Will use legacy key management.");
            }
        }

        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        // Stop the service
        Logger.LogInformation("Stopping Randomness service...");
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check the health of the service
        if (!IsEnclaveInitialized)
        {
            return ServiceHealth.Degraded;
        }

        // Check if there have been too many failures
        if (_requestCount > 0 && (double)_failureCount / _requestCount > 0.5)
        {
            return ServiceHealth.Degraded;
        }

        return await Task.FromResult(ServiceHealth.Healthy);
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
        UpdateMetric("StoredResultCount", _results.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposePersistenceResources();
        }
        base.Dispose(disposing);
    }

    private async Task EnsureSigningKeyExistsAsync()
    {
        if (_keyManagementService == null) return;

        try
        {
            // Check if key exists
            var keyMetadata = await _keyManagementService.GetKeyMetadataAsync(RandomnessSigningKeyId, BlockchainType.NeoN3);
        }
        catch (Exception)
        {
            // Key doesn't exist, create it
            Logger.LogInformation("Creating randomness signing key");
            await _keyManagementService.CreateKeyAsync(
                RandomnessSigningKeyId,
                "Secp256k1",
                "Sign,Verify",
                false, // Not exportable for security
                "Randomness service signing key for verifiable random number generation",
                BlockchainType.NeoN3);
        }
    }

    private string GenerateDevelopmentKey()
    {
        // Generate a deterministic development key based on service instance
        // This is NOT secure and should only be used in development
        using var sha256 = SHA256.Create();
        var instanceId = $"dev-randomness-{Environment.MachineName}-{Environment.ProcessId}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(instanceId));
        return Convert.ToHexString(hash);
    }

    private string GenerateDevelopmentPublicKey()
    {
        // Generate a deterministic development public key
        // This is NOT secure and should only be used in development
        using var sha256 = SHA256.Create();
        var instanceId = $"dev-randomness-pubkey-{Environment.MachineName}-{Environment.ProcessId}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(instanceId));
        return Convert.ToHexString(hash);
    }
}
