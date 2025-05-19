using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Storage")]
    public class EnclaveStorageIntegrationTests : IClassFixture<SimulationModeFixture>, IDisposable
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;
        private readonly string _testStorageDir;
        private readonly IPersistentStorageProvider _storageProvider;
        private readonly SecureStorage _secureStorage;

        public EnclaveStorageIntegrationTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EnclaveStorageIntegrationTests>();
            
            // Create a unique test directory
            _testStorageDir = Path.Combine(Path.GetTempPath(), $"enclave_storage_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testStorageDir);
            
            // Create storage provider
            _storageProvider = new OcclumFileStorageProvider(
                loggerFactory.CreateLogger<OcclumFileStorageProvider>(),
                new OcclumFileStorageOptions
                {
                    StorageDirectory = _testStorageDir,
                    EnableJournaling = true
                });
            
            // Initialize storage provider
            _storageProvider.InitializeAsync().GetAwaiter().GetResult();
            
            // Create secure storage
            _secureStorage = new SecureStorage(
                loggerFactory.CreateLogger<SecureStorage>(),
                _fixture.TeeInterface,
                new SecureStorageOptions
                {
                    StorageDirectory = _testStorageDir,
                    EnableCaching = true,
                    EnablePersistence = true
                },
                _storageProvider);
            
            // Initialize secure storage
            _secureStorage.InitializeAsync().GetAwaiter().GetResult();
            
            _logger.LogInformation("Test initialized with storage directory: {StorageDir}", _testStorageDir);
        }

        public void Dispose()
        {
            // Clean up
            _secureStorage.Dispose();
            _storageProvider.Dispose();
            
            // Delete test directory
            try
            {
                if (Directory.Exists(_testStorageDir))
                {
                    Directory.Delete(_testStorageDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete test directory: {StorageDir}", _testStorageDir);
            }
        }

        [Fact]
        public async Task SecureStorage_ShouldStoreAndRetrieveString()
        {
            // Arrange
            string key = "test-key-" + Guid.NewGuid().ToString();
            string value = "This is a test value with special characters: !@#$%^&*()_+";
            
            // Act - Store
            await _secureStorage.StoreAsync(key, value);
            
            // Act - Retrieve
            string retrievedValue = await _secureStorage.RetrieveAsync(key);
            
            // Assert
            Assert.Equal(value, retrievedValue);
            _logger.LogInformation("Successfully stored and retrieved string value for key: {Key}", key);
        }

        [Fact]
        public async Task SecureStorage_ShouldStoreAndRetrieveBytes()
        {
            // Arrange
            string key = "test-bytes-" + Guid.NewGuid().ToString();
            byte[] value = new byte[1024];
            new Random().NextBytes(value);
            
            // Act - Store
            await _secureStorage.StoreAsync(key, value);
            
            // Act - Retrieve
            byte[] retrievedValue = await _secureStorage.RetrieveBytesAsync(key);
            
            // Assert
            Assert.Equal(value, retrievedValue);
            _logger.LogInformation("Successfully stored and retrieved byte array for key: {Key}", key);
        }

        [Fact]
        public async Task SecureStorage_ShouldDeleteKey()
        {
            // Arrange
            string key = "test-delete-" + Guid.NewGuid().ToString();
            string value = "Value to be deleted";
            await _secureStorage.StoreAsync(key, value);
            
            // Act - Delete
            bool deleteResult = await _secureStorage.DeleteAsync(key);
            
            // Act - Try to retrieve
            string retrievedValue = await _secureStorage.RetrieveAsync(key);
            
            // Assert
            Assert.True(deleteResult);
            Assert.Null(retrievedValue);
            _logger.LogInformation("Successfully deleted key: {Key}", key);
        }

        [Fact]
        public async Task SecureStorage_ShouldListKeys()
        {
            // Arrange
            string keyPrefix = "test-list-" + Guid.NewGuid().ToString().Substring(0, 8);
            string key1 = $"{keyPrefix}-1";
            string key2 = $"{keyPrefix}-2";
            string key3 = $"{keyPrefix}-3";
            
            await _secureStorage.StoreAsync(key1, "Value 1");
            await _secureStorage.StoreAsync(key2, "Value 2");
            await _secureStorage.StoreAsync(key3, "Value 3");
            
            // Act
            var keys = await _secureStorage.ListKeysAsync();
            
            // Assert
            Assert.Contains(key1, keys);
            Assert.Contains(key2, keys);
            Assert.Contains(key3, keys);
            _logger.LogInformation("Successfully listed keys with prefix: {KeyPrefix}", keyPrefix);
        }

        [Fact]
        public async Task SecureStorage_ShouldHandleLargeData()
        {
            // Arrange
            string key = "test-large-" + Guid.NewGuid().ToString();
            byte[] largeData = new byte[5 * 1024 * 1024]; // 5 MB
            new Random().NextBytes(largeData);
            
            // Act - Store
            await _secureStorage.StoreAsync(key, largeData);
            
            // Act - Retrieve
            byte[] retrievedData = await _secureStorage.RetrieveBytesAsync(key);
            
            // Assert
            Assert.Equal(largeData.Length, retrievedData.Length);
            Assert.Equal(largeData, retrievedData);
            _logger.LogInformation("Successfully stored and retrieved large data ({Size} bytes) for key: {Key}", 
                largeData.Length, key);
        }

        [Fact]
        public async Task SecureStorage_ShouldPersistDataAcrossInstances()
        {
            // Arrange
            string key = "test-persist-" + Guid.NewGuid().ToString();
            string value = "This value should persist across storage instances";
            
            // Act - Store with current instance
            await _secureStorage.StoreAsync(key, value);
            
            // Create a new storage instance
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            
            var newStorageProvider = new OcclumFileStorageProvider(
                loggerFactory.CreateLogger<OcclumFileStorageProvider>(),
                new OcclumFileStorageOptions
                {
                    StorageDirectory = _testStorageDir,
                    EnableJournaling = true
                });
            
            await newStorageProvider.InitializeAsync();
            
            var newSecureStorage = new SecureStorage(
                loggerFactory.CreateLogger<SecureStorage>(),
                _fixture.TeeInterface,
                new SecureStorageOptions
                {
                    StorageDirectory = _testStorageDir,
                    EnableCaching = false, // Disable caching to ensure we read from disk
                    EnablePersistence = true
                },
                newStorageProvider);
            
            await newSecureStorage.InitializeAsync();
            
            // Act - Retrieve with new instance
            string retrievedValue = await newSecureStorage.RetrieveAsync(key);
            
            // Clean up
            newSecureStorage.Dispose();
            newStorageProvider.Dispose();
            
            // Assert
            Assert.Equal(value, retrievedValue);
            _logger.LogInformation("Successfully persisted data across storage instances for key: {Key}", key);
        }

        [Fact]
        public async Task SecureStorage_ShouldHandleEncryptedData()
        {
            // Arrange
            string key = "test-encrypted-" + Guid.NewGuid().ToString();
            string sensitiveData = "This is sensitive data that should be encrypted: SSN: 123-45-6789, CC: 4111-1111-1111-1111";
            
            // Act - Store
            await _secureStorage.StoreAsync(key, sensitiveData);
            
            // Act - Read raw data from storage provider to verify it's encrypted
            byte[] rawData = await _storageProvider.ReadAsync(key);
            string rawString = Encoding.UTF8.GetString(rawData);
            
            // Act - Retrieve through secure storage
            string decryptedData = await _secureStorage.RetrieveAsync(key);
            
            // Assert
            Assert.NotNull(rawData);
            Assert.NotEqual(sensitiveData, rawString); // Raw data should be encrypted
            Assert.Equal(sensitiveData, decryptedData); // Decrypted data should match original
            
            _logger.LogInformation("Successfully verified encryption for key: {Key}", key);
        }
    }
}
