using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using NeoServiceLayer.Tee.Host;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Performance")]
    public class PerformanceTests : IDisposable
    {
        private readonly ILogger<MockOpenEnclaveInterface> _logger;
        private readonly MockOpenEnclaveInterface _oeInterface;
        private readonly string _enclavePath;
        private readonly bool _skipTests;

        public PerformanceTests()
        {
            // Create a real logger for better diagnostics
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<MockOpenEnclaveInterface>();

            // Get the enclave path from environment variable or use a default
            _enclavePath = Environment.GetEnvironmentVariable("OE_ENCLAVE_PATH") ?? "liboe_enclave.signed.so";

            // Set simulation mode for testing
            Environment.SetEnvironmentVariable("OE_SIMULATION", "1");

            try
            {
                // Initialize the mock DLLs
                MockDllInitializer.Initialize(_logger);

                // Create a mock enclave file
                var mockEnclaveFileLogger = loggerFactory.CreateLogger<MockEnclaveFile>();
                var mockEnclaveFile = new MockEnclaveFile(mockEnclaveFileLogger);
                mockEnclaveFile.CreateAsync().Wait();

                // Create the MockOpenEnclaveInterface
                _oeInterface = new MockOpenEnclaveInterface(_logger, _enclavePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Open Enclave interface");
                _skipTests = true;
            }
        }

        public void Dispose()
        {
            _oeInterface?.Dispose();
        }

        [Fact]
        public void GetRandomBytes_Performance_IsAcceptable()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            int length = 1024;
            int iterations = 10;
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                _oeInterface.GetRandomBytes(length);
            }
            stopwatch.Stop();

            // Assert
            double averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
            _logger.LogInformation("GetRandomBytes average time: {AverageMs}ms", averageMs);

            // Performance threshold - adjust based on your requirements
            Assert.True(averageMs < 100, $"GetRandomBytes should take less than 100ms on average, but took {averageMs}ms");
        }

        [Fact]
        public void SignData_Performance_IsAcceptable()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] data = new byte[1024];
            new Random().NextBytes(data);
            int iterations = 10;
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                _oeInterface.SignData(data);
            }
            stopwatch.Stop();

            // Assert
            double averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
            _logger.LogInformation("SignData average time: {AverageMs}ms", averageMs);

            // Performance threshold - adjust based on your requirements
            Assert.True(averageMs < 200, $"SignData should take less than 200ms on average, but took {averageMs}ms");
        }

        [Fact]
        public void SealData_Performance_IsAcceptable()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] data = new byte[1024];
            new Random().NextBytes(data);
            int iterations = 10;
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                _oeInterface.SealData(data);
            }
            stopwatch.Stop();

            // Assert
            double averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
            _logger.LogInformation("SealData average time: {AverageMs}ms", averageMs);

            // Performance threshold - adjust based on your requirements
            Assert.True(averageMs < 200, $"SealData should take less than 200ms on average, but took {averageMs}ms");
        }

        [Fact]
        public async Task ExecuteJavaScript_Performance_IsAcceptable()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string code = @"
                function main(input) {
                    let result = 0;
                    for (let i = 0; i < input.iterations; i++) {
                        result += i;
                    }
                    return { result: result };
                }
            ";
            string input = @"{ ""iterations"": 10000 }";
            string secrets = @"{}";
            string functionId = "perf_test_function";
            string userId = "perf_test_user";
            int iterations = 5;
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                await _oeInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
            }
            stopwatch.Stop();

            // Assert
            double averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
            _logger.LogInformation("ExecuteJavaScript average time: {AverageMs}ms", averageMs);

            // Performance threshold - adjust based on your requirements
            Assert.True(averageMs < 1000, $"ExecuteJavaScript should take less than 1000ms on average, but took {averageMs}ms");
        }

        [Fact]
        public void GetAttestationReport_Performance_IsAcceptable()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] reportData = Encoding.UTF8.GetBytes("Performance test report data");
            int iterations = 5;
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                _oeInterface.GetAttestationReport(reportData);
            }
            stopwatch.Stop();

            // Assert
            double averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
            _logger.LogInformation("GetAttestationReport average time: {AverageMs}ms", averageMs);

            // Performance threshold - adjust based on your requirements
            Assert.True(averageMs < 500, $"GetAttestationReport should take less than 500ms on average, but took {averageMs}ms");
        }
    }
}
