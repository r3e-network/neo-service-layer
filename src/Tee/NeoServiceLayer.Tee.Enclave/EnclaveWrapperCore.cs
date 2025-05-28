using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Core implementation of the Occlum LibOS enclave wrapper.
/// Provides secure operations using Occlum LibOS trusted execution environment.
/// </summary>
public partial class EnclaveWrapper : IEnclaveWrapper
{
    private bool _disposed;
    private bool _initialized;
    private readonly ConcurrentDictionary<string, string> _secureStorage = new();
    private readonly ILogger<EnclaveWrapper> _logger;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveWrapper"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public EnclaveWrapper(ILogger<EnclaveWrapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _disposed = false;
        _initialized = false;
    }

    /// <summary>
    /// Initializes the enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    public bool Initialize()
    {
        if (_initialized)
        {
            return true;
        }

        int result = NativeOcclumEnclave.occlum_enclave_init();
        _initialized = result == 0;
        return _initialized;
    }

    /// <summary>
    /// Ensures the enclave is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the enclave is not initialized.</exception>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Enclave is not initialized. Call Initialize() first.");
        }
    }

    /// <summary>
    /// Disposes the enclave wrapper.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the enclave wrapper.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }

            // Dispose unmanaged resources
            if (_initialized)
            {
                NativeOcclumEnclave.occlum_enclave_destroy();
                _initialized = false;
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer for the enclave wrapper.
    /// </summary>
    ~EnclaveWrapper()
    {
        Dispose(false);
    }
}

/// <summary>
/// Native Occlum LibOS enclave function imports.
/// Provides P/Invoke declarations for Occlum LibOS enclave operations.
/// </summary>
internal static class NativeOcclumEnclave
{
    private const string OcclumEnclaveLibrary = "occlum_enclave_interface";

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_enclave_init();

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_enclave_destroy();

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_execute_js(
        byte[] functionCode,
        UIntPtr functionCodeSize,
        byte[] args,
        UIntPtr argsSize,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_get_data(
        byte[] dataSource,
        UIntPtr dataSourceSize,
        byte[] dataPath,
        UIntPtr dataPathSize,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_generate_random(
        int min,
        int max,
        IntPtr result);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_generate_random_bytes(
        byte[] buffer,
        UIntPtr length);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_kms_generate_key(
        byte[] keyId,
        byte[] keyType,
        byte[] keyUsage,
        int exportable,
        byte[] description,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_oracle_fetch_data(
        byte[] url,
        byte[] headers,
        byte[] processingScript,
        byte[] outputFormat,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_compute_execute(
        byte[] computationId,
        byte[] computationCode,
        byte[] parameters,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_storage_store(
        byte[] key,
        byte[] data,
        UIntPtr dataSize,
        byte[] encryptionKey,
        int compress,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_storage_retrieve(
        byte[] key,
        byte[] encryptionKey,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_storage_delete(
        byte[] key,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_storage_get_metadata(
        byte[] key,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_ai_train_model(
        byte[] modelId,
        byte[] modelType,
        double[] trainingData,
        UIntPtr dataSize,
        byte[] parameters,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_ai_predict(
        byte[] modelId,
        double[] inputData,
        UIntPtr inputSize,
        double[] outputData,
        UIntPtr outputSize,
        IntPtr actualOutputSize,
        byte[] resultMetadata,
        UIntPtr metadataSize,
        IntPtr actualMetadataSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_account_create(
        byte[] accountId,
        byte[] accountData,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_account_sign_transaction(
        byte[] accountId,
        byte[] transactionData,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);

    [DllImport(OcclumEnclaveLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int occlum_account_add_guardian(
        byte[] accountId,
        byte[] guardianData,
        byte[] result,
        UIntPtr resultSize,
        IntPtr actualResultSize);
}

/// <summary>
/// Exception thrown when an enclave operation fails.
/// </summary>
public class EnclaveException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public EnclaveException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EnclaveException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
