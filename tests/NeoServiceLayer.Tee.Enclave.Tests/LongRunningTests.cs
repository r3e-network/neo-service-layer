using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "LongRunning")]
    [Collection("SimulationMode")]
    public class LongRunningTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public LongRunningTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<LongRunningTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task Storage_ContinuousOperations_ShouldRemainStable()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            int operationCount = 1000;
            string keyPrefix = "long-running-storage-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for long-running storage test");
            var random = new Random();
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Starting long-running storage test with {Count} operations", operationCount);
            
            try
            {
                // Act - Perform continuous operations
                for (int i = 0; i < operationCount; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    
                    // Store data
                    bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                    Assert.True(storeResult, $"Failed to store data for key {key}");
                    
                    // Retrieve data
                    byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                    Assert.Equal(data, retrievedData);
                    
                    // Delete data
                    bool deleteResult = await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                    Assert.True(deleteResult, $"Failed to delete data for key {key}");
                    
                    if (i > 0 && i % 100 == 0)
                    {
                        _logger.LogInformation("Completed {Count} operations in {Time} ms", i, stopwatch.ElapsedMilliseconds);
                    }
                }
                
                stopwatch.Stop();
                _logger.LogInformation("Completed {Count} operations in {Time} ms", operationCount, stopwatch.ElapsedMilliseconds);
                
                // Assert - Check that the system is still stable
                // Store one more item to verify
                string finalKey = $"{keyPrefix}-final";
                bool finalStoreResult = await _fixture.TeeInterface.StorePersistentDataAsync(finalKey, data);
                Assert.True(finalStoreResult, "Failed to store final data");
                
                byte[] finalRetrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(finalKey);
                Assert.Equal(data, finalRetrievedData);
                
                await _fixture.TeeInterface.RemovePersistentDataAsync(finalKey);
                
                _logger.LogInformation("System remained stable after {Count} operations", operationCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in long-running storage test");
                throw;
            }
        }

        [Fact]
        public async Task JavaScript_ContinuousExecution_ShouldRemainStable()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            int executionCount = 100;
            string userId = "long-running-js-user-" + Guid.NewGuid().ToString();
            string functionId = "long-running-js-function-" + Guid.NewGuid().ToString();
            
            // JavaScript code that performs a computation
            string jsCode = @"
                function main(input) {
                    // Perform a computation
                    let result = 0;
                    for (let i = 0; i < input.iterations; i++) {
                        result += Math.sqrt(i);
                    }
                    
                    // Store the result
                    const storageKey = 'result-' + input.execution_id;
                    STORAGE.set(storageKey, result.toString());
                    
                    return { 
                        success: true, 
                        result: result,
                        execution_id: input.execution_id
                    };
                }
            ";
            
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Starting long-running JavaScript test with {Count} executions", executionCount);
            
            try
            {
                // Act - Perform continuous executions
                for (int i = 0; i < executionCount; i++)
                {
                    // Create input with different iteration counts
                    string input = JsonSerializer.Serialize(new { 
                        iterations = 10000 + (i * 1000),
                        execution_id = i
                    });
                    
                    // Execute JavaScript
                    string result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, input, "{}", functionId, userId);
                    
                    // Parse result
                    var resultObj = JsonDocument.Parse(result).RootElement;
                    bool success = resultObj.GetProperty("success").GetBoolean();
                    
                    // Assert
                    Assert.True(success, $"Execution {i} failed");
                    
                    if (i > 0 && i % 10 == 0)
                    {
                        _logger.LogInformation("Completed {Count} executions in {Time} ms", i, stopwatch.ElapsedMilliseconds);
                    }
                }
                
                stopwatch.Stop();
                _logger.LogInformation("Completed {Count} executions in {Time} ms", executionCount, stopwatch.ElapsedMilliseconds);
                
                // Assert - Check that the system is still stable
                // Execute one more time to verify
                string finalInput = JsonSerializer.Serialize(new { 
                    iterations = 1000,
                    execution_id = "final"
                });
                
                string finalResult = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, finalInput, "{}", functionId, userId);
                
                var finalResultObj = JsonDocument.Parse(finalResult).RootElement;
                bool finalSuccess = finalResultObj.GetProperty("success").GetBoolean();
                
                Assert.True(finalSuccess, "Final execution failed");
                
                _logger.LogInformation("System remained stable after {Count} executions", executionCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in long-running JavaScript test");
                throw;
            }
        }

        [Fact]
        public async Task Transactions_LongRunning_ShouldRemainStable()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            int transactionCount = 100;
            int operationsPerTransaction = 100;
            string keyPrefix = "long-running-tx-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for long-running transaction test");
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Starting long-running transaction test with {TxCount} transactions, {OpCount} operations each", 
                transactionCount, operationsPerTransaction);
            
            try
            {
                // Act - Perform continuous transactions
                for (int t = 0; t < transactionCount; t++)
                {
                    // Begin transaction
                    ulong txId = await _fixture.TeeInterface.BeginTransactionAsync();
                    
                    // Perform operations in transaction
                    for (int i = 0; i < operationsPerTransaction; i++)
                    {
                        string key = $"{keyPrefix}-tx{t}-op{i}";
                        bool result = await _fixture.TeeInterface.StoreInTransactionAsync(txId, key, data);
                        Assert.True(result, $"Failed to store data for key {key} in transaction {t}");
                    }
                    
                    // Commit transaction
                    bool commitResult = await _fixture.TeeInterface.CommitTransactionAsync(txId);
                    Assert.True(commitResult, $"Failed to commit transaction {t}");
                    
                    // Verify random operations
                    var random = new Random();
                    for (int i = 0; i < 10; i++)
                    {
                        int opIndex = random.Next(operationsPerTransaction);
                        string key = $"{keyPrefix}-tx{t}-op{opIndex}";
                        byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                        Assert.Equal(data, retrievedData);
                    }
                    
                    // Clean up
                    for (int i = 0; i < operationsPerTransaction; i++)
                    {
                        string key = $"{keyPrefix}-tx{t}-op{i}";
                        await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                    }
                    
                    if (t > 0 && t % 10 == 0)
                    {
                        _logger.LogInformation("Completed {Count} transactions in {Time} ms", t, stopwatch.ElapsedMilliseconds);
                    }
                }
                
                stopwatch.Stop();
                _logger.LogInformation("Completed {Count} transactions in {Time} ms", transactionCount, stopwatch.ElapsedMilliseconds);
                
                // Assert - Check that the system is still stable
                // Perform one more transaction to verify
                ulong finalTxId = await _fixture.TeeInterface.BeginTransactionAsync();
                
                string finalKey = $"{keyPrefix}-final";
                bool finalResult = await _fixture.TeeInterface.StoreInTransactionAsync(finalTxId, finalKey, data);
                Assert.True(finalResult, "Failed to store data in final transaction");
                
                bool finalCommitResult = await _fixture.TeeInterface.CommitTransactionAsync(finalTxId);
                Assert.True(finalCommitResult, "Failed to commit final transaction");
                
                byte[] finalRetrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(finalKey);
                Assert.Equal(data, finalRetrievedData);
                
                await _fixture.TeeInterface.RemovePersistentDataAsync(finalKey);
                
                _logger.LogInformation("System remained stable after {Count} transactions", transactionCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in long-running transaction test");
                throw;
            }
        }
    }
}
