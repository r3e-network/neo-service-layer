using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.EnclaveLifecycle;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.JavaScriptExecution;

namespace NeoServiceLayer.Tee.Host.Testing
{
    /// <summary>
    /// Runs tests on enclaves.
    /// </summary>
    public class EnclaveTestRunner : IEnclaveTestRunner
    {
        private readonly ILogger<EnclaveTestRunner> _logger;
        private readonly IEnclaveLifecycleManager _lifecycleManager;
        private readonly IJavaScriptExecutor _jsExecutor;
        private readonly EnclaveTestOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveTestRunner"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="lifecycleManager">The enclave lifecycle manager to use for creating and terminating enclaves.</param>
        /// <param name="jsExecutor">The JavaScript executor to use for executing JavaScript code.</param>
        /// <param name="options">The options for the enclave test runner.</param>
        public EnclaveTestRunner(
            ILogger<EnclaveTestRunner> logger,
            IEnclaveLifecycleManager lifecycleManager,
            IJavaScriptExecutor jsExecutor,
            EnclaveTestOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
            _jsExecutor = jsExecutor ?? throw new ArgumentNullException(nameof(jsExecutor));
            _options = options ?? new EnclaveTestOptions();
        }

        /// <summary>
        /// Runs a test on an enclave.
        /// </summary>
        /// <param name="test">The test to run.</param>
        /// <returns>The test result.</returns>
        public async Task<EnclaveTestResult> RunTestAsync(EnclaveTest test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            _logger.LogInformation("Running test {TestName}", test.Name);

            var result = new EnclaveTestResult
            {
                TestName = test.Name,
                StartTime = DateTime.UtcNow,
                Success = false
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Create the enclave
                _logger.LogDebug("Creating enclave for test {TestName}", test.Name);
                var enclaveId = $"test-{Guid.NewGuid():N}";
                var enclavePath = test.EnclavePath ?? _options.DefaultEnclavePath;
                var simulationMode = test.SimulationMode ?? _options.DefaultSimulationMode;
                var enclaveInterface = await _lifecycleManager.CreateEnclaveAsync(enclaveId, enclavePath, simulationMode);

                try
                {
                    // Execute the test code
                    _logger.LogDebug("Executing test code for test {TestName}", test.Name);
                    var jsResult = await _jsExecutor.ExecuteJavaScriptAsync(
                        test.Code,
                        test.Input ?? "{}",
                        test.Secrets ?? "{}",
                        test.FunctionId ?? "test",
                        test.UserId ?? "test-user");

                    // Set the test result
                    result.Output = jsResult.Output;
                    result.Error = jsResult.Error;
                    result.Success = jsResult.Success;
                    result.ExecutionTimeMs = jsResult.ExecutionTimeMs;
                    result.GasUsed = jsResult.GasUsed;
                    result.Metadata = jsResult.Metadata;

                    // Check assertions
                    if (test.Assertions != null && test.Assertions.Count > 0)
                    {
                        _logger.LogDebug("Checking assertions for test {TestName}", test.Name);
                        result.AssertionResults = new List<AssertionResult>();

                        foreach (var assertion in test.Assertions)
                        {
                            var assertionResult = new AssertionResult
                            {
                                Name = assertion.Name,
                                Success = false
                            };

                            try
                            {
                                // Execute the assertion
                                bool assertionSuccess = await ExecuteAssertionAsync(assertion, jsResult);
                                assertionResult.Success = assertionSuccess;
                                assertionResult.Message = assertionSuccess ? "Assertion passed" : "Assertion failed";
                            }
                            catch (Exception ex)
                            {
                                assertionResult.Success = false;
                                assertionResult.Message = $"Assertion failed with error: {ex.Message}";
                                _logger.LogError(ex, "Assertion {AssertionName} failed for test {TestName}", assertion.Name, test.Name);
                            }

                            result.AssertionResults.Add(assertionResult);
                        }

                        // Test is successful if all assertions pass
                        result.Success = result.AssertionResults.TrueForAll(a => a.Success);
                    }
                }
                finally
                {
                    // Terminate the enclave
                    _logger.LogDebug("Terminating enclave for test {TestName}", test.Name);
                    await _lifecycleManager.TerminateEnclaveAsync(enclaveId);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                _logger.LogError(ex, "Test {TestName} failed", test.Name);
            }

            stopwatch.Stop();
            result.EndTime = DateTime.UtcNow;
            result.TotalTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Test {TestName} completed in {TotalTimeMs}ms with result {Success}", 
                test.Name, result.TotalTimeMs, result.Success ? "SUCCESS" : "FAILURE");

            return result;
        }

        /// <summary>
        /// Runs multiple tests on enclaves.
        /// </summary>
        /// <param name="tests">The tests to run.</param>
        /// <returns>The test results.</returns>
        public async Task<IReadOnlyList<EnclaveTestResult>> RunTestsAsync(IEnumerable<EnclaveTest> tests)
        {
            if (tests == null)
            {
                throw new ArgumentNullException(nameof(tests));
            }

            var results = new List<EnclaveTestResult>();

            foreach (var test in tests)
            {
                var result = await RunTestAsync(test);
                results.Add(result);
            }

            return results;
        }

        private async Task<bool> ExecuteAssertionAsync(EnclaveTestAssertion assertion, JavaScriptExecutionResult jsResult)
        {
            if (assertion == null)
            {
                throw new ArgumentNullException(nameof(assertion));
            }

            if (jsResult == null)
            {
                throw new ArgumentNullException(nameof(jsResult));
            }

            switch (assertion.Type)
            {
                case AssertionType.OutputContains:
                    return jsResult.Output?.Contains(assertion.ExpectedValue) ?? false;
                case AssertionType.OutputEquals:
                    return jsResult.Output == assertion.ExpectedValue;
                case AssertionType.OutputStartsWith:
                    return jsResult.Output?.StartsWith(assertion.ExpectedValue) ?? false;
                case AssertionType.OutputEndsWith:
                    return jsResult.Output?.EndsWith(assertion.ExpectedValue) ?? false;
                case AssertionType.OutputMatches:
                    return System.Text.RegularExpressions.Regex.IsMatch(jsResult.Output ?? "", assertion.ExpectedValue);
                case AssertionType.ErrorContains:
                    return jsResult.Error?.Contains(assertion.ExpectedValue) ?? false;
                case AssertionType.ErrorEquals:
                    return jsResult.Error == assertion.ExpectedValue;
                case AssertionType.Success:
                    return jsResult.Success == bool.Parse(assertion.ExpectedValue);
                case AssertionType.ExecutionTimeMs:
                    return jsResult.ExecutionTimeMs <= long.Parse(assertion.ExpectedValue);
                case AssertionType.GasUsed:
                    return jsResult.GasUsed <= long.Parse(assertion.ExpectedValue);
                case AssertionType.Custom:
                    // Execute custom assertion code
                    var customResult = await _jsExecutor.ExecuteJavaScriptAsync(
                        assertion.CustomCode,
                        jsResult.Output ?? "",
                        "{}",
                        "assertion",
                        "test-user");
                    return customResult.Success && customResult.Output?.Trim().ToLower() == "true";
                default:
                    throw new ArgumentException($"Unsupported assertion type: {assertion.Type}");
            }
        }
    }
}
