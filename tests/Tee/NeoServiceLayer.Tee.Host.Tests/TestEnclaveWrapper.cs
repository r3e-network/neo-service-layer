using NeoServiceLayer.Tee.Enclave;

namespace NeoServiceLayer.Tee.Host.Tests;

/// <summary>
/// A test-friendly implementation of the IEnclaveWrapper interface for unit testing.
/// </summary>
public class TestEnclaveWrapper : IEnclaveWrapper
{
    private bool _initialized;
    private bool _disposed;
    private readonly Dictionary<string, byte[]> _mockStorage = new();
    private string _executeJavaScriptResult;
    private byte[] _signResult;
    private bool _verifyResult;
    private byte[] _encryptResult;
    private byte[] _decryptResult;
    private string _getDataResult;
    private int _generateRandomResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestEnclaveWrapper"/> class.
    /// </summary>
    public TestEnclaveWrapper()
    {
        _initialized = false;
        _disposed = false;
        _executeJavaScriptResult = "{}";
        _signResult = new byte[0];
        _verifyResult = false;
        _encryptResult = new byte[0];
        _decryptResult = new byte[0];
        _getDataResult = "";
        _generateRandomResult = 0;
    }

    /// <summary>
    /// Sets the result to return from ExecuteJavaScript.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetExecuteJavaScriptResult(string result)
    {
        _executeJavaScriptResult = result;
    }

    /// <summary>
    /// Sets the result to return from Sign.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetSignResult(byte[] result)
    {
        _signResult = result;
    }

    /// <summary>
    /// Sets the result to return from Verify.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetVerifyResult(bool result)
    {
        _verifyResult = result;
    }

    /// <summary>
    /// Sets the result to return from Encrypt.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetEncryptResult(byte[] result)
    {
        _encryptResult = result;
    }

    /// <summary>
    /// Sets the result to return from Decrypt.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetDecryptResult(byte[] result)
    {
        _decryptResult = result;
    }

    /// <summary>
    /// Sets the result to return from GetData.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetGetDataResult(string result)
    {
        _getDataResult = result;
    }

    /// <summary>
    /// Sets the result to return from GenerateRandom.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetGenerateRandomResult(int result)
    {
        _generateRandomResult = result;
    }

    /// <summary>
    /// Initializes the enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    public bool Initialize()
    {
        _initialized = true;
        return true;
    }

