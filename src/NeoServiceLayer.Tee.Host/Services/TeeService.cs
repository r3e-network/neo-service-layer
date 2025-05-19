using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Interfaces;

namespace NeoServiceLayer.Tee.Host.Services
{
    /// <summary>
    /// Service for interacting with the Trusted Execution Environment (TEE).
    /// </summary>
    public class TeeService : ITeeService
    {
        private readonly ILogger<TeeService> _logger;
        private readonly ITeeClient _teeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeeService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="teeClient">The TEE client.</param>
        public TeeService(ILogger<TeeService> logger, ITeeClient teeClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _teeClient = teeClient ?? throw new ArgumentNullException(nameof(teeClient));
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing TEE");
                bool result = await _teeClient.InitializeAsync();
                _logger.LogInformation("TEE initialization {Result}", result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetStatusAsync()
        {
            try
            {
                _logger.LogInformation("Getting TEE status");
                string status = await _teeClient.GetStatusAsync();
                _logger.LogInformation("TEE status retrieved successfully");
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TEE status");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ulong> CreateJavaScriptContextAsync()
        {
            try
            {
                _logger.LogInformation("Creating JavaScript context");
                ulong contextId = await _teeClient.CreateJavaScriptContextAsync();
                _logger.LogInformation("JavaScript context created with ID {ContextId}", contextId);
                return contextId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating JavaScript context");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DestroyJavaScriptContextAsync(ulong contextId)
        {
            try
            {
                _logger.LogInformation("Destroying JavaScript context {ContextId}", contextId);
                bool result = await _teeClient.DestroyJavaScriptContextAsync(contextId);
                _logger.LogInformation("JavaScript context {ContextId} destruction {Result}", contextId, result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error destroying JavaScript context {ContextId}", contextId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            try
            {
                _logger.LogInformation("Executing JavaScript function {FunctionId} for user {UserId}", functionId, userId);
                string result = await _teeClient.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
                _logger.LogInformation("JavaScript function {FunctionId} executed successfully", functionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript function {FunctionId} for user {UserId}", functionId, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> StoreUserSecretAsync(string userId, string secretName, string secretValue)
        {
            try
            {
                _logger.LogInformation("Storing secret {SecretName} for user {UserId}", secretName, userId);
                bool result = await _teeClient.StoreUserSecretAsync(userId, secretName, secretValue);
                _logger.LogInformation("Secret {SecretName} for user {UserId} storage {Result}", secretName, userId, result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secret {SecretName} for user {UserId}", secretName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetUserSecretAsync(string userId, string secretName)
        {
            try
            {
                _logger.LogInformation("Getting secret {SecretName} for user {UserId}", secretName, userId);
                string secretValue = await _teeClient.GetUserSecretAsync(userId, secretName);
                _logger.LogInformation("Secret {SecretName} for user {UserId} retrieved successfully", secretName, userId);
                return secretValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret {SecretName} for user {UserId}", secretName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteUserSecretAsync(string userId, string secretName)
        {
            try
            {
                _logger.LogInformation("Deleting secret {SecretName} for user {UserId}", secretName, userId);
                bool result = await _teeClient.DeleteUserSecretAsync(userId, secretName);
                _logger.LogInformation("Secret {SecretName} for user {UserId} deletion {Result}", secretName, userId, result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret {SecretName} for user {UserId}", secretName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateRandomBytesAsync(int length)
        {
            try
            {
                _logger.LogInformation("Generating {Length} random bytes", length);
                byte[] randomBytes = await _teeClient.GenerateRandomBytesAsync(length);
                _logger.LogInformation("Generated {Length} random bytes successfully", length);
                return randomBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Length} random bytes", length);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> SignDataAsync(byte[] data)
        {
            try
            {
                _logger.LogInformation("Signing data of length {Length}", data.Length);
                byte[] signature = await _teeClient.SignDataAsync(data);
                _logger.LogInformation("Data signed successfully");
                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifySignatureAsync(byte[] data, byte[] signature)
        {
            try
            {
                _logger.LogInformation("Verifying signature for data of length {Length}", data.Length);
                bool result = await _teeClient.VerifySignatureAsync(data, signature);
                _logger.LogInformation("Signature verification {Result}", result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> SealDataAsync(byte[] data)
        {
            try
            {
                _logger.LogInformation("Sealing data of length {Length}", data.Length);
                byte[] sealedData = await _teeClient.SealDataAsync(data);
                _logger.LogInformation("Data sealed successfully");
                return sealedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sealing data");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> UnsealDataAsync(byte[] sealedData)
        {
            try
            {
                _logger.LogInformation("Unsealing data of length {Length}", sealedData.Length);
                byte[] data = await _teeClient.UnsealDataAsync(sealedData);
                _logger.LogInformation("Data unsealed successfully");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsealing data");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeJavaScriptExecutorAsync()
        {
            try
            {
                _logger.LogInformation("Initializing JavaScript executor");
                
                var request = new
                {
                    message_type = (int)EnclaveMessageType.INITIALIZE_JS_EXECUTOR
                };
                
                string response = await _teeClient.SendMessageAsync(JsonSerializer.Serialize(request));
                var result = JsonSerializer.Deserialize<JsonElement>(response);
                
                bool success = result.GetProperty("success").GetBoolean();
                _logger.LogInformation("JavaScript executor initialization {Result}", success ? "succeeded" : "failed");
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing JavaScript executor");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptCodeAsync(string code, string filename)
        {
            try
            {
                _logger.LogInformation("Executing JavaScript code from {Filename}", filename);
                
                var request = new
                {
                    message_type = (int)EnclaveMessageType.EXECUTE_JS_CODE_NEW,
                    code,
                    filename
                };
                
                string response = await _teeClient.SendMessageAsync(JsonSerializer.Serialize(request));
                _logger.LogInformation("JavaScript code execution completed");
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code from {Filename}", filename);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptFunctionAsync(string functionName, IEnumerable<string> args)
        {
            try
            {
                _logger.LogInformation("Executing JavaScript function {FunctionName}", functionName);
                
                var request = new
                {
                    message_type = (int)EnclaveMessageType.EXECUTE_JS_FUNCTION,
                    function_name = functionName,
                    args = args.ToArray()
                };
                
                string response = await _teeClient.SendMessageAsync(JsonSerializer.Serialize(request));
                _logger.LogInformation("JavaScript function {FunctionName} execution completed", functionName);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript function {FunctionName}", functionName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CollectJavaScriptGarbageAsync()
        {
            try
            {
                _logger.LogInformation("Collecting JavaScript garbage");
                
                var request = new
                {
                    message_type = (int)EnclaveMessageType.COLLECT_JS_GARBAGE
                };
                
                string response = await _teeClient.SendMessageAsync(JsonSerializer.Serialize(request));
                var result = JsonSerializer.Deserialize<JsonElement>(response);
                
                bool success = result.GetProperty("success").GetBoolean();
                _logger.LogInformation("JavaScript garbage collection {Result}", success ? "succeeded" : "failed");
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting JavaScript garbage");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ShutdownJavaScriptExecutorAsync()
        {
            try
            {
                _logger.LogInformation("Shutting down JavaScript executor");
                
                var request = new
                {
                    message_type = (int)EnclaveMessageType.SHUTDOWN_JS_EXECUTOR
                };
                
                string response = await _teeClient.SendMessageAsync(JsonSerializer.Serialize(request));
                var result = JsonSerializer.Deserialize<JsonElement>(response);
                
                bool success = result.GetProperty("success").GetBoolean();
                _logger.LogInformation("JavaScript executor shutdown {Result}", success ? "succeeded" : "failed");
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down JavaScript executor");
                throw;
            }
        }
    }
}
