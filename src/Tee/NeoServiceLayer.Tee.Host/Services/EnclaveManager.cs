using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// Implementation of the enclave manager.
/// </summary>
public partial class EnclaveManager : IEnclaveManager, IDisposable
{
    private readonly ILogger<EnclaveManager> _logger;
    private readonly IEnclaveWrapper _enclaveWrapper;
    private bool _disposed;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveWrapper">The enclave wrapper instance.</param>
    public EnclaveManager(ILogger<EnclaveManager> logger, IEnclaveWrapper enclaveWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enclaveWrapper = enclaveWrapper ?? throw new ArgumentNullException(nameof(enclaveWrapper));
        _disposed = false;
        _initialized = false;
    }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public Task InitializeAsync(string? enclavePath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing enclave...");
            bool result = _enclaveWrapper.Initialize();
            if (result)
            {
                _initialized = true;
                _logger.LogInformation("Enclave initialized successfully.");
            }
            else
            {
                _logger.LogError("Failed to initialize enclave.");
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing enclave.");
            throw;
        }
    }

    /// <summary>
    /// Initializes the enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    public Task<bool> InitializeEnclaveAsync()
    {
        try
        {
            _logger.LogInformation("Initializing enclave...");
            bool result = _enclaveWrapper.Initialize();
            if (result)
            {
                _initialized = true;
                _logger.LogInformation("Enclave initialized successfully.");
            }
            else
            {
                _logger.LogError("Failed to initialize enclave.");
            }
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing enclave.");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DestroyEnclaveAsync()
    {
        try
        {
            _logger.LogInformation("Destroying enclave...");
            _enclaveWrapper.Dispose();
            _initialized = false;
            _logger.LogInformation("Enclave destroyed successfully.");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error destroying enclave.");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public Task<string> ExecuteJavaScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing JavaScript script: {Script}", script);
            string result = _enclaveWrapper.ExecuteJavaScript(script, "{}");
            _logger.LogDebug("JavaScript script executed successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing JavaScript script.");
            throw;
        }
    }

    /// <summary>
    /// Executes a JavaScript function with arguments.
    /// </summary>
    /// <param name="functionCode">The JavaScript function code.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The result of the JavaScript execution as a string.</returns>
    public Task<string> ExecuteJavaScriptAsync(string functionCode, string args)
    {
        try
        {
            _logger.LogDebug("Executing JavaScript function: {FunctionCode}", functionCode);
            string result = _enclaveWrapper.ExecuteJavaScript(functionCode, args);
            _logger.LogDebug("JavaScript function executed successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing JavaScript function.");
            throw;
        }
    }

    /// <summary>
    /// Executes a computation in the enclave with enhanced environment and error handling.
    /// </summary>
    /// <param name="computationId">The unique identifier for the computation.</param>
    /// <param name="computationCode">The JavaScript code to execute.</param>
    /// <param name="parameters">JSON string containing computation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string containing the computation result and metadata.</returns>
    public Task<string> ExecuteComputationAsync(string computationId, string computationCode, string parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing computation: {ComputationId}", computationId);

            // Use the real enclave compute function
            string result = _enclaveWrapper.ExecuteComputation(computationId, computationCode, parameters);

            _logger.LogDebug("Computation executed successfully: {ComputationId}", computationId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing computation: {ComputationId}", computationId);
            throw;
        }
    }


    /// <summary>
    /// Retrieves data from the enclave using encrypted storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted data as a string.</returns>
    public Task<string> StorageRetrieveDataAsync(string key, string encryptionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving data with key: {Key}", key);

            // Use the real enclave storage function
            byte[] result = _enclaveWrapper.RetrieveData(key, encryptionKey);

            _logger.LogDebug("Data retrieved successfully: {Key}", key);
            return Task.FromResult(Convert.ToBase64String(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Deletes data from the enclave storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion was successful.</returns>
    public Task<bool> StorageDeleteDataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting data with key: {Key}", key);

            // Use the real enclave storage function
            string result = _enclaveWrapper.DeleteData(key);

            _logger.LogDebug("Data deleted successfully: {Key}", key);
            return Task.FromResult(result.Contains("success") || result.Contains("true"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string containing the metadata.</returns>
    public Task<string> StorageGetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting metadata for key: {Key}", key);

            // Use the real enclave storage function
            string result = _enclaveWrapper.GetStorageMetadata(key);

            _logger.LogDebug("Metadata retrieved successfully: {Key}", key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Trains an AI/ML model in the enclave.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="modelType">The type of model.</param>
    /// <param name="trainingData">Array of training data points.</param>
    /// <param name="parameters">JSON string containing training parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string containing the training result and metadata.</returns>
    public Task<string> TrainAIModelAsync(string modelId, string modelType, double[] trainingData, string parameters = "{}", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Training AI model: {ModelId} of type {ModelType}", modelId, modelType);

            // Use the real enclave AI training function
            string result = _enclaveWrapper.TrainAIModel(modelId, modelType, trainingData, parameters);

            _logger.LogDebug("AI model trained successfully: {ModelId}", modelId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training AI model: {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// Makes predictions using a trained AI/ML model.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="inputData">Array of input data for prediction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing prediction results and metadata.</returns>
    public Task<(double[] predictions, string metadata)> PredictWithAIModelAsync(string modelId, double[] inputData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Making prediction with AI model: {ModelId}", modelId);

            // Use the real enclave AI prediction function
            var result = _enclaveWrapper.PredictWithAIModel(modelId, inputData);

            _logger.LogDebug("AI prediction completed successfully: {ModelId}", modelId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making AI prediction: {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// Creates an abstract account using the enclave.
    /// </summary>
    /// <param name="accountId">The unique identifier for the account.</param>
    /// <param name="accountData">JSON string containing account creation data.</param>
    /// <returns>JSON string containing the account creation result.</returns>
    public async Task<string> CreateAbstractAccountAsync(string accountId, string accountData)
    {
        try
        {
            _logger.LogDebug("Creating abstract account: {AccountId}", accountId);
            string result = _enclaveWrapper.CreateAbstractAccount(accountId, accountData);
            _logger.LogDebug("Abstract account created successfully: {AccountId}", accountId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating abstract account: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Signs and executes a transaction using an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="transactionData">JSON string containing transaction data.</param>
    /// <returns>JSON string containing the transaction result.</returns>
    public async Task<string> SignAndExecuteTransactionAsync(string accountId, string transactionData)
    {
        try
        {
            _logger.LogDebug("Signing transaction for account: {AccountId}", accountId);
            string result = _enclaveWrapper.SignAbstractAccountTransaction(accountId, transactionData);
            _logger.LogDebug("Transaction signed successfully for account: {AccountId}", accountId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing transaction for account: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Adds a guardian to an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="guardianData">JSON string containing guardian data.</param>
    /// <returns>JSON string containing the guardian addition result.</returns>
    public async Task<string> AddAccountGuardianAsync(string accountId, string guardianData)
    {
        try
        {
            _logger.LogDebug("Adding guardian to account: {AccountId}", accountId);
            string result = _enclaveWrapper.AddAbstractAccountGuardian(accountId, guardianData);
            _logger.LogDebug("Guardian added successfully to account: {AccountId}", accountId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding guardian to account: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Initiates account recovery process.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="recoveryData">JSON string containing recovery data.</param>
    /// <returns>JSON string containing the recovery initiation result.</returns>
    public async Task<string> InitiateAccountRecoveryAsync(string accountId, string recoveryData)
    {
        try
        {
            _logger.LogDebug("Initiating recovery for account: {AccountId}", accountId);

            // Use the real enclave account recovery initiation function
            string result = _enclaveWrapper.ExecuteJavaScript($"initiateAccountRecovery('{accountId}', {recoveryData})", "");
            
            // Parse and validate the result to ensure it contains required fields
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(result);
                if (!doc.RootElement.TryGetProperty("success", out var success) || !success.GetBoolean())
                {
                    throw new InvalidOperationException("Recovery initiation failed in enclave");
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response from enclave recovery initiation");
                throw new InvalidOperationException("Invalid response from enclave recovery initiation", ex);
            }

            _logger.LogDebug("Recovery initiated successfully for account: {AccountId}", accountId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating recovery for account: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Completes account recovery process.
    /// </summary>
    /// <param name="recoveryId">The recovery identifier.</param>
    /// <param name="recoveryData">JSON string containing recovery completion data.</param>
    /// <returns>JSON string containing the recovery completion result.</returns>
    public async Task<string> CompleteAccountRecoveryAsync(string recoveryId, string recoveryData)
    {
        try
        {
            _logger.LogDebug("Completing recovery: {RecoveryId}", recoveryId);

            // Use the real enclave account recovery completion function
            string result = _enclaveWrapper.ExecuteJavaScript($"completeAccountRecovery('{recoveryId}', {recoveryData})", "");
            
            // Parse and validate the result to ensure it contains required fields
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(result);
                if (!doc.RootElement.TryGetProperty("success", out var success) || !success.GetBoolean())
                {
                    throw new InvalidOperationException("Recovery completion failed in enclave");
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response from enclave recovery completion");
                throw new InvalidOperationException("Invalid response from enclave recovery completion", ex);
            }

            _logger.LogDebug("Recovery completed successfully: {RecoveryId}", recoveryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing recovery: {RecoveryId}", recoveryId);
            throw;
        }
    }

    /// <summary>
    /// Creates a session key for an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="sessionKeyData">JSON string containing session key data.</param>
    /// <returns>JSON string containing the session key creation result.</returns>
    public async Task<string> CreateSessionKeyAsync(string accountId, string sessionKeyData)
    {
        try
        {
            _logger.LogDebug("Creating session key for account: {AccountId}", accountId);

            // Use the real enclave session key creation function
            string result = _enclaveWrapper.ExecuteJavaScript($"createSessionKey('{accountId}', {sessionKeyData})", "");
            
            // Parse and validate the result to ensure it contains required fields
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(result);
                if (!doc.RootElement.TryGetProperty("success", out var success) || !success.GetBoolean())
                {
                    throw new InvalidOperationException("Session key creation failed in enclave");
                }
                
                // Verify that essential fields are present
                if (!doc.RootElement.TryGetProperty("session_key_id", out _) ||
                    !doc.RootElement.TryGetProperty("public_key", out _))
                {
                    throw new InvalidOperationException("Enclave session key creation returned incomplete data");
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response from enclave session key creation");
                throw new InvalidOperationException("Invalid response from enclave session key creation", ex);
            }

            _logger.LogDebug("Session key created successfully for account: {AccountId}", accountId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session key for account: {AccountId}", accountId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> CallEnclaveFunctionAsync(string functionName, string jsonPayload, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling enclave function: {FunctionName} with payload: {JsonPayload}", functionName, jsonPayload);

            // Create a JavaScript wrapper to call the function
            string script = $@"
                function callEnclaveFunction() {{
                    const payload = {jsonPayload};
                    return {functionName}(payload);
                }}
                callEnclaveFunction();
            ";

            string result = _enclaveWrapper.ExecuteJavaScript(script, "{}");
            _logger.LogDebug("Enclave function called successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling enclave function.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> GetDataAsync(string dataSource, string dataPath)
    {
        try
        {
            _logger.LogDebug("Getting data from {DataSource} with path {DataPath}", dataSource, dataPath);
            string result = _enclaveWrapper.GetData(dataSource, dataPath);
            _logger.LogDebug("Data retrieved successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<int> GenerateRandomAsync(int min, int max)
    {
        try
        {
            _logger.LogDebug("Generating random number between {Min} and {Max}", min, max);
            int result = _enclaveWrapper.GenerateRandom(min, max);
            _logger.LogDebug("Random number generated successfully: {Result}", result);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random number.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> EncryptAsync(byte[] data, byte[] key)
    {
        try
        {
            _logger.LogDebug("Encrypting data with key length {KeyLength}", key.Length);
            byte[] result = _enclaveWrapper.Encrypt(data, key);
            _logger.LogDebug("Data encrypted successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> DecryptAsync(byte[] data, byte[] key)
    {
        try
        {
            _logger.LogDebug("Decrypting data with key length {KeyLength}", key.Length);
            byte[] result = _enclaveWrapper.Decrypt(data, key);
            _logger.LogDebug("Data decrypted successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> SignAsync(byte[] data, byte[] key)
    {
        try
        {
            _logger.LogDebug("Signing data with key length {KeyLength}", key.Length);
            byte[] result = _enclaveWrapper.Sign(data, key);
            _logger.LogDebug("Data signed successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> VerifyAsync(byte[] data, byte[] signature, byte[] key)
    {
        try
        {
            _logger.LogDebug("Verifying signature with key length {KeyLength}", key.Length);
            bool result = _enclaveWrapper.Verify(data, signature, key);
            _logger.LogDebug("Signature verification result: {Result}", result);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> SignDataAsync(byte[] data, byte[] key)
    {
        // This is an alias for SignAsync for compatibility
        return SignAsync(data, key);
    }

    /// <inheritdoc/>
    public Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, byte[] key)
    {
        // This is an alias for VerifyAsync for compatibility
        return VerifyAsync(data, signature, key);
    }

    /// <inheritdoc/>
    public Task<byte[]> GenerateRandomBytesAsync(int length, string? seed = null)
    {
        try
        {
            _logger.LogDebug("Generating {Length} random bytes", length);

            byte[] result;

            // If a seed is provided, use it to generate deterministic random bytes
            if (!string.IsNullOrEmpty(seed))
            {
                // Use the seed to initialize a deterministic random number generator
                byte[] seedBytes = System.Text.Encoding.UTF8.GetBytes(seed);
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                byte[] seedHash = sha256.ComputeHash(seedBytes);

                // Use the hash as a seed for random generation
                var random = new Random(BitConverter.ToInt32(seedHash, 0));
                result = new byte[length];
                random.NextBytes(result);
            }
            else
            {
                // Use efficient batch generation from enclave
                result = _enclaveWrapper.GenerateRandomBytes(length);
            }

            _logger.LogDebug("Random bytes generated successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random bytes.");
            throw;
        }
    }





    /// <inheritdoc/>
    public Task<string> OracleFetchAndProcessDataAsync(
        string url,
        string httpMethod,
        string headersJson,
        string requestBody,
        string parsingScript,
        string scriptEngineOptionsJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching and processing data from URL: {Url}", url);

            string jsonPayload = $@"{{
                ""url"": ""{url}"",
                ""httpMethod"": ""{httpMethod}"",
                ""headersJson"": {headersJson},
                ""requestBody"": ""{requestBody}"",
                ""parsingScript"": ""{parsingScript}"",
                ""scriptEngineOptionsJson"": {scriptEngineOptionsJson}
            }}";

            return CallEnclaveFunctionAsync("oracleFetchAndProcessData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> EncryptDataAsync(string data, string keyHex, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Encrypting data with key");

            string jsonPayload = $@"{{
                ""data"": ""{data}"",
                ""keyHex"": ""{keyHex}""
            }}";

            string result = _enclaveWrapper.ExecuteJavaScript($"encryptData({jsonPayload})", "{}");
            _logger.LogDebug("Data encrypted successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> DecryptDataAsync(string encryptedData, string keyHex, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Decrypting data with key");

            string jsonPayload = $@"{{
                ""encryptedData"": ""{encryptedData}"",
                ""keyHex"": ""{keyHex}""
            }}";

            string result = _enclaveWrapper.ExecuteJavaScript($"decryptData({jsonPayload})", "{}");
            _logger.LogDebug("Data decrypted successfully.");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data.");
            throw;
        }
    }
}
