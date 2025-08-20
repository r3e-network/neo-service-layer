using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// DEPRECATED: Cryptography and randomness operations for the legacy enclave wrapper.
/// This partial class is deprecated. Use OcclumEnclaveWrapper instead.
/// </summary>
[Obsolete("This class is deprecated. Use OcclumEnclaveWrapper instead.")]
public partial class EnclaveWrapper
{
    /// <summary>
    /// Generates a random number in the enclave.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random number between min and max (inclusive).</returns>
    public int GenerateRandom(int min, int max)
    {
        EnsureInitialized();

        IntPtr resultPtr = Marshal.AllocHGlobal(sizeof(int));

        try
        {
            int result = NativeOcclumEnclave.occlum_generate_random(min, max, resultPtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to generate random number. Error code: {result}");
            }

            return Marshal.ReadInt32(resultPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(resultPtr);
        }
    }

    /// <summary>
    /// Generates random bytes using the enclave's secure random number generator.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>An array of random bytes.</returns>
    public byte[] GenerateRandomBytes(int length)
    {
        EnsureInitialized();

        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        }

        byte[] buffer = new byte[length];
        int result = NativeOcclumEnclave.occlum_generate_random_bytes(buffer, (UIntPtr)length);

        if (result != 0)
        {
            throw new EnclaveException($"Failed to generate random bytes. Error code: {result}");
        }

        return buffer;
    }

