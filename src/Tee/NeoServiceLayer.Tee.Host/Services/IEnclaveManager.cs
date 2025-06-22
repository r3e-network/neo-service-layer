// src/Tee/NeoServiceLayer.Tee.Host/Services/IEnclaveManager.cs
// MODIFIED FILE

using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Services
{
    /// <summary>
    /// Manages interactions with the Trusted Execution Environment (TEE/Enclave).
    /// Responsible for initializing the enclave, executing code within it,
    /// and handling secure data operations.
    /// </summary>
    public interface IEnclaveManager : IAsyncDisposable // Added IAsyncDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the enclave is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary. Uses a default if null.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InitializeAsync(string? enclavePath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes the enclave.
        /// </summary>
        /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
        Task<bool> InitializeEnclaveAsync();

        /// <summary>
        /// Destroys the enclave.
        /// </summary>
        /// <returns>True if the enclave was destroyed successfully, false otherwise.</returns>
        Task<bool> DestroyEnclaveAsync();

        /// <summary>
        /// Executes a JavaScript string within the enclave.
        /// </summary>
        /// <param name="script">The JavaScript code to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the JavaScript execution as a string.</returns>
        Task<string> ExecuteJavaScriptAsync(string script, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a JavaScript function with arguments.
        /// </summary>
        /// <param name="functionCode">The JavaScript function code.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The result of the JavaScript execution as a string.</returns>
        Task<string> ExecuteJavaScriptAsync(string functionCode, string args);

        // --- Generic Enclave Function Call (Advanced Usage) ---
        /// <summary>
        /// Calls a generic, named function within the enclave with a JSON payload.
        /// This is for advanced scenarios where specific P/Invoke wrappers are not yet created.
        /// </summary>
        /// <param name="functionName">The name of the function to call inside the enclave.</param>
        /// <param name="jsonPayload">The JSON string payload for the function.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The JSON string response from the enclave function.</returns>
        Task<string> CallEnclaveFunctionAsync(string functionName, string jsonPayload, CancellationToken cancellationToken = default);


        // --- Key Management Service (KMS) specific enclave operations ---

        /// <summary>
        /// Requests the enclave to generate a cryptographic key.
        /// </summary>
        /// <param name="keyId">The identifier for the new key.</param>
        /// <param name="keyType">The type of key (e.g., "Secp256k1", "AES256").</param>
        /// <param name="keyUsage">Allowed usages for the key (e.g., "Sign,Verify").</param>
        /// <param name="exportable">Whether the private key material can be exported.</param>
        /// <param name="description">Optional description for the key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JSON string representing the KeyMetadata of the generated key.</returns>
        Task<string> KmsGenerateKeyAsync(string keyId, string keyType, string keyUsage, bool exportable, string description, CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests the enclave to generate a cryptographic key.
        /// </summary>
        /// <param name="keyId">The identifier for the new key.</param>
        /// <param name="keyType">The type of key (e.g., "Secp256k1", "AES256").</param>
        /// <param name="keyUsage">Allowed usages for the key (e.g., "Sign,Verify").</param>
        /// <param name="exportable">Whether the private key material can be exported.</param>
        /// <param name="description">Optional description for the key.</param>
        /// <returns>JSON string representing the KeyMetadata of the generated key.</returns>
        Task<string> KmsGenerateKeyAsync(string keyId, string keyType, string keyUsage, bool exportable, string description);

        /// <summary>
        /// Retrieves metadata for a key managed by the KMS in the enclave.
        /// </summary>
        /// <param name="keyId">The identifier of the key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JSON string representing the KeyMetadata.</returns>
        Task<string> KmsGetKeyMetadataAsync(string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves metadata for a key managed by the KMS in the enclave.
        /// </summary>
        /// <param name="keyId">The identifier of the key.</param>
        /// <returns>JSON string representing the KeyMetadata.</returns>
        Task<string> KmsGetKeyMetadataAsync(string keyId);

        /// <summary>
        /// Lists keys managed by the KMS in the enclave.
        /// </summary>
        /// <param name="skip">Number of keys to skip (for pagination).</param>
        /// <param name="take">Maximum number of keys to return (for pagination).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JSON string representing a list of KeyMetadata objects.</returns>
        Task<string> KmsListKeysAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists keys managed by the KMS in the enclave.
        /// </summary>
        /// <param name="skip">Number of keys to skip (for pagination).</param>
        /// <param name="take">Maximum number of keys to return (for pagination).</param>
        /// <returns>JSON string representing a list of KeyMetadata objects.</returns>
        Task<string> KmsListKeysAsync(int skip, int take);


        /// <summary>
        /// Signs data using a key managed by the KMS within the enclave.
        /// </summary>
        /// <param name="keyId">The identifier of the signing key.</param>
        /// <param name="dataHex">The data to sign, provided as a hex string.</param>
        /// <param name="signingAlgorithm">Optional signing algorithm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The signature as a hex string.</returns>
        Task<string> KmsSignDataAsync(string keyId, string dataHex, string signingAlgorithm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Signs data using a key managed by the KMS within the enclave.
        /// </summary>
        /// <param name="keyId">The identifier of the signing key.</param>
        /// <param name="dataHex">The data to sign, provided as a hex string.</param>
        /// <param name="signingAlgorithm">Optional signing algorithm.</param>
        /// <returns>The signature as a hex string.</returns>
        Task<string> KmsSignDataAsync(string keyId, string dataHex, string signingAlgorithm);

        /// <summary>
        /// Verifies a signature using a key managed by the KMS or a provided public key.
        /// </summary>
        /// <param name="keyIdOrPublicKeyHex">Key ID or public key hex.</param>
        /// <param name="dataHex">Original data hex.</param>
        /// <param name="signatureHex">Signature hex.</param>
        /// <param name="signingAlgorithm">Optional signing algorithm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> KmsVerifySignatureAsync(string keyIdOrPublicKeyHex, string dataHex, string signatureHex, string signingAlgorithm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a signature using a key managed by the KMS or a provided public key.
        /// </summary>
        /// <param name="keyIdOrPublicKeyHex">Key ID or public key hex.</param>
        /// <param name="dataHex">Original data hex.</param>
        /// <param name="signatureHex">Signature hex.</param>
        /// <param name="signingAlgorithm">Optional signing algorithm.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> KmsVerifySignatureAsync(string keyIdOrPublicKeyHex, string dataHex, string signatureHex, string signingAlgorithm);

        /// <summary>
        /// Encrypts data using a key managed by the KMS.
        /// </summary>
        /// <param name="keyIdOrPublicKeyHex">Key ID or public key hex for asymmetric encryption.</param>
        /// <param name="dataHex">Data to encrypt (hex string).</param>
        /// <param name="encryptionAlgorithm">Optional encryption algorithm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Encrypted data as a hex string.</returns>
        Task<string> KmsEncryptDataAsync(string keyIdOrPublicKeyHex, string dataHex, string encryptionAlgorithm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Encrypts data using a key managed by the KMS.
        /// </summary>
        /// <param name="keyIdOrPublicKeyHex">Key ID or public key hex for asymmetric encryption.</param>
        /// <param name="dataHex">Data to encrypt (hex string).</param>
        /// <param name="encryptionAlgorithm">Optional encryption algorithm.</param>
        /// <returns>Encrypted data as a hex string.</returns>
        Task<string> KmsEncryptDataAsync(string keyIdOrPublicKeyHex, string dataHex, string encryptionAlgorithm);

        /// <summary>
        /// Decrypts data using a key managed by the KMS.
        /// </summary>
        /// <param name="keyId">The identifier of the decryption key.</param>
        /// <param name="encryptedDataHex">Encrypted data (hex string).</param>
        /// <param name="encryptionAlgorithm">Optional encryption algorithm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Decrypted data as a hex string.</returns>
        Task<string> KmsDecryptDataAsync(string keyId, string encryptedDataHex, string encryptionAlgorithm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypts data using a key managed by the KMS.
        /// </summary>
        /// <param name="keyId">The identifier of the decryption key.</param>
        /// <param name="encryptedDataHex">Encrypted data (hex string).</param>
        /// <param name="encryptionAlgorithm">Optional encryption algorithm.</param>
        /// <returns>Decrypted data as a hex string.</returns>
        Task<string> KmsDecryptDataAsync(string keyId, string encryptedDataHex, string encryptionAlgorithm);

        /// <summary>
        /// Deletes a key managed by the KMS from the enclave.
        /// </summary>
        /// <param name="keyId">The identifier of the key to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> KmsDeleteKeyAsync(string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a key managed by the KMS from the enclave.
        /// </summary>
        /// <param name="keyId">The identifier of the key to delete.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> KmsDeleteKeyAsync(string keyId);


        // --- Storage Service specific enclave operations ---

        /// <summary>
        /// Stores data in the enclave storage system.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to store.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the storage operation.</returns>
        Task<string> StorageStoreDataAsync(string key, string data, string encryptionKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves data from the enclave storage system.
        /// </summary>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The retrieved data.</returns>
        Task<string> StorageRetrieveDataAsync(string key, string encryptionKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes data from the enclave storage system.
        /// </summary>
        /// <param name="key">The key of the data to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> StorageDeleteDataAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists keys in the enclave storage system.
        /// </summary>
        /// <param name="prefix">The prefix to filter keys.</param>
        /// <param name="skip">Number of keys to skip.</param>
        /// <param name="take">Number of keys to take.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The list of keys as JSON.</returns>
        Task<string> StorageListKeysAsync(string prefix, int skip, int take, CancellationToken cancellationToken = default);


        // --- Oracle Service specific enclave operations ---

        /// <summary>
        /// Fetches data from a URL, optionally processes it with a script, all within the enclave.
        /// </summary>
        /// <param name="url">The URL to fetch data from.</param>
        /// <param name="httpMethod">HTTP method (GET, POST, etc.).</param>
        /// <param name="headersJson">JSON string of request headers.</param>
        /// <param name="requestBody">Request body for POST/PUT.</param>
        /// <param name="parsingScript">JavaScript code to parse/process the response. Can be null or empty.</param>
        /// <param name="scriptEngineOptionsJson">JSON string of options for the script engine (e.g., timeout). Can be null.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed data as a string, or raw data if no script.</returns>
        Task<string> OracleFetchAndProcessDataAsync(
            string url,
            string httpMethod,
            string headersJson,
            string requestBody,
            string parsingScript,
            string scriptEngineOptionsJson,
            CancellationToken cancellationToken = default);

        // --- TEE Attestation ---
        /// <summary>
        /// Retrieves an attestation report from the enclave.
        /// </summary>
        /// <param name="challengeHex">A challenge (e.g., a nonce) as a hex string, to be included in the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The attestation report as a JSON string (or other structured format).</returns>
        Task<string> GetAttestationReportAsync(string challengeHex, CancellationToken cancellationToken = default);


        // --- Legacy/Direct Data Operations (to be phased out or used carefully) ---
        // These methods might still be useful for direct operations not tied to KMS,
        // but KMS should be preferred for key-related crypto.

        /// <summary>
        /// Signs data using a raw private key (use with extreme caution, prefer KMS).
        /// </summary>
        Task<string> SignDataAsync(string data, string privateKeyHex); // Consider deprecating or making internal to specific services

        /// <summary>
        /// Verifies a signature using a raw public key (use with caution, prefer KMS).
        /// </summary>
        Task<bool> VerifySignatureAsync(string data, string signatureHex, string publicKeyHex); // Consider deprecating

        /// <summary>
        /// Encrypts data using a raw key (use with caution, prefer KMS).
        /// </summary>
        Task<string> EncryptDataAsync(string data, string keyHex); // Consider deprecating

        /// <summary>
        /// Encrypts data using a raw key with cancellation token (use with caution, prefer KMS).
        /// </summary>
        Task<string> EncryptDataAsync(string data, string keyHex, CancellationToken cancellationToken); // Consider deprecating

        /// <summary>
        /// Decrypts data using a raw key (use with caution, prefer KMS).
        /// </summary>
        Task<string> DecryptDataAsync(string encryptedData, string keyHex); // Consider deprecating

        /// <summary>
        /// Decrypts data using a raw key with cancellation token (use with caution, prefer KMS).
        /// </summary>
        Task<string> DecryptDataAsync(string encryptedData, string keyHex, CancellationToken cancellationToken); // Consider deprecating

        /// <summary>
        /// Gets data from the enclave.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="dataPath">The data path.</param>
        /// <returns>The data as a string.</returns>
        Task<string> GetDataAsync(string dataSource, string dataPath);

        /// <summary>
        /// Generates a random number within the specified range.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A random number within the specified range.</returns>
        Task<int> GenerateRandomAsync(int min, int max);

        /// <summary>
        /// Generates random bytes.
        /// </summary>
        /// <param name="length">The number of bytes to generate.</param>
        /// <param name="seed">Optional seed for deterministic random generation.</param>
        /// <returns>An array of random bytes.</returns>
        Task<byte[]> GenerateRandomBytesAsync(int length, string? seed = null);

        /// <summary>
        /// Fetches data from a URL directly (less secure than OracleFetchAndProcessDataAsync which runs fully in enclave).
        /// This method might be used if the enclave only needs to make an outbound call without complex processing.
        /// </summary>
        /// <param name="url">URL to fetch from.</param>
        /// <param name="headersJson">JSON string of request headers.</param>
        /// <returns>Response data as string.</returns>
        [Obsolete("Prefer OracleFetchAndProcessDataAsync for oracle tasks, or a more specific enclave function.")]
        Task<string> FetchDataFromUrlAsync(string url, string headersJson);

        // --- Abstract Account Service specific enclave operations ---

        /// <summary>
        /// Creates an abstract account in the enclave.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="accountDataJson">The account data as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The account creation result as JSON.</returns>
        Task<string> CreateAbstractAccountAsync(string accountId, string accountDataJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Signs and executes a transaction for an abstract account in the enclave.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="transactionDataJson">The transaction data as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The transaction result as JSON.</returns>
        Task<string> SignAndExecuteTransactionAsync(string accountId, string transactionDataJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a guardian to an abstract account in the enclave.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="guardianDataJson">The guardian data as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The guardian addition result as JSON.</returns>
        Task<string> AddAccountGuardianAsync(string accountId, string guardianDataJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates account recovery in the enclave.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="recoveryDataJson">The recovery data as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recovery initiation result as JSON.</returns>
        Task<string> InitiateAccountRecoveryAsync(string accountId, string recoveryDataJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes account recovery in the enclave.
        /// </summary>
        /// <param name="recoveryId">The recovery identifier.</param>
        /// <param name="recoveryDataJson">The recovery completion data as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recovery completion result as JSON.</returns>
        Task<string> CompleteAccountRecoveryAsync(string recoveryId, string recoveryDataJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a session key for an abstract account in the enclave.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="sessionKeyDataJson">The session key data as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The session key creation result as JSON.</returns>
        Task<string> CreateSessionKeyAsync(string accountId, string sessionKeyDataJson, CancellationToken cancellationToken = default);
    }
}
