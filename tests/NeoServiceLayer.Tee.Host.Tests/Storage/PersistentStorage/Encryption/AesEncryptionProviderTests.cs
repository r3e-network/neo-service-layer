using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage.Encryption
{
    public class AesEncryptionProviderTests : IDisposable
    {
        private readonly Mock<ILogger<AesEncryptionProvider>> _loggerMock;
        private readonly AesEncryptionOptions _options;
        private readonly AesEncryptionProvider _provider;
        private readonly string _testDirectory;
        private readonly string _keyFile;

        public AesEncryptionProviderTests()
        {
            _loggerMock = new Mock<ILogger<AesEncryptionProvider>>();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"aes_encryption_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _keyFile = Path.Combine(_testDirectory, "aes_key.bin");
            _options = new AesEncryptionOptions
            {
                KeyFile = _keyFile,
                KeySizeBits = 256
            };
            _provider = new AesEncryptionProvider(_loggerMock.Object, _options);
        }

        public void Dispose()
        {
            _provider.Dispose();

            // Clean up the test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }

        [Fact]
        public async Task InitializeAsync_CreatesKeyFile()
        {
            // Act
            await _provider.InitializeAsync();

            // Assert
            Assert.True(File.Exists(_keyFile));
        }

        [Fact]
        public async Task EncryptAsync_DecryptAsync_RoundTrip()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act
            byte[] encryptedData = await _provider.EncryptAsync(data);
            byte[] decryptedData = await _provider.DecryptAsync(encryptedData);

            // Assert
            Assert.NotEqual(data, encryptedData); // Encryption should change the data
            Assert.Equal(data, decryptedData); // Decryption should restore the original data
        }

        [Fact]
        public async Task EncryptAsync_WithContext_DecryptAsync_WithContext_RoundTrip()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            byte[] context = Encoding.UTF8.GetBytes("context_data");

            // Act
            byte[] encryptedData = await _provider.EncryptAsync(data, context);
            byte[] decryptedData = await _provider.DecryptAsync(encryptedData, context);

            // Assert
            Assert.NotEqual(data, encryptedData); // Encryption should change the data
            Assert.Equal(data, decryptedData); // Decryption should restore the original data
        }

        [Fact]
        public async Task EncryptAsync_WithContext_DecryptAsync_WithWrongContext_ThrowsException()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            byte[] context = Encoding.UTF8.GetBytes("context_data");
            byte[] wrongContext = Encoding.UTF8.GetBytes("wrong_context_data");

            // Act
            byte[] encryptedData = await _provider.EncryptAsync(data, context);

            // Assert
            await Assert.ThrowsAsync<StorageException>(() => _provider.DecryptAsync(encryptedData, wrongContext));
        }

        [Fact]
        public async Task RotateKeyAsync_ChangesKey()
        {
            // Arrange
            await _provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            byte[] encryptedData = await _provider.EncryptAsync(data);
            
            // Get the original key file content
            byte[] originalKeyContent = File.ReadAllBytes(_keyFile);

            // Act
            await _provider.RotateKeyAsync();
            
            // Get the new key file content
            byte[] newKeyContent = File.ReadAllBytes(_keyFile);
            
            // Try to decrypt the data with the new key
            byte[] decryptedData = await _provider.DecryptAsync(encryptedData);

            // Assert
            Assert.NotEqual(originalKeyContent, newKeyContent); // Key should have changed
            Assert.Equal(data, decryptedData); // Decryption should still work with the new key
        }

        [Fact]
        public async Task GetProviderInfo_ReturnsCorrectInfo()
        {
            // Arrange
            await _provider.InitializeAsync();

            // Act
            var info = _provider.GetProviderInfo();

            // Assert
            Assert.Equal("AES", info.Name);
            Assert.Equal("AES-GCM", info.Algorithm);
            Assert.Equal(256, info.KeySizeBits);
            Assert.Equal(128, info.BlockSizeBits);
            Assert.Equal(96, info.IVSizeBits);
            Assert.True(info.SupportsAuthenticatedEncryption);
            Assert.True(info.SupportsKeyRotation);
            Assert.NotNull(info.CurrentKeyCreationTime);
            Assert.NotNull(info.AdditionalProperties);
            Assert.True(info.AdditionalProperties.ContainsKey("KeyFile"));
            Assert.Equal(_keyFile, info.AdditionalProperties["KeyFile"]);
        }

        [Fact]
        public async Task InitializeAsync_WithProvidedKey_UsesProvidedKey()
        {
            // Arrange
            byte[] key = new byte[32]; // 256 bits
            new Random().NextBytes(key);
            var options = new AesEncryptionOptions
            {
                Key = key
            };
            var provider = new AesEncryptionProvider(_loggerMock.Object, options);

            // Act
            await provider.InitializeAsync();
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            byte[] encryptedData = await provider.EncryptAsync(data);
            byte[] decryptedData = await provider.DecryptAsync(encryptedData);

            // Assert
            Assert.Equal(data, decryptedData); // Decryption should work with the provided key
        }
    }
}
