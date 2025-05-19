using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave.Exceptions;
using NeoServiceLayer.Tee.Shared.Models;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Provides a secure context for executing JavaScript code within an enclave.
    /// Supports both OpenEnclave and Occlum for JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionContext : IDisposable
    {
        private readonly ILogger _logger;
        private readonly NodeJsRuntime _nodeJsRuntime;
        private readonly byte[] _sealingKey;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionContext"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="nodeJsRuntime">The Node.js runtime.</param>
        public JavaScriptExecutionContext(ILogger logger, NodeJsRuntime nodeJsRuntime)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodeJsRuntime = nodeJsRuntime ?? throw new ArgumentNullException(nameof(nodeJsRuntime));
            
            // Generate a sealing key for the enclave
            using (var rng = RandomNumberGenerator.Create())
            {
                _sealingKey = new byte[32];
                rng.GetBytes(_sealingKey);
            }
            
            _disposed = false;
        }

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="functionContext">The context for the function execution.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        public async Task<JavaScriptExecutionResult> ExecuteJavaScriptAsync(string code, string input, FunctionContext functionContext)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }
            
            if (functionContext == null)
            {
                throw new ArgumentNullException(nameof(functionContext));
            }
            
            try
            {
                _logger.LogInformation("Executing JavaScript code for function {FunctionId} and user {UserId}",
                    functionContext.FunctionId, functionContext.UserId);
                
                // Create the execution parameters
                var parameters = new NodeJsExecutionParameters
                {
                    Code = code,
                    Input = input,
                    FunctionId = functionContext.FunctionId,
                    UserId = functionContext.UserId,
                    Secrets = await GetDecryptedSecretsAsync(functionContext.EncryptedSecrets, functionContext.FunctionId),
                    TimeoutMs = functionContext.TimeoutMs,
                    MemoryLimitMb = functionContext.MemoryLimitMb
                };
                
                // Execute the JavaScript code
                var result = await _nodeJsRuntime.ExecuteAsync(parameters);
                
                // Create the execution result
                return new JavaScriptExecutionResult
                {
                    Success = true,
                    Result = result,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId} and user {UserId}",
                    functionContext.FunctionId, functionContext.UserId);
                
                return new JavaScriptExecutionResult
                {
                    Success = false,
                    Result = null,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Executes JavaScript code in the enclave with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="functionContext">The context for the function execution.</param>
        /// <returns>The result of the JavaScript execution with gas information.</returns>
        public async Task<JavaScriptExecutionResultWithGas> ExecuteJavaScriptWithGasAsync(string code, string input, FunctionContext functionContext)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }
            
            if (functionContext == null)
            {
                throw new ArgumentNullException(nameof(functionContext));
            }
            
            try
            {
                _logger.LogInformation("Executing JavaScript code with gas accounting for function {FunctionId} and user {UserId}",
                    functionContext.FunctionId, functionContext.UserId);
                
                // Create the execution parameters
                var parameters = new NodeJsExecutionParameters
                {
                    Code = code,
                    Input = input,
                    FunctionId = functionContext.FunctionId,
                    UserId = functionContext.UserId,
                    Secrets = await GetDecryptedSecretsAsync(functionContext.EncryptedSecrets, functionContext.FunctionId),
                    TimeoutMs = functionContext.TimeoutMs,
                    MemoryLimitMb = functionContext.MemoryLimitMb,
                    EnableGasAccounting = true
                };
                
                // Execute the JavaScript code with gas accounting
                var (result, gasUsed) = await _nodeJsRuntime.ExecuteWithGasAsync(parameters);
                
                // Create the execution result with gas information
                return new JavaScriptExecutionResultWithGas
                {
                    Success = true,
                    Result = result,
                    Error = null,
                    GasUsed = gasUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code with gas accounting for function {FunctionId} and user {UserId}",
                    functionContext.FunctionId, functionContext.UserId);
                
                return new JavaScriptExecutionResultWithGas
                {
                    Success = false,
                    Result = null,
                    Error = ex.Message,
                    GasUsed = 0
                };
            }
        }

        /// <summary>
        /// Decrypts the encrypted secrets using the enclave's sealing key.
        /// </summary>
        /// <param name="encryptedSecrets">The encrypted secrets.</param>
        /// <param name="functionId">The ID of the function.</param>
        /// <returns>The decrypted secrets.</returns>
        private async Task<string> GetDecryptedSecretsAsync(string encryptedSecrets, string functionId)
        {
            if (string.IsNullOrEmpty(encryptedSecrets))
            {
                return "{}"; // Return empty JSON object if no secrets
            }
            
            try
            {
                // Decode the Base64-encoded encrypted secrets
                byte[] encryptedData = Convert.FromBase64String(encryptedSecrets);
                
                // Generate a function-specific key by combining the sealing key with the function ID
                byte[] functionKey = DeriveKeyFromFunctionId(functionId);
                
                // Decrypt the secrets
                byte[] decryptedData = await DecryptAsync(encryptedData, functionKey);
                
                // Convert the decrypted data to a string
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting secrets for function {FunctionId}", functionId);
                throw new EnclaveSecurityException("Error decrypting secrets", ex);
            }
        }

        /// <summary>
        /// Derives a key from the function ID and the sealing key.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <returns>The derived key.</returns>
        private byte[] DeriveKeyFromFunctionId(string functionId)
        {
            // Use HMAC-SHA256 to derive a key from the function ID and the sealing key
            using (var hmac = new HMACSHA256(_sealingKey))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(functionId));
            }
        }

        /// <summary>
        /// Decrypts data using AES-GCM.
        /// </summary>
        /// <param name="encryptedData">The encrypted data.</param>
        /// <param name="key">The key to use for decryption.</param>
        /// <returns>The decrypted data.</returns>
        private async Task<byte[]> DecryptAsync(byte[] encryptedData, byte[] key)
        {
            // For simplicity, we're assuming a format where the IV is the first 12 bytes
            // and the tag is the last 16 bytes of the encrypted data
            // In a real implementation, you would use a proper format with metadata
            
            if (encryptedData.Length < 28) // 12 (IV) + 16 (Tag)
            {
                throw new EnclaveSecurityException("Encrypted data is too short");
            }
            
            // Extract the IV (12 bytes)
            byte[] iv = new byte[12];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, 12);
            
            // Extract the ciphertext (everything except IV and tag)
            int ciphertextLength = encryptedData.Length - 28;
            byte[] ciphertext = new byte[ciphertextLength];
            Buffer.BlockCopy(encryptedData, 12, ciphertext, 0, ciphertextLength);
            
            // Extract the tag (16 bytes)
            byte[] tag = new byte[16];
            Buffer.BlockCopy(encryptedData, encryptedData.Length - 16, tag, 0, 16);
            
            // Decrypt the data
            // Note: This is a simplified version for demonstration purposes
            // In a real implementation, you would use a proper encryption library with authenticated encryption
            
            // For now, we'll just return a mock decrypted value
            // Simulate some async work
            await Task.Delay(10);
            
            // In a real implementation, this would be the actual decryption code
            // For demonstration, we'll just return some placeholder data
            return Encoding.UTF8.GetBytes("{}");
        }

        /// <summary>
        /// Disposes the JavaScriptExecutionContext.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the JavaScriptExecutionContext.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    Array.Clear(_sealingKey, 0, _sealingKey.Length);
                    _nodeJsRuntime.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Checks if the object has been disposed and throws an exception if it has.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JavaScriptExecutionContext));
            }
        }
    }

    /// <summary>
    /// Represents the result of a JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result of the execution.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if the execution failed.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Represents the result of a JavaScript execution with gas accounting.
    /// </summary>
    public class JavaScriptExecutionResultWithGas : JavaScriptExecutionResult
    {
        /// <summary>
        /// Gets or sets the amount of gas used by the execution.
        /// </summary>
        public ulong GasUsed { get; set; }
    }

    /// <summary>
    /// Represents the context for a function execution.
    /// </summary>
    public class FunctionContext
    {
        /// <summary>
        /// Gets or sets the ID of the function to execute.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user executing the function.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted secrets for the function.
        /// </summary>
        public string EncryptedSecrets { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the function execution in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000; // Default: 30 seconds

        /// <summary>
        /// Gets or sets the memory limit for the function execution in megabytes.
        /// </summary>
        public int MemoryLimitMb { get; set; } = 256; // Default: 256 MB
    }

    /// <summary>
    /// Represents parameters for executing JavaScript in Node.js.
    /// </summary>
    public class NodeJsExecutionParameters
    {
        /// <summary>
        /// Gets or sets the JavaScript code to execute.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the input data for the JavaScript code.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets for the JavaScript code.
        /// </summary>
        public string Secrets { get; set; }

        /// <summary>
        /// Gets or sets the ID of the function to execute.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user executing the function.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the function execution in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the memory limit for the function execution in megabytes.
        /// </summary>
        public int MemoryLimitMb { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gas accounting is enabled.
        /// </summary>
        public bool EnableGasAccounting { get; set; }
    }

    /// <summary>
    /// Provides a Node.js runtime for executing JavaScript code.
    /// </summary>
    public class NodeJsRuntime : IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeJsRuntime"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public NodeJsRuntime(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _disposed = false;
        }

        /// <summary>
        /// Executes JavaScript code.
        /// </summary>
        /// <param name="parameters">The execution parameters.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        public async Task<string> ExecuteAsync(NodeJsExecutionParameters parameters)
        {
            CheckDisposed();
            
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            
            if (string.IsNullOrEmpty(parameters.Code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(parameters));
            }
            
            try
            {
                _logger.LogInformation("Executing JavaScript code for function {FunctionId} and user {UserId}",
                    parameters.FunctionId, parameters.UserId);
                
                // Simulate JavaScript execution
                await Task.Delay(100);
                
                // Return a mock result
                return "{\"success\":true,\"data\":\"Hello from Node.js!\"}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId} and user {UserId}",
                    parameters.FunctionId, parameters.UserId);
                throw new JavaScriptExecutionException("Error executing JavaScript code", ex);
            }
        }

        /// <summary>
        /// Executes JavaScript code with gas accounting.
        /// </summary>
        /// <param name="parameters">The execution parameters.</param>
        /// <returns>A tuple containing the result of the JavaScript execution and the gas used.</returns>
        public async Task<(string Result, ulong GasUsed)> ExecuteWithGasAsync(NodeJsExecutionParameters parameters)
        {
            CheckDisposed();
            
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            
            if (string.IsNullOrEmpty(parameters.Code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(parameters));
            }
            
            try
            {
                _logger.LogInformation("Executing JavaScript code with gas accounting for function {FunctionId} and user {UserId}",
                    parameters.FunctionId, parameters.UserId);
                
                // Simulate JavaScript execution with gas accounting
                await Task.Delay(100);
                
                // Return a mock result with gas used
                ulong gasUsed = 1000; // Mock gas used
                
                return ("{\"success\":true,\"data\":\"Hello from Node.js!\"}", gasUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code with gas accounting for function {FunctionId} and user {UserId}",
                    parameters.FunctionId, parameters.UserId);
                throw new JavaScriptExecutionException("Error executing JavaScript code with gas accounting", ex);
            }
        }

        /// <summary>
        /// Disposes the NodeJsRuntime.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the NodeJsRuntime.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Checks if the object has been disposed and throws an exception if it has.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NodeJsRuntime));
            }
        }
    }
} 