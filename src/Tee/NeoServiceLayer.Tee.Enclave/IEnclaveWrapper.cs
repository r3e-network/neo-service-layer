using System;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Represents a secure enclave wrapper interface for trusted execution environments.
/// Provides cryptographically secure operations for data processing, storage, computation,
/// and blockchain-related functionality within Intel SGX or Occlum LibOS enclaves.
/// </summary>
/// <remarks>
/// This interface defines the contract for enclave operations that require trusted execution.
/// All operations are performed within the secure enclave boundary, ensuring data confidentiality
/// and integrity. Implementations should handle proper attestation, sealing, and secure communication.
/// </remarks>
public interface IEnclaveWrapper : IDisposable
{
    #region Core Lifecycle Operations

    /// <summary>
    /// Initializes the trusted execution environment enclave.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the enclave was initialized successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="EnclaveException">
    /// Thrown when enclave initialization fails due to hardware, driver, or configuration issues.
    /// </exception>
    /// <remarks>
    /// This method must be called before any other enclave operations.
    /// Initialization includes loading the enclave, verifying its integrity,
    /// and establishing the secure communication channel.
    /// </remarks>
    bool Initialize();

    #endregion

    #region Secure Computation and JavaScript Execution

    /// <summary>
    /// Executes JavaScript code within the secure enclave environment.
    /// </summary>
    /// <param name="functionCode">
    /// The JavaScript function code to execute. Must be valid JavaScript.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="args">
    /// JSON-formatted arguments to pass to the JavaScript function.
    /// If null, an empty object "{}" will be used.
    /// </param>
    /// <returns>
    /// JSON string containing the execution result, including return value and metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="functionCode"/> is null or empty.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when JavaScript execution fails due to syntax errors, runtime errors,
    /// or enclave security violations.
    /// </exception>
    /// <remarks>
    /// JavaScript execution is sandboxed within the enclave with restricted access
    /// to external resources for security purposes.
    /// </remarks>
    string ExecuteJavaScript(string functionCode, string args);

    /// <summary>
    /// Executes a secure computation within the enclave with enhanced monitoring and error handling.
    /// </summary>
    /// <param name="computationId">
    /// Unique identifier for the computation session. Cannot be null or empty.
    /// Used for logging, monitoring, and result correlation.
    /// </param>
    /// <param name="computationCode">
    /// The computation code to execute (typically JavaScript). Cannot be null or empty.
    /// </param>
    /// <param name="parameters">
    /// JSON-formatted parameters for the computation. If null, "{}" will be used.
    /// </param>
    /// <returns>
    /// JSON string containing computation results, execution metadata, and performance metrics.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="computationId"/> or <paramref name="computationCode"/> 
    /// is null or empty.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when computation execution fails or times out.
    /// </exception>
    string ExecuteComputation(string computationId, string computationCode, string parameters);

    #endregion

    #region Oracle and External Data Operations

    /// <summary>
    /// Retrieves data from an external source through the enclave's secure networking layer.
    /// </summary>
    /// <param name="dataSource">
    /// The data source identifier or URL. Cannot be null or empty.
    /// </param>
    /// <param name="dataPath">
    /// Optional path within the data source. Can be null for root-level access.
    /// </param>
    /// <returns>
    /// JSON string containing the retrieved data and source metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="dataSource"/> is null or empty.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when data retrieval fails due to network, authentication, or parsing errors.
    /// </exception>
    string GetData(string dataSource, string dataPath);

