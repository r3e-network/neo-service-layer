using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;


namespace NeoServiceLayer.Services.KeyManagement.Infrastructure
{
    /// <summary>
    /// Implementation of cryptographic operations
    /// </summary>
    public class CryptographicService : ICryptographicService
    {
        private readonly ILogger<CryptographicService> _logger;
        private readonly IKeyStore _keyStore;

        public CryptographicService(
            IKeyStore keyStore,
            ILogger<CryptographicService> logger)
        {
            _keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<KeyGenerationResult> GenerateKeyAsync(
            string algorithm,
            int keySize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Generating {Algorithm} key with size {KeySize}",
                    algorithm, keySize);

                KeyGenerationResult result;

                switch (algorithm.ToUpperInvariant())
                {
                    case "RSA":
                        result = GenerateRSAKey(keySize);
                        break;

                    case "ECDSA":
                        result = GenerateECDSAKey(keySize);
                        break;

                    case "AES":
                        result = GenerateAESKey(keySize);
                        break;

                    default:
                        throw new NotSupportedException($"Algorithm {algorithm} is not supported");
                }

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to generate {Algorithm} key with size {KeySize}",
                    algorithm, keySize);
                throw;
            }
        }

        public async Task<string> SignDataAsync(
            string keyId,
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var keyData = await _keyStore.GetKeyDataAsync(keyId, cancellationToken);
                if (keyData == null)
                    throw new InvalidOperationException($"Key {keyId} not found in key store");

                rsa.ImportRSAPrivateKey(keyData.PrivateKeyBytes, out _);

                var signature = rsa.SignData(
                    data,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                return Convert.ToBase64String(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign data with key {KeyId}", keyId);
                throw;
            }
        }

        public async Task<bool> VerifySignatureAsync(
            string keyId,
            byte[] data,
            string signature,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var keyData = await _keyStore.GetKeyDataAsync(keyId, cancellationToken);
                if (keyData == null)
                    throw new InvalidOperationException($"Key {keyId} not found in key store");

                rsa.ImportSubjectPublicKeyInfo(keyData.PublicKeyBytes, out _);

                var signatureBytes = Convert.FromBase64String(signature);

                return rsa.VerifyData(
                    data,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to verify signature with key {KeyId}",
                    keyId);
                throw;
            }
        }

        public async Task<byte[]> EncryptAsync(
            string keyId,
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var keyData = await _keyStore.GetKeyDataAsync(keyId, cancellationToken);
                if (keyData == null)
                    throw new InvalidOperationException($"Key {keyId} not found in key store");

                if (keyData.Algorithm == "AES")
                {
                    return EncryptWithAES(keyData.PrivateKeyBytes, data);
                }
                else if (keyData.Algorithm == "RSA")
                {
                    rsa.ImportSubjectPublicKeyInfo(keyData.PublicKeyBytes, out _);
                    return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                }
                else
                {
                    throw new NotSupportedException($"Encryption not supported for algorithm {keyData.Algorithm}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data with key {KeyId}", keyId);
                throw;
            }
        }

        public async Task<byte[]> DecryptAsync(
            string keyId,
            byte[] encryptedData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var keyData = await _keyStore.GetKeyDataAsync(keyId, cancellationToken);
                if (keyData == null)
                    throw new InvalidOperationException($"Key {keyId} not found in key store");

                if (keyData.Algorithm == "AES")
                {
                    return DecryptWithAES(keyData.PrivateKeyBytes, encryptedData);
                }
                else if (keyData.Algorithm == "RSA")
                {
                    rsa.ImportRSAPrivateKey(keyData.PrivateKeyBytes, out _);
                    return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
                }
                else
                {
                    throw new NotSupportedException($"Decryption not supported for algorithm {keyData.Algorithm}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data with key {KeyId}", keyId);
                throw;
            }
        }

        private KeyGenerationResult GenerateRSAKey(int keySize)
        {

            var publicKey = rsa.ExportSubjectPublicKeyInfo();
            var privateKey = rsa.ExportRSAPrivateKey();

            // Encrypt private key for storage
            var encryptedPrivateKey = ProtectPrivateKey(privateKey);

            return new KeyGenerationResult
            {
                PublicKey = Convert.ToBase64String(publicKey),
                EncryptedPrivateKey = encryptedPrivateKey,
                Algorithm = "RSA",
                KeySize = keySize,
                PublicKeyBytes = publicKey,
                PrivateKeyBytes = privateKey
            };
        }

        private KeyGenerationResult GenerateECDSAKey(int keySize)
        {
            var curve = keySize switch
            {
                256 => ECCurve.NamedCurves.nistP256,
                384 => ECCurve.NamedCurves.nistP384,
                521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentException($"Invalid key size {keySize} for ECDSA")
            };


            var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            var privateKey = ecdsa.ExportECPrivateKey();

            var encryptedPrivateKey = ProtectPrivateKey(privateKey);

            return new KeyGenerationResult
            {
                PublicKey = Convert.ToBase64String(publicKey),
                EncryptedPrivateKey = encryptedPrivateKey,
                Algorithm = "ECDSA",
                KeySize = keySize,
                PublicKeyBytes = publicKey,
                PrivateKeyBytes = privateKey
            };
        }

        private KeyGenerationResult GenerateAESKey(int keySize)
        {
            if (keySize != 128 && keySize != 192 && keySize != 256)
                throw new ArgumentException($"Invalid key size {keySize} for AES");

            aes.KeySize = keySize;
            aes.GenerateKey();

            var encryptedKey = ProtectPrivateKey(aes.Key);

            return new KeyGenerationResult
            {
                PublicKey = Convert.ToBase64String(aes.Key), // For AES, public and private are the same
                EncryptedPrivateKey = encryptedKey,
                Algorithm = "AES",
                KeySize = keySize,
                PublicKeyBytes = aes.Key,
                PrivateKeyBytes = aes.Key
            };
        }

        private string ProtectPrivateKey(byte[] privateKey)
        {
            // Production key protection using DPAPI with additional security layers
            var protectedData = ProtectedData.Protect(
                privateKey,
                Encoding.UTF8.GetBytes("NeoServiceLayer"),
                DataProtectionScope.LocalMachine);

            return Convert.ToBase64String(protectedData);
        }

        private byte[] UnprotectPrivateKey(string encryptedPrivateKey)
        {
            var protectedData = Convert.FromBase64String(encryptedPrivateKey);

            return ProtectedData.Unprotect(
                protectedData,
                Encoding.UTF8.GetBytes("NeoServiceLayer"),
                DataProtectionScope.LocalMachine);
        }

        private byte[] EncryptWithAES(byte[] key, byte[] data)
        {
            aes.Key = key;
            aes.GenerateIV();

            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            // Prepend IV to encrypted data
            var result = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return result;
        }

        private byte[] DecryptWithAES(byte[] key, byte[] encryptedData)
        {
            aes.Key = key;

            // Extract IV from encrypted data
            var iv = new byte[aes.BlockSize / 8];
            var cipherText = new byte[encryptedData.Length - iv.Length];

            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

            aes.IV = iv;

            return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }
    }

    /// <summary>
    /// Interface for key storage
    /// </summary>
    public interface IKeyStore
    {
        Task<KeyData?> GetKeyDataAsync(string keyId, CancellationToken cancellationToken = default);
        Task SaveKeyDataAsync(string keyId, KeyData keyData, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Key data for storage
    /// </summary>
    public class KeyData
    {
        public string KeyId { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public byte[] PublicKeyBytes { get; set; } = Array.Empty<byte>();
        public byte[] PrivateKeyBytes { get; set; } = Array.Empty<byte>();
    }
}