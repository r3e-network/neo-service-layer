using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Storage")]
    public class EnclaveStoragePersistenceTests : IClassFixture<SimulationModeFixture>
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public EnclaveStoragePersistenceTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EnclaveStoragePersistenceTests>();
        }

        [Fact]
        public async Task EnclaveInterface_ShouldPersistUserSecrets()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping user secrets persistence test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue = "test-secret-value";
            
            // Act - Store secret
            bool storeResult = await _fixture.TeeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
            
            // Act - Retrieve secret
            string retrievedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userId, secretName);
            
            // Assert
            Assert.True(storeResult, "Storing secret should succeed");
            Assert.Equal(secretValue, retrievedSecret);
            
            _logger.LogInformation("Successfully stored and retrieved user secret");
            
            // Now restart the enclave to test persistence
            _logger.LogInformation("Restarting enclave to test persistence...");
            
            // Create a new enclave interface
            var newFixture = new SimulationModeFixture();
            
            // Act - Retrieve secret from new enclave
            string retrievedSecretAfterRestart = await newFixture.TeeInterface.GetUserSecretAsync(userId, secretName);
            
            // Assert
            Assert.Equal(secretValue, retrievedSecretAfterRestart);
            
            _logger.LogInformation("Successfully retrieved user secret after enclave restart");
            
            // Clean up
            bool deleteResult = await newFixture.TeeInterface.DeleteUserSecretAsync(userId, secretName);
            Assert.True(deleteResult, "Deleting secret should succeed");
        }

        [Fact]
        public async Task EnclaveInterface_ShouldPersistDataAcrossRestarts()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping data persistence test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "test-persistent-data-" + Guid.NewGuid().ToString();
            string value = "This is some test data that should persist across enclave restarts";
            byte[] data = Encoding.UTF8.GetBytes(value);
            
            // Act - Store data
            bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
            
            // Act - Retrieve data
            byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
            
            // Assert
            Assert.True(storeResult, "Storing data should succeed");
            Assert.NotNull(retrievedData);
            Assert.Equal(data, retrievedData);
            
            _logger.LogInformation("Successfully stored and retrieved persistent data");
            
            // Now restart the enclave to test persistence
            _logger.LogInformation("Restarting enclave to test persistence...");
            
            // Create a new enclave interface
            var newFixture = new SimulationModeFixture();
            
            // Act - Retrieve data from new enclave
            byte[] retrievedDataAfterRestart = await newFixture.TeeInterface.RetrievePersistentDataAsync(key);
            
            // Assert
            Assert.NotNull(retrievedDataAfterRestart);
            Assert.Equal(data, retrievedDataAfterRestart);
            
            _logger.LogInformation("Successfully retrieved persistent data after enclave restart");
            
            // Clean up
            bool deleteResult = await newFixture.TeeInterface.DeletePersistentDataAsync(key);
            Assert.True(deleteResult, "Deleting data should succeed");
        }

        [Fact]
        public async Task EnclaveInterface_ShouldHandleLargeData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping large data test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "test-large-data-" + Guid.NewGuid().ToString();
            byte[] data = new byte[5 * 1024 * 1024]; // 5 MB
            new Random().NextBytes(data);
            
            // Act - Store data
            bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
            
            // Act - Retrieve data
            byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
            
            // Assert
            Assert.True(storeResult, "Storing large data should succeed");
            Assert.NotNull(retrievedData);
            Assert.Equal(data.Length, retrievedData.Length);
            Assert.Equal(data, retrievedData);
            
            _logger.LogInformation("Successfully stored and retrieved large data ({Size} bytes)", data.Length);
            
            // Clean up
            bool deleteResult = await _fixture.TeeInterface.DeletePersistentDataAsync(key);
            Assert.True(deleteResult, "Deleting large data should succeed");
        }

        [Fact]
        public async Task EnclaveInterface_ShouldHandleMultipleDataItems()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping multiple data items test because we're not using the real SDK");
                return;
            }

            // Arrange
            int itemCount = 100;
            string keyPrefix = "test-multi-" + Guid.NewGuid().ToString();
            
            // Act - Store multiple data items
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] data = Encoding.UTF8.GetBytes($"Data item {i}");
                
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                Assert.True(storeResult, $"Storing data item {i} should succeed");
            }
            
            // Act - Retrieve multiple data items
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] expectedData = Encoding.UTF8.GetBytes($"Data item {i}");
                
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                
                Assert.NotNull(retrievedData);
                Assert.Equal(expectedData, retrievedData);
            }
            
            _logger.LogInformation("Successfully stored and retrieved {Count} data items", itemCount);
            
            // Clean up
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                bool deleteResult = await _fixture.TeeInterface.DeletePersistentDataAsync(key);
                Assert.True(deleteResult, $"Deleting data item {i} should succeed");
            }
        }
    }
}