    /// <summary>
    /// Fetches data from an external URL using the secure Oracle service within the enclave.
    /// </summary>
    /// <param name="url">
    /// The URL to fetch data from. Must be a valid HTTP/HTTPS URL. Cannot be null or empty.
    /// </param>
    /// <param name="headers">
    /// Optional HTTP headers as JSON object string. If null, default headers will be used.
    /// </param>
    /// <param name="processingScript">
    /// Optional JavaScript code for processing the fetched data within the enclave.
    /// If null, raw data will be returned.
    /// </param>
    /// <param name="outputFormat">
    /// Desired output format. Supported values: "json", "raw", "xml".
    /// Defaults to "json" if null or empty.
    /// </param>
    /// <returns>
    /// JSON string containing the fetched data, HTTP response metadata, and processing results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="url"/> is null, empty, or not a valid URL.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when HTTP request fails, processing script errors occur, or network issues arise.
    /// </exception>
    string FetchOracleData(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json");

    #endregion

    #region Cryptographic Operations

    /// <summary>
    /// Generates a cryptographically secure random number within the specified range.
    /// </summary>
    /// <param name="min">
    /// The minimum value (inclusive). Must be less than <paramref name="max"/>.
    /// </param>
    /// <param name="max">
    /// The maximum value (inclusive). Must be greater than <paramref name="min"/>.
    /// </param>
    /// <returns>
    /// A cryptographically secure random integer between <paramref name="min"/> and 
    /// <paramref name="max"/> (inclusive).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="min"/> is greater than or equal to <paramref name="max"/>.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when the secure random number generator fails.
    /// </exception>
    int GenerateRandom(int min, int max);

    /// <summary>
    /// Generates cryptographically secure random bytes using the enclave's hardware random number generator.
    /// </summary>
    /// <param name="length">
    /// The number of random bytes to generate. Must be greater than 0 and less than 1MB.
    /// </param>
    /// <returns>
    /// An array of cryptographically secure random bytes.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="length"/> is less than or equal to 0, or greater than 1MB.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when the secure random byte generation fails.
    /// </exception>
    byte[] GenerateRandomBytes(int length);

    /// <summary>
    /// Encrypts data using the enclave's secure cryptographic functions.
    /// </summary>
    /// <param name="data">
    /// The data to encrypt. Cannot be null. Maximum size: 10MB.
    /// </param>
    /// <param name="key">
    /// The encryption key. Cannot be null. Must be 16, 24, or 32 bytes for AES.
    /// </param>
    /// <returns>
    /// The encrypted data with integrity protection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> or <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> has invalid length or <paramref name="data"/> exceeds size limit.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when encryption operation fails.
    /// </exception>
    byte[] Encrypt(byte[] data, byte[] key);

    /// <summary>
    /// Decrypts data using the enclave's secure cryptographic functions.
    /// </summary>
    /// <param name="data">
    /// The encrypted data to decrypt. Cannot be null.
    /// </param>
    /// <param name="key">
    /// The decryption key. Cannot be null. Must match the encryption key.
    /// </param>
    /// <returns>
    /// The decrypted plaintext data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> or <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when decryption fails due to invalid key, corrupted data, or integrity check failure.
    /// </exception>
    byte[] Decrypt(byte[] data, byte[] key);

    /// <summary>
    /// Creates a digital signature for the provided data using enclave-protected signing keys.
    /// </summary>
    /// <param name="data">
    /// The data to sign. Cannot be null.
    /// </param>
    /// <param name="key">
    /// The signing key. Cannot be null. Must be a valid private key for the selected algorithm.
    /// </param>
    /// <returns>
    /// The digital signature bytes.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> or <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when signing operation fails due to invalid key or cryptographic errors.
    /// </exception>
    byte[] Sign(byte[] data, byte[] key);

    /// <summary>
    /// Verifies a digital signature against the original data using the corresponding public key.
    /// </summary>
    /// <param name="data">
    /// The original data that was signed. Cannot be null.
    /// </param>
    /// <param name="signature">
    /// The digital signature to verify. Cannot be null.
    /// </param>
    /// <param name="key">
    /// The verification key (public key). Cannot be null.
    /// </param>
    /// <returns>
    /// <c>true</c> if the signature is valid and matches the data; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    bool Verify(byte[] data, byte[] signature, byte[] key);

    #endregion

    #region Key Management Operations

    /// <summary>
    /// Generates a cryptographic key within the secure enclave using hardware-backed entropy.
    /// </summary>
    /// <param name="keyId">
    /// Unique identifier for the key. Cannot be null or empty.
    /// Must be alphanumeric and unique within the enclave.
    /// </param>
    /// <param name="keyType">
    /// The cryptographic algorithm type. Supported values: "Secp256k1", "Ed25519", "RSA2048", "AES256".
    /// Cannot be null or empty.
    /// </param>
    /// <param name="keyUsage">
    /// Comma-separated list of allowed key operations. 
    /// Valid operations: "Sign", "Verify", "Encrypt", "Decrypt", "KeyDerivation".
    /// Cannot be null or empty.
    /// </param>
    /// <param name="exportable">
    /// <c>true</c> if the private key material can be exported from the enclave; 
    /// otherwise, <c>false</c> for maximum security.
    /// </param>
    /// <param name="description">
    /// Optional human-readable description of the key's purpose. Can be null.
    /// </param>
    /// <returns>
    /// JSON string containing key metadata including key ID, type, creation timestamp,
    /// public key (if applicable), and usage constraints.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="keyId"/>, <paramref name="keyType"/>, or 
    /// <paramref name="keyUsage"/> is null, empty, or contains invalid values.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when key generation fails due to entropy issues, algorithm limitations,
    /// or enclave storage constraints.
    /// </exception>
    string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description);

