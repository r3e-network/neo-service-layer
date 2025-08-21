using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Cryptography
{
    /// <summary>
    /// Comprehensive cryptographic service providing secure operations for all Neo Service Layer components
    /// Integrates with SGX enclaves for hardware-backed security
    /// </summary>
    public interface ICryptographicService
    {
        /// <summary>
        /// Encrypts data using specified algorithm and key
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="encryptionOptions">Encryption configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Encryption result with encrypted data and metadata</returns>
        Task<CryptographicResult> EncryptAsync(
            byte[] data,
            EncryptionOptions encryptionOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypts data using specified key and algorithm
        /// </summary>
        /// <param name="encryptedData">Data to decrypt</param>
        /// <param name="decryptionOptions">Decryption configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Decryption result with plaintext data</returns>
        Task<CryptographicResult> DecryptAsync(
            byte[] encryptedData,
            DecryptionOptions decryptionOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a cryptographic hash of the input data
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <param name="hashOptions">Hashing configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Hash result</returns>
        Task<HashResult> HashAsync(
            byte[] data,
            HashOptions hashOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a hash against the original data
        /// </summary>
        /// <param name="data">Original data</param>
        /// <param name="hash">Hash to verify</param>
        /// <param name="hashOptions">Hashing configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verification result</returns>
        Task<VerificationResult> VerifyHashAsync(
            byte[] data,
            byte[] hash,
            HashOptions hashOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a digital signature for the given data
        /// </summary>
        /// <param name="data">Data to sign</param>
        /// <param name="signingOptions">Signing configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Signature result</returns>
        Task<SignatureResult> SignAsync(
            byte[] data,
            SigningOptions signingOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a digital signature against the original data
        /// </summary>
        /// <param name="data">Original data</param>
        /// <param name="signature">Signature to verify</param>
        /// <param name="verificationOptions">Verification configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verification result</returns>
        Task<VerificationResult> VerifySignatureAsync(
            byte[] data,
            byte[] signature,
            SignatureVerificationOptions verificationOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates cryptographically secure random data
        /// </summary>
        /// <param name="length">Number of bytes to generate</param>
        /// <param name="randomOptions">Random generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Random data</returns>
        Task<RandomResult> GenerateRandomAsync(
            int length,
            RandomGenerationOptions? randomOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a cryptographic key pair
        /// </summary>
        /// <param name="keyOptions">Key generation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key pair result</returns>
        Task<KeyPairResult> GenerateKeyPairAsync(
            KeyGenerationOptions keyOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Derives a key from a password using secure key derivation functions
        /// </summary>
        /// <param name="password">Password to derive from</param>
        /// <param name="derivationOptions">Key derivation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Derived key result</returns>
        Task<DerivedKeyResult> DeriveKeyAsync(
            string password,
            KeyDerivationOptions derivationOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs key exchange using specified algorithm
        /// </summary>
        /// <param name="localPrivateKey">Local private key</param>
        /// <param name="remotePublicKey">Remote public key</param>
        /// <param name="exchangeOptions">Key exchange configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Shared secret result</returns>
        Task<KeyExchangeResult> PerformKeyExchangeAsync(
            byte[] localPrivateKey,
            byte[] remotePublicKey,
            KeyExchangeOptions exchangeOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Securely stores a cryptographic key
        /// </summary>
        /// <param name="key">Key to store</param>
        /// <param name="keyMetadata">Key metadata</param>
        /// <param name="storageOptions">Storage configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key storage result</returns>
        Task<KeyStorageResult> StoreKeyAsync(
            CryptographicKey key,
            KeyMetadata keyMetadata,
            KeyStorageOptions storageOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a stored cryptographic key
        /// </summary>
        /// <param name="keyId">Key identifier</param>
        /// <param name="retrievalOptions">Retrieval configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key retrieval result</returns>
        Task<KeyRetrievalResult> RetrieveKeyAsync(
            string keyId,
            KeyRetrievalOptions retrievalOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists stored cryptographic keys
        /// </summary>
        /// <param name="listOptions">Listing configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key listing result</returns>
        Task<KeyListResult> ListKeysAsync(
            KeyListOptions? listOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Securely deletes a cryptographic key
        /// </summary>
        /// <param name="keyId">Key identifier</param>
        /// <param name="deletionOptions">Deletion configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key deletion result</returns>
        Task<KeyDeletionResult> DeleteKeyAsync(
            string keyId,
            KeyDeletionOptions deletionOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cryptographic service health and statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service health information</returns>
        Task<CryptographicServiceHealth> GetHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration options for encryption operations
    /// </summary>
    public class EncryptionOptions
    {
        /// <summary>
        /// Encryption algorithm to use
        /// </summary>
        public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256GCM;

        /// <summary>
        /// Key identifier or raw key data
        /// </summary>
        public string? KeyId { get; set; }

        /// <summary>
        /// Raw key data (alternative to KeyId)
        /// </summary>
        public byte[]? KeyData { get; set; }

        /// <summary>
        /// Initialization vector (optional, will be generated if not provided)
        /// </summary>
        public byte[]? InitializationVector { get; set; }

        /// <summary>
        /// Additional authenticated data for AEAD ciphers
        /// </summary>
        public byte[]? AdditionalData { get; set; }

        /// <summary>
        /// Whether to use hardware-backed encryption (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;

        /// <summary>
        /// Key derivation options if key needs to be derived
        /// </summary>
        public KeyDerivationOptions? KeyDerivation { get; set; }
    }

    /// <summary>
    /// Configuration options for decryption operations
    /// </summary>
    public class DecryptionOptions
    {
        /// <summary>
        /// Decryption algorithm to use
        /// </summary>
        public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256GCM;

        /// <summary>
        /// Key identifier or raw key data
        /// </summary>
        public string? KeyId { get; set; }

        /// <summary>
        /// Raw key data (alternative to KeyId)
        /// </summary>
        public byte[]? KeyData { get; set; }

        /// <summary>
        /// Initialization vector
        /// </summary>
        public byte[]? InitializationVector { get; set; }

        /// <summary>
        /// Authentication tag for AEAD ciphers
        /// </summary>
        public byte[]? AuthenticationTag { get; set; }

        /// <summary>
        /// Additional authenticated data for AEAD ciphers
        /// </summary>
        public byte[]? AdditionalData { get; set; }

        /// <summary>
        /// Whether to use hardware-backed decryption (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for hashing operations
    /// </summary>
    public class HashOptions
    {
        /// <summary>
        /// Hash algorithm to use
        /// </summary>
        public HashAlgorithm Algorithm { get; set; } = HashAlgorithm.SHA256;

        /// <summary>
        /// Salt for the hash (optional)
        /// </summary>
        public byte[]? Salt { get; set; }

        /// <summary>
        /// Number of iterations for key stretching
        /// </summary>
        public int Iterations { get; set; } = 1;

        /// <summary>
        /// Whether to use hardware-backed hashing (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for signing operations
    /// </summary>
    public class SigningOptions
    {
        /// <summary>
        /// Signing algorithm to use
        /// </summary>
        public SignatureAlgorithm Algorithm { get; set; } = SignatureAlgorithm.ECDSA_P256_SHA256;

        /// <summary>
        /// Private key identifier or raw key data
        /// </summary>
        public string? PrivateKeyId { get; set; }

        /// <summary>
        /// Raw private key data (alternative to PrivateKeyId)
        /// </summary>
        public byte[]? PrivateKeyData { get; set; }

        /// <summary>
        /// Whether to use hardware-backed signing (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;

        /// <summary>
        /// Additional parameters for the signature algorithm
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for signature verification
    /// </summary>
    public class SignatureVerificationOptions
    {
        /// <summary>
        /// Signature algorithm used
        /// </summary>
        public SignatureAlgorithm Algorithm { get; set; } = SignatureAlgorithm.ECDSA_P256_SHA256;

        /// <summary>
        /// Public key identifier or raw key data
        /// </summary>
        public string? PublicKeyId { get; set; }

        /// <summary>
        /// Raw public key data (alternative to PublicKeyId)
        /// </summary>
        public byte[]? PublicKeyData { get; set; }

        /// <summary>
        /// Whether to use hardware-backed verification (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;

        /// <summary>
        /// Additional parameters for the signature algorithm
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for random generation
    /// </summary>
    public class RandomGenerationOptions
    {
        /// <summary>
        /// Random number generator type
        /// </summary>
        public RandomGeneratorType GeneratorType { get; set; } = RandomGeneratorType.HardwareBacked;

        /// <summary>
        /// Seed for deterministic generation (optional)
        /// </summary>
        public byte[]? Seed { get; set; }

        /// <summary>
        /// Whether to use hardware-backed random generation (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for key generation
    /// </summary>
    public class KeyGenerationOptions
    {
        /// <summary>
        /// Key algorithm to generate
        /// </summary>
        public KeyAlgorithm Algorithm { get; set; } = KeyAlgorithm.ECDSA_P256;

        /// <summary>
        /// Key size in bits (where applicable)
        /// </summary>
        public int KeySizeInBits { get; set; } = 256;

        /// <summary>
        /// Key usage flags
        /// </summary>
        public KeyUsage Usage { get; set; } = KeyUsage.Signing | KeyUsage.Verification;

        /// <summary>
        /// Whether to use hardware-backed key generation (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;

        /// <summary>
        /// Additional parameters for key generation
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for key derivation
    /// </summary>
    public class KeyDerivationOptions
    {
        /// <summary>
        /// Key derivation function to use
        /// </summary>
        public KeyDerivationFunction Function { get; set; } = KeyDerivationFunction.PBKDF2_SHA256;

        /// <summary>
        /// Salt for key derivation
        /// </summary>
        public byte[] Salt { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Number of iterations
        /// </summary>
        public int Iterations { get; set; } = 100000;

        /// <summary>
        /// Desired key length in bytes
        /// </summary>
        public int KeyLengthInBytes { get; set; } = 32;

        /// <summary>
        /// Whether to use hardware-backed derivation (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for key exchange
    /// </summary>
    public class KeyExchangeOptions
    {
        /// <summary>
        /// Key exchange algorithm
        /// </summary>
        public KeyExchangeAlgorithm Algorithm { get; set; } = KeyExchangeAlgorithm.ECDH_P256;

        /// <summary>
        /// Whether to use hardware-backed key exchange (SGX)
        /// </summary>
        public bool UseHardwareBacking { get; set; } = true;

        /// <summary>
        /// Key derivation options for the shared secret
        /// </summary>
        public KeyDerivationOptions? KeyDerivation { get; set; }
    }

    /// <summary>
    /// Cryptographic algorithms enumeration
    /// </summary>
    public enum EncryptionAlgorithm
    {
        AES128GCM,
        AES192GCM,
        AES256GCM,
        AES128CBC,
        AES192CBC,
        AES256CBC,
        ChaCha20Poly1305
    }

    public enum HashAlgorithm
    {
        SHA256,
        SHA384,
        SHA512,
        SHA3_256,
        SHA3_384,
        SHA3_512,
        Blake2b,
        Blake3
    }

    public enum SignatureAlgorithm
    {
        ECDSA_P256_SHA256,
        ECDSA_P384_SHA384,
        ECDSA_P521_SHA512,
        RSA_2048_PSS_SHA256,
        RSA_3072_PSS_SHA256,
        RSA_4096_PSS_SHA256,
        EdDSA_Ed25519,
        EdDSA_Ed448
    }

    public enum KeyAlgorithm
    {
        ECDSA_P256,
        ECDSA_P384,
        ECDSA_P521,
        RSA_2048,
        RSA_3072,
        RSA_4096,
        Ed25519,
        Ed448,
        AES_128,
        AES_192,
        AES_256
    }

    public enum KeyDerivationFunction
    {
        PBKDF2_SHA256,
        PBKDF2_SHA512,
        Scrypt,
        Argon2id,
        HKDF_SHA256,
        HKDF_SHA512
    }

    public enum KeyExchangeAlgorithm
    {
        ECDH_P256,
        ECDH_P384,
        ECDH_P521,
        X25519,
        X448
    }

    public enum RandomGeneratorType
    {
        SystemSecure,
        HardwareBacked,
        Deterministic
    }

    [Flags]
    public enum KeyUsage
    {
        None = 0,
        Encryption = 1,
        Decryption = 2,
        Signing = 4,
        Verification = 8,
        KeyExchange = 16,
        KeyDerivation = 32,
        All = Encryption | Decryption | Signing | Verification | KeyExchange | KeyDerivation
    }

    /// <summary>
    /// Result classes for cryptographic operations
    /// </summary>
    public class CryptographicResult
    {
        public bool Success { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte[]? InitializationVector { get; set; }
        public byte[]? AuthenticationTag { get; set; }
        public string? KeyId { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class HashResult
    {
        public bool Success { get; set; }
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public HashAlgorithm Algorithm { get; set; }
        public byte[]? Salt { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class SignatureResult
    {
        public bool Success { get; set; }
        public byte[] Signature { get; set; } = Array.Empty<byte>();
        public SignatureAlgorithm Algorithm { get; set; }
        public string? KeyId { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class RandomResult
    {
        public bool Success { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public RandomGeneratorType GeneratorType { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class KeyPairResult
    {
        public bool Success { get; set; }
        public CryptographicKey? PublicKey { get; set; }
        public CryptographicKey? PrivateKey { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class DerivedKeyResult
    {
        public bool Success { get; set; }
        public byte[] DerivedKey { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public int Iterations { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class KeyExchangeResult
    {
        public bool Success { get; set; }
        public byte[] SharedSecret { get; set; } = Array.Empty<byte>();
        public KeyExchangeAlgorithm Algorithm { get; set; }
        public string? ErrorMessage { get; set; }
        public CryptographicMetrics Metrics { get; set; } = new();
    }

    public class KeyStorageResult
    {
        public bool Success { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public DateTime StoredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class KeyRetrievalResult
    {
        public bool Success { get; set; }
        public CryptographicKey? Key { get; set; }
        public KeyMetadata? Metadata { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class KeyListResult
    {
        public bool Success { get; set; }
        public List<KeyInfo> Keys { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class KeyDeletionResult
    {
        public bool Success { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
        public bool SecurelyWiped { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a cryptographic key
    /// </summary>
    public class CryptographicKey
    {
        public string KeyId { get; set; } = string.Empty;
        public KeyAlgorithm Algorithm { get; set; }
        public byte[] KeyData { get; set; } = Array.Empty<byte>();
        public KeyUsage Usage { get; set; }
        public int KeySizeInBits { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsHardwareBacked { get; set; }
    }

    /// <summary>
    /// Metadata for cryptographic keys
    /// </summary>
    public class KeyMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new();
        public KeyUsage Usage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
    }

    /// <summary>
    /// Brief information about a key
    /// </summary>
    public class KeyInfo
    {
        public string KeyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public KeyAlgorithm Algorithm { get; set; }
        public KeyUsage Usage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsHardwareBacked { get; set; }
    }

    /// <summary>
    /// Configuration options for various key operations
    /// </summary>
    public class KeyStorageOptions
    {
        public bool RequireHardwareBacking { get; set; } = true;
        public bool EnableBackup { get; set; } = true;
        public TimeSpan? ExpirationTime { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class KeyRetrievalOptions
    {
        public bool IncludeRevoked { get; set; } = false;
        public bool VerifyIntegrity { get; set; } = true;
    }

    public class KeyListOptions
    {
        public KeyAlgorithm? Algorithm { get; set; }
        public KeyUsage? Usage { get; set; }
        public bool IncludeRevoked { get; set; } = false;
        public bool IncludeExpired { get; set; } = false;
        public int MaxResults { get; set; } = 100;
        public string? NameFilter { get; set; }
        public Dictionary<string, string>? TagFilter { get; set; }
    }

    public class KeyDeletionOptions
    {
        public bool SecureWipe { get; set; } = true;
        public bool ForceDelete { get; set; } = false;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Metrics for cryptographic operations
    /// </summary>
    public class CryptographicMetrics
    {
        public TimeSpan ExecutionTime { get; set; }
        public long InputSizeBytes { get; set; }
        public long OutputSizeBytes { get; set; }
        public bool UsedHardwareBacking { get; set; }
        public string? AlgorithmUsed { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Health information for the cryptographic service
    /// </summary>
    public class CryptographicServiceHealth
    {
        public HealthStatus Status { get; set; }
        public bool HardwareBackingAvailable { get; set; }
        public bool SgxEnclaveHealthy { get; set; }
        public long TotalOperationsPerformed { get; set; }
        public long SuccessfulOperations { get; set; }
        public long FailedOperations { get; set; }
        public TimeSpan Uptime { get; set; }
        public int StoredKeysCount { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime CheckedAt { get; set; }
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }
}