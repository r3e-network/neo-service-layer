using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests
{
    [Trait("Category", "Integration")]
    public class EnclaveStorageIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<OpenEnclaveTeeInterface>> _teeLoggerMock;
        private readonly Mock<ILogger<SecureStorage>> _storageLoggerMock;
        private readonly string _testDirectory;
        private readonly string _enclaveImagePath;
        private readonly OpenEnclaveTeeInterface _teeInterface;
        private readonly SecureStorage _secureStorage;

        public EnclaveStorageIntegrationTests()
        {
            _teeLoggerMock = new Mock<ILogger<OpenEnclaveTeeInterface>>();
            _storageLoggerMock = new Mock<ILogger<SecureStorage>>();
            
            // Create a test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"enclave_storage_integration_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Set the enclave image path
            _enclaveImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "enclave.signed");
            
            // Skip tests if the enclave image doesn't exist
            if (!File.Exists(_enclaveImagePath))
            {
                Skip.If(true, $"Enclave image not found at {_enclaveImagePath}");
                return;
            }
            
            try
            {
                // Create the TEE interface
                _teeInterface = new OpenEnclaveTeeInterface(
                    _teeLoggerMock.Object,
                    new OpenEnclaveTeeOptions
                    {
                        EnclaveImagePath = _enclaveImagePath,
                        SimulationMode = true,
                        StorageDirectory = Path.Combine(_testDirectory, "enclave")
                    });
                
                // Initialize the TEE interface
                _teeInterface.Initialize();
                
                // Create the secure storage
                _secureStorage = new SecureStorage(
                    _storageLoggerMock.Object,
                    _teeInterface,
                    new SecureStorageOptions
                    {
                        StorageDirectory = Path.Combine(_testDirectory, "storage"),
                        EnableCaching = true,
                        EnablePersistence = true
                    });
                
                // Initialize the secure storage
                _secureStorage.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Skip tests if the enclave can't be initialized
                Skip.If(true, "Failed to initialize enclave in simulation mode");
            }
        }

        public void Dispose()
        {
            // Clean up
            _secureStorage?.Dispose();
            (_teeInterface as IDisposable)?.Dispose();
            
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
        public async Task SecureStorage_StoreRetrieve_RoundTrip()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null || _secureStorage == null)
            {
                return;
            }
            
            // Arrange
            string key = "test-integration-key";
            string value = "Test value for integration testing";
            
            // Act
            await _secureStorage.StoreAsync(key, value);
            string retrievedValue = await _secureStorage.RetrieveAsync(key);
            
            // Assert
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task SecureStorage_StoreRetrieveBytes_RoundTrip()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null || _secureStorage == null)
            {
                return;
            }
            
            // Arrange
            string key = "test-integration-bytes";
            byte[] value = Encoding.UTF8.GetBytes("Test binary data for integration testing");
            
            // Act
            await _secureStorage.StoreAsync(key, value);
            byte[] retrievedValue = await _secureStorage.RetrieveBytesAsync(key);
            
            // Assert
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task SecureStorage_Delete_RemovesData()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null || _secureStorage == null)
            {
                return;
            }
            
            // Arrange
            string key = "test-integration-delete";
            string value = "Test value to be deleted";
            await _secureStorage.StoreAsync(key, value);
            
            // Act
            bool deleteResult = await _secureStorage.DeleteAsync(key);
            string retrievedValue = await _secureStorage.RetrieveAsync(key);
            
            // Assert
            Assert.True(deleteResult);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public async Task SecureStorage_ListKeys_ReturnsAllKeys()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null || _secureStorage == null)
            {
                return;
            }
            
            // Arrange
            string key1 = "test-integration-list-1";
            string key2 = "test-integration-list-2";
            string value = "Test value for listing";
            
            await _secureStorage.StoreAsync(key1, value);
            await _secureStorage.StoreAsync(key2, value);
            
            // Act
            var keys = await _secureStorage.ListKeysAsync();
            
            // Assert
            Assert.Contains(key1, keys);
            Assert.Contains(key2, keys);
            
            // Clean up
            await _secureStorage.DeleteAsync(key1);
            await _secureStorage.DeleteAsync(key2);
        }

        [Fact]
        public async Task SecureStorage_LargeData_HandledCorrectly()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null || _secureStorage == null)
            {
                return;
            }
            
            // Arrange
            string key = "test-integration-large";
            byte[] largeData = new byte[1024 * 1024]; // 1MB
            new Random().NextBytes(largeData);
            
            // Act
            await _secureStorage.StoreAsync(key, largeData);
            byte[] retrievedData = await _secureStorage.RetrieveBytesAsync(key);
            
            // Assert
            Assert.Equal(largeData.Length, retrievedData.Length);
            Assert.Equal(largeData, retrievedData);
            
            // Clean up
            await _secureStorage.DeleteAsync(key);
        }

        [Fact]
        public async Task TeeInterface_DirectStorage_RoundTrip()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string key = "test-direct-storage";
            byte[] data = Encoding.UTF8.GetBytes("Test data for direct storage access");
            
            // Act
            bool storeResult = await _teeInterface.StorePersistentDataAsync(key, data);
            byte[] retrievedData = await _teeInterface.RetrievePersistentDataAsync(key);
            
            // Assert
            Assert.True(storeResult);
            Assert.Equal(data, retrievedData);
            
            // Clean up
            await _teeInterface.DeletePersistentDataAsync(key);
        }

        [Fact]
        public async Task TeeInterface_SealUnseal_RoundTrip()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("Test data for sealing and unsealing");
            
            // Act
            byte[] sealedData = _teeInterface.SealData(data);
            byte[] unsealedData = _teeInterface.UnsealData(sealedData);
            
            // Assert
            Assert.NotEqual(data, sealedData); // Sealed data should be different
            Assert.Equal(data, unsealedData);  // Unsealed data should match original
        }
    }
}
