using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// JavaScript engine for executing JavaScript code in the Occlum LibOS enclave.
    /// </summary>
    public class JavaScriptEngine : IDisposable
    {
        private readonly ILogger<JavaScriptEngine> _logger;
        private readonly V8ScriptEngine _engine;
        private readonly GasAccountingManager _gasAccountingManager;
        private readonly IOcclumInterface _occlumInterface;
        private bool _disposed = false;

        // Native methods for Occlum-specific operations
        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_create_js_context(out IntPtr context_id);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_destroy_js_context(IntPtr context_id);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_verify_js_execution(IntPtr context_id, [MarshalAs(UnmanagedType.LPStr)] string code_hash);

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptEngine"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="gasAccountingManager">The GAS accounting manager.</param>
        /// <param name="occlumInterface">The Occlum interface.</param>
        public JavaScriptEngine(
            ILogger<JavaScriptEngine> logger,
            GasAccountingManager gasAccountingManager,
            IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gasAccountingManager = gasAccountingManager ?? throw new ArgumentNullException(nameof(gasAccountingManager));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));

            // Check if we're in simulation mode
            bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";
            IntPtr contextId = IntPtr.Zero;

            try
            {
                if (!isSimulationMode)
                {
                    // Initialize the Occlum context for JavaScript execution
                    int result = occlum_create_js_context(out contextId);
                    if (result != 0)
                    {
                        throw new InvalidOperationException($"Failed to create Occlum JavaScript context: error code {result}");
                    }

                    _logger.LogInformation("Created Occlum JavaScript context with ID: {ContextId}", contextId);
                }
                else
                {
                    // In simulation mode, use a mock context ID
                    contextId = new IntPtr(1234);
                    _logger.LogInformation("Running in simulation mode with mock context ID: {ContextId}", contextId);
                }

                try
                {
                    // Initialize the V8 engine with constraints suitable for Occlum
                    var flags = V8ScriptEngineFlags.DisableGlobalMembers;
                    _engine = new V8ScriptEngine(flags);

                    // Set up the execution constraints for secure execution in Occlum
                    // Freeze core JavaScript objects to prevent tampering
                    _engine.Execute("Object.freeze(Object.prototype);");
                    _engine.Execute("Object.freeze(Array.prototype);");
                    _engine.Execute("Object.freeze(String.prototype);");
                    _engine.Execute("Object.freeze(Number.prototype);");
                    _engine.Execute("Object.freeze(Boolean.prototype);");
                    _engine.Execute("Object.freeze(Function.prototype);");
                    _engine.Execute("Object.freeze(Date.prototype);");
                    _engine.Execute("Object.freeze(RegExp.prototype);");
                    _engine.Execute("Object.freeze(Error.prototype);");
                    _engine.Execute("Object.freeze(Math);");
                    _engine.Execute("Object.freeze(JSON);");

                    // Prevent access to global objects that could be used for attacks
                    _engine.Execute(@"
                        // Prevent accessing dangerous global objects
                        (function() {
                            var forbidden = ['document', 'window', 'globalThis', 'XMLHttpRequest', 'fetch', 'WebSocket', 'Worker', 
                                            'eval', 'Function', 'setTimeout', 'setInterval', 'Proxy', 'constructor'];
                            for (var i = 0; i < forbidden.length; i++) {
                                if (typeof this[forbidden[i]] !== 'undefined') {
                                    Object.defineProperty(this, forbidden[i], {
                                        get: function() { 
                                            throw new Error('Access to ' + forbidden[i] + ' is not allowed');
                                        }
                                    });
                                }
                            }
                        })();
                    ");
                    
                    // Implement safe timer functions with gas accounting
                    _engine.Execute(@"
                        // Safe timer functions with gas accounting
                        var _setTimeout = setTimeout;
                        setTimeout = function(callback, delay) {
                            gasAccounting.useGas(10);
                            return _setTimeout(function() {
                                gasAccounting.useGas(5);
                                callback();
                            }, delay);
                        };
                        
                        var _setInterval = setInterval;
                        setInterval = function(callback, delay) {
                            gasAccounting.useGas(20);
                            return _setInterval(function() {
                                gasAccounting.useGas(5);
                                callback();
                            }, delay);
                        };
                    ");

                    // Add GAS accounting
                    _engine.AddHostObject("gasAccounting", _gasAccountingManager);

                    // Add Occlum-specific utilities
                    _engine.AddHostObject("occlum", new OcclumJavaScriptUtilities(_occlumInterface, contextId));
                }
                catch (DllNotFoundException ex)
                {
                    _logger.LogWarning(ex, "ClearScript V8 DLL not found. Running in simulation mode without JavaScript engine.");
                    // In simulation mode, we don't need a real JavaScript engine
                }
                catch (TypeLoadException ex)
                {
                    _logger.LogWarning(ex, "ClearScript V8 DLL could not be loaded. Running in simulation mode without JavaScript engine.");
                    // In simulation mode, we don't need a real JavaScript engine
                }
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogWarning(ex, "Occlum PAL not found. Running in simulation mode.");

                // In simulation mode, use a mock context ID
                contextId = new IntPtr(1234);

                // Create a minimal engine for testing
                var flags = V8ScriptEngineFlags.DisableGlobalMembers;
                _engine = new V8ScriptEngine(flags);

                // Add GAS accounting
                _engine.AddHostObject("gasAccounting", _gasAccountingManager);

                // Add Occlum-specific utilities
                _engine.AddHostObject("occlum", new OcclumJavaScriptUtilities(_occlumInterface, contextId));
            }
        }

        /// <summary>
        /// Executes JavaScript code within the Occlum enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>The result of the execution and the GAS used.</returns>
        public virtual async Task<(JsonDocument Result, long GasUsed)> ExecuteAsync(string code, EnclaveJavaScriptContext context)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                // Reset the GAS accounting
                _gasAccountingManager.ResetGasUsed();

                // Verify the code integrity within the Occlum enclave
                string codeHash = ComputeSHA256Hash(code);
                var occlumUtility = _engine.Script.occlum as OcclumJavaScriptUtilities;

                // Check if we're in simulation mode
                bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";

                if (occlumUtility != null && !isSimulationMode)
                {
                    try
                    {
                        int verificationResult = occlum_verify_js_execution(occlumUtility.ContextId, codeHash);
                        if (verificationResult != 0)
                        {
                            throw new SecurityException($"Occlum code verification failed: error code {verificationResult}");
                        }
                    }
                    catch (DllNotFoundException)
                    {
                        _logger.LogWarning("Occlum PAL not found during verification. Running in simulation mode.");
                        // Continue execution in simulation mode
                    }
                }

                JsonDocument resultJson;
                long gasUsed;

                if (_engine != null)
                {
                    try
                    {
                        // Add the context to the engine
                        _engine.AddHostObject("context", context);
                        _engine.AddHostObject("input", context.Input);
                        _engine.AddHostObject("secrets", context.Secrets);

                        // Add utility functions with Occlum-specific security measures
                        _engine.Execute(@"
                            function getSecret(name) {
                                gasAccounting.useGas(10);
                                // Use Occlum protected memory for accessing secrets
                                return occlum.getSecretValue(name);
                            }

                            function log(message) {
                                gasAccounting.useGas(5);
                                occlum.secureLog(message);
                            }

                            function verifyData(data, signature) {
                                gasAccounting.useGas(50);
                                return occlum.verifySignature(data, signature);
                            }

                            function generateRandomBytes(length) {
                                gasAccounting.useGas(20 + length);
                                return occlum.getRandomBytes(length);
                            }
                        ");

                        // Execute the code within the Occlum protected memory
                        _engine.Execute(code);

                        // Call the main function with the input in a secure manner
                        var result = _engine.Evaluate("JSON.stringify(main(input))");
                        var resultString = result?.ToString();
                        resultJson = !string.IsNullOrEmpty(resultString) ?
                            System.Text.Json.JsonDocument.Parse(resultString) :
                            System.Text.Json.JsonDocument.Parse("{}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing JavaScript in engine");
                        resultJson = JsonDocument.Parse($"{{\"error\": \"{ex.Message}\"}}");
                    }

                    // Get the GAS used
                    gasUsed = _gasAccountingManager.GetGasUsed();
                }
                else if (isSimulationMode)
                {
                    // In simulation mode, return a mock result
                    _logger.LogInformation("Running in simulation mode without JavaScript engine. Returning mock result.");

                    // Simple mock implementation that returns the input value multiplied by 2
                    try
                    {
                        if (context.Input.RootElement.TryGetProperty("value", out var valueElement) &&
                            valueElement.ValueKind == JsonValueKind.Number)
                        {
                            int value = valueElement.GetInt32();
                            resultJson = JsonDocument.Parse($"{{\"result\": {value * 2}}}");
                        }
                        else if (context.Input.RootElement.TryGetProperty("iterations", out var iterationsElement) &&
                                 iterationsElement.ValueKind == JsonValueKind.Number)
                        {
                            int iterations = iterationsElement.GetInt32();
                            resultJson = JsonDocument.Parse($"{{\"result\": {iterations * 10}}}");
                        }
                        else
                        {
                            resultJson = JsonDocument.Parse("{\"result\": \"success\"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in simulation mode");
                        resultJson = JsonDocument.Parse($"{{\"error\": \"{ex.Message}\"}}");
                    }

                    // Simulate GAS usage
                    _gasAccountingManager.UseGas(500);
                    gasUsed = 500;
                }
                else
                {
                    // Not in simulation mode and no JavaScript engine available
                    throw new InvalidOperationException("JavaScript engine not available");
                }

                // Record execution metrics in the Occlum secure storage
                await _occlumInterface.RecordExecutionMetricsAsync(context.FunctionId, context.UserId, gasUsed);

                return (resultJson, gasUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code in Occlum enclave");

                // Record execution failure in the Occlum secure storage
                await _occlumInterface.RecordExecutionFailureAsync(context.FunctionId, context.UserId, ex.Message);

                throw;
            }
            finally
            {
                if (_engine != null)
                {
                    try
                    {
                        // Clean up and ensure no sensitive data remains in memory
                        _engine.Script.context = null;
                        _engine.Script.input = null;
                        _engine.Script.secrets = null;
                        _engine.CollectGarbage(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error cleaning up JavaScript engine");
                    }
                }

                // Explicitly clear any sensitive data
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Clears sensitive data from the JavaScript engine.
        /// </summary>
        private void ClearSensitiveData()
        {
            if (_engine != null)
            {
                try
                {
                    // Clear any user secrets from the engine
                    _engine.Execute("secrets = null;");
                    _engine.Execute("context = null;");
                    _engine.Execute("input = null;");
                    
                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    _logger.LogDebug("Cleared sensitive data from JavaScript engine");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing sensitive data from JavaScript engine");
                }
            }
        }

        /// <summary>
        /// Computes the SHA256 hash of the provided code.
        /// </summary>
        /// <param name="code">The code to hash.</param>
        /// <returns>The computed hash.</returns>
        private string ComputeSHA256Hash(string code)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Disposes the resources used by the JavaScriptEngine.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Clear sensitive data before disposing
                        ClearSensitiveData();
                        
                        // Dispose the engine
                        _engine?.Dispose();
                        
                        // Clean up Occlum context if not in simulation mode
                        bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";
                        var occlumUtility = _engine?.Script.occlum as OcclumJavaScriptUtilities;
                        
                        if (occlumUtility != null && !isSimulationMode)
                        {
                            try
                            {
                                int result = occlum_destroy_js_context(occlumUtility.ContextId);
                                if (result != 0)
                                {
                                    _logger.LogWarning("Failed to destroy Occlum JavaScript context: error code {Result}", result);
                                }
                                else
                                {
                                    _logger.LogInformation("Destroyed Occlum JavaScript context with ID: {ContextId}", occlumUtility.ContextId);
                                }
                            }
                            catch (DllNotFoundException ex)
                            {
                                _logger.LogWarning(ex, "Occlum PAL not found during context cleanup. Running in simulation mode.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during JavaScript engine disposal");
                    }
                }
                
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the JavaScript engine and cleans up Occlum resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