    #endregion

    #region Secure Storage Operations

    /// <summary>
    /// Stores encrypted data in the enclave's secure storage with integrity protection.
    /// </summary>
    /// <param name="key">
    /// Storage key identifier. Cannot be null or empty. Maximum length: 256 characters.
    /// Must be unique within the enclave storage namespace.
    /// </param>
    /// <param name="data">
    /// The data to store. Cannot be null. Maximum size: 100MB.
    /// Data will be encrypted automatically before storage.
    /// </param>
    /// <param name="encryptionKey">
    /// The encryption key for additional data protection. Cannot be null or empty.
    /// Recommended to use a key derived from user credentials or enclave master key.
    /// </param>
    /// <param name="compress">
    /// <c>true</c> to compress data before encryption to save storage space;
    /// otherwise, <c>false</c>. Compression is recommended for large data.
    /// </param>
    /// <returns>
    /// JSON string containing storage operation results including data hash,
    /// storage timestamp, compressed size, and encryption metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> or <paramref name="encryptionKey"/> is null/empty,
    /// or when <paramref name="data"/> exceeds size limits.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when storage operation fails due to encryption errors, storage quota limits,
    /// or file system issues.
    /// </exception>
    string StoreData(string key, byte[] data, string encryptionKey, bool compress = false);

    /// <summary>
    /// Retrieves and decrypts data from the enclave's secure storage.
    /// </summary>
    /// <param name="key">
    /// Storage key identifier. Cannot be null or empty.
    /// Must match the key used during data storage.
    /// </param>
    /// <param name="encryptionKey">
    /// The decryption key. Cannot be null or empty.
    /// Must match the encryption key used during data storage.
    /// </param>
    /// <returns>
    /// The original decrypted and decompressed data.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> or <paramref name="encryptionKey"/> is null or empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified storage key does not exist.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when decryption fails due to invalid key, corrupted data,
    /// or integrity check failures.
    /// </exception>
    byte[] RetrieveData(string key, string encryptionKey);

    /// <summary>
    /// Permanently deletes data from the enclave's secure storage.
    /// </summary>
    /// <param name="key">
    /// Storage key identifier of the data to delete. Cannot be null or empty.
    /// </param>
    /// <returns>
    /// JSON string containing deletion confirmation with timestamp and operation status.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is null or empty.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when deletion operation fails due to storage system errors.
    /// </exception>
    /// <remarks>
    /// This operation is irreversible. The data will be securely wiped from storage.
    /// </remarks>
    string DeleteData(string key);

