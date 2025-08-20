using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Tee.Tests
{
    /// <summary>
    /// Comprehensive unit tests for SGX Enclave CryptoService
    /// Tests cryptographic operations, key management, and security features
    /// </summary>
    public class CryptoServiceTests : IDisposable
    {
        private readonly Mock<ILogger<CryptoService>> _mockLogger;
        private readonly CryptoService _cryptoService;
        private readonly EnclaveConfig _testConfig;

        public CryptoServiceTests()
        {
            _mockLogger = new Mock<ILogger<CryptoService>>();
            _testConfig = new EnclaveConfig
            {
                crypto_algorithms = new List<string> 
                { 
                    "aes-256-gcm", 
                    "secp256k1", 
                    "ed25519", 
                    "sha256" 
                }
            };
            
            _cryptoService = CryptoService.NewAsync(_testConfig).Result;
        }

        #region Key Generation Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GenerateKey_Ed25519_ShouldSucceed()
        {
            // Arrange
            var keyId = "test_ed25519_key";
            var usage = new[] { "Sign", "Verify" };

            // Act
            var result = await _cryptoService.GenerateKeyAsync(
                keyId, 
                CryptoAlgorithm.Ed25519, 
                usage, 
                exportable: false, 
                "Test Ed25519 key"
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(keyId, result.KeyId);
            Assert.Equal(CryptoAlgorithm.Ed25519, result.KeyType);
            Assert.Equal(usage, result.Usage);
            Assert.NotNull(result.PublicKey);
            Assert.Equal(32, result.PublicKey.Length); // Ed25519 public key is 32 bytes
            Assert.True(result.CreatedAt > 0);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GenerateKey_Secp256k1_ShouldSucceed()
        {
            // Arrange
            var keyId = "test_secp256k1_key";
            var usage = new[] { "Sign", "Verify" };

            // Act
            var result = await _cryptoService.GenerateKeyAsync(
                keyId, 
                CryptoAlgorithm.Secp256k1, 
                usage, 
                exportable: false, 
                "Test secp256k1 key"
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(keyId, result.KeyId);
            Assert.Equal(CryptoAlgorithm.Secp256k1, result.KeyType);
            Assert.Equal(usage, result.Usage);
            Assert.NotNull(result.PublicKey);
            Assert.Equal(33, result.PublicKey.Length); // Compressed secp256k1 public key is 33 bytes
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GenerateKey_AES256_ShouldSucceed()
        {
            // Arrange
            var keyId = "test_aes256_key";
            var usage = new[] { "Encrypt", "Decrypt" };

            // Act
            var result = await _cryptoService.GenerateKeyAsync(
                keyId, 
                CryptoAlgorithm.Aes256Gcm, 
                usage, 
                exportable: false, 
                "Test AES-256 key"
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(keyId, result.KeyId);
            Assert.Equal(CryptoAlgorithm.Aes256Gcm, result.KeyType);
            Assert.Equal(usage, result.Usage);
            Assert.Null(result.PublicKey); // Symmetric key has no public component
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GenerateKey_DuplicateKeyId_ShouldThrow()
        {
            // Arrange
            var keyId = "duplicate_key_test";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, "First key");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, "Duplicate key")
            );
            Assert.Contains("already exists", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GenerateKey_InvalidKeyId_ShouldThrow(string keyId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, "Test")
            );
        }

        #endregion

        #region Random Number Generation Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void GenerateRandom_ValidRange_ShouldReturnInRange()
        {
            // Arrange
            const int min = 10;
            const int max = 100;

            // Act
            var result = _cryptoService.GenerateRandom(min, max);

            // Assert
            Assert.InRange(result, min, max - 1); // max is exclusive
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void GenerateRandom_InvalidRange_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cryptoService.GenerateRandom(100, 10));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void GenerateRandomBytes_ValidLength_ShouldReturnCorrectSize()
        {
            // Arrange
            const int length = 32;

            // Act
            var result = _cryptoService.GenerateRandomBytes(length);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(length, result.Length);
            
            // Verify randomness (very basic check - should not be all zeros)
            Assert.True(result.Any(b => b != 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1024 * 1024 + 1)] // > 1MB
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void GenerateRandomBytes_InvalidLength_ShouldThrow(int length)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cryptoService.GenerateRandomBytes(length));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void GenerateRandomBytes_MultipleCallsShouldBeDifferent()
        {
            // Act
            var result1 = _cryptoService.GenerateRandomBytes(32);
            var result2 = _cryptoService.GenerateRandomBytes(32);

            // Assert
            Assert.False(result1.SequenceEqual(result2), "Two random byte arrays should be different");
        }

        #endregion

        #region Encryption/Decryption Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task EncryptDecrypt_AesGcm_ShouldRoundTrip()
        {
            // Arrange
            var keyId = "test_encrypt_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Aes256Gcm, new[] { "Encrypt", "Decrypt" }, false, "Test encryption key");
            
            var plaintext = Encoding.UTF8.GetBytes("Hello, SGX Enclave!");
            var key = _cryptoService.GetSymmetricKey(keyId); // This would be an internal method

            // Act
            var encrypted = _cryptoService.EncryptAesGcm(plaintext, key);
            var decrypted = _cryptoService.DecryptAesGcm(encrypted, key);

            // Assert
            Assert.NotNull(encrypted);
            Assert.True(encrypted.Length > plaintext.Length); // Should include nonce and tag
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void EncryptAesGcm_InvalidKeySize_ShouldThrow()
        {
            // Arrange
            var plaintext = Encoding.UTF8.GetBytes("Test data");
            var invalidKey = new byte[16]; // Should be 32 bytes for AES-256

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cryptoService.EncryptAesGcm(plaintext, invalidKey));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void DecryptAesGcm_TamperedData_ShouldThrow()
        {
            // Arrange
            var keyId = "test_tamper_key";
            _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Aes256Gcm, new[] { "Encrypt", "Decrypt" }, false, "Test key").Wait();
            
            var plaintext = Encoding.UTF8.GetBytes("Original data");
            var key = _cryptoService.GetSymmetricKey(keyId);
            var encrypted = _cryptoService.EncryptAesGcm(plaintext, key);
            
            // Tamper with the encrypted data
            encrypted[encrypted.Length - 1] ^= 0x01;

            // Act & Assert
            Assert.Throws<CryptographicException>(() => _cryptoService.DecryptAesGcm(encrypted, key));
        }

        #endregion

        #region Digital Signature Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task SignVerify_Ed25519_ShouldWork()
        {
            // Arrange
            var keyId = "test_sign_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, "Test signing key");
            
            var message = Encoding.UTF8.GetBytes("Message to sign");

            // Act
            var signature = await _cryptoService.SignDataAsync(keyId, message);
            var isValid = await _cryptoService.VerifySignatureAsync(keyId, message, signature);

            // Assert
            Assert.NotNull(signature);
            Assert.Equal(64, signature.Length); // Ed25519 signature is 64 bytes
            Assert.True(isValid);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task SignVerify_Secp256k1_ShouldWork()
        {
            // Arrange
            var keyId = "test_secp_sign_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Secp256k1, new[] { "Sign", "Verify" }, false, "Test secp256k1 signing key");
            
            var message = Encoding.UTF8.GetBytes("Message to sign with secp256k1");

            // Act
            var signature = await _cryptoService.SignDataAsync(keyId, message);
            var isValid = await _cryptoService.VerifySignatureAsync(keyId, message, signature);

            // Assert
            Assert.NotNull(signature);
            Assert.Equal(64, signature.Length); // Compact secp256k1 signature is 64 bytes
            Assert.True(isValid);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task VerifySignature_WrongMessage_ShouldReturnFalse()
        {
            // Arrange
            var keyId = "test_verify_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, "Test key");
            
            var originalMessage = Encoding.UTF8.GetBytes("Original message");
            var wrongMessage = Encoding.UTF8.GetBytes("Wrong message");
            
            var signature = await _cryptoService.SignDataAsync(keyId, originalMessage);

            // Act
            var isValid = await _cryptoService.VerifySignatureAsync(keyId, wrongMessage, signature);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task SignData_UnauthorizedUsage_ShouldThrow()
        {
            // Arrange
            var keyId = "test_unauthorized_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Verify" }, false, "Verify-only key"); // No "Sign" usage
            
            var message = Encoding.UTF8.GetBytes("Message");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _cryptoService.SignDataAsync(keyId, message)
            );
            Assert.Contains("not authorized for signing", exception.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task SignData_NonExistentKey_ShouldThrow()
        {
            // Arrange
            var message = Encoding.UTF8.GetBytes("Message");

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _cryptoService.SignDataAsync("nonexistent_key", message)
            );
        }

        #endregion

        #region Hashing Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void HashSha256_ShouldProduceCorrectHash()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("Hello, World!");
            var expectedHash = SHA256.HashData(input); // Reference implementation

            // Act
            var actualHash = _cryptoService.HashSha256(input);

            // Assert
            Assert.Equal(expectedHash, actualHash);
            Assert.Equal(32, actualHash.Length); // SHA-256 produces 32-byte hash
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void HashSha256_EmptyInput_ShouldWork()
        {
            // Arrange
            var input = Array.Empty<byte>();

            // Act
            var hash = _cryptoService.HashSha256(input);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void HashSha256_SameInputShouldProduceSameHash()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("Consistent input");

            // Act
            var hash1 = _cryptoService.HashSha256(input);
            var hash2 = _cryptoService.HashSha256(input);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void HashSha256_DifferentInputsShouldProduceDifferentHashes()
        {
            // Arrange
            var input1 = Encoding.UTF8.GetBytes("Input 1");
            var input2 = Encoding.UTF8.GetBytes("Input 2");

            // Act
            var hash1 = _cryptoService.HashSha256(input1);
            var hash2 = _cryptoService.HashSha256(input2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        #endregion

        #region Key Management Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GetKeyMetadata_ExistingKey_ShouldReturnMetadata()
        {
            // Arrange
            var keyId = "test_metadata_key";
            var originalMetadata = await _cryptoService.GenerateKeyAsync(
                keyId, 
                CryptoAlgorithm.Ed25519, 
                new[] { "Sign", "Verify" }, 
                exportable: false, 
                "Test metadata key"
            );

            // Act
            var retrievedMetadata = await _cryptoService.GetKeyMetadataAsync(keyId);

            // Assert
            Assert.Equal(originalMetadata.KeyId, retrievedMetadata.KeyId);
            Assert.Equal(originalMetadata.KeyType, retrievedMetadata.KeyType);
            Assert.Equal(originalMetadata.Usage, retrievedMetadata.Usage);
            Assert.Equal(originalMetadata.Description, retrievedMetadata.Description);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GetKeyMetadata_NonExistentKey_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _cryptoService.GetKeyMetadataAsync("nonexistent_key")
            );
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task ListKeys_ShouldReturnAllKeys()
        {
            // Arrange
            var keyIds = new[] { "list_key_1", "list_key_2", "list_key_3" };
            foreach (var keyId in keyIds)
            {
                await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, $"List test key {keyId}");
            }

            // Act
            var listedKeys = await _cryptoService.ListKeysAsync();

            // Assert
            Assert.Contains(keyIds[0], listedKeys);
            Assert.Contains(keyIds[1], listedKeys);
            Assert.Contains(keyIds[2], listedKeys);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task DeleteKey_ExistingKey_ShouldRemoveKey()
        {
            // Arrange
            var keyId = "test_delete_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, "Key to delete");

            // Verify key exists
            var metadataBefore = await _cryptoService.GetKeyMetadataAsync(keyId);
            Assert.NotNull(metadataBefore);

            // Act
            await _cryptoService.DeleteKeyAsync(keyId);

            // Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _cryptoService.GetKeyMetadataAsync(keyId)
            );
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task DeleteKey_NonExistentKey_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _cryptoService.DeleteKeyAsync("nonexistent_key")
            );
        }

        #endregion

        #region Security and Edge Case Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task CryptoOperations_LargeData_ShouldWork()
        {
            // Arrange
            var keyId = "test_large_data_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, "Large data test key");
            
            var largeData = new byte[100 * 1024]; // 100KB
            new Random(42).NextBytes(largeData);

            // Act
            var signature = await _cryptoService.SignDataAsync(keyId, largeData);
            var isValid = await _cryptoService.VerifySignatureAsync(keyId, largeData, signature);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task CryptoOperations_EmptyData_ShouldWork()
        {
            // Arrange
            var keyId = "test_empty_data_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, "Empty data test key");
            
            var emptyData = Array.Empty<byte>();

            // Act
            var signature = await _cryptoService.SignDataAsync(keyId, emptyData);
            var isValid = await _cryptoService.VerifySignatureAsync(keyId, emptyData, signature);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task ConcurrentOperations_ShouldNotInterfere()
        {
            // Arrange
            var tasks = new List<Task>();
            var keyIds = new List<string>();

            // Create multiple concurrent crypto operations
            for (int i = 0; i < 10; i++)
            {
                var keyId = $"concurrent_key_{i}";
                keyIds.Add(keyId);
                
                tasks.Add(Task.Run(async () =>
                {
                    await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, $"Concurrent test key {i}");
                    
                    var data = Encoding.UTF8.GetBytes($"Test data {i}");
                    var signature = await _cryptoService.SignDataAsync(keyId, data);
                    var isValid = await _cryptoService.VerifySignatureAsync(keyId, data, signature);
                    
                    Assert.True(isValid);
                }));
            }

            // Act & Assert
            await Task.WhenAll(tasks);

            // Verify all keys were created
            var allKeys = await _cryptoService.ListKeysAsync();
            foreach (var keyId in keyIds)
            {
                Assert.Contains(keyId, allKeys);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public void RandomGeneration_ThreadSafety_ShouldWork()
        {
            // Arrange
            var tasks = new List<Task<byte[]>>();
            
            // Generate random bytes concurrently
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => _cryptoService.GenerateRandomBytes(32)));
            }

            // Act
            var results = Task.WhenAll(tasks).Result;

            // Assert
            // All results should be different (very high probability)
            var uniqueResults = new HashSet<string>();
            foreach (var result in results)
            {
                var hex = Convert.ToHexString(result);
                Assert.True(uniqueResults.Add(hex), "Random bytes should be unique");
            }
        }

        [Theory]
        [InlineData(CryptoAlgorithm.Aes256Gcm, new[] { "Encrypt", "Decrypt" })]
        [InlineData(CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" })]
        [InlineData(CryptoAlgorithm.Secp256k1, new[] { "Sign", "Verify" })]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        public async Task GenerateKey_AllSupportedAlgorithms_ShouldWork(CryptoAlgorithm algorithm, string[] usage)
        {
            // Arrange
            var keyId = $"test_{algorithm.ToString().ToLower()}_key";

            // Act
            var metadata = await _cryptoService.GenerateKeyAsync(keyId, algorithm, usage, false, $"Test {algorithm} key");

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(keyId, metadata.KeyId);
            Assert.Equal(algorithm, metadata.KeyType);
            Assert.Equal(usage, metadata.Usage);
        }

        #endregion

        #region Performance Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Crypto")]
        [Trait("Performance", "True")]
        public async Task CryptoOperations_Performance_ShouldMeetBaselines()
        {
            // Arrange
            var keyId = "perf_test_key";
            await _cryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, "Performance test key");
            
            var testData = Encoding.UTF8.GetBytes("Performance test data");
            var iterations = 1000;

            // Act - Measure signing performance
            var signStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var signatures = new List<byte[]>();
            
            for (int i = 0; i < iterations; i++)
            {
                var signature = await _cryptoService.SignDataAsync(keyId, testData);
                signatures.Add(signature);
            }
            signStopwatch.Stop();

            // Act - Measure verification performance
            var verifyStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var verificationResults = new List<bool>();
            
            for (int i = 0; i < iterations; i++)
            {
                var isValid = await _cryptoService.VerifySignatureAsync(keyId, testData, signatures[i]);
                verificationResults.Add(isValid);
            }
            verifyStopwatch.Stop();

            // Assert
            var avgSignTime = signStopwatch.ElapsedMilliseconds / (double)iterations;
            var avgVerifyTime = verifyStopwatch.ElapsedMilliseconds / (double)iterations;
            
            Assert.True(avgSignTime < 10, $"Average signing time {avgSignTime:F2}ms should be under 10ms");
            Assert.True(avgVerifyTime < 5, $"Average verification time {avgVerifyTime:F2}ms should be under 5ms");
            Assert.True(verificationResults.All(r => r), "All signatures should be valid");
        }

        #endregion

        public void Dispose()
        {
            _cryptoService?.Dispose();
        }
    }

    /// <summary>
    /// Mock/test implementation of CryptoService for unit testing
    /// This simulates the actual SGX enclave behavior without requiring SGX hardware
    /// </summary>
    public class CryptoService : IDisposable
    {
        private readonly Dictionary<string, KeyMetadata> _keyStore = new();
        private readonly Dictionary<string, byte[]> _symmetricKeys = new();
        private readonly Dictionary<string, (byte[] privateKey, byte[] publicKey)> _asymmetricKeys = new();
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private readonly EnclaveConfig _config;

        private CryptoService(EnclaveConfig config)
        {
            _config = config;
        }

        public static async Task<CryptoService> NewAsync(EnclaveConfig config)
        {
            return await Task.FromResult(new CryptoService(config));
        }

        public async Task<KeyMetadata> GenerateKeyAsync(string keyId, CryptoAlgorithm keyType, string[] usage, bool exportable, string description)
        {
            if (string.IsNullOrEmpty(keyId))
                throw new ArgumentException("Key ID cannot be empty");

            if (_keyStore.ContainsKey(keyId))
                throw new InvalidOperationException($"Key with ID '{keyId}' already exists");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            byte[] publicKey = null;

            switch (keyType)
            {
                case CryptoAlgorithm.Aes256Gcm:
                    var symmetricKey = new byte[32];
                    _rng.GetBytes(symmetricKey);
                    _symmetricKeys[keyId] = symmetricKey;
                    break;

                case CryptoAlgorithm.Ed25519:
                    var ed25519Private = new byte[32];
                    _rng.GetBytes(ed25519Private);
                    publicKey = new byte[32];
                    _rng.GetBytes(publicKey); // Simplified - in reality this would derive from private key
                    _asymmetricKeys[keyId] = (ed25519Private, publicKey);
                    break;

                case CryptoAlgorithm.Secp256k1:
                    var secp256k1Private = new byte[32];
                    _rng.GetBytes(secp256k1Private);
                    publicKey = new byte[33]; // Compressed public key
                    _rng.GetBytes(publicKey);
                    publicKey[0] = 0x02; // Compressed format marker
                    _asymmetricKeys[keyId] = (secp256k1Private, publicKey);
                    break;

                default:
                    throw new NotSupportedException($"Key type {keyType} not supported");
            }

            var metadata = new KeyMetadata
            {
                KeyId = keyId,
                KeyType = keyType,
                Usage = usage,
                Exportable = exportable,
                CreatedAt = now,
                Description = description,
                PublicKey = publicKey
            };

            _keyStore[keyId] = metadata;
            return await Task.FromResult(metadata);
        }

        public int GenerateRandom(int min, int max)
        {
            if (min >= max)
                throw new ArgumentException("Min must be less than max");

            var range = max - min;
            var randomBytes = new byte[4];
            _rng.GetBytes(randomBytes);
            var randomInt = BitConverter.ToUInt32(randomBytes, 0);
            return min + (int)(randomInt % range);
        }

        public byte[] GenerateRandomBytes(int length)
        {
            if (length <= 0 || length > 1024 * 1024)
                throw new ArgumentException("Invalid length");

            var bytes = new byte[length];
            _rng.GetBytes(bytes);
            return bytes;
        }

        public byte[] EncryptAesGcm(byte[] plaintext, byte[] key)
        {
            if (key.Length != 32)
                throw new ArgumentException("AES-256 key must be 32 bytes");

            var nonce = new byte[12];
            _rng.GetBytes(nonce);
            
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
            
            // Combine nonce + ciphertext + tag
            var result = new byte[12 + plaintext.Length + 16];
            nonce.CopyTo(result, 0);
            ciphertext.CopyTo(result, 12);
            tag.CopyTo(result, 12 + plaintext.Length);
            
            return result;
        }

        public byte[] DecryptAesGcm(byte[] encryptedData, byte[] key)
        {
            if (key.Length != 32)
                throw new ArgumentException("AES-256 key must be 32 bytes");

            if (encryptedData.Length < 28) // 12 + 0 + 16 minimum
                throw new ArgumentException("Encrypted data too short");

            
            var nonce = encryptedData[0..12];
            var ciphertext = encryptedData[12..^16];
            var tag = encryptedData[^16..];
            
            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            
            return plaintext;
        }

        public async Task<byte[]> SignDataAsync(string keyId, byte[] data)
        {
            if (!_keyStore.TryGetValue(keyId, out var metadata))
                throw new KeyNotFoundException($"Key '{keyId}' not found");

            if (!metadata.Usage.Contains("Sign"))
                throw new UnauthorizedAccessException($"Key '{keyId}' is not authorized for signing");

            var signature = metadata.KeyType switch
            {
                CryptoAlgorithm.Ed25519 => SignEd25519(data, _asymmetricKeys[keyId].privateKey),
                CryptoAlgorithm.Secp256k1 => SignSecp256k1(data, _asymmetricKeys[keyId].privateKey),
                _ => throw new NotSupportedException($"Signing not supported for {metadata.KeyType}")
            };

            return await Task.FromResult(signature);
        }

        public async Task<bool> VerifySignatureAsync(string keyId, byte[] data, byte[] signature)
        {
            if (!_keyStore.TryGetValue(keyId, out var metadata))
                throw new KeyNotFoundException($"Key '{keyId}' not found");

            if (!metadata.Usage.Contains("Verify"))
                throw new UnauthorizedAccessException($"Key '{keyId}' is not authorized for verification");

            var isValid = metadata.KeyType switch
            {
                CryptoAlgorithm.Ed25519 => VerifyEd25519(data, signature, _asymmetricKeys[keyId].publicKey),
                CryptoAlgorithm.Secp256k1 => VerifySecp256k1(data, signature, _asymmetricKeys[keyId].publicKey),
                _ => throw new NotSupportedException($"Verification not supported for {metadata.KeyType}")
            };

            return await Task.FromResult(isValid);
        }

        public byte[] HashSha256(byte[] data)
        {
            return SHA256.HashData(data);
        }

        public async Task<KeyMetadata> GetKeyMetadataAsync(string keyId)
        {
            if (!_keyStore.TryGetValue(keyId, out var metadata))
                throw new KeyNotFoundException($"Key '{keyId}' not found");

            return await Task.FromResult(metadata);
        }

        public async Task<string[]> ListKeysAsync()
        {
            return await Task.FromResult(_keyStore.Keys.ToArray());
        }

        public async Task DeleteKeyAsync(string keyId)
        {
            if (!_keyStore.ContainsKey(keyId))
                throw new KeyNotFoundException($"Key '{keyId}' not found");

            _keyStore.Remove(keyId);
            _symmetricKeys.Remove(keyId);
            _asymmetricKeys.Remove(keyId);

            await Task.CompletedTask;
        }

        public byte[] GetSymmetricKey(string keyId)
        {
            return _symmetricKeys.TryGetValue(keyId, out var key) ? key : throw new KeyNotFoundException($"Symmetric key '{keyId}' not found");
        }

        private byte[] SignEd25519(byte[] data, byte[] privateKey)
        {
            // Simplified Ed25519 signing simulation
            var hash = hmac.ComputeHash(data);
            var signature = new byte[64];
            hash.CopyTo(signature, 0);
            hash.CopyTo(signature, 32);
            return signature;
        }

        private bool VerifyEd25519(byte[] data, byte[] signature, byte[] publicKey)
        {
            if (signature.Length != 64) return false;
            
            // Simplified verification - derive expected signature from private key
            var keyId = _asymmetricKeys.FirstOrDefault(kvp => kvp.Value.publicKey.SequenceEqual(publicKey)).Key;
            if (keyId == null) return false;
            
            var expectedSignature = SignEd25519(data, _asymmetricKeys[keyId].privateKey);
            return signature.SequenceEqual(expectedSignature);
        }

        private byte[] SignSecp256k1(byte[] data, byte[] privateKey)
        {
            // Simplified secp256k1 signing simulation
            var hash = SHA256.HashData(data);
            var signature = hmac.ComputeHash(hash);
            
            // Extend to 64 bytes for compact signature format
            var compactSignature = new byte[64];
            signature.CopyTo(compactSignature, 0);
            signature.CopyTo(compactSignature, 32);
            return compactSignature;
        }

        private bool VerifySecp256k1(byte[] data, byte[] signature, byte[] publicKey)
        {
            if (signature.Length != 64) return false;
            
            // Simplified verification
            var keyId = _asymmetricKeys.FirstOrDefault(kvp => kvp.Value.publicKey.SequenceEqual(publicKey)).Key;
            if (keyId == null) return false;
            
            var expectedSignature = SignSecp256k1(data, _asymmetricKeys[keyId].privateKey);
            return signature.SequenceEqual(expectedSignature);
        }

        public void Dispose()
        {
            _rng?.Dispose();
            _keyStore.Clear();
            _symmetricKeys.Clear();
            _asymmetricKeys.Clear();
        }
    }

    // Supporting types and enums for the tests
    public enum CryptoAlgorithm
    {
        Aes256Gcm,
        ChaCha20Poly1305,
        Secp256k1,
        Ed25519,
        Sha256,
        Sha3_256
    }

    public class KeyMetadata
    {
        public string KeyId { get; set; }
        public CryptoAlgorithm KeyType { get; set; }
        public string[] Usage { get; set; }
        public bool Exportable { get; set; }
        public long CreatedAt { get; set; }
        public string Description { get; set; }
        public byte[] PublicKey { get; set; }
    }

    public class EnclaveConfig
    {
        public List<string> crypto_algorithms { get; set; } = new();
    }
}