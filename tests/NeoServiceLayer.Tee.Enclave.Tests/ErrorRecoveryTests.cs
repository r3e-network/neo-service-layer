using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "ErrorRecovery")]
    [Collection("SimulationMode")]
    public class ErrorRecoveryTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public ErrorRecoveryTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<ErrorRecoveryTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task Storage_RecoverFromCorruptedData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "recovery-test-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for recovery");
            
            try
            {
                // Act - Store data
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                Assert.True(storeResult, "Failed to store data");
                
                // Act - Corrupt the data by writing invalid data to the same key
                // In a real scenario, we would corrupt the file directly, but for this test
                // we'll simulate corruption by writing invalid data
                await _fixture.TeeInterface.CorruptStorageDataAsync(key);
                
                // Act - Try to retrieve the corrupted data
                // This should fail with an error, but the system should recover
                try
                {
                    byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                    _logger.LogWarning("Retrieved corrupted data without error");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Expected error retrieving corrupted data: {Error}", ex.Message);
                }
                
                // Act - Store new data with the same key
                // This should succeed, even though the previous data was corrupted
                bool storeResult2 = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                Assert.True(storeResult2, "Failed to store data after corruption");
                
                // Act - Retrieve the new data
                byte[] retrievedData2 = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                
                // Assert
                Assert.Equal(data, retrievedData2);
                
                // Clean up
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                
                _logger.LogInformation("Successfully recovered from corrupted data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recovery test");
                throw;
            }
        }

        [Fact]
        public async Task Storage_RecoverFromTransactionFailure()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string keyPrefix = "tx-recovery-test-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for transaction recovery");
            
            try
            {
                // Act - Begin transaction
                ulong txId = await _fixture.TeeInterface.BeginTransactionAsync();
                
                // Act - Store data in transaction
                for (int i = 0; i < 10; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    bool result = await _fixture.TeeInterface.StoreInTransactionAsync(txId, key, data);
                    Assert.True(result, $"Failed to store item {i} in transaction");
                }
                
                // Act - Simulate transaction failure by forcing a rollback
                bool rollbackResult = await _fixture.TeeInterface.RollbackTransactionAsync(txId);
                Assert.True(rollbackResult, "Failed to rollback transaction");
                
                // Act - Verify that none of the data was stored
                for (int i = 0; i < 10; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    try
                    {
                        byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                        Assert.Null(retrievedData);
                    }
                    catch
                    {
                        // Expected - data should not exist
                    }
                }
                
                // Act - Begin a new transaction
                ulong txId2 = await _fixture.TeeInterface.BeginTransactionAsync();
                
                // Act - Store data in new transaction
                for (int i = 0; i < 10; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    bool result = await _fixture.TeeInterface.StoreInTransactionAsync(txId2, key, data);
                    Assert.True(result, $"Failed to store item {i} in new transaction");
                }
                
                // Act - Commit the new transaction
                bool commitResult = await _fixture.TeeInterface.CommitTransactionAsync(txId2);
                Assert.True(commitResult, "Failed to commit new transaction");
                
                // Act - Verify that all data was stored
                for (int i = 0; i < 10; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                    Assert.Equal(data, retrievedData);
                }
                
                // Clean up
                for (int i = 0; i < 10; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                }
                
                _logger.LogInformation("Successfully recovered from transaction failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction recovery test");
                throw;
            }
        }

        [Fact]
        public async Task JavaScript_RecoverFromExecutionFailure()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "recovery-test-user-" + Guid.NewGuid().ToString();
            string functionId = "recovery-test-function-" + Guid.NewGuid().ToString();
            
            // JavaScript code that will fail
            string failingCode = @"
                function main(input) {
                    // This will throw an error
                    throw new Error('Intentional error for testing');
                    return { success: true };
                }
            ";
            
            // JavaScript code that will succeed
            string successCode = @"
                function main(input) {
                    return { success: true, input: input };
                }
            ";
            
            string input = JsonSerializer.Serialize(new { value = 42 });
            
            try
            {
                // Act - Execute failing JavaScript
                try
                {
                    string result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(failingCode, input, "{}", functionId, userId);
                    _logger.LogWarning("Executed failing JavaScript without error: {Result}", result);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Expected error executing failing JavaScript: {Error}", ex.Message);
                }
                
                // Act - Execute succeeding JavaScript with the same function ID
                string result2 = await _fixture.TeeInterface.ExecuteJavaScriptAsync(successCode, input, "{}", functionId, userId);
                
                // Parse result
                var resultObj = JsonDocument.Parse(result2).RootElement;
                bool success = resultObj.GetProperty("success").GetBoolean();
                
                // Assert
                Assert.True(success, "Succeeding JavaScript should execute successfully after failure");
                
                _logger.LogInformation("Successfully recovered from JavaScript execution failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JavaScript recovery test");
                throw;
            }
        }

        [Fact]
        public async Task EnclaveRestart_ShouldRecoverState()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "restart-recovery-test-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for restart recovery");
            
            try
            {
                // Act - Store data
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                Assert.True(storeResult, "Failed to store data");
                
                // Act - Simulate enclave restart
                await _fixture.TeeInterface.SimulateEnclaveRestartAsync();
                
                // Act - Retrieve data after restart
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                
                // Assert
                Assert.Equal(data, retrievedData);
                
                // Clean up
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                
                _logger.LogInformation("Successfully recovered state after enclave restart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enclave restart test");
                throw;
            }
        }
    }
}
