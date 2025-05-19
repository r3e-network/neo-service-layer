using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Tee.Host.Interfaces;
using NeoServiceLayer.Tee.Host.Models;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Services
{
    /// <summary>
    /// Implementation of <see cref="ITeeClient"/> using Occlum.
    /// </summary>
    public class OcclumTeeClient : ITeeClient
    {
        private readonly IOcclumInterface _occlumInterface;
        private readonly ILogger<OcclumTeeClient> _logger;
        private readonly TeeOptions _options;
        private readonly string _occlumInstanceDir = "/occlum_instance";
        private readonly string _occlumLogLevel = "info";

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumTeeClient"/> class.
        /// </summary>
        /// <param name="occlumInterface">The Occlum interface.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The TEE options.</param>
        public OcclumTeeClient(
            IOcclumInterface occlumInterface,
            ILogger<OcclumTeeClient> logger,
            IOptions<TeeOptions> options)
        {
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            _logger.LogInformation("Initializing Occlum TEE client");

            try
            {
                _occlumInterface.Initialize();

                // Initialize Occlum if it's enabled
                if (_occlumInterface.IsOcclumSupportEnabled())
                {
                    await _occlumInterface.InitializeOcclumAsync(
                        _occlumInstanceDir,
                        _occlumLogLevel);
                }

                _logger.LogInformation("Successfully initialized Occlum TEE client");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Occlum TEE client");
                return false;
            }
        }

        /// <inheritdoc/>
        public Task<string> GetStatusAsync()
        {
            _logger.LogInformation("Getting Occlum TEE status");

            try
            {
                // Create a status object with relevant information
                var status = new
                {
                    IsInitialized = _occlumInterface != null,
                    OcclumVersion = _occlumInterface.GetOcclumVersion(),
                    OcclumSupport = _occlumInterface.IsOcclumSupportEnabled(),
                    ProductId = _occlumInterface.ProductId,
                    SecurityVersion = _occlumInterface.SecurityVersion,
                    EnclaveConfiguration = _occlumInterface.GetEnclaveConfiguration()
                };

                // Serialize the status to JSON
                var json = System.Text.Json.JsonSerializer.Serialize(status);
                _logger.LogInformation("Successfully retrieved Occlum TEE status");
                return Task.FromResult(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Occlum TEE status");
                return Task.FromResult("{ \"error\": \"" + ex.Message + "\" }");
            }
        }

        /// <inheritdoc/>
        public async Task<ulong> CreateJavaScriptContextAsync()
        {
            _logger.LogInformation("Creating JavaScript context in Occlum TEE");

            try
            {
                // Execute a command to create a JavaScript context
                // This is a simulation as the actual implementation would depend on the Occlum JavaScript runtime
                var result = await _occlumInterface.ExecuteOcclumCommandAsync(
                    "/bin/js-runtime",
                    new[] { "create-context" },
                    new[] { "JS_RUNTIME_ENV=production" });

                // Parse the result to get the context ID
                // In a real implementation, this would be handled differently
                ulong contextId = (ulong)Math.Abs(result);
                _logger.LogInformation("Successfully created JavaScript context in Occlum TEE with ID {ContextId}", contextId);
                return contextId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating JavaScript context in Occlum TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DestroyJavaScriptContextAsync(ulong contextId)
        {
            _logger.LogInformation("Destroying JavaScript context {ContextId} in Occlum TEE", contextId);

            try
            {
                // Execute a command to destroy a JavaScript context
                // This is a simulation as the actual implementation would depend on the Occlum JavaScript runtime
                var result = await _occlumInterface.ExecuteOcclumCommandAsync(
                    "/bin/js-runtime",
                    new[] { "destroy-context", contextId.ToString() },
                    new[] { "JS_RUNTIME_ENV=production" });

                var success = result == 0;
                _logger.LogInformation("Successfully destroyed JavaScript context {ContextId} in Occlum TEE", contextId);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error destroying JavaScript context {ContextId} in Occlum TEE", contextId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptAsync(string code, string input, string userId, string functionId)
        {
            _logger.LogInformation("Executing JavaScript code for function {FunctionId} for user {UserId}", functionId, userId);

            try
            {
                // Get any user secrets that might be needed for execution
                var secrets = await GetUserSecretsAsync(userId);

                // Execute the JavaScript code
                var result = await _occlumInterface.ExecuteJavaScriptAsync(
                    code,
                    input,
                    secrets,
                    functionId,
                    userId);

                _logger.LogInformation("Successfully executed JavaScript code for function {FunctionId}", functionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            _logger.LogInformation("Executing JavaScript code for function {FunctionId} for user {UserId} with provided secrets", functionId, userId);

            try
            {
                // Execute the JavaScript code
                var result = await _occlumInterface.ExecuteJavaScriptAsync(
                    code,
                    input,
                    secrets,
                    functionId,
                    userId);

                _logger.LogInformation("Successfully executed JavaScript code for function {FunctionId}", functionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(string Result, ulong GasUsed)> ExecuteJavaScriptWithGasAsync(string code, string input, string userId, string functionId)
        {
            _logger.LogInformation("Executing JavaScript code with gas accounting for function {FunctionId} for user {UserId}", functionId, userId);

            try
            {
                // Get any user secrets that might be needed for execution
                var secrets = await GetUserSecretsAsync(userId);

                // Execute the JavaScript code with gas accounting
                ulong gasUsed = 0;
                var result = await _occlumInterface.ExecuteJavaScriptWithGasAsync(
                    code,
                    input,
                    secrets,
                    functionId,
                    userId,
                    out gasUsed);

                _logger.LogInformation("Successfully executed JavaScript code with gas accounting for function {FunctionId}. Gas used: {GasUsed}", functionId, gasUsed);

                return (result, gasUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code with gas accounting for function {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> StoreUserSecretAsync(string userId, string secretName, string secretValue)
        {
            _logger.LogInformation("Storing user secret {SecretName} for user {UserId}", secretName, userId);

            try
            {
                var result = await _occlumInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                _logger.LogInformation("Successfully stored user secret {SecretName} for user {UserId}", secretName, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing user secret {SecretName} for user {UserId}", secretName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetUserSecretAsync(string userId, string secretName)
        {
            _logger.LogInformation("Getting user secret {SecretName} for user {UserId}", secretName, userId);

            try
            {
                var result = await _occlumInterface.GetUserSecretAsync(userId, secretName);
                _logger.LogInformation("Successfully retrieved user secret {SecretName} for user {UserId}", secretName, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user secret {SecretName} for user {UserId}", secretName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteUserSecretAsync(string userId, string secretName)
        {
            _logger.LogInformation("Deleting user secret {SecretName} for user {UserId}", secretName, userId);

            try
            {
                var result = await _occlumInterface.DeleteUserSecretAsync(userId, secretName);
                _logger.LogInformation("Successfully deleted user secret {SecretName} for user {UserId}", secretName, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user secret {SecretName} for user {UserId}", secretName, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string[]> ListUserSecretsAsync(string userId)
        {
            _logger.LogInformation("Listing user secrets for user {UserId}", userId);

            try
            {
                var result = await _occlumInterface.ListUserSecretsAsync(userId);
                _logger.LogInformation("Successfully listed user secrets for user {UserId}", userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing user secrets for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<byte[]> GenerateRandomBytesAsync(int length)
        {
            _logger.LogInformation("Generating {Length} random bytes in Occlum TEE", length);

            try
            {
                var result = _occlumInterface.GetRandomBytes(length);
                _logger.LogInformation("Successfully generated {Length} random bytes in Occlum TEE", length);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random bytes in Occlum TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<byte[]> SignDataAsync(byte[] data)
        {
            _logger.LogInformation("Signing data in Occlum TEE");

            try
            {
                var result = _occlumInterface.SignData(data);
                _logger.LogInformation("Successfully signed data in Occlum TEE");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data in Occlum TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<bool> VerifySignatureAsync(byte[] data, byte[] signature)
        {
            _logger.LogInformation("Verifying signature in Occlum TEE");

            try
            {
                var result = _occlumInterface.VerifySignature(data, signature);
                _logger.LogInformation("Successfully verified signature in Occlum TEE");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature in Occlum TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<byte[]> SealDataAsync(byte[] data)
        {
            _logger.LogInformation("Sealing data in Occlum TEE");

            try
            {
                var result = _occlumInterface.SealData(data);
                _logger.LogInformation("Successfully sealed data in Occlum TEE");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sealing data in Occlum TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<byte[]> UnsealDataAsync(byte[] sealedData)
        {
            _logger.LogInformation("Unsealing data in Occlum TEE");

            try
            {
                var result = _occlumInterface.UnsealData(sealedData);
                _logger.LogInformation("Successfully unsealed data in Occlum TEE");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsealing data in Occlum TEE");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<string> SendMessageAsync(string message)
        {
            _logger.LogInformation("Sending message to Occlum TEE");

            try
            {
                // This is a simulation as the actual implementation would depend on the Occlum message handling
                // In a real implementation, this would be handled by sending a message to the enclave
                var response = $"{{\"status\": \"success\", \"message\": \"Processed: {message}\"}}";
                _logger.LogInformation("Successfully sent message to Occlum TEE");
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Occlum TEE");
                return Task.FromResult($"{{\"status\": \"error\", \"message\": \"{ex.Message}\"}}");
            }
        }

        /// <summary>
        /// Gets all user secrets as a JSON string.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user secrets as a JSON string.</returns>
        private async Task<string> GetUserSecretsAsync(string userId)
        {
            try
            {
                var secretNames = await _occlumInterface.ListUserSecretsAsync(userId);
                if (secretNames == null || secretNames.Length == 0)
                {
                    return "{}";
                }

                var secretsJson = new System.Text.StringBuilder("{");
                for (int i = 0; i < secretNames.Length; i++)
                {
                    var secretName = secretNames[i];
                    var secretValue = await _occlumInterface.GetUserSecretAsync(userId, secretName);
                    if (i > 0)
                    {
                        secretsJson.Append(",");
                    }
                    secretsJson.Append($"\"{secretName}\":\"{secretValue}\"");
                }
                secretsJson.Append("}");

                return secretsJson.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user secrets for user {UserId}", userId);
                return "{}";
            }
        }
    }
} 