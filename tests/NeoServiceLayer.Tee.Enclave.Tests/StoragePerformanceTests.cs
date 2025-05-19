using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Performance")]
    public class StoragePerformanceTests : IClassFixture<SimulationModeFixture>
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public StoragePerformanceTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<StoragePerformanceTests>();
        }

        [Fact]
        public async Task MeasureStoragePerformance_SmallData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping performance test because we're not using the real SDK");
                return;
            }

            // Arrange
            int iterations = 100;
            string keyPrefix = "perf-small-" + Guid.NewGuid().ToString();
            byte[] smallData = Encoding.UTF8.GetBytes("This is a small data item for performance testing");
            
            // Act - Measure store performance
            var storeWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.StorePersistentDataAsync(key, smallData);
            }
            storeWatch.Stop();
            
            // Act - Measure retrieve performance
            var retrieveWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
            }
            retrieveWatch.Stop();
            
            // Act - Measure delete performance
            var deleteWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            }
            deleteWatch.Stop();
            
            // Log results
            double storeAvgMs = storeWatch.ElapsedMilliseconds / (double)iterations;
            double retrieveAvgMs = retrieveWatch.ElapsedMilliseconds / (double)iterations;
            double deleteAvgMs = deleteWatch.ElapsedMilliseconds / (double)iterations;
            
            _logger.LogInformation("Small Data Performance ({0} bytes, {1} iterations):", smallData.Length, iterations);
            _logger.LogInformation("  Store: {0:F2} ms per operation", storeAvgMs);
            _logger.LogInformation("  Retrieve: {0:F2} ms per operation", retrieveAvgMs);
            _logger.LogInformation("  Delete: {0:F2} ms per operation", deleteAvgMs);
            
            // Assert - Set reasonable performance expectations
            Assert.True(storeAvgMs < 50, $"Store operation should take less than 50ms (actual: {storeAvgMs:F2}ms)");
            Assert.True(retrieveAvgMs < 50, $"Retrieve operation should take less than 50ms (actual: {retrieveAvgMs:F2}ms)");
            Assert.True(deleteAvgMs < 50, $"Delete operation should take less than 50ms (actual: {deleteAvgMs:F2}ms)");
        }

        [Fact]
        public async Task MeasureStoragePerformance_MediumData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping performance test because we're not using the real SDK");
                return;
            }

            // Arrange
            int iterations = 50;
            string keyPrefix = "perf-medium-" + Guid.NewGuid().ToString();
            byte[] mediumData = new byte[50 * 1024]; // 50 KB
            new Random().NextBytes(mediumData);
            
            // Act - Measure store performance
            var storeWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.StorePersistentDataAsync(key, mediumData);
            }
            storeWatch.Stop();
            
            // Act - Measure retrieve performance
            var retrieveWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
            }
            retrieveWatch.Stop();
            
            // Act - Measure delete performance
            var deleteWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            }
            deleteWatch.Stop();
            
            // Log results
            double storeAvgMs = storeWatch.ElapsedMilliseconds / (double)iterations;
            double retrieveAvgMs = retrieveWatch.ElapsedMilliseconds / (double)iterations;
            double deleteAvgMs = deleteWatch.ElapsedMilliseconds / (double)iterations;
            
            _logger.LogInformation("Medium Data Performance ({0:F2} KB, {1} iterations):", mediumData.Length / 1024.0, iterations);
            _logger.LogInformation("  Store: {0:F2} ms per operation", storeAvgMs);
            _logger.LogInformation("  Retrieve: {0:F2} ms per operation", retrieveAvgMs);
            _logger.LogInformation("  Delete: {0:F2} ms per operation", deleteAvgMs);
            
            // Assert - Set reasonable performance expectations
            Assert.True(storeAvgMs < 100, $"Store operation should take less than 100ms (actual: {storeAvgMs:F2}ms)");
            Assert.True(retrieveAvgMs < 100, $"Retrieve operation should take less than 100ms (actual: {retrieveAvgMs:F2}ms)");
            Assert.True(deleteAvgMs < 100, $"Delete operation should take less than 100ms (actual: {deleteAvgMs:F2}ms)");
        }

        [Fact]
        public async Task MeasureStoragePerformance_LargeData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping performance test because we're not using the real SDK");
                return;
            }

            // Arrange
            int iterations = 10;
            string keyPrefix = "perf-large-" + Guid.NewGuid().ToString();
            byte[] largeData = new byte[1 * 1024 * 1024]; // 1 MB
            new Random().NextBytes(largeData);
            
            // Act - Measure store performance
            var storeWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.StorePersistentDataAsync(key, largeData);
            }
            storeWatch.Stop();
            
            // Act - Measure retrieve performance
            var retrieveWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
            }
            retrieveWatch.Stop();
            
            // Act - Measure delete performance
            var deleteWatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string key = $"{keyPrefix}-{i}";
                await _fixture.TeeInterface.RemovePersistentDataAsync(key);
            }
            deleteWatch.Stop();
            
            // Log results
            double storeAvgMs = storeWatch.ElapsedMilliseconds / (double)iterations;
            double retrieveAvgMs = retrieveWatch.ElapsedMilliseconds / (double)iterations;
            double deleteAvgMs = deleteWatch.ElapsedMilliseconds / (double)iterations;
            
            _logger.LogInformation("Large Data Performance ({0:F2} MB, {1} iterations):", largeData.Length / (1024.0 * 1024.0), iterations);
            _logger.LogInformation("  Store: {0:F2} ms per operation", storeAvgMs);
            _logger.LogInformation("  Retrieve: {0:F2} ms per operation", retrieveAvgMs);
            _logger.LogInformation("  Delete: {0:F2} ms per operation", deleteAvgMs);
            
            // Assert - Set reasonable performance expectations
            Assert.True(storeAvgMs < 500, $"Store operation should take less than 500ms (actual: {storeAvgMs:F2}ms)");
            Assert.True(retrieveAvgMs < 500, $"Retrieve operation should take less than 500ms (actual: {retrieveAvgMs:F2}ms)");
            Assert.True(deleteAvgMs < 500, $"Delete operation should take less than 500ms (actual: {deleteAvgMs:F2}ms)");
        }
    }
}
