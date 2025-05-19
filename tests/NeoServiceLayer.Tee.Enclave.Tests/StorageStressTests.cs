using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Stress")]
    public class StorageStressTests : IClassFixture<SimulationModeFixture>
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public StorageStressTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<StorageStressTests>();
        }

        [Fact]
        public async Task StressTest_ManySmallItems()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping stress test because we're not using the real SDK");
                return;
            }

            // Arrange
            int itemCount = 500;
            string keyPrefix = "stress-small-" + Guid.NewGuid().ToString();
            byte[] smallData = Encoding.UTF8.GetBytes("This is a small data item for stress testing");
            
            _logger.LogInformation("Starting stress test with {Count} small items", itemCount);
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Store many items
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                bool result = await _fixture.TeeInterface.StorePersistentDataAsync(key, smallData);
                Assert.True(result, $"Failed to store item {i}");
                
                if (i > 0 && i % 100 == 0)
                {
                    _logger.LogInformation("Stored {Count} items...", i);
                }
            }
            
            // Act - Verify items
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                Assert.Equal(smallData, retrievedData);
                
                if (i > 0 && i % 100 == 0)
                {
                    _logger.LogInformation("Verified {Count} items...", i);
                }
            }
            
            // Act - Delete items
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                bool result = await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                Assert.True(result, $"Failed to delete item {i}");
                
                if (i > 0 && i % 100 == 0)
                {
                    _logger.LogInformation("Deleted {Count} items...", i);
                }
            }
            
            stopwatch.Stop();
            _logger.LogInformation("Stress test completed in {Time} ms", stopwatch.ElapsedMilliseconds);
        }

        [Fact]
        public async Task StressTest_LargeDataChunks()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping stress test because we're not using the real SDK");
                return;
            }

            // Arrange
            int itemCount = 20;
            int chunkSize = 1 * 1024 * 1024; // 1 MB
            string keyPrefix = "stress-large-" + Guid.NewGuid().ToString();
            
            _logger.LogInformation("Starting stress test with {Count} large items ({Size} MB each)", 
                itemCount, chunkSize / (1024 * 1024));
            var stopwatch = Stopwatch.StartNew();
            
            // Generate random data chunks
            var dataChunks = new Dictionary<string, byte[]>();
            for (int i = 0; i < itemCount; i++)
            {
                string key = $"{keyPrefix}-{i}";
                byte[] data = new byte[chunkSize];
                new Random().NextBytes(data);
                dataChunks[key] = data;
            }
            
            // Act - Store large chunks
            foreach (var kvp in dataChunks)
            {
                _logger.LogInformation("Storing large chunk for key {Key}...", kvp.Key);
                bool result = await _fixture.TeeInterface.StorePersistentDataAsync(kvp.Key, kvp.Value);
                Assert.True(result, $"Failed to store large chunk for key {kvp.Key}");
            }
            
            // Act - Verify chunks
            foreach (var kvp in dataChunks)
            {
                _logger.LogInformation("Verifying large chunk for key {Key}...", kvp.Key);
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(kvp.Key);
                Assert.Equal(kvp.Value.Length, retrievedData.Length);
                Assert.Equal(kvp.Value, retrievedData);
            }
            
            // Act - Delete chunks
            foreach (var kvp in dataChunks)
            {
                _logger.LogInformation("Deleting large chunk for key {Key}...", kvp.Key);
                bool result = await _fixture.TeeInterface.RemovePersistentDataAsync(kvp.Key);
                Assert.True(result, $"Failed to delete large chunk for key {kvp.Key}");
            }
            
            stopwatch.Stop();
            _logger.LogInformation("Stress test completed in {Time} ms", stopwatch.ElapsedMilliseconds);
        }

        [Fact]
        public async Task StressTest_MixedOperations()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping stress test because we're not using the real SDK");
                return;
            }

            // Arrange
            int operationCount = 1000;
            string keyPrefix = "stress-mixed-" + Guid.NewGuid().ToString();
            var random = new Random();
            var keys = new HashSet<string>();
            
            _logger.LogInformation("Starting mixed operations stress test with {Count} operations", operationCount);
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Perform mixed operations
            for (int i = 0; i < operationCount; i++)
            {
                int operationType = random.Next(3); // 0 = store, 1 = retrieve, 2 = delete
                
                if (operationType == 0 || keys.Count < 10)
                {
                    // Store operation
                    string key = $"{keyPrefix}-{Guid.NewGuid()}";
                    int dataSize = random.Next(1, 10) * 1024; // 1-10 KB
                    byte[] data = new byte[dataSize];
                    random.NextBytes(data);
                    
                    bool result = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                    Assert.True(result, $"Failed to store data for key {key}");
                    keys.Add(key);
                }
                else if (operationType == 1 && keys.Count > 0)
                {
                    // Retrieve operation
                    string key = keys.ElementAt(random.Next(keys.Count));
                    byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                    Assert.NotNull(retrievedData);
                }
                else if (keys.Count > 0)
                {
                    // Delete operation
                    string key = keys.ElementAt(random.Next(keys.Count));
                    bool result = await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                    Assert.True(result, $"Failed to delete data for key {key}");
                    keys.Remove(key);
                }
                
                if (i > 0 && i % 100 == 0)
                {
                    _logger.LogInformation("Completed {Count} operations...", i);
                }
            }
            
            // Clean up any remaining keys
            foreach (var key in keys)
            {
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            }
            
            stopwatch.Stop();
            _logger.LogInformation("Mixed operations stress test completed in {Time} ms", stopwatch.ElapsedMilliseconds);
        }

        [Fact]
        public async Task StressTest_ConcurrentTransactions()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping stress test because we're not using the real SDK");
                return;
            }

            // Arrange
            int transactionCount = 20;
            int operationsPerTransaction = 50;
            string keyPrefix = "stress-tx-" + Guid.NewGuid().ToString();
            
            _logger.LogInformation("Starting concurrent transactions stress test with {TxCount} transactions, {OpCount} operations each", 
                transactionCount, operationsPerTransaction);
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Start multiple concurrent transactions
            var tasks = new List<Task<bool>>();
            var allKeys = new List<string>();
            
            for (int t = 0; t < transactionCount; t++)
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
            
            stopwatch.Stop();
            
            // Assert
            Assert.True(results.All(r => r), "All concurrent transactions should succeed");
            _logger.LogInformation("Concurrent transactions stress test completed in {Time} ms", stopwatch.ElapsedMilliseconds);
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
