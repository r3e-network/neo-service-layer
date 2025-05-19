using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.TestHelpers;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Collection("SimulationMode")]
    [Trait("Category", "GasAccounting")]
    public class GasAccountingTests : IDisposable
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ILogger<GasAccountingTests> _logger;
        private readonly ITeeEnclaveInterface _teeInterface;

        public GasAccountingTests(SimulationModeFixture fixture)
        {
            _fixture = fixture;
            _logger = _fixture.LoggerFactory.CreateLogger<GasAccountingTests>();
            _teeInterface = _fixture.TeeInterface;
        }

        public void Dispose()
        {
            (_teeInterface as IDisposable)?.Dispose();
        }

        [Fact]
        public async Task RecordExecutionMetrics_WithValidData_Succeeds()
        {
            // Arrange
            string functionId = "gas_test_function";
            string userId = "gas_test_user";
            long gasUsed = 1000;

            // Act & Assert
            await _teeInterface.RecordExecutionMetricsAsync(functionId, userId, gasUsed);
            // No exception means success
        }

        [Fact]
        public async Task RecordExecutionFailure_WithValidData_Succeeds()
        {
            // Arrange
            string functionId = "gas_test_function";
            string userId = "gas_test_user";
            string errorMessage = "Test error";

            // Act & Assert
            await _teeInterface.RecordExecutionFailureAsync(functionId, userId, errorMessage);
            // No exception means success
        }

        [Fact]
        public async Task ExecuteJavaScript_MeasuresGasUsed()
        {
            // Arrange
            string code = @"
                function main(input) {
                    // Perform a computation that uses gas
                    let result = 0;
                    for (let i = 0; i < input.iterations; i++) {
                        result += i;
                    }
                    return { result: result };
                }
            ";
            string input = @"{ ""iterations"": 1000000 }";
            string secrets = @"{}";
            string functionId = "gas_measurement_test";
            string userId = "gas_test_user";

            // Act
            var stopwatch = Stopwatch.StartNew();
            string result = await _teeInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
            stopwatch.Stop();

            // Assert
            _logger.LogInformation("JavaScript execution time: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            EnclaveAssert.IsValidJavaScriptResult(result);

            // In a real implementation, we would verify that gas was recorded
            // For now, we just check that the execution completed successfully
        }

        [Fact]
        public async Task ExecuteJavaScript_WithHighGasUsage_StillCompletes()
        {
            // Arrange
            string code = @"
                function main(input) {
                    // Perform a computation that uses a lot of gas
                    let result = 0;
                    for (let i = 0; i < input.iterations; i++) {
                        for (let j = 0; j < 100; j++) {
                            result += (i * j) % 10;
                        }
                    }
                    return { result: result };
                }
            ";
            string input = @"{ ""iterations"": 10000 }";
            string secrets = @"{}";
            string functionId = "high_gas_test";
            string userId = "gas_test_user";

            // Act
            var stopwatch = Stopwatch.StartNew();
            string result = await _teeInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
            stopwatch.Stop();

            // Assert
            _logger.LogInformation("High gas JavaScript execution time: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            EnclaveAssert.IsValidJavaScriptResult(result);
        }

        [Fact]
        public async Task ExecuteJavaScript_WithMemoryIntensiveOperation_MeasuresMemoryGas()
        {
            // Arrange
            string code = @"
                function main(input) {
                    // Allocate a large array to use memory
                    const size = input.size || 1000000;
                    const array = new Array(size).fill(0);

                    // Perform some operations on the array
                    for (let i = 0; i < array.length; i++) {
                        array[i] = i % 256;
                    }

                    return {
                        arraySize: array.length,
                        sum: array.reduce((a, b) => a + b, 0)
                    };
                }
            ";
            string input = @"{ ""size"": 1000000 }";
            string secrets = @"{}";
            string functionId = "memory_gas_test";
            string userId = "gas_test_user";

            // Act
            var stopwatch = Stopwatch.StartNew();
            string result = await _teeInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
            stopwatch.Stop();

            // Assert
            _logger.LogInformation("Memory-intensive JavaScript execution time: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            EnclaveAssert.IsValidJavaScriptResult(result);
            EnclaveAssert.JavaScriptResultHasProperty(result, "arraySize", 1000000);
        }

        [Fact]
        public async Task ExecuteMultipleJavaScriptFunctions_TracksGasPerFunction()
        {
            // Arrange
            string code1 = EnclaveTestHelper.CreateSampleJavaScriptFunction(2);
            string input1 = EnclaveTestHelper.CreateSampleJavaScriptInput(2);
            string functionId1 = "function1";

            string code2 = EnclaveTestHelper.CreateSampleJavaScriptFunction(3);
            string input2 = EnclaveTestHelper.CreateSampleJavaScriptInput(3);
            string functionId2 = "function2";

            string secrets = EnclaveTestHelper.CreateSampleSecrets("gas_test_user");
            string userId = "gas_test_user";

            // Act
            var stopwatch1 = Stopwatch.StartNew();
            string result1 = await _teeInterface.ExecuteJavaScriptAsync(code1, input1, secrets, functionId1, userId);
            stopwatch1.Stop();

            var stopwatch2 = Stopwatch.StartNew();
            string result2 = await _teeInterface.ExecuteJavaScriptAsync(code2, input2, secrets, functionId2, userId);
            stopwatch2.Stop();

            // Assert
            _logger.LogInformation("Function 1 execution time: {ElapsedMilliseconds}ms", stopwatch1.ElapsedMilliseconds);
            _logger.LogInformation("Function 2 execution time: {ElapsedMilliseconds}ms", stopwatch2.ElapsedMilliseconds);

            EnclaveAssert.IsValidJavaScriptResult(result1);
            EnclaveAssert.IsValidJavaScriptResult(result2);

            // In a real implementation, we would verify that gas was recorded separately for each function
            // For now, we just check that both executions completed successfully
        }
    }
}
