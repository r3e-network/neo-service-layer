namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Interface for enclave wrapper.
/// </summary>
public interface IEnclaveWrapper : IDisposable
{
    /// <summary>
    /// Initializes the enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    bool Initialize();

    /// <summary>
    /// Executes a JavaScript function in the enclave.
    /// </summary>
    /// <param name="functionCode">The JavaScript function code to execute.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The result of the function execution.</returns>
    string ExecuteJavaScript(string functionCode, string args);

    /// <summary>
    /// Gets data from an external source in the enclave.
    /// </summary>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="dataPath">The path to the data within the source.</param>
    /// <returns>The data from the external source.</returns>
    string GetData(string dataSource, string dataPath);

    /// <summary>
    /// Generates a random number in the enclave.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random number between min and max (inclusive).</returns>
    int GenerateRandom(int min, int max);

    /// <summary>
    /// Generates random bytes using the enclave's secure random number generator.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>An array of random bytes.</returns>
    byte[] GenerateRandomBytes(int length);

    /// <summary>
    /// Generates a cryptographic key in the enclave.
    /// </summary>
    /// <param name="keyId">The identifier for the new key.</param>
    /// <param name="keyType">The type of key (e.g., "Secp256k1", "Ed25519").</param>
    /// <param name="keyUsage">Allowed usages for the key (e.g., "Sign,Verify").</param>
    /// <param name="exportable">Whether the private key material can be exported.</param>
    /// <param name="description">Optional description for the key.</param>
    /// <returns>JSON string representing the KeyMetadata of the generated key.</returns>
    string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description);

    /// <summary>
    /// Fetches data from an external URL using the Oracle service in the enclave.
    /// </summary>
    /// <param name="url">The URL to fetch data from.</param>
    /// <param name="headers">Optional HTTP headers.</param>
    /// <param name="processingScript">Optional JavaScript for data processing.</param>
    /// <param name="outputFormat">Desired output format (e.g., "json", "raw").</param>
    /// <returns>JSON string containing the fetched data and metadata.</returns>
    string FetchOracleData(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json");

    /// <summary>
    /// Executes a computation in the enclave with enhanced environment and error handling.
    /// </summary>
    /// <param name="computationId">The unique identifier for the computation.</param>
    /// <param name="computationCode">The JavaScript code to execute.</param>
    /// <param name="parameters">JSON string containing computation parameters.</param>
    /// <returns>JSON string containing the computation result and metadata.</returns>
    string ExecuteComputation(string computationId, string computationCode, string parameters);

    /// <summary>
    /// Stores encrypted data in the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="compress">Whether to compress the data.</param>
    /// <returns>JSON string containing the storage result and metadata.</returns>
    string StoreData(string key, byte[] data, string encryptionKey, bool compress = false);

    /// <summary>
    /// Retrieves and decrypts data from the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The decrypted data.</returns>
    byte[] RetrieveData(string key, string encryptionKey);

    /// <summary>
    /// Deletes stored data from the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>JSON string containing the deletion result.</returns>
    string DeleteData(string key);

    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>JSON string containing the metadata.</returns>
    string GetStorageMetadata(string key);

    /// <summary>
    /// Trains an AI/ML model in the enclave.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="modelType">The type of model (e.g., "linear_regression", "anomaly_detection").</param>
    /// <param name="trainingData">Array of training data points.</param>
    /// <param name="parameters">JSON string containing training parameters.</param>
    /// <returns>JSON string containing the training result and metadata.</returns>
    string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters = "{}");

    /// <summary>
    /// Makes predictions using a trained AI/ML model.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="inputData">Array of input data for prediction.</param>
    /// <returns>Tuple containing prediction results and metadata.</returns>
    (double[] predictions, string metadata) PredictWithAIModel(string modelId, double[] inputData);

    /// <summary>
    /// Creates an abstract account in the enclave.
    /// </summary>
    /// <param name="accountId">The unique identifier for the account.</param>
    /// <param name="accountData">JSON string containing account creation data.</param>
    /// <returns>JSON string containing the account creation result.</returns>
    string CreateAbstractAccount(string accountId, string accountData);

    /// <summary>
    /// Signs a transaction using an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="transactionData">JSON string containing transaction data.</param>
    /// <returns>JSON string containing the transaction result.</returns>
    string SignAbstractAccountTransaction(string accountId, string transactionData);

    /// <summary>
    /// Adds a guardian to an abstract account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="guardianData">JSON string containing guardian data.</param>
    /// <returns>JSON string containing the guardian addition result.</returns>
    string AddAbstractAccountGuardian(string accountId, string guardianData);

    /// <summary>
    /// Encrypts data in the enclave.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted data.</returns>
    byte[] Encrypt(byte[] data, byte[] key);

    /// <summary>
    /// Decrypts data in the enclave.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The decrypted data.</returns>
    byte[] Decrypt(byte[] data, byte[] key);

    /// <summary>
    /// Signs data in the enclave.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="key">The signing key.</param>
    /// <returns>The signature.</returns>
    byte[] Sign(byte[] data, byte[] key);

    /// <summary>
    /// Verifies a signature in the enclave.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The verification key.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    bool Verify(byte[] data, byte[] signature, byte[] key);
}