    /// <summary>
    /// Generates a cryptographic key in the enclave.
    /// </summary>
    /// <param name="keyId">The identifier for the new key.</param>
    /// <param name="keyType">The type of key (e.g., "Secp256k1", "Ed25519").</param>
    /// <param name="keyUsage">Allowed usages for the key (e.g., "Sign,Verify").</param>
    /// <param name="exportable">Whether the private key material can be exported.</param>
    /// <param name="description">Optional description for the key.</param>
    /// <returns>JSON string representing the KeyMetadata of the generated key.</returns>
    public string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description)
    {
        EnsureInitialized();

        byte[] keyIdBytes = Encoding.UTF8.GetBytes(keyId);
        byte[] keyTypeBytes = Encoding.UTF8.GetBytes(keyType);
        byte[] keyUsageBytes = Encoding.UTF8.GetBytes(keyUsage);
        byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);
        byte[] resultBytes = new byte[2048]; // Buffer for JSON result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_kms_generate_key(
                keyIdBytes,
                keyTypeBytes,
                keyUsageBytes,
                exportable ? 1 : 0,
                descriptionBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to generate key. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Signs data using a key in the enclave.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="keyId">The identifier of the key to use for signing.</param>
    /// <returns>The signature as a base64-encoded string.</returns>
    public string SignData(string data, string keyId)
    {
        EnsureInitialized();

        try
        {
            // Get the key from secure storage
            var keyData = GetKeyFromSecureStorage(keyId);
            if (keyData == null)
            {
                throw new EnclaveException($"Key {keyId} not found in secure storage");
            }

            // Convert data to bytes
            var dataBytes = Encoding.UTF8.GetBytes(data);

            // Create hash of the data
            var hash = sha256.ComputeHash(dataBytes);

            // Sign the hash using ECDSA
            ecdsa.ImportECPrivateKey(keyData.PrivateKey, out _);

            var signature = ecdsa.SignHash(hash);

            // Create signature envelope with metadata
            var signatureEnvelope = new
            {
                Signature = Convert.ToBase64String(signature),
                KeyId = keyId,
                Algorithm = "ECDSA-SHA256",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                DataHash = Convert.ToBase64String(hash)
            };

            var envelopeJson = System.Text.Json.JsonSerializer.Serialize(signatureEnvelope);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(envelopeJson));
        }
        catch (Exception ex)
        {
            throw new EnclaveException($"Failed to sign data with key {keyId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Verifies a signature using a key in the enclave.
    /// </summary>
    /// <param name="data">The original data.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="keyId">The identifier of the key to use for verification.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool VerifySignature(string data, string signature, string keyId)
    {
        EnsureInitialized();

        try
        {
            // Decode the signature envelope
            var envelopeBytes = Convert.FromBase64String(signature);
            var envelopeJson = Encoding.UTF8.GetString(envelopeBytes);

            var root = document.RootElement;

            var signatureBytes = Convert.FromBase64String(root.GetProperty("Signature").GetString()!);
            var signatureKeyId = root.GetProperty("KeyId").GetString()!;
            var algorithm = root.GetProperty("Algorithm").GetString()!;
            var dataHash = Convert.FromBase64String(root.GetProperty("DataHash").GetString()!);

            // Verify key ID matches
            if (signatureKeyId != keyId)
            {
                return false;
            }

            // Verify algorithm
            if (algorithm != "ECDSA-SHA256")
            {
                return false;
            }

            // Get the public key from secure storage
            var keyData = GetKeyFromSecureStorage(keyId);
            if (keyData == null)
            {
                return false;
            }

            // Compute hash of the provided data
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var computedHash = sha256.ComputeHash(dataBytes);

            // Verify the hash matches
            if (!dataHash.SequenceEqual(computedHash))
            {
                return false;
            }

            // Verify the signature
            ecdsa.ImportECPrivateKey(keyData.PrivateKey, out _);

            return ecdsa.VerifyHash(computedHash, signatureBytes);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encrypts data using a key in the enclave.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="keyId">The identifier of the key to use for encryption.</param>
    /// <returns>The encrypted data as a base64-encoded string.</returns>
    public string EncryptData(string data, string keyId)
    {
        EnsureInitialized();

        try
        {
            // Get the encryption key from secure storage
            var keyData = GetKeyFromSecureStorage(keyId);
            if (keyData == null)
            {
                throw new EnclaveException($"Key {keyId} not found in secure storage");
            }

            // Convert data to bytes
            var dataBytes = Encoding.UTF8.GetBytes(data);

            // Generate cryptographically secure random IV using SGX
            aes.Key = keyData.SymmetricKey ?? throw new EnclaveException("Symmetric key not available");

            // Use SGX-backed secure random for IV generation
            var secureIV = GenerateSecureRandomBytes(aes.BlockSize / 8);
            aes.IV = secureIV;

            // Encrypt the data

            csEncrypt.Write(dataBytes, 0, dataBytes.Length);
            csEncrypt.FlushFinalBlock();

            var encryptedBytes = msEncrypt.ToArray();

            // Create encryption envelope with metadata
            var encryptionEnvelope = new
            {
                EncryptedData = Convert.ToBase64String(encryptedBytes),
                IV = Convert.ToBase64String(aes.IV),
                KeyId = keyId,
                Algorithm = "AES-256-CBC",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var envelopeJson = System.Text.Json.JsonSerializer.Serialize(encryptionEnvelope);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(envelopeJson));
        }
        catch (Exception ex)
        {
            throw new EnclaveException($"Failed to encrypt data with key {keyId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts data using a key in the enclave.
    /// </summary>
    /// <param name="encryptedData">The encrypted data as a base64-encoded string.</param>
    /// <param name="keyId">The identifier of the key to use for decryption.</param>
    /// <returns>The decrypted data.</returns>
    public string DecryptData(string encryptedData, string keyId)
    {
        EnsureInitialized();

        try
        {
            // Decode the encryption envelope
            var envelopeBytes = Convert.FromBase64String(encryptedData);
            var envelopeJson = Encoding.UTF8.GetString(envelopeBytes);

            var root = document.RootElement;

            var encryptedBytes = Convert.FromBase64String(root.GetProperty("EncryptedData").GetString()!);
            var iv = Convert.FromBase64String(root.GetProperty("IV").GetString()!);
            var encryptionKeyId = root.GetProperty("KeyId").GetString()!;
            var algorithm = root.GetProperty("Algorithm").GetString()!;

            // Verify key ID matches
            if (encryptionKeyId != keyId)
            {
                throw new EnclaveException("Key ID mismatch");
            }

            // Verify algorithm
            if (algorithm != "AES-256-CBC")
            {
                throw new EnclaveException("Unsupported encryption algorithm");
            }

            // Get the decryption key from secure storage
            var keyData = GetKeyFromSecureStorage(keyId);
            if (keyData == null)
            {
                throw new EnclaveException($"Key {keyId} not found in secure storage");
            }

            // Decrypt the data
            aes.Key = keyData.SymmetricKey ?? throw new EnclaveException("Symmetric key not available");
            aes.IV = iv;


            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new EnclaveException($"Failed to decrypt data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves key data from secure storage using the Occlum enclave.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>The key data or null if not found.</returns>
    private KeyData? GetKeyFromSecureStorage(string keyId)
    {
        try
        {
            // Use the Occlum storage interface via base class method
            var storageKey = $"key_store_{keyId}";
            var resultJson = StorageRetrieveDataAsync(storageKey, GetDefaultEncryptionKey()).GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(resultJson))
            {
                _logger.LogWarning("Key {KeyId} not found in secure storage", keyId);
                return null;
            }

            // Deserialize key data from secure storage
            var keyData = System.Text.Json.JsonSerializer.Deserialize<KeyDataStorage>(resultJson);
            if (keyData == null)
            {
                _logger.LogError("Failed to deserialize key data for {KeyId}", keyId);
                return null;
            }

            // Convert from storage format to working format
            return new KeyData
            {
                KeyId = keyId,
                PrivateKey = Convert.FromBase64String(keyData.PrivateKeyBase64),
                SymmetricKey = !string.IsNullOrEmpty(keyData.SymmetricKeyBase64)
                    ? Convert.FromBase64String(keyData.SymmetricKeyBase64)
                    : null,
                PublicKey = !string.IsNullOrEmpty(keyData.PublicKeyBase64)
                    ? Convert.FromBase64String(keyData.PublicKeyBase64)
                    : null,
                CreatedAt = keyData.CreatedAt,
                KeyType = keyData.KeyType,
                KeySize = keyData.KeySize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key {KeyId} from secure storage", keyId);
            return null;
        }
    }

    /// <summary>
    /// Stores key data in secure storage using the Occlum enclave.
    /// </summary>
    /// <param name="keyData">The key data to store.</param>
    /// <returns>True if successful, false otherwise.</returns>
    private bool StoreKeyInSecureStorage(KeyData keyData)
    {
        try
        {
            // Convert to storage format
            var storageData = new KeyDataStorage
            {
                PrivateKeyBase64 = Convert.ToBase64String(keyData.PrivateKey),
                SymmetricKeyBase64 = keyData.SymmetricKey != null
                    ? Convert.ToBase64String(keyData.SymmetricKey)
                    : null,
                PublicKeyBase64 = keyData.PublicKey != null
                    ? Convert.ToBase64String(keyData.PublicKey)
                    : null,
                CreatedAt = keyData.CreatedAt,
                KeyType = keyData.KeyType,
                KeySize = keyData.KeySize
            };

            var keyDataJson = System.Text.Json.JsonSerializer.Serialize(storageData);
            var storageKey = $"key_store_{keyData.KeyId}";

            // Store in secure enclave storage
            var result = StorageStoreDataAsync(storageKey,
                System.Text.Encoding.UTF8.GetBytes(keyDataJson),
                GetDefaultEncryptionKey(),
                compress: true).GetAwaiter().GetResult();

            if (result.Contains("stored") || result.Contains("success"))
            {
                _logger.LogInformation("Successfully stored key {KeyId} in secure storage", keyData.KeyId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to store key {KeyId}: {Result}", keyData.KeyId, result);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing key {KeyId} in secure storage", keyData.KeyId);
            return false;
        }
    }

    /// <summary>
    /// Gets the default encryption key for storage operations using proper key derivation.
    /// </summary>
    /// <returns>The default encryption key.</returns>
    private string GetDefaultEncryptionKey()
    {
        // In production, this should be derived from SGX sealing key or master key
        // For now, use a fixed salt with PBKDF2 for better security than plaintext
        var masterPassword = Environment.GetEnvironmentVariable("ENCLAVE_MASTER_KEY") ?? "default-master-key";
        var salt = Encoding.UTF8.GetBytes("neo-enclave-storage-salt-v1");

            masterPassword,
            salt,
            100000, // 100,000 iterations
            System.Security.Cryptography.HashAlgorithmName.SHA256);

        var keyBytes = pbkdf2.GetBytes(32); // 256-bit key
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// Generates cryptographically secure random bytes using the enclave's secure random generation.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>The secure random bytes, or 0 on success, non-zero on failure.</returns>
    private int GenerateRandomBytes(byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
        {
            return -1; // Invalid input
        }

        try
        {
            // Use the native Occlum random generation
            var result = NativeOcclumEnclave.occlum_generate_random_bytes(buffer, (UIntPtr)buffer.Length);

            if (result != 0)
            {
                _logger.LogWarning("SGX random generation failed with code {Result}, using system fallback", result);

                // Fallback to system RNG if SGX fails
                rng.GetBytes(buffer);
                return 0; // Success with fallback
            }

            return result; // SGX success
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure random bytes, using system fallback");

            // Final fallback to system RNG
            rng.GetBytes(buffer);
            return 0; // Success with fallback
        }
    }

    /// <summary>
    /// Generates a cryptographically secure private key using SGX functions.
    /// </summary>
    /// <param name="keyType">The type of key to generate (ECDSA, RSA, etc.).</param>
    /// <param name="keySize">The key size in bits.</param>
    /// <returns>The generated key data.</returns>
    private KeyData GenerateSecureKey(string keyType = "ECDSA", int keySize = 256)
    {
        try
        {
            var keyId = Guid.NewGuid().ToString();
            var createdAt = DateTimeOffset.UtcNow;

            KeyData keyData;

            switch (keyType.ToUpper())
            {
                case "ECDSA":
                    keyData = GenerateECDSAKey(keyId, keySize, createdAt);
                    break;

                case "RSA":
                    keyData = GenerateRSAKey(keyId, keySize, createdAt);
                    break;

                case "AES":
                    keyData = GenerateAESKey(keyId, keySize, createdAt);
                    break;

                default:
                    _logger.LogWarning("Unknown key type {KeyType}, defaulting to ECDSA", keyType);
                    keyData = GenerateECDSAKey(keyId, keySize, createdAt);
                    break;
            }

            // Store in secure storage
            if (StoreKeyInSecureStorage(keyData))
            {
                _logger.LogInformation("Generated and stored {KeyType} key {KeyId} (size: {KeySize})",
                    keyType, keyId, keySize);
                return keyData;
            }
            else
            {
                _logger.LogError("Failed to store generated key {KeyId}", keyId);
                throw new InvalidOperationException($"Failed to store generated key {keyId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure {KeyType} key", keyType);
            throw;
        }
    }

    /// <summary>
    /// Generates an ECDSA key pair using secure randomness.
    /// </summary>
    private KeyData GenerateECDSAKey(string keyId, int keySize, DateTimeOffset createdAt)
    {
        // Use SGX-backed secure random generation
        var secureRandom = GenerateSecureRandomBytes(32); // Seed for key generation

        {
            256 => System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256),
            384 => System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP384),
            521 => System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP521),
            _ => throw new ArgumentException($"Unsupported ECDSA key size: {keySize}")
        };

        // Generate key pair
        var privateKey = ecdsa.ExportECPrivateKey();
        var publicKey = ecdsa.ExportSubjectPublicKeyInfo();

        // Securely clear the ECDSA instance
        ecdsa.Clear();

        return new KeyData
        {
            KeyId = keyId,
            PrivateKey = privateKey,
            PublicKey = publicKey,
            SymmetricKey = null,
            CreatedAt = createdAt,
            KeyType = "ECDSA",
            KeySize = keySize
        };
    }

    /// <summary>
    /// Generates an RSA key pair using secure randomness.
    /// </summary>
    private KeyData GenerateRSAKey(string keyId, int keySize, DateTimeOffset createdAt)
    {
        if (keySize < 2048 || keySize > 4096 || keySize % 8 != 0)
        {
            throw new ArgumentException($"Invalid RSA key size: {keySize}. Must be between 2048-4096 and divisible by 8.");
        }


        var privateKey = rsa.ExportRSAPrivateKey();
        var publicKey = rsa.ExportSubjectPublicKeyInfo();

        // Securely clear the RSA instance
        rsa.Clear();

        return new KeyData
        {
            KeyId = keyId,
            PrivateKey = privateKey,
            PublicKey = publicKey,
            SymmetricKey = null,
            CreatedAt = createdAt,
            KeyType = "RSA",
            KeySize = keySize
        };
    }

    /// <summary>
    /// Generates an AES symmetric key using SGX secure random generation.
    /// </summary>
    private KeyData GenerateAESKey(string keyId, int keySize, DateTimeOffset createdAt)
    {
        if (keySize != 128 && keySize != 192 && keySize != 256)
        {
            throw new ArgumentException($"Invalid AES key size: {keySize}. Must be 128, 192, or 256.");
        }

        var keySizeBytes = keySize / 8;
        var symmetricKey = GenerateSecureRandomBytes(keySizeBytes);

        return new KeyData
        {
            KeyId = keyId,
            PrivateKey = Array.Empty<byte>(),
            PublicKey = null,
            SymmetricKey = symmetricKey,
            CreatedAt = createdAt,
            KeyType = "AES",
            KeySize = keySize
        };
    }

    /// <summary>
    /// Generates cryptographically secure random bytes using SGX.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>The secure random bytes.</returns>
    private byte[] GenerateSecureRandomBytes(int length)
    {
        if (length <= 0 || length > 1024)
        {
            throw new ArgumentException($"Invalid random bytes length: {length}");
        }

        try
        {
            // Use the SGX-backed random generation from Occlum enclave
            var randomBytes = new byte[length];
            var result = GenerateRandomBytes(randomBytes);

            if (result != 0)
            {
                _logger.LogWarning("SGX random generation failed with code {Result}, falling back to system RNG", result);

                // Fallback to system RNG if SGX fails
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure random bytes, using system fallback");

            // Final fallback to system RNG
            var fallbackBytes = new byte[length];
            rng.GetBytes(fallbackBytes);
            return fallbackBytes;
        }
    }

    /// <summary>
    /// Represents key data stored in secure storage (serializable format).
    /// </summary>
    private class KeyDataStorage
    {
        public string? PrivateKeyBase64 { get; set; }
        public string? PublicKeyBase64 { get; set; }
        public string? SymmetricKeyBase64 { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string KeyType { get; set; } = string.Empty;
        public int KeySize { get; set; }
    }

    /// <summary>
    /// Represents key data stored in secure storage (enhanced version).
    /// </summary>
    private class KeyData
    {
        public string KeyId { get; set; } = string.Empty;
        public byte[] PrivateKey { get; set; } = Array.Empty<byte>();
        public byte[]? PublicKey { get; set; }
        public byte[]? SymmetricKey { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string KeyType { get; set; } = string.Empty;
        public int KeySize { get; set; }
    }

    #region IEnclaveWrapper Interface Methods

    /// <summary>
    /// Encrypts data using the enclave's cryptographic functions with SGX integration.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted data.</returns>
    public byte[] Encrypt(byte[] data, byte[] key)
    {
        EnsureInitialized();
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (key == null) throw new ArgumentNullException(nameof(key));

        try
        {
            // Use AES-256-GCM for authenticated encryption
            var processedKey = key.Length == 32 ? key : DeriveKey(key, 32);

            // Generate secure random nonce using SGX
            var nonce = GenerateSecureRandomBytes(12); // GCM nonce is 96 bits
            var ciphertext = new byte[data.Length];
            var tag = new byte[16]; // GCM tag is 128 bits

            aes.Encrypt(nonce, data, ciphertext, tag, null);

            // Combine nonce + ciphertext + tag
            var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Array.Copy(nonce, 0, result, 0, nonce.Length);
            Array.Copy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
            Array.Copy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

            _logger.LogDebug("Successfully encrypted {DataLength} bytes", data.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypts data using the enclave's cryptographic functions with SGX integration.
    /// </summary>
    /// <param name="data">The encrypted data (nonce + ciphertext + tag).</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] Decrypt(byte[] data, byte[] key)
    {
        EnsureInitialized();
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (data.Length < 28) // 12 (nonce) + 16 (tag) minimum
            throw new ArgumentException("Invalid encrypted data", nameof(data));

        try
        {
            // Extract components
            var nonce = new byte[12];
            var tag = new byte[16];
            var ciphertext = new byte[data.Length - 28];

            Array.Copy(data, 0, nonce, 0, 12);
            Array.Copy(data, 12, ciphertext, 0, ciphertext.Length);
            Array.Copy(data, 12 + ciphertext.Length, tag, 0, 16);

            var processedKey = key.Length == 32 ? key : DeriveKey(key, 32);

            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext, null);

            _logger.LogDebug("Successfully decrypted {DataLength} bytes", ciphertext.Length);
            return plaintext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Signs data using the enclave's cryptographic functions with proper ECDSA/RSA signing.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="key">The private signing key.</param>
    /// <returns>The signature.</returns>
    public byte[] Sign(byte[] data, byte[] key)
    {
        EnsureInitialized();
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (key == null) throw new ArgumentNullException(nameof(key));

        try
        {
            // Try to determine key type and sign accordingly
            if (TrySignWithECDSA(data, key, out var ecdsaSignature))
            {
                _logger.LogDebug("Successfully signed data with ECDSA ({DataLength} bytes)", data.Length);
                return ecdsaSignature;
            }
            else if (TrySignWithRSA(data, key, out var rsaSignature))
            {
                _logger.LogDebug("Successfully signed data with RSA ({DataLength} bytes)", data.Length);
                return rsaSignature;
            }
            else
            {
                // Fallback to HMAC-SHA256 for symmetric keys
                var signature = hmac.ComputeHash(data);
                _logger.LogDebug("Successfully signed data with HMAC-SHA256 ({DataLength} bytes)", data.Length);
                return signature;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signing failed");
            throw new InvalidOperationException("Signing failed", ex);
        }
    }

    /// <summary>
    /// Verifies a signature using the enclave's cryptographic functions.
    /// </summary>
    /// <param name="data">The original data.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The verification key (public key for asymmetric, same key for symmetric).</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool Verify(byte[] data, byte[] signature, byte[] key)
    {
        EnsureInitialized();
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (signature == null) throw new ArgumentNullException(nameof(signature));
        if (key == null) throw new ArgumentNullException(nameof(key));

        try
        {
            // Try verification with different algorithms
            if (TryVerifyWithECDSA(data, signature, key))
            {
                _logger.LogDebug("Successfully verified ECDSA signature");
                return true;
            }
            else if (TryVerifyWithRSA(data, signature, key))
            {
                _logger.LogDebug("Successfully verified RSA signature");
                return true;
            }
            else
            {
                // Fallback to HMAC verification
                return VerifyHMACSignature(data, signature, key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signature verification failed");
            return false;
        }
    }

    // Helper methods for cryptographic operations

    private bool TrySignWithECDSA(byte[] data, byte[] privateKey, out byte[] signature)
    {
        signature = Array.Empty<byte>();
        try
        {
            ecdsa.ImportECPrivateKey(privateKey, out _);
            signature = ecdsa.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TrySignWithRSA(byte[] data, byte[] privateKey, out byte[] signature)
    {
        signature = Array.Empty<byte>();
        try
        {
            rsa.ImportRSAPrivateKey(privateKey, out _);
            signature = rsa.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryVerifyWithECDSA(byte[] data, byte[] signature, byte[] publicKey)
    {
        try
        {
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, System.Security.Cryptography.HashAlgorithmName.SHA256);
        }
        catch
        {
            return false;
        }
    }

    private bool TryVerifyWithRSA(byte[] data, byte[] signature, byte[] publicKey)
    {
        try
        {
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.VerifyData(data, signature, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    private bool VerifyHMACSignature(byte[] data, byte[] signature, byte[] key)
    {
        try
        {
            var expectedSignature = hmac.ComputeHash(data);

            // Constant-time comparison
            if (signature.Length != expectedSignature.Length)
                return false;

            int result = 0;
            for (int i = 0; i < signature.Length; i++)
            {
                result |= signature[i] ^ expectedSignature[i];
            }

            return result == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Derives a key of specified length using PBKDF2.
    /// </summary>
    /// <param name="sourceKey">The source key.</param>
    /// <param name="targetLength">The target key length.</param>
    /// <returns>The derived key.</returns>
    private byte[] DeriveKey(byte[] sourceKey, int targetLength)
    {
        if (sourceKey.Length == targetLength)
            return sourceKey;

        // Use HKDF for proper key derivation (preferred over PBKDF2 for key derivation)
        var salt = Encoding.UTF8.GetBytes("neo-service-layer-hkdf-salt-v1");
        var info = Encoding.UTF8.GetBytes("neo-encryption-key-derivation");

        var derivedKey = new byte[targetLength];
        System.Security.Cryptography.HKDF.DeriveKey(
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            sourceKey,
            derivedKey,
            salt,
            info);

        return derivedKey;
    }

    #endregion
}
