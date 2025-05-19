using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Enclave;
using NeoServiceLayer.Tee.Shared.Functions;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.JavaScriptExecution;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Functions
{
    /// <summary>
    /// Enclave for executing functions.
    /// </summary>
    public class FunctionExecutionEnclave : IFunctionExecutionEnclave
    {
        private readonly ILogger<FunctionExecutionEnclave> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly IJavaScriptExecutor _javaScriptExecutor;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionExecutionEnclave"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for enclave operations.</param>
        public FunctionExecutionEnclave(
            ILogger<FunctionExecutionEnclave> logger,
            IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _javaScriptExecutor = new JavaScriptExecutor(
                new Logger<JavaScriptExecutor>(new LoggerFactory()),
                _occlumInterface);
            _initialized = false;
            _disposed = false;
        }

        /// <summary>
        /// Initializes the enclave.
        /// </summary>
        /// <returns>True if the initialization was successful, false otherwise.</returns>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await _occlumInterface.InitializeAsync();
                _initialized = true;
                _logger.LogInformation("Function execution enclave initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing function execution enclave");
                return false;
            }
        }

        /// <summary>
        /// Executes a function.
        /// </summary>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <param name="code">The code of the function to execute.</param>
        /// <param name="input">The input for the function.</param>
        /// <returns>The result of the function execution.</returns>
        public async Task<string> ExecuteFunctionAsync(string functionId, string userId, string code, string input)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (input == null)
            {
                input = "{}";
            }

            _logger.LogDebug("Executing function {FunctionId} for user {UserId}", functionId, userId);

            try
            {
                // Get user secrets for the function
                string secrets = await GetUserSecretsAsync(userId, functionId);

                // Execute the function in the enclave
                string result = await _occlumInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

                _logger.LogDebug("Function {FunctionId} executed successfully for user {UserId}", functionId, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionId} for user {UserId}", functionId, userId);
                throw new FunctionExecutionException($"Error executing function {functionId}", ex);
            }
        }

        /// <summary>
        /// Executes a function with gas accounting.
        /// </summary>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <param name="code">The code of the function to execute.</param>
        /// <param name="input">The input for the function.</param>
        /// <returns>The result of the function execution and the gas used.</returns>
        public async Task<(string Result, ulong GasUsed)> ExecuteFunctionWithGasAsync(string functionId, string userId, string code, string input)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (input == null)
            {
                input = "{}";
            }

            _logger.LogDebug("Executing function {FunctionId} with gas accounting for user {UserId}", functionId, userId);

            try
            {
                // Get user secrets for the function
                string secrets = await GetUserSecretsAsync(userId, functionId);

                // Execute the function in the enclave with gas accounting
                ulong gasUsed;
                string result = await _occlumInterface.ExecuteJavaScriptWithGasAsync(code, input, secrets, functionId, userId, out gasUsed);

                _logger.LogDebug("Function {FunctionId} executed successfully with gas accounting for user {UserId}. Gas used: {GasUsed}", functionId, userId, gasUsed);
                return (result, gasUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionId} with gas accounting for user {UserId}", functionId, userId);
                throw new FunctionExecutionException($"Error executing function {functionId} with gas accounting", ex);
            }
        }

        /// <summary>
        /// Gets user secrets for a function.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="functionId">The ID of the function.</param>
        /// <returns>The user secrets as a JSON string.</returns>
        private async Task<string> GetUserSecretsAsync(string userId, string functionId)
        {
            try
            {
                // Get the list of available secrets for the user
                string[] secretNames = await _occlumInterface.ListUserSecretsAsync(userId);
                if (secretNames == null || secretNames.Length == 0)
                {
                    return "{}";
                }

                // Build a JSON object with the secrets
                var secretsBuilder = new System.Text.StringBuilder("{");
                for (int i = 0; i < secretNames.Length; i++)
                {
                    var secretName = secretNames[i];
                    var secretValue = await _occlumInterface.GetUserSecretAsync(userId, secretName);
                    if (i > 0)
                    {
                        secretsBuilder.Append(",");
                    }
                    secretsBuilder.Append($"\"{secretName}\":\"{secretValue}\"");
                }
                secretsBuilder.Append("}");

                return secretsBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user secrets for user {UserId} and function {FunctionId}", userId, functionId);
                return "{}";
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionExecutionResult> ExecuteFunctionAsync(
            string functionId,
            string functionCode,
            string entryPoint,
            FunctionRuntime runtime,
            string input,
            Dictionary<string, string> secrets,
            string userId,
            ulong gasLimit,
            int timeoutMs)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(functionCode))
            {
                throw new ArgumentException("Function code cannot be null or empty", nameof(functionCode));
            }

            if (string.IsNullOrEmpty(entryPoint))
            {
                throw new ArgumentException("Entry point cannot be null or empty", nameof(entryPoint));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            try
            {
                // Create the execution request
                var request = new
                {
                    function_id = functionId,
                    function_code = functionCode,
                    entry_point = entryPoint,
                    runtime = runtime.ToString(),
                    input = input ?? "{}",
                    secrets = secrets ?? new Dictionary<string, string>(),
                    user_id = userId,
                    gas_limit = gasLimit,
                    timeout_ms = timeoutMs
                };

                // Execute the function in the enclave
                var result = await _occlumInterface.ExecuteJavaScriptAsync(functionCode, input, GetUserSecretsAsync(userId, functionId), functionId, userId);
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Error executing function in enclave");
                    return new FunctionExecutionResult
                    {
                        Success = false,
                        Error = "Error executing function in enclave",
                        GasUsed = 0,
                        ExecutionTimeMs = 0,
                        MemoryUsed = 0,
                        Logs = new List<string>()
                    };
                }

                // Parse the result
                var executionResult = System.Text.Json.JsonSerializer.Deserialize<FunctionExecutionResult>(result);
                return executionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionId} for user {UserId}", functionId, userId);
                return new FunctionExecutionResult
                {
                    Success = false,
                    Error = $"Error executing function: {ex.Message}",
                    GasUsed = 0,
                    ExecutionTimeMs = 0,
                    MemoryUsed = 0,
                    Logs = new List<string>()
                };
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> ValidateFunctionAsync(
            string functionCode,
            string entryPoint,
            FunctionRuntime runtime)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionCode))
            {
                throw new ArgumentException("Function code cannot be null or empty", nameof(functionCode));
            }

            if (string.IsNullOrEmpty(entryPoint))
            {
                throw new ArgumentException("Entry point cannot be null or empty", nameof(entryPoint));
            }

            try
            {
                // Create the validation request
                var request = new
                {
                    function_code = functionCode,
                    entry_point = entryPoint,
                    runtime = runtime.ToString()
                };

                // Validate the function in the enclave
                var result = await _occlumInterface.ExecuteJavaScriptAsync(functionCode, "{}", "{}", "validate_function", "validate_function");
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Error validating function in enclave");
                    return new List<string> { "Error validating function in enclave" };
                }

                // Parse the result
                var validationResult = System.Text.Json.JsonSerializer.Deserialize<List<string>>(result);
                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating function");
                return new List<string> { $"Error validating function: {ex.Message}" };
            }
        }

        /// <inheritdoc/>
        public async Task<string> CalculateCodeHashAsync(string functionCode)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionCode))
            {
                throw new ArgumentException("Function code cannot be null or empty", nameof(functionCode));
            }

            try
            {
                // Create the hash request
                var request = new
                {
                    function_code = functionCode
                };

                // Calculate the hash in the enclave
                var result = await _occlumInterface.ExecuteJavaScriptAsync(functionCode, "{}", "{}", "calculate_code_hash", "calculate_code_hash");
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Error calculating code hash in enclave");
                    throw new InvalidOperationException("Error calculating code hash in enclave");
                }

                // Parse the result
                var hashResult = System.Text.Json.JsonSerializer.Deserialize<string>(result);
                return hashResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating code hash");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyCodeHashAsync(string functionCode, string hash)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionCode))
            {
                throw new ArgumentException("Function code cannot be null or empty", nameof(functionCode));
            }

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
            }

            try
            {
                // Create the verification request
                var request = new
                {
                    function_code = functionCode,
                    hash = hash
                };

                // Verify the hash in the enclave
                var result = await _occlumInterface.ExecuteJavaScriptAsync(functionCode, "{}", "{}", "verify_code_hash", "verify_code_hash");
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Error verifying code hash in enclave");
                    throw new InvalidOperationException("Error verifying code hash in enclave");
                }

                // Parse the result
                var verificationResult = System.Text.Json.JsonSerializer.Deserialize<bool>(result);
                return verificationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying code hash");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetAttestationAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the attestation from the enclave
                var result = await _occlumInterface.ExecuteJavaScriptAsync("{}", "{}", "{}", "get_attestation", "get_attestation");
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Error getting attestation from enclave");
                    throw new InvalidOperationException("Error getting attestation from enclave");
                }

                // Parse the result
                var attestation = System.Text.Json.JsonSerializer.Deserialize<string>(result);
                return attestation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<EnclaveInfo> GetEnclaveInfoAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the enclave info
                var result = await _occlumInterface.ExecuteJavaScriptAsync("{}", "{}", "{}", "get_enclave_info", "get_enclave_info");
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogError("Error getting enclave info");
                    throw new InvalidOperationException("Error getting enclave info");
                }

                // Parse the result
                var enclaveInfo = System.Text.Json.JsonSerializer.Deserialize<EnclaveInfo>(result);
                return enclaveInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enclave info");
                throw;
            }
        }

        /// <summary>
        /// Checks if the enclave is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Function execution enclave is not initialized");
            }
        }

        /// <summary>
        /// Checks if the enclave is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FunctionExecutionEnclave));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the enclave.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _occlumInterface.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the enclave.
        /// </summary>
        ~FunctionExecutionEnclave()
        {
            Dispose(false);
        }
    }
}
