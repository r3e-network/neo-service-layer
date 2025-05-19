using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage
{
    public class SecureStorageTests
    {
        private readonly Mock<ILogger<SecureStorage>> _loggerMock;
        private readonly Mock<IOpenEnclaveInterface> _enclaveInterfaceMock;
        private readonly Mock<IPersistentStorageProvider> _storageProviderMock;
        private readonly SecureStorageOptions _options;
        private readonly SecureStorage _secureStorage;

        public SecureStorageTests()
        {
            _loggerMock = new Mock<ILogger<SecureStorage>>();
            _enclaveInterfaceMock = new Mock<IOpenEnclaveInterface>();
            _storageProviderMock = new Mock<IPersistentStorageProvider>();
            _options = new SecureStorageOptions
            {
                StorageDirectory = "test_storage",
                EnableCaching = true,
                EnablePersistence = true
            };

            // Setup the enclave interface mock to simulate encryption/decryption
            _enclaveInterfaceMock.Setup(e => e.SealData(It.IsAny<byte[]>()))
                .Returns<byte[]>(data => {
                    // Simple "encryption" for testing - just append a marker
                    byte[] encrypted = new byte[data.Length + 4];
                    Array.Copy(data, 0, encrypted, 0, data.Length);
                    encrypted[data.Length] = 0xDE;
                    encrypted[data.Length + 1] = 0xAD;
                    encrypted[data.Length + 2] = 0xBE;
                    encrypted[data.Length + 3] = 0xEF;
                    return encrypted;
                });
            
            _enclaveInterfaceMock.Setup(e => e.UnsealData(It.IsAny<byte[]>()))
                .Returns<byte[]>(encrypted => {
                    // Simple "decryption" for testing - just remove the marker
                    if (encrypted.Length < 4)
                        throw new ArgumentException("Invalid encrypted data");
                    
                    // Check for our marker
                    if (encrypted[encrypted.Length - 4] != 0xDE ||
                        encrypted[encrypted.Length - 3] != 0xAD ||
                        encrypted[encrypted.Length - 2] != 0xBE ||
                        encrypted[encrypted.Length - 1] != 0xEF)
                        throw new ArgumentException("Invalid encrypted data");
                    
                    byte[] decrypted = new byte[encrypted.Length - 4];
                    Array.Copy(encrypted, 0, decrypted, 0, decrypted.Length);
                    return decrypted;
                });

            _secureStorage = new SecureStorage(
                _loggerMock.Object,
                _enclaveInterfaceMock.Object,
                _options,
                _storageProviderMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_CallsStorageProviderInitialize()
        {
            // Act
            await _secureStorage.InitializeAsync();

            // Assert
            _storageProviderMock.Verify(p => p.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task StoreAsync_WithoutInitialization_ThrowsInvalidOperationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _secureStorage.StoreAsync("test_key", "test_value"));
        }

        [Fact]
        public async Task StoreAsync_WithCachingAndPersistence_StoresValueInCacheAndStorage()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            string value = "test_value";
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            byte[] sealedValue = _enclaveInterfaceMock.Object.SealData(valueBytes);

            // Act
            await _secureStorage.StoreAsync(key, value);

            // Assert
            _storageProviderMock.Verify(p => p.WriteAsync(key, It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public async Task RetrieveAsync_ValueInCache_ReturnsValueFromCache()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            string value = "test_value";
            
            // Store the value first
            await _secureStorage.StoreAsync(key, value);
            
            // Clear the verification count
            _storageProviderMock.Invocations.Clear();

            // Act
            string result = await _secureStorage.RetrieveAsync(key);

            // Assert
            Assert.Equal(value, result);
            _storageProviderMock.Verify(p => p.ReadAsync(key), Times.Never);
        }

        [Fact]
        public async Task RetrieveAsync_ValueNotInCacheButInStorage_ReturnsValueFromStorage()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            string value = "test_value";
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            byte[] sealedValue = _enclaveInterfaceMock.Object.SealData(valueBytes);
            
            // Setup the storage provider to return the sealed value
            _storageProviderMock.Setup(p => p.ReadAsync(key))
                .ReturnsAsync(sealedValue);

            // Act
            string result = await _secureStorage.RetrieveAsync(key);

            // Assert
            Assert.Equal(value, result);
            _storageProviderMock.Verify(p => p.ReadAsync(key), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_RemovesValueFromCacheAndStorage()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            string value = "test_value";
            
            // Store the value first
            await _secureStorage.StoreAsync(key, value);
            
            // Setup the storage provider to return true for delete
            _storageProviderMock.Setup(p => p.DeleteAsync(key))
                .ReturnsAsync(true);
            
            // Clear the verification count
            _storageProviderMock.Invocations.Clear();

            // Act
            bool result = await _secureStorage.RemoveAsync(key);

            // Assert
            Assert.True(result);
            _storageProviderMock.Verify(p => p.DeleteAsync(key), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_KeyInCache_ReturnsTrue()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            string value = "test_value";
            
            // Store the value first
            await _secureStorage.StoreAsync(key, value);
            
            // Clear the verification count
            _storageProviderMock.Invocations.Clear();

            // Act
            bool result = await _secureStorage.ExistsAsync(key);

            // Assert
            Assert.True(result);
            _storageProviderMock.Verify(p => p.ExistsAsync(key), Times.Never);
        }

        [Fact]
        public async Task ExistsAsync_KeyNotInCacheButInStorage_ReturnsTrue()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            
            // Setup the storage provider to return true for exists
            _storageProviderMock.Setup(p => p.ExistsAsync(key))
                .ReturnsAsync(true);

            // Act
            bool result = await _secureStorage.ExistsAsync(key);

            // Assert
            Assert.True(result);
            _storageProviderMock.Verify(p => p.ExistsAsync(key), Times.Once);
        }

        [Fact]
        public async Task GetAllKeysAsync_ReturnsCombinedKeysFromCacheAndStorage()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key1 = "test_key_1";
            string key2 = "test_key_2";
            string value = "test_value";
            
            // Store one value in cache
            await _secureStorage.StoreAsync(key1, value);
            
            // Setup the storage provider to return keys
            _storageProviderMock.Setup(p => p.GetAllKeysAsync())
                .ReturnsAsync(new[] { key2 });

            // Act
            var keys = await _secureStorage.GetAllKeysAsync();

            // Assert
            Assert.Equal(2, keys.Count);
            Assert.Contains(key1, keys);
            Assert.Contains(key2, keys);
            _storageProviderMock.Verify(p => p.GetAllKeysAsync(), Times.Once);
        }

        [Fact]
        public async Task ClearAsync_ClearsCacheAndStorage()
        {
            // Arrange
            await _secureStorage.InitializeAsync();
            string key = "test_key";
            string value = "test_value";
            
            // Store the value first
            await _secureStorage.StoreAsync(key, value);
            
            // Setup the storage provider to return keys
            _storageProviderMock.Setup(p => p.GetAllKeysAsync())
                .ReturnsAsync(new[] { key });
            
            // Clear the verification count
            _storageProviderMock.Invocations.Clear();

            // Act
            await _secureStorage.ClearAsync();

            // Assert
            _storageProviderMock.Verify(p => p.GetAllKeysAsync(), Times.Once);
            _storageProviderMock.Verify(p => p.DeleteAsync(key), Times.Once);
        }

        [Fact]
        public void Dispose_DisposesStorageProvider()
        {
            // Act
            _secureStorage.Dispose();

            // Assert
            _storageProviderMock.Verify(p => p.Dispose(), Times.Once);
        }
    }
}
