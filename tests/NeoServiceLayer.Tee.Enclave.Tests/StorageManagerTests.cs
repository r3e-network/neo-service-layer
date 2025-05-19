using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "OpenEnclave")]
    [Collection("SimulationMode")]
    public class StorageManagerTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public StorageManagerTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<StorageManagerTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task StorageManager_ShouldStoreAndRetrieveData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "test-key-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("This is test data for storage");
            
            // Act - Store data
            bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
            
            // Act - Retrieve data
            byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
            
            // Act - Check if key exists
            bool keyExists = await _fixture.TeeInterface.PersistentDataExistsAsync(key);
            
            // Act - Delete data
            bool deleteResult = await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            
            // Act - Check if key exists after deletion
            bool keyExistsAfterDeletion = await _fixture.TeeInterface.PersistentDataExistsAsync(key);
            
            // Assert
            Assert.True(storeResult, "Storing data should succeed");
            Assert.Equal(data, retrievedData);
            Assert.True(keyExists, "Key should exist after storing data");
            Assert.True(deleteResult, "Deleting data should succeed");
            Assert.False(keyExistsAfterDeletion, "Key should not exist after deletion");
            
            _logger.LogInformation("Storage test passed for key {Key}", key);
        }

        [Fact]
        public async Task StorageManager_ShouldHandleTransactions()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key1 = "test-key1-" + Guid.NewGuid().ToString();
            string key2 = "test-key2-" + Guid.NewGuid().ToString();
            byte[] data1 = Encoding.UTF8.GetBytes("This is test data 1");
            byte[] data2 = Encoding.UTF8.GetBytes("This is test data 2");
            
            // Act - Begin transaction
            ulong transactionId = await _fixture.TeeInterface.BeginTransactionAsync();
            
            // Act - Store data in transaction
            bool storeResult1 = await _fixture.TeeInterface.StoreInTransactionAsync(transactionId, key1, data1);
            bool storeResult2 = await _fixture.TeeInterface.StoreInTransactionAsync(transactionId, key2, data2);
            
            // Act - Check if keys exist before commit
            bool key1ExistsBeforeCommit = await _fixture.TeeInterface.PersistentDataExistsAsync(key1);
            bool key2ExistsBeforeCommit = await _fixture.TeeInterface.PersistentDataExistsAsync(key2);
            
            // Act - Commit transaction
            bool commitResult = await _fixture.TeeInterface.CommitTransactionAsync(transactionId);
            
            // Act - Check if keys exist after commit
            bool key1ExistsAfterCommit = await _fixture.TeeInterface.PersistentDataExistsAsync(key1);
            bool key2ExistsAfterCommit = await _fixture.TeeInterface.PersistentDataExistsAsync(key2);
            
            // Act - Retrieve data
            byte[] retrievedData1 = await _fixture.TeeInterface.RetrievePersistentDataAsync(key1);
            byte[] retrievedData2 = await _fixture.TeeInterface.RetrievePersistentDataAsync(key2);
            
            // Act - Clean up
            await _fixture.TeeInterface.RemovePersistentDataAsync(key1);
            await _fixture.TeeInterface.RemovePersistentDataAsync(key2);
            
            // Assert
            Assert.True(transactionId > 0, "Transaction ID should be greater than zero");
            Assert.True(storeResult1, "Storing data 1 should succeed");
            Assert.True(storeResult2, "Storing data 2 should succeed");
            Assert.False(key1ExistsBeforeCommit, "Key 1 should not exist before commit");
            Assert.False(key2ExistsBeforeCommit, "Key 2 should not exist before commit");
            Assert.True(commitResult, "Committing transaction should succeed");
            Assert.True(key1ExistsAfterCommit, "Key 1 should exist after commit");
            Assert.True(key2ExistsAfterCommit, "Key 2 should exist after commit");
            Assert.Equal(data1, retrievedData1);
            Assert.Equal(data2, retrievedData2);
            
            _logger.LogInformation("Transaction test passed for transaction {TransactionId}", transactionId);
        }

        [Fact]
        public async Task StorageManager_ShouldHandleRollback()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "test-key-rollback-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("This is test data for rollback");
            
            // Act - Begin transaction
            ulong transactionId = await _fixture.TeeInterface.BeginTransactionAsync();
            
            // Act - Store data in transaction
            bool storeResult = await _fixture.TeeInterface.StoreInTransactionAsync(transactionId, key, data);
            
            // Act - Rollback transaction
            bool rollbackResult = await _fixture.TeeInterface.RollbackTransactionAsync(transactionId);
            
            // Act - Check if key exists after rollback
            bool keyExistsAfterRollback = await _fixture.TeeInterface.PersistentDataExistsAsync(key);
            
            // Assert
            Assert.True(transactionId > 0, "Transaction ID should be greater than zero");
            Assert.True(storeResult, "Storing data should succeed");
            Assert.True(rollbackResult, "Rolling back transaction should succeed");
            Assert.False(keyExistsAfterRollback, "Key should not exist after rollback");
            
            _logger.LogInformation("Rollback test passed for transaction {TransactionId}", transactionId);
        }

        [Fact]
        public async Task StorageManager_ShouldHandleMultipleOperations()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string keyPrefix = "test-key-multi-" + Guid.NewGuid().ToString();
            int numKeys = 10;
            
            // Act - Store multiple keys
            for (int i = 0; i < numKeys; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] data = Encoding.UTF8.GetBytes($"This is test data {i}");
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                Assert.True(storeResult, $"Storing data for key {key} should succeed");
            }
            
            // Act - List keys
            var keys = await _fixture.TeeInterface.ListPersistentDataKeysAsync();
            
            // Act - Retrieve and verify all keys
            for (int i = 0; i < numKeys; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] expectedData = Encoding.UTF8.GetBytes($"This is test data {i}");
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                Assert.Equal(expectedData, retrievedData);
            }
            
            // Act - Delete all keys
            for (int i = 0; i < numKeys; i++)
            {
                string key = $"{keyPrefix}-{i}";
                bool deleteResult = await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                Assert.True(deleteResult, $"Deleting data for key {key} should succeed");
            }
            
            // Assert
            Assert.True(keys.Count >= numKeys, $"There should be at least {numKeys} keys");
            
            _logger.LogInformation("Multiple operations test passed for {NumKeys} keys", numKeys);
        }
    }
}
