using System.Runtime.InteropServices;
using System.Text;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Occlum LibOS specific enclave wrapper implementation.
/// Provides secure operations using Occlum LibOS trusted execution environment.
/// </summary>
public class OcclumEnclaveWrapper : IEnclaveWrapper
{
    private bool _disposed;
    private bool _initialized;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OcclumEnclaveWrapper"/> class.
    /// </summary>
    public OcclumEnclaveWrapper()
    {
        _disposed = false;
        _initialized = false;
    }

    /// <summary>
    /// Initializes the Occlum enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    public bool Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return true;
            }

            int result = NativeOcclumLibOS.occlum_init();
            _initialized = result == 0;
            return _initialized;
        }
    }

    /// <summary>
    /// Ensures the enclave is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the enclave is not initialized.</exception>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Occlum enclave is not initialized. Call Initialize() first.");
        }
    }

    /// <summary>
    /// Executes JavaScript code in the Occlum enclave.
    /// </summary>
    /// <param name="functionCode">The JavaScript function code to execute.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The result of the function execution.</returns>
    public string ExecuteJavaScript(string functionCode, string args)
    {
        EnsureInitialized();

        byte[] functionCodeBytes = Encoding.UTF8.GetBytes(functionCode);
        byte[] argsBytes = Encoding.UTF8.GetBytes(args);
        byte[] resultBytes = new byte[4096];
        IntPtr resultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumLibOS.occlum_execute_js(
                functionCodeBytes, (UIntPtr)functionCodeBytes.Length,
                argsBytes, (UIntPtr)argsBytes.Length,
                resultBytes, (UIntPtr)resultBytes.Length,
                resultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to execute JavaScript in Occlum enclave. Error code: {result}");
            }

            int resultSize = Marshal.ReadInt32(resultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, resultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(resultSizePtr);
        }
    }

    /// <summary>
    /// Generates a random number using Occlum's secure random number generator.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random number between min and max (inclusive).</returns>
    public int GenerateRandom(int min, int max)
    {
        EnsureInitialized();

        IntPtr resultPtr = Marshal.AllocHGlobal(sizeof(int));

        try
        {
            int result = NativeOcclumLibOS.occlum_generate_random(min, max, resultPtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to generate random number in Occlum enclave. Error code: {result}");
            }

            return Marshal.ReadInt32(resultPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(resultPtr);
        }
    }

    /// <summary>
    /// Generates random bytes using Occlum's secure random number generator.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>An array of random bytes.</returns>
    public byte[] GenerateRandomBytes(int length)
    {
        EnsureInitialized();

        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        }

        byte[] buffer = new byte[length];
        int result = NativeOcclumLibOS.occlum_generate_random_bytes(buffer, (UIntPtr)length);

        if (result != 0)
        {
            throw new EnclaveException($"Failed to generate random bytes in Occlum enclave. Error code: {result}");
        }

        return buffer;
    }

    /// <summary>
    /// Stores data in Occlum's secure file system.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="compress">Whether to compress the data.</param>
    /// <returns>JSON string containing the storage result and metadata.</returns>
    public string StoreData(string key, byte[] data, string encryptionKey, bool compress = false)
    {
        EnsureInitialized();

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] resultBytes = new byte[4096];
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumLibOS.occlum_storage_store(
                keyBytes,
                data,
                (UIntPtr)data.Length,
                encryptionKeyBytes,
                compress ? 1 : 0,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to store data in Occlum enclave with key '{key}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Retrieves data from Occlum's secure file system.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] RetrieveData(string key, string encryptionKey)
    {
        EnsureInitialized();

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] resultBytes = new byte[1024 * 1024]; // 1MB buffer
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumLibOS.occlum_storage_retrieve(
                keyBytes,
                encryptionKeyBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to retrieve data from Occlum enclave with key '{key}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            byte[] actualResult = new byte[actualResultSize];
            Array.Copy(resultBytes, actualResult, actualResultSize);
            return actualResult;
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Gets the Occlum enclave attestation report.
    /// </summary>
    /// <returns>The attestation report as a JSON string.</returns>
    public string GetAttestationReport()
    {
        EnsureInitialized();

        byte[] resultBytes = new byte[8192]; // 8KB buffer for attestation
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumLibOS.occlum_get_attestation_report(
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to get attestation report from Occlum enclave. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Disposes the Occlum enclave wrapper.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the Occlum enclave wrapper.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources
                if (_initialized)
                {
                    NativeOcclumLibOS.occlum_destroy();
                    _initialized = false;
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Finalizer for the Occlum enclave wrapper.
    /// </summary>
    ~OcclumEnclaveWrapper()
    {
        Dispose(false);
    }

    // Implement remaining IEnclaveWrapper methods with Occlum-specific implementations
    public string GetData(string dataSource, string dataPath)
    {
        // Mock implementation for now
        return $"{{\"data\": \"mock_data\", \"source\": \"{dataSource}\", \"path\": \"{dataPath}\"}}";
    }

    public string ExecuteComputation(string computationId, string computationCode, string parameters)
    {
        // Use JavaScript execution for computations
        return ExecuteJavaScript(computationCode, parameters);
    }

    public string FetchOracleData(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json")
    {
        // Mock implementation for now
        return $"{{\"url\": \"{url}\", \"data\": \"mock_oracle_data\", \"format\": \"{outputFormat}\"}}";
    }

    public string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description)
    {
        // Mock implementation for now
        return $"{{\"keyId\": \"{keyId}\", \"keyType\": \"{keyType}\", \"created\": true}}";
    }

    public string DeleteData(string key)
    {
        // Mock implementation for now
        return $"{{\"key\": \"{key}\", \"deleted\": true}}";
    }

    public string GetStorageMetadata(string key)
    {
        // Mock implementation for now
        return $"{{\"key\": \"{key}\", \"size\": 1024, \"created\": \"2024-01-01T00:00:00Z\"}}";
    }

    public string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters = "{}")
    {
        // Mock implementation for now
        return $"{{\"modelId\": \"{modelId}\", \"modelType\": \"{modelType}\", \"trained\": true}}";
    }

    public (double[] predictions, string metadata) PredictWithAIModel(string modelId, double[] inputData)
    {
        // Mock implementation for now
        var predictions = new double[inputData.Length];
        for (int i = 0; i < inputData.Length; i++)
        {
            predictions[i] = inputData[i] * 1.1; // Simple mock prediction
        }
        var metadata = $"{{\"modelId\": \"{modelId}\", \"inputSize\": {inputData.Length}}}";
        return (predictions, metadata);
    }

    public string CreateAbstractAccount(string accountId, string accountData)
    {
        // Mock implementation for now
        return $"{{\"accountId\": \"{accountId}\", \"created\": true}}";
    }

    public string SignAbstractAccountTransaction(string accountId, string transactionData)
    {
        // Mock implementation for now
        return $"{{\"accountId\": \"{accountId}\", \"signed\": true, \"signature\": \"mock_signature\"}}";
    }

    public string AddAbstractAccountGuardian(string accountId, string guardianData)
    {
        // Mock implementation for now
        return $"{{\"accountId\": \"{accountId}\", \"guardianAdded\": true}}";
    }

    public byte[] Encrypt(byte[] data, byte[] key)
    {
        // Simple XOR encryption for mock implementation
        var encrypted = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            encrypted[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return encrypted;
    }

    public byte[] Decrypt(byte[] data, byte[] key)
    {
        // Simple XOR decryption for mock implementation
        return Encrypt(data, key); // XOR is symmetric
    }

    public byte[] Sign(byte[] data, byte[] key)
    {
        // Mock signature implementation
        var signature = new byte[32]; // Mock 32-byte signature
        for (int i = 0; i < signature.Length; i++)
        {
            signature[i] = (byte)((data[i % data.Length] + key[i % key.Length]) % 256);
        }
        return signature;
    }

    public bool Verify(byte[] data, byte[] signature, byte[] key)
    {
        // Mock verification implementation
        var expectedSignature = Sign(data, key);
        if (signature.Length != expectedSignature.Length) return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (signature[i] != expectedSignature[i]) return false;
        }
        return true;
    }
}

/// <summary>
/// Native Occlum LibOS function imports.
/// Provides P/Invoke declarations for Occlum LibOS operations.
/// </summary>
internal static class NativeOcclumLibOS
{
    private const string OcclumLibrary = "libocclum_pal.so";

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_init();

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_destroy();

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_execute_js(
        byte[] functionCode,
        UIntPtr functionCodeSize,
        byte[] args,
        UIntPtr argsSize,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_generate_random(
        int min,
        int max,
        IntPtr result);

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_generate_random_bytes(
        byte[] buffer,
        UIntPtr length);

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_storage_store(
        byte[] key,
        byte[] data,
        UIntPtr dataSize,
        byte[] encryptionKey,
        int compress,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_storage_retrieve(
        byte[] key,
        byte[] encryptionKey,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_get_attestation_report(
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);
}