    /// <summary>
    /// Executes JavaScript code in the enclave.
    /// </summary>
    /// <param name="functionCode">The JavaScript code to execute.</param>
    /// <param name="args">The arguments to pass to the JavaScript code.</param>
    /// <returns>The result of the JavaScript execution as a string.</returns>
    public string ExecuteJavaScript(string functionCode, string args)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _executeJavaScriptResult;
    }

    /// <summary>
    /// Signs data using a key.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="key">The key to use for signing.</param>
    /// <returns>The signature.</returns>
    public byte[] Sign(byte[] data, byte[] key)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _signResult;
    }

    /// <summary>
    /// Verifies a signature.
    /// </summary>
    /// <param name="data">The data that was signed.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The key to use for verification.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool Verify(byte[] data, byte[] signature, byte[] key)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _verifyResult;
    }

    /// <summary>
    /// Encrypts data using a key.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The key to use for encryption.</param>
    /// <returns>The encrypted data.</returns>
    public byte[] Encrypt(byte[] data, byte[] key)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _encryptResult;
    }

    /// <summary>
    /// Decrypts data using a key.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="key">The key to use for decryption.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] Decrypt(byte[] data, byte[] key)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _decryptResult;
    }

    /// <summary>
    /// Gets data from the enclave.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <param name="dataPath">The data path.</param>
    /// <returns>The data as a string.</returns>
    public string GetData(string dataSource, string dataPath)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _getDataResult;
    }

    /// <summary>
    /// Generates a random number within the specified range.
    /// </summary>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <returns>A random number within the specified range.</returns>
    public int GenerateRandom(int min, int max)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        return _generateRandomResult;
    }

    /// <summary>
    /// Generates random bytes using the enclave's secure random number generator.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>An array of random bytes.</returns>
    public byte[] GenerateRandomBytes(int length)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        byte[] result = new byte[length];
        Random.Shared.NextBytes(result);
        return result;
    }

    /// <summary>
    /// Generates a cryptographic key in the enclave.
    /// </summary>
    /// <param name="keyId">The identifier for the new key.</param>
    /// <param name="keyType">The type of key (e.g., "Secp256k1", "Ed25519").</param>
    /// <param name="keyUsage">Allowed usages for the key (e.g., "Sign,Verify").</param>
    /// <param name="exportable">Whether the private key material can be exported.</param>
    /// <param name="description">Optional description for the key.</param>
    /// <returns>JSON string representing the KeyMetadata of the generated key.</returns>
    public string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        // Generate a mock key metadata JSON response
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var publicKeyHex = "0x" + string.Join("", Enumerable.Range(0, 32).Select(_ => Random.Shared.Next(0, 256).ToString("x2")));

        return $@"{{
            ""keyId"": ""{keyId}"",
            ""keyType"": ""{keyType}"",
            ""keyUsage"": ""{keyUsage}"",
            ""exportable"": {exportable.ToString().ToLowerInvariant()},
            ""description"": ""{description}"",
            ""createdAt"": ""{timestamp}"",
            ""publicKeyHex"": ""{publicKeyHex}""
        }}";
    }

    /// <summary>
    /// Fetches data from an external URL using the Oracle service in the enclave.
    /// </summary>
    /// <param name="url">The URL to fetch data from.</param>
    /// <param name="headers">Optional HTTP headers.</param>
    /// <param name="processingScript">Optional JavaScript for data processing.</param>
    /// <param name="outputFormat">Desired output format (e.g., "json", "raw").</param>
    /// <returns>JSON string containing the fetched data and metadata.</returns>
    public string FetchOracleData(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json")
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        // Generate mock Oracle response
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var mockData = url.Contains("price") ?
            "{\"price\": 42.50, \"symbol\": \"BTC\", \"currency\": \"USD\"}" :
            "{\"data\": \"mock_response\", \"value\": 123.45}";

        return $@"{{
            ""success"": true,
            ""data"": {mockData},
            ""metadata"": {{
                ""url"": ""{url}"",
                ""response_code"": 200,
                ""content_length"": {mockData.Length},
                ""timestamp"": ""{timestamp}""
            }}
        }}";
    }

    /// <summary>
    /// Executes a computation in the enclave with enhanced environment and error handling.
    /// </summary>
    /// <param name="computationId">The unique identifier for the computation.</param>
    /// <param name="computationCode">The JavaScript code to execute.</param>
    /// <param name="parameters">JSON string containing computation parameters.</param>
    /// <returns>JSON string containing the computation result and metadata.</returns>
    public string ExecuteComputation(string computationId, string computationCode, string parameters)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        // Generate mock computation result
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Simple computation simulation based on code content
        var mockResult = "null";
        if (computationCode.Contains("input * 2"))
        {
            // Parse parameters to get input value
            try
            {
                var paramsObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parameters);
                if (paramsObj?.TryGetValue("input", out var inputValue) == true)
                {
                    if (double.TryParse(inputValue.ToString(), out var input))
                    {
                        mockResult = (input * 2).ToString();
                    }
                }
            }
            catch
            {
                mockResult = "84"; // Default result for input * 2
            }
        }
        else if (computationCode.Contains("add"))
        {
            mockResult = "42";
        }
        else
        {
            mockResult = "\"computation_executed\"";
        }

        return $@"{{
            ""success"": true,
            ""computationId"": ""{computationId}"",
            ""result"": {mockResult},
            ""executionTimeMs"": 50,
            ""timestamp"": {timestamp}
        }}";
    }

    /// <summary>
    /// Stores encrypted data in the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="compress">Whether to compress the data.</param>
    /// <returns>JSON string containing the storage result and metadata.</returns>
    public string StoreData(string key, byte[] data, string encryptionKey, bool compress = false)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        // Store data in mock storage
        _mockStorage[key] = data;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $@"{{
            ""success"": true,
            ""key"": ""{key}"",
            ""metadata"": {{
                ""key"": ""{key}"",
                ""size"": {data.Length},
                ""encrypted_size"": {data.Length + 32},
                ""compressed"": {(compress ? "true" : "false")},
                ""encrypted"": true,
                ""timestamp"": {timestamp}
            }}
        }}";
    }

    /// <summary>
    /// Retrieves and decrypts data from the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] RetrieveData(string key, string encryptionKey)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        if (!_mockStorage.TryGetValue(key, out var data))
        {
            throw new EnclaveException($"Data with key '{key}' not found.");
        }

        return data;
    }

    /// <summary>
    /// Deletes stored data from the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>JSON string containing the deletion result.</returns>
    public string DeleteData(string key)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        bool deleted = _mockStorage.Remove(key);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $@"{{
            ""success"": {(deleted ? "true" : "false")},
            ""key"": ""{key}"",
            ""deleted"": {(deleted ? "true" : "false")},
            ""timestamp"": {timestamp}
        }}";
    }

    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>JSON string containing the metadata.</returns>
    public string GetStorageMetadata(string key)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        if (!_mockStorage.TryGetValue(key, out var data))
        {
            throw new EnclaveException($"Data with key '{key}' not found.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $@"{{
            ""success"": true,
            ""key"": ""{key}"",
            ""metadata"": {{
                ""key"": ""{key}"",
                ""size"": {data.Length},
                ""encrypted_size"": {data.Length + 32},
                ""compressed"": false,
                ""encrypted"": true,
                ""timestamp"": {timestamp}
            }}
        }}";
    }

    /// <summary>
    /// Trains an AI/ML model in the enclave.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="modelType">The type of model (e.g., "linear_regression", "anomaly_detection").</param>
    /// <param name="trainingData">Array of training data points.</param>
    /// <param name="parameters">JSON string containing training parameters.</param>
    /// <returns>JSON string containing the training result and metadata.</returns>
    public string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters = "{}")
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        // Store model for later predictions
        _mockStorage[$"ai_model_{modelId}"] = System.Text.Encoding.UTF8.GetBytes(modelType);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $@"{{
            ""success"": true,
            ""model_id"": ""{modelId}"",
            ""model_type"": ""{modelType}"",
            ""weights_count"": {(modelType == "linear_regression" ? 2 : 3)},
            ""metadata"": {{
                ""model_id"": ""{modelId}"",
                ""model_type"": ""{modelType}"",
                ""training_samples"": {trainingData.Length},
                ""weights_count"": {(modelType == "linear_regression" ? 2 : 3)},
                ""trained_at"": {timestamp},
                ""accuracy"": 0.85
            }}
        }}";
    }

    /// <summary>
    /// Makes predictions using a trained AI/ML model.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="inputData">Array of input data for prediction.</param>
    /// <returns>Tuple containing prediction results and metadata.</returns>
    public (double[] predictions, string metadata) PredictWithAIModel(string modelId, double[] inputData)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        if (!_mockStorage.TryGetValue($"ai_model_{modelId}", out var modelData))
        {
            throw new EnclaveException($"AI model '{modelId}' not found.");
        }

        string modelType = System.Text.Encoding.UTF8.GetString(modelData);

        // Generate mock prediction based on model type
        double prediction = modelType switch
        {
            "linear_regression" => inputData.Length > 0 ? inputData[0] * 2 + 5 : 5.0,
            "anomaly_detection" => inputData.Length > 0 && inputData[0] > 60 ? 1.0 : 0.0,
            "pattern_classification" => inputData.Length > 0 && inputData[0] > 50 ? 1.0 : 0.0,
            _ => 0.5
        };

        var predictions = new[] { prediction };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var metadata = $@"{{
            ""model_id"": ""{modelId}"",
            ""model_type"": ""{modelType}"",
            ""prediction"": {prediction},
            ""confidence"": 0.85,
            ""input_size"": {inputData.Length},
            ""predicted_at"": {timestamp}
        }}";

        return (predictions, metadata);
    }

    /// <summary>
    /// Creates an abstract account in the enclave.
    /// </summary>
    /// <param name="accountId">The unique identifier for the account.</param>
    /// <param name="accountData">JSON string containing account creation data.</param>
    /// <returns>JSON string containing the account creation result.</returns>
    public string CreateAbstractAccount(string accountId, string accountData)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        // Store account for later operations
        _mockStorage[$"account_{accountId}"] = System.Text.Encoding.UTF8.GetBytes(accountData);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var accountAddress = $"0x{accountId.GetHashCode():x8}";
        var masterPublicKey = $"0x{timestamp:x16}";

        return $@"{{
            ""success"": true,
            ""account_id"": ""{accountId}"",
            ""account_address"": ""{accountAddress}"",
            ""master_public_key"": ""{masterPublicKey}"",
            ""transaction_hash"": ""0x{timestamp:x16}""
        }}";
    }

    /// <summary>
    /// Signs a transaction using an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="transactionData">JSON string containing transaction data.</param>
    /// <returns>JSON string containing the transaction result.</returns>
    public string SignAbstractAccountTransaction(string accountId, string transactionData)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        if (!_mockStorage.ContainsKey($"account_{accountId}"))
        {
            throw new EnclaveException($"Abstract account '{accountId}' not found.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var transactionHash = $"0x{timestamp:x16}";
        var signature = $"0x{(timestamp * 2):x16}";

        return $@"{{
            ""success"": true,
            ""account_id"": ""{accountId}"",
            ""transaction_hash"": ""{transactionHash}"",
            ""signature"": ""{signature}"",
            ""gas_used"": 21000,
            ""timestamp"": {timestamp}
        }}";
    }

    /// <summary>
    /// Adds a guardian to an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="guardianData">JSON string containing guardian data.</param>
    /// <returns>JSON string containing the guardian addition result.</returns>
    public string AddAbstractAccountGuardian(string accountId, string guardianData)
    {
        if (!_initialized)
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        if (!_mockStorage.ContainsKey($"account_{accountId}"))
        {
            throw new EnclaveException($"Abstract account '{accountId}' not found.");
        }

        var guardianId = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Store guardian data
        _mockStorage[$"guardian_{guardianId}"] = System.Text.Encoding.UTF8.GetBytes(guardianData);

        return $@"{{
            ""success"": true,
            ""account_id"": ""{accountId}"",
            ""guardian_id"": ""{guardianId}"",
            ""transaction_hash"": ""0x{timestamp:x16}"",
            ""timestamp"": {timestamp}
        }}";
    }

    /// <summary>
    /// Disposes the enclave wrapper.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_initialized)
        {
            _initialized = false;
        }

        _disposed = true;
    }
}
