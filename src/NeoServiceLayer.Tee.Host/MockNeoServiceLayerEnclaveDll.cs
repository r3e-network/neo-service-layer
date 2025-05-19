using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// A mock implementation of the NeoServiceLayerEnclave_u DLL for testing.
    /// </summary>
    /// <remarks>
    /// This class provides a simulation of the NeoServiceLayerEnclave_u DLL for testing purposes.
    /// It implements all the native methods that would normally be provided by the NeoServiceLayerEnclave_u DLL.
    /// 
    /// To use this class, call <see cref="Initialize"/> before any enclave operations are performed.
    /// This will set up the necessary hooks to intercept calls to the native methods.
    /// </remarks>
    public static class MockNeoServiceLayerEnclaveDll
    {
        private static ILogger _logger;
        private static bool _initialized;
        private static readonly Dictionary<IntPtr, MockEnclaveState> _enclaveStates = new Dictionary<IntPtr, MockEnclaveState>();

        /// <summary>
        /// Initializes the mock NeoServiceLayerEnclave_u DLL.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initialized = true;
            _logger.LogInformation("Mock NeoServiceLayerEnclave_u DLL initialized");
        }

        /// <summary>
        /// Initializes a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int enclave_initialize(IntPtr enclaveId)
        {
            if (!_initialized)
            {
                return -1;
            }

            _logger.LogInformation("Initializing mock enclave: {Id}", enclaveId);

            // Create a new mock enclave state
            var state = new MockEnclaveState
            {
                Initialized = true,
                OcclumInitialized = false,
                UserSecrets = new Dictionary<string, Dictionary<string, string>>(),
                ExecutionMetrics = new Dictionary<string, Dictionary<string, long>>(),
                ExecutionFailures = new Dictionary<string, Dictionary<string, string>>()
            };

            // Add the state to the dictionary
            _enclaveStates[enclaveId] = state;

            return 0;
        }

        /// <summary>
        /// Gets the status of a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="statusBuffer">The buffer to store the status.</param>
        /// <param name="bufferSize">The size of the buffer.</param>
        /// <param name="statusSize">The size of the status.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int enclave_get_status(
            IntPtr enclaveId,
            byte[] statusBuffer,
            int bufferSize,
            out int statusSize)
        {
            if (!_initialized)
            {
                statusSize = 0;
                return -1;
            }

            _logger.LogInformation("Getting status of mock enclave: {Id}", enclaveId);

            // Check if the enclave exists
            if (!_enclaveStates.TryGetValue(enclaveId, out var state))
            {
                statusSize = 0;
                return -1;
            }

            // Create a mock status
            var status = new
            {
                Initialized = state.Initialized,
                OcclumInitialized = state.OcclumInitialized,
                UserSecretCount = state.UserSecrets.Count,
                ExecutionMetricsCount = state.ExecutionMetrics.Count,
                ExecutionFailuresCount = state.ExecutionFailures.Count
            };

            // Convert the status to JSON
            string statusJson = JsonSerializer.Serialize(status);
            byte[] statusBytes = Encoding.UTF8.GetBytes(statusJson);

            // Check if the buffer is large enough
            if (bufferSize < statusBytes.Length)
            {
                statusSize = statusBytes.Length;
                return 2; // OE_BUFFER_TOO_SMALL
            }

            // Copy the status to the buffer
            Array.Copy(statusBytes, statusBuffer, statusBytes.Length);
            statusSize = statusBytes.Length;

            return 0;
        }

        /// <summary>
        /// Executes JavaScript in a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input to the JavaScript code.</param>
        /// <param name="secrets">The secrets to make available to the JavaScript code.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="resultBuffer">The buffer to store the result.</param>
        /// <param name="bufferSize">The size of the buffer.</param>
        /// <param name="resultSize">The size of the result.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int enclave_execute_javascript(
            IntPtr enclaveId,
            string code,
            string input,
            string secrets,
            string functionId,
            string userId,
            byte[] resultBuffer,
            int bufferSize,
            out int resultSize)
        {
            if (!_initialized)
            {
                resultSize = 0;
                return -1;
            }

            _logger.LogInformation("Executing JavaScript in mock enclave: {Id}, Function: {FunctionId}, User: {UserId}", enclaveId, functionId, userId);

            // Check if the enclave exists
            if (!_enclaveStates.TryGetValue(enclaveId, out var state))
            {
                resultSize = 0;
                return -1;
            }

            // Check if the enclave is initialized
            if (!state.Initialized)
            {
                resultSize = 0;
                return -1;
            }

            // Simulate JavaScript execution
            string result;
            try
            {
                // Parse the input
                var inputJson = JsonDocument.Parse(input);

                // Handle memory-intensive operation test
                if (inputJson.RootElement.TryGetProperty("size", out var sizeElement) &&
                    sizeElement.ValueKind == JsonValueKind.Number)
                {
                    int size = sizeElement.GetInt32();
                    result = $"{{\"result\": \"Created array\", \"arraySize\": {size}}}";
                }
                // Handle value test
                else if (inputJson.RootElement.TryGetProperty("value", out var valueElement) &&
                    valueElement.ValueKind == JsonValueKind.Number)
                {
                    int value = valueElement.GetInt32();
                    result = $"{{\"result\": {value * 2}}}";
                }
                // Handle iterations test
                else if (inputJson.RootElement.TryGetProperty("iterations", out var iterationsElement) &&
                    iterationsElement.ValueKind == JsonValueKind.Number)
                {
                    int iterations = iterationsElement.GetInt32();
                    result = $"{{\"result\": {iterations * 10}}}";
                }
                // Default response
                else
                {
                    result = "{\"result\": \"success\"}";
                }
            }
            catch
            {
                result = "{\"error\": \"Failed to execute JavaScript\"}";
            }

            // Convert the result to bytes
            byte[] resultBytes = Encoding.UTF8.GetBytes(result);

            // Check if the buffer is large enough
            if (bufferSize < resultBytes.Length)
            {
                resultSize = resultBytes.Length;
                return 2; // OE_BUFFER_TOO_SMALL
            }

            // Copy the result to the buffer
            Array.Copy(resultBytes, resultBuffer, resultBytes.Length);
            resultSize = resultBytes.Length;

            // Record execution metrics
            if (!state.ExecutionMetrics.ContainsKey(userId))
            {
                state.ExecutionMetrics[userId] = new Dictionary<string, long>();
            }
            state.ExecutionMetrics[userId][functionId] = 1000; // Mock gas usage

            return 0;
        }

        /// <summary>
        /// Records execution metrics in a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasUsed">The amount of gas used.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int enclave_record_execution_metrics(
            IntPtr enclaveId,
            string functionId,
            string userId,
            long gasUsed)
        {
            if (!_initialized)
            {
                return -1;
            }

            _logger.LogInformation("Recording execution metrics in mock enclave: {Id}, Function: {FunctionId}, User: {UserId}, Gas: {GasUsed}", enclaveId, functionId, userId, gasUsed);

            // Check if the enclave exists
            if (!_enclaveStates.TryGetValue(enclaveId, out var state))
            {
                return -1;
            }

            // Check if the enclave is initialized
            if (!state.Initialized)
            {
                return -1;
            }

            // Record execution metrics
            if (!state.ExecutionMetrics.ContainsKey(userId))
            {
                state.ExecutionMetrics[userId] = new Dictionary<string, long>();
            }
            state.ExecutionMetrics[userId][functionId] = gasUsed;

            return 0;
        }

        /// <summary>
        /// Records execution failure in a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int enclave_record_execution_failure(
            IntPtr enclaveId,
            string functionId,
            string userId,
            string errorMessage)
        {
            if (!_initialized)
            {
                return -1;
            }

            _logger.LogInformation("Recording execution failure in mock enclave: {Id}, Function: {FunctionId}, User: {UserId}, Error: {ErrorMessage}", enclaveId, functionId, userId, errorMessage);

            // Check if the enclave exists
            if (!_enclaveStates.TryGetValue(enclaveId, out var state))
            {
                return -1;
            }

            // Check if the enclave is initialized
            if (!state.Initialized)
            {
                return -1;
            }

            // Record execution failure
            if (!state.ExecutionFailures.ContainsKey(userId))
            {
                state.ExecutionFailures[userId] = new Dictionary<string, string>();
            }
            state.ExecutionFailures[userId][functionId] = errorMessage;

            return 0;
        }

        /// <summary>
        /// A mock enclave state.
        /// </summary>
        private class MockEnclaveState
        {
            public bool Initialized { get; set; }
            public bool OcclumInitialized { get; set; }
            public Dictionary<string, Dictionary<string, string>> UserSecrets { get; set; }
            public Dictionary<string, Dictionary<string, long>> ExecutionMetrics { get; set; }
            public Dictionary<string, Dictionary<string, string>> ExecutionFailures { get; set; }
        }
    }
}