    /// <summary>
    /// Retrieves metadata information for stored data without decrypting the actual content.
    /// </summary>
    /// <param name="key">
    /// Storage key identifier. Cannot be null or empty.
    /// </param>
    /// <returns>
    /// JSON string containing metadata such as creation timestamp, size,
    /// compression status, encryption algorithm, and access history.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is null or empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified storage key does not exist.
    /// </exception>
    string GetStorageMetadata(string key);

    #endregion

    #region AI/ML Operations

    /// <summary>
    /// Trains a machine learning model within the secure enclave using confidential data.
    /// </summary>
    /// <param name="modelId">
    /// Unique identifier for the model. Cannot be null or empty.
    /// Must be alphanumeric and unique within the enclave.
    /// </param>
    /// <param name="modelType">
    /// The machine learning algorithm type. 
    /// Supported values: "LinearRegression", "LogisticRegression", "NeuralNetwork", 
    /// "RandomForest", "SVM", "AnomalyDetection".
    /// Cannot be null or empty.
    /// </param>
    /// <param name="trainingData">
    /// Array of training data points. Cannot be null.
    /// Maximum size: 10 million data points for performance reasons.
    /// </param>
    /// <param name="parameters">
    /// JSON-formatted training parameters specific to the model type.
    /// If null or empty, default parameters will be used.
    /// Example: {"learningRate": 0.01, "epochs": 100, "batchSize": 32}
    /// </param>
    /// <returns>
    /// JSON string containing training results including model accuracy,
    /// loss metrics, training duration, and model metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="modelId"/> or <paramref name="modelType"/> is null/empty,
    /// or when parameters contain invalid values.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="trainingData"/> is null.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when training fails due to insufficient data, algorithm errors,
    /// or computational resource constraints.
    /// </exception>
    string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters = "{}");

    /// <summary>
    /// Makes predictions using a previously trained machine learning model.
    /// </summary>
    /// <param name="modelId">
    /// Identifier of the trained model. Cannot be null or empty.
    /// The model must have been successfully trained before calling this method.
    /// </param>
    /// <param name="inputData">
    /// Array of input features for prediction. Cannot be null.
    /// The feature dimensions must match the trained model requirements.
    /// </param>
    /// <returns>
    /// Tuple containing:
    /// - predictions: Array of prediction results
    /// - metadata: JSON string with confidence scores, model version, and prediction timestamp
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="modelId"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inputData"/> is null.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified model ID does not exist or is not trained.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when prediction fails due to input dimension mismatch,
    /// model corruption, or computational errors.
    /// </exception>
    (double[] predictions, string metadata) PredictWithAIModel(string modelId, double[] inputData);

    #endregion

    #region Abstract Account Operations

    /// <summary>
    /// Creates a new abstract account within the secure enclave for blockchain operations.
    /// </summary>
    /// <param name="accountId">
    /// Unique identifier for the account. Cannot be null or empty.
    /// Must be alphanumeric and unique within the enclave.
    /// </param>
    /// <param name="accountData">
    /// JSON-formatted account configuration data.
    /// Must include required fields such as account type, initial guardians, and security settings.
    /// Cannot be null or empty.
    /// </param>
    /// <returns>
    /// JSON string containing account creation results including account address,
    /// public key, creation timestamp, and initial configuration.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="accountId"/> or <paramref name="accountData"/> 
    /// is null, empty, or contains invalid JSON.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when account creation fails due to configuration errors,
    /// key generation issues, or storage constraints.
    /// </exception>
    string CreateAbstractAccount(string accountId, string accountData);

    /// <summary>
    /// Signs a blockchain transaction using an abstract account's private key within the enclave.
    /// </summary>
    /// <param name="accountId">
    /// Identifier of the abstract account. Cannot be null or empty.
    /// The account must exist and be accessible within the enclave.
    /// </param>
    /// <param name="transactionData">
    /// JSON-formatted transaction data to sign. Cannot be null or empty.
    /// Must include all required transaction fields such as recipient, amount, and nonce.
    /// </param>
    /// <returns>
    /// JSON string containing the signed transaction with signature,
    /// transaction hash, and signing metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="accountId"/> or <paramref name="transactionData"/>
    /// is null, empty, or contains invalid transaction data.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified account ID does not exist.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when signing fails due to invalid transaction format,
    /// cryptographic errors, or account access restrictions.
    /// </exception>
    string SignAbstractAccountTransaction(string accountId, string transactionData);

    /// <summary>
    /// Adds a guardian to an existing abstract account for enhanced security.
    /// </summary>
    /// <param name="accountId">
    /// Identifier of the abstract account. Cannot be null or empty.
    /// The account must exist and allow guardian modifications.
    /// </param>
    /// <param name="guardianData">
    /// JSON-formatted guardian information including public key,
    /// permissions, and recovery settings. Cannot be null or empty.
    /// </param>
    /// <returns>
    /// JSON string containing guardian addition results with guardian ID,
    /// permissions summary, and updated account security configuration.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="accountId"/> or <paramref name="guardianData"/>
    /// is null, empty, or contains invalid guardian configuration.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified account ID does not exist.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when guardian addition fails due to permission restrictions,
    /// configuration errors, or maximum guardian limit exceeded.
    /// </exception>
    string AddAbstractAccountGuardian(string accountId, string guardianData);

    #endregion

    #region SGX-Specific Operations

    /// <summary>
    /// Generates an SGX attestation report proving the enclave's identity and integrity.
    /// </summary>
    /// <returns>
    /// JSON string containing the complete attestation report with enclave measurements,
    /// platform configuration, and signature chain for remote verification.
    /// </returns>
    /// <exception cref="EnclaveException">
    /// Thrown when attestation generation fails due to SGX platform issues,
    /// quote generation errors, or enclave measurement problems.
    /// </exception>
    /// <remarks>
    /// The attestation report can be used by remote parties to verify the enclave's
    /// authenticity and establish secure communication channels.
    /// </remarks>
    string GetAttestationReport();

    /// <summary>
    /// Seals data using SGX platform-specific sealing keys for persistent storage.
    /// </summary>
    /// <param name="data">
    /// The data to seal. Cannot be null. Maximum size: 1MB.
    /// Sealed data can only be unsealed by the same enclave on the same platform.
    /// </param>
    /// <returns>
    /// The sealed data blob including encrypted content and platform binding information.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="data"/> exceeds the maximum size limit.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when sealing operation fails due to SGX platform errors,
    /// key derivation issues, or encryption failures.
    /// </exception>
    /// <remarks>
    /// SGX sealing binds data to the enclave's identity and platform configuration.
    /// Sealed data cannot be accessed by other enclaves or on different platforms.
    /// </remarks>
    byte[] SealData(byte[] data);

    /// <summary>
    /// Unseals data that was previously sealed using SGX platform-specific sealing keys.
    /// </summary>
    /// <param name="sealedData">
    /// The sealed data blob to unseal. Cannot be null.
    /// Must have been created by the same enclave on the same platform.
    /// </param>
    /// <returns>
    /// The original unsealed data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sealedData"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sealedData"/> has invalid format or structure.
    /// </exception>
    /// <exception cref="EnclaveException">
    /// Thrown when unsealing fails due to platform mismatch, enclave identity mismatch,
    /// data corruption, or key derivation errors.
    /// </exception>
    byte[] UnsealData(byte[] sealedData);

    /// <summary>
    /// Retrieves cryptographically secure trusted time from the SGX platform.
    /// </summary>
    /// <returns>
    /// Trusted time as Unix timestamp in milliseconds, guaranteed to be monotonic
    /// and resistant to system clock manipulation.
    /// </returns>
    /// <exception cref="EnclaveException">
    /// Thrown when trusted time retrieval fails due to SGX platform issues
    /// or secure time service unavailability.
    /// </exception>
    /// <remarks>
    /// Trusted time is essential for time-sensitive cryptographic operations
    /// and ensuring temporal ordering of transactions within the enclave.
    /// </remarks>
    long GetTrustedTime();

    #endregion
}
