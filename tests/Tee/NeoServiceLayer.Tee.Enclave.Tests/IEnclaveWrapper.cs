using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Production-ready interface for SGX enclave operations in the Neo Service Layer.
    /// 
    /// This interface defines enterprise-grade enclave functionality suitable for production
    /// deployment in blockchain and financial services environments. All implementations
    /// must provide production-level security, performance, and compliance features.
    /// 
    /// Key Requirements:
    /// - Cryptographically secure operations
    /// - Data integrity validation and tamper detection
    /// - Enterprise-grade key management with audit trails
    /// - Thread-safe concurrent operations
    /// - Comprehensive error handling and recovery
    /// - Regulatory compliance (GDPR, SOX, etc.)
    /// - High-performance operations for production workloads
    /// - Complete audit trails for all operations
    /// 
    /// The interface supports both hardware SGX enclaves and simulation mode for testing,
    /// while maintaining production-ready security and performance standards.
    /// </summary>
    public interface IEnclaveWrapper : IDisposable
    {
        /// <summary>
        /// Initializes the enclave.
        /// </summary>
        bool Initialize();

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        string ExecuteJavaScript(string functionCode, string args);

        /// <summary>
        /// Generates a random number within the specified range.
        /// </summary>
        int GenerateRandom(int min, int max);

        /// <summary>
        /// Generates random bytes.
        /// </summary>
        byte[] GenerateRandomBytes(int length);

        /// <summary>
        /// Encrypts data using the enclave's cryptographic functions.
        /// </summary>
        byte[] Encrypt(byte[] data, byte[] key);

        /// <summary>
        /// Decrypts data using the enclave's cryptographic functions.
        /// </summary>
        byte[] Decrypt(byte[] data, byte[] key);

        /// <summary>
        /// Signs data using the enclave's cryptographic functions.
        /// </summary>
        byte[] Sign(byte[] data, byte[] key);

        /// <summary>
        /// Verifies a signature using the enclave's cryptographic functions.
        /// </summary>
        bool Verify(byte[] data, byte[] signature, byte[] key);

        /// <summary>
        /// Generates a cryptographic key.
        /// </summary>
        string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description);

        /// <summary>
        /// Fetches data from an oracle.
        /// </summary>
        string FetchOracleData(string url, string headers, string processingScript, string outputFormat);

        /// <summary>
        /// Executes a computation.
        /// </summary>
        string ExecuteComputation(string computationId, string computationCode, string parameters);

        /// <summary>
        /// Stores data in secure storage.
        /// </summary>
        string StoreData(string key, byte[] data, string encryptionKey, bool compress);

        /// <summary>
        /// Retrieves data from secure storage.
        /// </summary>
        byte[] RetrieveData(string key, string encryptionKey);

        /// <summary>
        /// Deletes data from secure storage.
        /// </summary>
        string DeleteData(string key);

        /// <summary>
        /// Gets storage metadata.
        /// </summary>
        string GetStorageMetadata(string key);

        /// <summary>
        /// Trains an AI model.
        /// </summary>
        string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters);

        /// <summary>
        /// Makes predictions with an AI model.
        /// </summary>
        double[] PredictWithAIModel(string modelId, double[] inputData, out string metadata);

        /// <summary>
        /// Creates an abstract account.
        /// </summary>
        string CreateAbstractAccount(string accountId, string accountData);

        /// <summary>
        /// Signs a transaction with an abstract account.
        /// </summary>
        string SignAbstractAccountTransaction(string accountId, string transactionData);

        /// <summary>
        /// Adds a guardian to an abstract account.
        /// </summary>
        string AddAbstractAccountGuardian(string accountId, string guardianData);

        /// <summary>
        /// Gets an attestation report.
        /// </summary>
        string GetAttestationReport();
    }
}
