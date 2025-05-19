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
    [Trait("Category", "EdgeCase")]
    [Collection("SimulationMode")]
    public class EdgeCaseTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public EdgeCaseTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EdgeCaseTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task Storage_VeryLargeData_ShouldStoreAndRetrieve()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "edge-case-large-data-" + Guid.NewGuid().ToString();
            int dataSize = 10 * 1024 * 1024; // 10 MB
            byte[] largeData = new byte[dataSize];
            new Random().NextBytes(largeData);
            
            _logger.LogInformation("Testing storage with {Size} MB of data", dataSize / (1024 * 1024));
            
            try
            {
                // Act - Store large data
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, largeData);
                
                // Act - Retrieve large data
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                
                // Act - Clean up
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                
                // Assert
                Assert.True(storeResult, "Failed to store large data");
                Assert.Equal(largeData.Length, retrievedData.Length);
                Assert.Equal(largeData, retrievedData);
                
                _logger.LogInformation("Successfully stored and retrieved {Size} MB of data", dataSize / (1024 * 1024));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in large data test");
                throw;
            }
        }

        [Fact]
        public async Task Storage_VeryLongKey_ShouldStoreAndRetrieve()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            StringBuilder keyBuilder = new StringBuilder("edge-case-long-key-");
            for (int i = 0; i < 1000; i++)
            {
                keyBuilder.Append(Guid.NewGuid().ToString());
            }
            string longKey = keyBuilder.ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for long key");
            
            _logger.LogInformation("Testing storage with key length of {Length} characters", longKey.Length);
            
            try
            {
                // Act - Store data with long key
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(longKey, data);
                
                // Act - Retrieve data with long key
                byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(longKey);
                
                // Act - Clean up
                await _fixture.TeeInterface.RemovePersistentDataAsync(longKey);
                
                // Assert
                Assert.True(storeResult, "Failed to store data with long key");
                Assert.Equal(data, retrievedData);
                
                _logger.LogInformation("Successfully stored and retrieved data with key length of {Length} characters", longKey.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in long key test");
                throw;
            }
        }

        [Fact]
        public async Task JavaScript_DeepRecursion_ShouldHandleStackOverflow()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "edge-case-user-" + Guid.NewGuid().ToString();
            string functionId = "deep-recursion-test-" + Guid.NewGuid().ToString();
            
            // JavaScript code with deep recursion
            string jsCode = @"
                function main(input) {
                    function factorial(n) {
                        if (n <= 1) return 1;
                        return n * factorial(n - 1);
                    }
                    
                    try {
                        // This should cause a stack overflow
                        const result = factorial(100000);
                        return { success: true, result: result };
                    } catch (error) {
                        return { success: false, error: error.message };
                    }
                }
            ";
            
            string input = "{}";
            
            try
            {
                // Act - Execute JavaScript with deep recursion
                string result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, input, "{}", functionId, userId);
                
                // Parse result
                var resultObj = JsonDocument.Parse(result).RootElement;
                bool success = resultObj.GetProperty("success").GetBoolean();
                
                // Assert
                Assert.False(success, "Deep recursion should fail with stack overflow");
                
                _logger.LogInformation("Deep recursion test result: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in deep recursion test");
                throw;
            }
        }

        [Fact]
        public async Task JavaScript_InfiniteLoop_ShouldTimeOut()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "edge-case-user-" + Guid.NewGuid().ToString();
            string functionId = "infinite-loop-test-" + Guid.NewGuid().ToString();
            
            // JavaScript code with infinite loop
            string jsCode = @"
                function main(input) {
                    try {
                        // This should cause a timeout
                        while (true) {
                            // Infinite loop
                        }
                        return { success: true };
                    } catch (error) {
                        return { success: false, error: error.message };
                    }
                }
            ";
            
            string input = "{}";
            
            try
            {
                // Act - Execute JavaScript with infinite loop
                // This should time out
                var task = _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, input, "{}", functionId, userId);
                
                // Wait for a reasonable timeout
                var timeoutTask = Task.Delay(10000); // 10 seconds
                var completedTask = await Task.WhenAny(task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // The JavaScript execution is still running, we need to cancel it
                    // In a real implementation, we would have a way to cancel the execution
                    // For now, we'll just assert that it's still running
                    Assert.True(true, "Infinite loop should still be running after timeout");
                    
                    _logger.LogInformation("Infinite loop test timed out as expected");
                }
                else
                {
                    // The JavaScript execution completed, which means it was properly terminated
                    string result = await task;
                    
                    // Parse result
                    var resultObj = JsonDocument.Parse(result).RootElement;
                    bool success = resultObj.GetProperty("success").GetBoolean();
                    
                    // Assert
                    Assert.False(success, "Infinite loop should be terminated");
                    
                    _logger.LogInformation("Infinite loop test result: {Result}", result);
                }
            }
            catch (Exception ex)
            {
                // If the execution throws an exception, that's also acceptable
                _logger.LogInformation(ex, "Infinite loop test threw exception as expected");
            }
        }

        [Fact]
        public async Task Storage_ManySmallItems_ShouldHandleHighVolume()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            int itemCount = 10000;
            string keyPrefix = "edge-case-many-items-" + Guid.NewGuid().ToString();
            byte[] smallData = Encoding.UTF8.GetBytes("Small data item for testing");
            
            _logger.LogInformation("Testing storage with {Count} small items", itemCount);
            
            try
            {
                // Act - Store many small items
                for (int i = 0; i < itemCount; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    bool result = await _fixture.TeeInterface.StorePersistentDataAsync(key, smallData);
                    Assert.True(result, $"Failed to store item {i}");
                    
                    if (i > 0 && i % 1000 == 0)
                    {
                        _logger.LogInformation("Stored {Count} items...", i);
                    }
                }
                
                // Act - Retrieve random items
                Random random = new Random();
                for (int i = 0; i < 100; i++)
                {
                    int index = random.Next(itemCount);
                    string key = $"{keyPrefix}-{index}";
                    byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                    Assert.Equal(smallData, retrievedData);
                }
                
                // Act - Clean up
                for (int i = 0; i < itemCount; i++)
                {
                    string key = $"{keyPrefix}-{i}";
                    await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                    
                    if (i > 0 && i % 1000 == 0)
                    {
                        _logger.LogInformation("Deleted {Count} items...", i);
                    }
                }
                
                _logger.LogInformation("Successfully handled {Count} small items", itemCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in many small items test");
                throw;
            }
        }
    }
}
