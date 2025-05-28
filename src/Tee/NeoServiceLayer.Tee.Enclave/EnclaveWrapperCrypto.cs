using System.Runtime.InteropServices;
using System.Text;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Cryptography and randomness operations for the enclave wrapper.
/// </summary>
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
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(dataBytes);

            // Sign the hash using ECDSA
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
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

            using var document = System.Text.Json.JsonDocument.Parse(envelopeJson);
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
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var computedHash = sha256.ComputeHash(dataBytes);

            // Verify the hash matches
            if (!dataHash.SequenceEqual(computedHash))
            {
                return false;
            }

            // Verify the signature
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
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

            // Generate random IV
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyData.SymmetricKey ?? throw new EnclaveException("Symmetric key not available");
            aes.GenerateIV();

            // Encrypt the data
            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write);

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

            using var document = System.Text.Json.JsonDocument.Parse(envelopeJson);
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
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyData.SymmetricKey ?? throw new EnclaveException("Symmetric key not available");
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedBytes);
            using var csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new EnclaveException($"Failed to decrypt data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets key data from secure storage.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>The key data or null if not found.</returns>
    private KeyData? GetKeyFromSecureStorage(string keyId)
    {
        // This would interface with the actual secure storage implementation
        // For now, return a mock key data structure
        return new KeyData
        {
            KeyId = keyId,
            PrivateKey = GenerateMockPrivateKey(),
            SymmetricKey = GenerateMockSymmetricKey()
        };
    }

    /// <summary>
    /// Generates a mock private key for testing.
    /// </summary>
    private byte[] GenerateMockPrivateKey()
    {
        using var ecdsa = System.Security.Cryptography.ECDsa.Create();
        return ecdsa.ExportECPrivateKey();
    }

    /// <summary>
    /// Generates a mock symmetric key for testing.
    /// </summary>
    private byte[] GenerateMockSymmetricKey()
    {
        var key = new byte[32]; // 256-bit key
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return key;
    }

    /// <summary>
    /// Represents key data stored in secure storage.
    /// </summary>
    private class KeyData
    {
        public string KeyId { get; set; } = string.Empty;
        public byte[] PrivateKey { get; set; } = Array.Empty<byte>();
        public byte[]? SymmetricKey { get; set; }
    }
}
