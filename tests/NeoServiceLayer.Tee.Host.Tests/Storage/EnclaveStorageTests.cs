using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage
{
    public class EnclaveStorageTests : IDisposable
    {
        private readonly Mock<ILogger<SecureStorage>> _loggerMock;
        private readonly Mock<ITeeInterface> _teeInterfaceMock;
        private readonly string _testDirectory;
        private readonly IPersistentStorageProvider _storageProvider;
        private readonly SecureStorage _secureStorage;

        public EnclaveStorageTests()
        {
            _loggerMock = new Mock<ILogger<SecureStorage>>();
            _teeInterfaceMock = new Mock<ITeeInterface>();
            
            // Set up the enclave interface mock
            _teeInterfaceMock.Setup(e => e.SealData(It.IsAny<byte[]>()))
                .Returns<byte[]>(data => 
                {
                    // Simple "encryption" for testing - prepend "SEALED:" to the data
                    byte[] prefix = Encoding.UTF8.GetBytes("SEALED:");
                    byte[] result = new byte[prefix.Length + data.Length];
                    Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
                    Buffer.BlockCopy(data, 0, result, prefix.Length, data.Length);
                    return result;
                });
                
            _teeInterfaceMock.Setup(e => e.UnsealData(It.IsAny<byte[]>()))
                .Returns<byte[]>(sealedData => 
                {
                    // Simple "decryption" for testing - remove "SEALED:" prefix
                    string sealedString = Encoding.UTF8.GetString(sealedData);
                    if (sealedString.StartsWith("SEALED:"))
                    {
                        return Encoding.UTF8.GetBytes(sealedString.Substring(7));
                    }
                    return sealedData;
                });
            
            // Create a test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"enclave_storage_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Create a storage provider
            var storageLoggerMock = new Mock<ILogger<OcclumFileStorageProvider>>();
            _storageProvider = new OcclumFileStorageProvider(
                storageLoggerMock.Object,
                new OcclumFileStorageOptions
                {
                    StorageDirectory = _testDirectory
                });
            
            // Initialize the storage provider
            _storageProvider.InitializeAsync().GetAwaiter().GetResult();
            
            // Create the secure storage
            _secureStorage = new SecureStorage(
                _loggerMock.Object,
                _teeInterfaceMock.Object,
                new SecureStorageOptions
                {
                    StorageDirectory = _testDirectory,
                    EnableCaching = true,
                    EnablePersistence = true
                },
                _storageProvider);
            
            // Initialize the secure storage
            _secureStorage.InitializeAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            // Clean up
            _secureStorage.Dispose();
            
            // Delete the test directory
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        [Fact]
        public async Task StoreAsync_String_StoresAndSealsData()
        {
            // Arrange
            string key = "test-key";
            string value = "test-value";
            
            // Act
            await _secureStorage.StoreAsync(key, value);
            
            // Assert
            _teeInterfaceMock.Verify(e => e.SealData(It.IsAny<byte[]>()), Times.Once);
            
            // Verify that the data was stored in the storage provider
            byte[] storedData = await _storageProvider.ReadAsync(key);
            Assert.NotNull(storedData);
            
            // Verify that the stored data is sealed
            string storedString = Encoding.UTF8.GetString(storedData);
            Assert.StartsWith("SEALED:", storedString);
        }

        [Fact]
        public async Task RetrieveAsync_String_RetrievesAndUnsealsData()
        {
            // Arrange
            string key = "test-key";
            string value = "test-value";
            await _secureStorage.StoreAsync(key, value);
            
            // Act
            string retrievedValue = await _secureStorage.RetrieveAsync(key);
            
            // Assert
            _teeInterfaceMock.Verify(e => e.UnsealData(It.IsAny<byte[]>()), Times.Once);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task StoreAsync_Bytes_StoresAndSealsData()
        {
            // Arrange
            string key = "test-key-bytes";
            byte[] value = Encoding.UTF8.GetBytes("test-value-bytes");
            
            // Act
            await _secureStorage.StoreAsync(key, value);
            
            // Assert
            _teeInterfaceMock.Verify(e => e.SealData(It.IsAny<byte[]>()), Times.Once);
            
            // Verify that the data was stored in the storage provider
            byte[] storedData = await _storageProvider.ReadAsync(key);
            Assert.NotNull(storedData);
            
            // Verify that the stored data is sealed
            string storedString = Encoding.UTF8.GetString(storedData);
            Assert.StartsWith("SEALED:", storedString);
        }

        [Fact]
        public async Task RetrieveBytesAsync_RetrievesAndUnsealsData()
        {
            // Arrange
            string key = "test-key-bytes";
            byte[] value = Encoding.UTF8.GetBytes("test-value-bytes");
            await _secureStorage.StoreAsync(key, value);
            
            // Act
            byte[] retrievedValue = await _secureStorage.RetrieveBytesAsync(key);
            
            // Assert
            _teeInterfaceMock.Verify(e => e.UnsealData(It.IsAny<byte[]>()), Times.Once);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task DeleteAsync_RemovesDataFromStorageAndCache()
        {
            // Arrange
            string key = "test-key-delete";
            string value = "test-value-delete";
            await _secureStorage.StoreAsync(key, value);
            
            // Act
            bool result = await _secureStorage.DeleteAsync(key);
            
            // Assert
            Assert.True(result);
            
            // Verify that the data was removed from the storage provider
            byte[] storedData = await _storageProvider.ReadAsync(key);
            Assert.Null(storedData);
            
            // Verify that the data was removed from the cache
            string retrievedValue = await _secureStorage.RetrieveAsync(key);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public async Task ListKeysAsync_ReturnsAllKeys()
        {
            // Arrange
            await _secureStorage.StoreAsync("key1", "value1");
            await _secureStorage.StoreAsync("key2", "value2");
            await _secureStorage.StoreAsync("key3", "value3");
            
            // Act
            var keys = await _secureStorage.ListKeysAsync();
            
            // Assert
            Assert.Equal(3, keys.Count);
            Assert.Contains("key1", keys);
            Assert.Contains("key2", keys);
            Assert.Contains("key3", keys);
        }

        [Fact]
        public async Task SecureStorage_HandlesLargeData()
        {
            // Arrange
            string key = "large-data";
            byte[] largeData = new byte[1024 * 1024]; // 1MB
            new Random().NextBytes(largeData);
            
            // Act
            await _secureStorage.StoreAsync(key, largeData);
            byte[] retrievedData = await _secureStorage.RetrieveBytesAsync(key);
            
            // Assert
            Assert.Equal(largeData.Length, retrievedData.Length);
            Assert.Equal(largeData, retrievedData);
        }

        [Fact]
        public async Task SecureStorage_HandlesMultipleOperations()
        {
            // Arrange & Act
            // Store multiple values
            await _secureStorage.StoreAsync("multi1", "value1");
            await _secureStorage.StoreAsync("multi2", "value2");
            
            // Retrieve values
            string value1 = await _secureStorage.RetrieveAsync("multi1");
            string value2 = await _secureStorage.RetrieveAsync("multi2");
            
            // Delete a value
            bool deleteResult = await _secureStorage.DeleteAsync("multi1");
            
            // List keys
            var keys = await _secureStorage.ListKeysAsync();
            
            // Assert
            Assert.Equal("value1", value1);
            Assert.Equal("value2", value2);
            Assert.True(deleteResult);
            Assert.DoesNotContain("multi1", keys);
            Assert.Contains("multi2", keys);
        }
    }
}
