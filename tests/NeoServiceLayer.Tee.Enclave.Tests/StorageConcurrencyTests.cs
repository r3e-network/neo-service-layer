using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Concurrency")]
    public class StorageConcurrencyTests : IClassFixture<SimulationModeFixture>
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public StorageConcurrencyTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<StorageConcurrencyTests>();
        }

        [Fact]
        public async Task ConcurrentStoreOperations_ShouldSucceed()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping concurrency test because we're not using the real SDK");
                return;
            }

            // Arrange
            int concurrentOperations = 20;
            string keyPrefix = "concurrent-store-" + Guid.NewGuid().ToString();
            var tasks = new List<Task<bool>>();
            
            // Act - Start multiple concurrent store operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] data = Encoding.UTF8.GetBytes($"Concurrent data {i}");
                
                tasks.Add(_fixture.TeeInterface.StorePersistentDataAsync(key, data));
            }
            
            // Wait for all tasks to complete
            bool[] results = await Task.WhenAll(tasks);
            
            // Clean up
            for (int i = 0; i < concurrentOperations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            }
            
            // Assert
            Assert.True(results.All(r => r), "All concurrent store operations should succeed");
            _logger.LogInformation("Successfully completed {Count} concurrent store operations", concurrentOperations);
        }

        [Fact]
        public async Task ConcurrentRetrieveOperations_ShouldSucceed()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping concurrency test because we're not using the real SDK");
                return;
            }

            // Arrange
            int concurrentOperations = 20;
            string keyPrefix = "concurrent-retrieve-" + Guid.NewGuid().ToString();
            var expectedData = new Dictionary<string, byte[]>();
            
            // Store data first
            for (int i = 0; i < concurrentOperations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] data = Encoding.UTF8.GetBytes($"Concurrent data {i}");
                expectedData[key] = data;
                
                await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
            }
            
            // Act - Start multiple concurrent retrieve operations
            var tasks = new List<Task<byte[]>>();
            for (int i = 0; i < concurrentOperations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                tasks.Add(_fixture.TeeInterface.RetrievePersistentDataAsync(key));
            }
            
            // Wait for all tasks to complete
            byte[][] results = await Task.WhenAll(tasks);
            
            // Clean up
            for (int i = 0; i < concurrentOperations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            }
            
            // Assert
            for (int i = 0; i < concurrentOperations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                Assert.Equal(expectedData[key], results[i]);
            }
            
            _logger.LogInformation("Successfully completed {Count} concurrent retrieve operations", concurrentOperations);
        }

        [Fact]
        public async Task ConcurrentMixedOperations_ShouldSucceed()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping concurrency test because we're not using the real SDK");
                return;
            }

            // Arrange
            int concurrentOperations = 30;
            string keyPrefix = "concurrent-mixed-" + Guid.NewGuid().ToString();
            var tasks = new List<Task>();
            var keys = new List<string>();
            
            // Create some initial data
            for (int i = 0; i < 10; i++)
            {
                string key = $"{keyPrefix}-initial-{i}";
                keys.Add(key);
                byte[] data = Encoding.UTF8.GetBytes($"Initial data {i}");
                await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
            }
            
            // Act - Start mixed concurrent operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                int operationType = i % 3; // 0 = store, 1 = retrieve, 2 = delete
                string key;
                
                if (operationType == 0)
                {
                    // Store operation
                    key = $"{keyPrefix}-new-{i}";
                    keys.Add(key);
                    byte[] data = Encoding.UTF8.GetBytes($"New data {i}");
                    tasks.Add(_fixture.TeeInterface.StorePersistentDataAsync(key, data));
                }
                else if (operationType == 1 && keys.Count > 0)
                {
                    // Retrieve operation
                    key = keys[i % keys.Count];
                    tasks.Add(_fixture.TeeInterface.RetrievePersistentDataAsync(key));
                }
                else if (keys.Count > 0)
                {
                    // Delete operation
                    key = keys[i % keys.Count];
                    tasks.Add(_fixture.TeeInterface.RemovePersistentDataAsync(key));
                }
            }
            
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
            
            // Clean up any remaining keys
            foreach (var key in keys)
            {
                try
                {
                    await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            
            _logger.LogInformation("Successfully completed {Count} concurrent mixed operations", concurrentOperations);
        }

        [Fact]
        public async Task ConcurrentTransactions_ShouldSucceed()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping concurrency test because we're not using the real SDK");
                return;
            }

            // Arrange
            int concurrentTransactions = 5;
            int operationsPerTransaction = 10;
            string keyPrefix = "concurrent-tx-" + Guid.NewGuid().ToString();
            var tasks = new List<Task<bool>>();
            var allKeys = new List<string>();
            
            // Act - Start multiple concurrent transactions
            for (int t = 0; t < concurrentTransactions; t++)
            {
                tasks.Add(RunTransaction(t, keyPrefix, operationsPerTransaction, allKeys));
            }
            
            // Wait for all transactions to complete
            bool[] results = await Task.WhenAll(tasks);
            
            // Clean up
            foreach (var key in allKeys)
            {
                try
                {
                    await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            
            // Assert
            Assert.True(results.All(r => r), "All concurrent transactions should succeed");
            _logger.LogInformation("Successfully completed {Count} concurrent transactions", concurrentTransactions);
        }

        private async Task<bool> RunTransaction(int transactionId, string keyPrefix, int operationsCount, List<string> allKeys)
        {
            try
            {
                // Begin transaction
                ulong txId = await _fixture.TeeInterface.BeginTransactionAsync();
                
                // Perform operations in transaction
                var txKeys = new List<string>();
                for (int i = 0; i < operationsCount; i++)
                {
                    string key = $"{keyPrefix}-tx{transactionId}-op{i}";
                    txKeys.Add(key);
                    byte[] data = Encoding.UTF8.GetBytes($"Transaction {transactionId} data {i}");
                    
                    bool result = await _fixture.TeeInterface.StoreInTransactionAsync(txId, key, data);
                    if (!result)
                    {
                        _logger.LogError("Failed to store data in transaction {TxId} for key {Key}", txId, key);
                        await _fixture.TeeInterface.RollbackTransactionAsync(txId);
                        return false;
                    }
                }
                
                // Commit transaction
                bool commitResult = await _fixture.TeeInterface.CommitTransactionAsync(txId);
                if (commitResult)
                {
                    lock (allKeys)
                    {
                        allKeys.AddRange(txKeys);
                    }
                }
                
                return commitResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction {TxId}", transactionId);
                return false;
            }
        }
    }
}
