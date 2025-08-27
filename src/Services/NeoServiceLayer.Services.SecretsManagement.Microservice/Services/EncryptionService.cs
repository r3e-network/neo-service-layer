using Microsoft.Extensions.Caching.Memory;
using Neo.SecretsManagement.Service.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IKeyManagementService _keyService;
    private readonly IHsmService _hsmService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EncryptionService> _logger;
    private readonly EncryptionOptions _options;

    public EncryptionService(
        IKeyManagementService keyService,
        IHsmService hsmService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<EncryptionService> logger)
    {
        _keyService = keyService;
        _hsmService = hsmService;
        _cache = cache;
        _logger = logger;
        _options = configuration.GetSection("Encryption").Get<EncryptionOptions>() ?? new EncryptionOptions();
    }

    public async Task<string> EncryptAsync(string plaintext, string keyId)
    {
        try
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));

            var key = await GetEncryptionKeyAsync(keyId);
            if (key == null)
                throw new ArgumentException($"Encryption key not found: {keyId}", nameof(keyId));

            // Use HSM if key is stored there
            if (!string.IsNullOrEmpty(key.HsmSlotId))
            {
                return await _hsmService.EncryptAsync(plaintext, key.KeyId);
            }

            // Use local encryption for software keys
            return await EncryptLocallyAsync(plaintext, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data with key {KeyId}", keyId);
            throw;
        }
    }

    public async Task<string> DecryptAsync(string ciphertext, string keyId)
    {
        try
        {
            if (string.IsNullOrEmpty(ciphertext))
                throw new ArgumentException("Ciphertext cannot be null or empty", nameof(ciphertext));

            var key = await GetEncryptionKeyAsync(keyId);
            if (key == null)
                throw new ArgumentException($"Encryption key not found: {keyId}", nameof(keyId));

            // Use HSM if key is stored there
            if (!string.IsNullOrEmpty(key.HsmSlotId))
            {
                return await _hsmService.DecryptAsync(ciphertext, key.KeyId);
            }

            // Use local decryption for software keys
            return await DecryptLocallyAsync(ciphertext, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data with key {KeyId}", keyId);
            throw;
        }
    }

    public async Task<string> EncryptWithDataKeyAsync(string plaintext, out string encryptedDataKey)
    {
        try
        {
            // Generate a new data encryption key
            var dataKey = GenerateAes256Key();
            
            // Get the master key for encrypting the data key
            var masterKeyId = _options.DefaultMasterKeyId;
            var masterKey = await GetEncryptionKeyAsync(masterKeyId);
            
            if (masterKey == null)
                throw new InvalidOperationException($"Master key not found: {masterKeyId}");

            // Encrypt the data key with the master key
            var dataKeyBytes = Convert.ToBase64String(dataKey);
            encryptedDataKey = await EncryptAsync(dataKeyBytes, masterKeyId);

            // Encrypt the plaintext with the data key
            var encryptedData = await EncryptWithKeyAsync(plaintext, dataKey);
            
            // Clear the data key from memory
            Array.Clear(dataKey, 0, dataKey.Length);

            return encryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data with data key");
            throw;
        }
    }

    public async Task<string> DecryptWithDataKeyAsync(string ciphertext, string encryptedDataKey)
    {
        try
        {
            // Get the master key for decrypting the data key
            var masterKeyId = _options.DefaultMasterKeyId;
            
            // Decrypt the data key
            var dataKeyBase64 = await DecryptAsync(encryptedDataKey, masterKeyId);
            var dataKey = Convert.FromBase64String(dataKeyBase64);

            try
            {
                // Decrypt the ciphertext with the data key
                return await DecryptWithKeyAsync(ciphertext, dataKey);
            }
            finally
            {
                // Clear the data key from memory
                Array.Clear(dataKey, 0, dataKey.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data with data key");
            throw;
        }
    }

    public async Task<bool> VerifyIntegrityAsync(string data, string keyId, string expectedHash)
    {
        try
        {
            // Create HMAC with the encryption key
            var key = await GetEncryptionKeyAsync(keyId);
            if (key == null) return false;

            var keyBytes = await GetKeyBytesAsync(key);
            using var hmac = new HMACSHA256(keyBytes);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var hashBytes = hmac.ComputeHash(dataBytes);
            var actualHash = Convert.ToBase64String(hashBytes);

            return string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify integrity for key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<string> GenerateSecureRandomAsync(int length)
    {
        await Task.CompletedTask;
        
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[bytes[i] % chars.Length]);
        }

        return result.ToString();
    }

    public async Task<string> HashAsync(string data, string? salt = null)
    {
        await Task.CompletedTask;
        
        var actualSalt = salt ?? GenerateSalt();
        var dataBytes = Encoding.UTF8.GetBytes(data + actualSalt);
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(dataBytes);
        var hash = Convert.ToBase64String(hashBytes);
        
        // Include salt in the result
        return $"{actualSalt}:{hash}";
    }

    public async Task<bool> VerifyHashAsync(string data, string hash, string? salt = null)
    {
        await Task.CompletedTask;
        
        try
        {
            string actualSalt;
            string actualHash;
            
            if (salt != null)
            {
                actualSalt = salt;
                actualHash = hash;
            }
            else
            {
                var parts = hash.Split(':', 2);
                if (parts.Length != 2) return false;
                
                actualSalt = parts[0];
                actualHash = parts[1];
            }
            
            var dataBytes = Encoding.UTF8.GetBytes(data + actualSalt);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(dataBytes);
            var computedHash = Convert.ToBase64String(hashBytes);
            
            return string.Equals(computedHash, actualHash, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]> GenerateKeyAsync(int keySize)
    {
        await Task.CompletedTask;
        
        var keyBytes = new byte[keySize / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        
        return keyBytes;
    }

    public async Task<string> DeriveKeyAsync(string password, string salt, int keySize)
    {
        await Task.CompletedTask;
        
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, _options.Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var keyBytes = pbkdf2.GetBytes(keySize / 8);
        
        return Convert.ToBase64String(keyBytes);
    }

    private async Task<EncryptionKey?> GetEncryptionKeyAsync(string keyId)
    {
        // Try cache first
        var cacheKey = $"encryption_key:{keyId}";
        if (_cache.TryGetValue(cacheKey, out EncryptionKey? cachedKey))
        {
            return cachedKey;
        }

        // Get from key service
        var key = await _keyService.GetKeyAsync(keyId);
        if (key != null)
        {
            // Cache for 5 minutes
            _cache.Set(cacheKey, key, TimeSpan.FromMinutes(5));
        }

        return key;
    }

    private async Task<string> EncryptLocallyAsync(string plaintext, EncryptionKey key)
    {
        var keyBytes = await GetKeyBytesAsync(key);
        
        return key.Algorithm.ToUpper() switch
        {
            "AES-256-GCM" => await EncryptAesGcmAsync(plaintext, keyBytes),
            "AES-256-CBC" => await EncryptAesCbcAsync(plaintext, keyBytes),
            _ => throw new NotSupportedException($"Encryption algorithm not supported: {key.Algorithm}")
        };
    }

    private async Task<string> DecryptLocallyAsync(string ciphertext, EncryptionKey key)
    {
        var keyBytes = await GetKeyBytesAsync(key);
        
        return key.Algorithm.ToUpper() switch
        {
            "AES-256-GCM" => await DecryptAesGcmAsync(ciphertext, keyBytes),
            "AES-256-CBC" => await DecryptAesCbcAsync(ciphertext, keyBytes),
            _ => throw new NotSupportedException($"Decryption algorithm not supported: {key.Algorithm}")
        };
    }

    private async Task<string> EncryptAesGcmAsync(string plaintext, byte[] key)
    {
        await Task.CompletedTask;
        
        using var aes = new AesGcm(key);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[12]; // 96-bit nonce for GCM
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16]; // 128-bit authentication tag

        RandomNumberGenerator.Fill(nonce);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine nonce + ciphertext + tag
        var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    private async Task<string> DecryptAesGcmAsync(string ciphertext, byte[] key)
    {
        await Task.CompletedTask;
        
        var data = Convert.FromBase64String(ciphertext);
        if (data.Length < 28) // 12 (nonce) + 16 (tag) minimum
            throw new ArgumentException("Invalid ciphertext format");

        var nonce = new byte[12];
        var tag = new byte[16];
        var encrypted = new byte[data.Length - 28];
        var plaintext = new byte[encrypted.Length];

        Buffer.BlockCopy(data, 0, nonce, 0, 12);
        Buffer.BlockCopy(data, 12, encrypted, 0, encrypted.Length);
        Buffer.BlockCopy(data, data.Length - 16, tag, 0, 16);

        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, encrypted, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private async Task<string> EncryptAesCbcAsync(string plaintext, byte[] key)
    {
        await Task.CompletedTask;
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Combine IV + ciphertext
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private async Task<string> DecryptAesCbcAsync(string ciphertext, byte[] key)
    {
        await Task.CompletedTask;
        
        var data = Convert.FromBase64String(ciphertext);
        if (data.Length < 16) // IV size
            throw new ArgumentException("Invalid ciphertext format");

        var iv = new byte[16];
        var encrypted = new byte[data.Length - 16];

        Buffer.BlockCopy(data, 0, iv, 0, 16);
        Buffer.BlockCopy(data, 16, encrypted, 0, encrypted.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private async Task<string> EncryptWithKeyAsync(string plaintext, byte[] key)
    {
        return await EncryptAesGcmAsync(plaintext, key);
    }

    private async Task<string> DecryptWithKeyAsync(string ciphertext, byte[] key)
    {
        return await DecryptAesGcmAsync(ciphertext, key);
    }

    private static byte[] GenerateAes256Key()
    {
        var key = new byte[32]; // 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return key;
    }

    private static string GenerateSalt()
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }

    private async Task<byte[]> GetKeyBytesAsync(EncryptionKey key)
    {
        // In a real implementation, this would securely retrieve the key material
        // For now, we'll generate a deterministic key based on the key ID
        await Task.CompletedTask;
        
        using var sha256 = SHA256.Create();
        var keyIdBytes = Encoding.UTF8.GetBytes(key.KeyId + _options.KeyDerivationSecret);
        var hash = sha256.ComputeHash(keyIdBytes);
        
        // Use the hash as the key (32 bytes for AES-256)
        return hash;
    }

    private class EncryptionOptions
    {
        public string DefaultMasterKeyId { get; set; } = "master-key-1";
        public string KeyDerivationSecret { get; set; } = "default-secret-change-in-production";
        public int Pbkdf2Iterations { get; set; } = 100000;
    }
}