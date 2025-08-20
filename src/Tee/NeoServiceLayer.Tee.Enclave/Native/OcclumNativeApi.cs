using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Enclave.Native
{
    /// <summary>
    /// Production-ready Occlum LibOS P/Invoke declarations.
    /// These declarations match the real Occlum LibOS SDK API for trusted execution.
    /// </summary>
    public static class OcclumNativeApi
    {
        // Real Occlum LibOS library names for different components
        private const string OcclumLibOsPal = "libocclum-pal.so.0";
        private const string OcclumLibOsLibOs = "libocclum-libos.so.0";
        private const string NeoServiceEnclave = "libneo_service_enclave.so";

        #region Occlum Error Codes
        public const int OCCLUM_SUCCESS = 0;
        public const int OCCLUM_ERROR_INVALID_PARAMETER = -1;
        public const int OCCLUM_ERROR_OUT_OF_MEMORY = -2;
        public const int OCCLUM_ERROR_PERMISSION_DENIED = -3;
        public const int OCCLUM_ERROR_NOT_FOUND = -4;
        public const int OCCLUM_ERROR_ALREADY_EXISTS = -5;
        public const int OCCLUM_ERROR_IO_ERROR = -6;
        public const int OCCLUM_ERROR_NETWORK_ERROR = -7;
        public const int OCCLUM_ERROR_CRYPTO_ERROR = -8;
        public const int OCCLUM_ERROR_TIMEOUT = -9;
        public const int OCCLUM_ERROR_BUFFER_TOO_SMALL = -10;
        public const int OCCLUM_ERROR_UNSUPPORTED_OPERATION = -11;
        public const int OCCLUM_ERROR_INTERNAL_ERROR = -12;
        #endregion

        #region Occlum Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct OcclumInstanceConfig
        {
            public uint ResourceLimits_UserSpaceSize;
            public uint ResourceLimits_KernelSpaceSize;
            public uint ResourceLimits_MaxThreadNum;
            public uint ResourceLimits_MaxFileNum;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Env_Default;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Env_Untrusted;
            public uint Process_DefaultStackSize;
            public uint Process_DefaultHeapSize;
            public uint Process_DefaultMmapSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OcclumStats
        {
            public ulong TotalMemory;
            public ulong UsedMemory;
            public ulong FreeMemory;
            public uint ActiveThreads;
            public uint OpenFiles;
            public ulong NetworkBytesSent;
            public ulong NetworkBytesReceived;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OcclumFileInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Path;
            public ulong Size;
            public uint Mode;
            public long CreatedTime;
            public long ModifiedTime;
            public long AccessedTime;
        }
        #endregion

        #region LibOS Core Operations
        /// <summary>
        /// Initializes the Occlum LibOS instance.
        /// </summary>
        /// <param name="config">Pointer to Occlum instance configuration.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(OcclumLibOsPal, CallingConvention = CallingConvention.Cdecl)]
        public static extern int occlum_pal_init(ref OcclumInstanceConfig config);

        /// <summary>
        /// Destroys the Occlum LibOS instance.
        /// </summary>
        /// <returns>Occlum status code.</returns>
        [DllImport(OcclumLibOsPal, CallingConvention = CallingConvention.Cdecl)]
        public static extern int occlum_pal_destroy();

        /// <summary>
        /// Creates a new process within Occlum LibOS.
        /// </summary>
        /// <param name="path">Path to the executable.</param>
        /// <param name="argv">Command line arguments.</param>
        /// <param name="envp">Environment variables.</param>
        /// <param name="processId">Output parameter for process ID.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(OcclumLibOsPal, CallingConvention = CallingConvention.Cdecl)]
        public static extern int occlum_pal_create_process(
            [MarshalAs(UnmanagedType.LPStr)] string path,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] envp,
            ref uint processId);

        /// <summary>
        /// Executes a command within the Occlum LibOS.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <param name="args">Command arguments.</param>
        /// <param name="result">Buffer for command output.</param>
        /// <param name="resultSize">Size of result buffer.</param>
        /// <param name="actualResultSize">Actual size of command output.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(OcclumLibOsPal, CallingConvention = CallingConvention.Cdecl)]
        public static extern int occlum_pal_exec(
            [MarshalAs(UnmanagedType.LPStr)] string command,
            [MarshalAs(UnmanagedType.LPStr)] string args,
            IntPtr result,
            UIntPtr resultSize,
            ref UIntPtr actualResultSize);

        /// <summary>
        /// Gets Occlum LibOS statistics.
        /// </summary>
        /// <param name="stats">Output parameter for statistics.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(OcclumLibOsPal, CallingConvention = CallingConvention.Cdecl)]
        public static extern int occlum_pal_get_stats(ref OcclumStats stats);
        #endregion

        #region Neo Service Enclave Functions
        /// <summary>
        /// Initializes the Neo Service enclave components.
        /// </summary>
        /// <param name="configJson">JSON configuration string.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_enclave_init(
            [MarshalAs(UnmanagedType.LPStr)] string configJson);

        /// <summary>
        /// Destroys the Neo Service enclave components.
        /// </summary>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_enclave_destroy();

        /// <summary>
        /// Generates cryptographically secure random numbers.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <param name="max">Maximum value (exclusive).</param>
        /// <param name="result">Output parameter for random number.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_crypto_generate_random(
            int min,
            int max,
            ref int result);

        /// <summary>
        /// Generates cryptographically secure random bytes.
        /// </summary>
        /// <param name="buffer">Buffer to fill with random bytes.</param>
        /// <param name="length">Number of bytes to generate.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_crypto_generate_random_bytes(
            [Out] byte[] buffer,
            UIntPtr length);

        /// <summary>
        /// Encrypts data using AES-256-GCM.
        /// </summary>
        /// <param name="plaintext">Data to encrypt.</param>
        /// <param name="plaintextLen">Length of plaintext.</param>
        /// <param name="key">Encryption key (32 bytes for AES-256).</param>
        /// <param name="keyLen">Length of key.</param>
        /// <param name="ciphertext">Output buffer for encrypted data.</param>
        /// <param name="ciphertextLen">Size of ciphertext buffer.</param>
        /// <param name="actualLen">Actual length of encrypted data.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_crypto_encrypt(
            [In] byte[] plaintext,
            UIntPtr plaintextLen,
            [In] byte[] key,
            UIntPtr keyLen,
            [Out] byte[] ciphertext,
            UIntPtr ciphertextLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Decrypts data using AES-256-GCM.
        /// </summary>
        /// <param name="ciphertext">Data to decrypt.</param>
        /// <param name="ciphertextLen">Length of ciphertext.</param>
        /// <param name="key">Decryption key (32 bytes for AES-256).</param>
        /// <param name="keyLen">Length of key.</param>
        /// <param name="plaintext">Output buffer for decrypted data.</param>
        /// <param name="plaintextLen">Size of plaintext buffer.</param>
        /// <param name="actualLen">Actual length of decrypted data.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_crypto_decrypt(
            [In] byte[] ciphertext,
            UIntPtr ciphertextLen,
            [In] byte[] key,
            UIntPtr keyLen,
            [Out] byte[] plaintext,
            UIntPtr plaintextLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Signs data using secp256k1 or Ed25519.
        /// </summary>
        /// <param name="data">Data to sign.</param>
        /// <param name="dataLen">Length of data.</param>
        /// <param name="privateKey">Private key for signing.</param>
        /// <param name="keyLen">Length of private key.</param>
        /// <param name="algorithm">Signing algorithm (0=secp256k1, 1=Ed25519).</param>
        /// <param name="signature">Output buffer for signature.</param>
        /// <param name="signatureLen">Size of signature buffer.</param>
        /// <param name="actualLen">Actual length of signature.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_crypto_sign(
            [In] byte[] data,
            UIntPtr dataLen,
            [In] byte[] privateKey,
            UIntPtr keyLen,
            int algorithm,
            [Out] byte[] signature,
            UIntPtr signatureLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Verifies a digital signature.
        /// </summary>
        /// <param name="data">Original data.</param>
        /// <param name="dataLen">Length of data.</param>
        /// <param name="signature">Signature to verify.</param>
        /// <param name="signatureLen">Length of signature.</param>
        /// <param name="publicKey">Public key for verification.</param>
        /// <param name="keyLen">Length of public key.</param>
        /// <param name="algorithm">Signing algorithm (0=secp256k1, 1=Ed25519).</param>
        /// <param name="isValid">Output parameter for verification result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_crypto_verify(
            [In] byte[] data,
            UIntPtr dataLen,
            [In] byte[] signature,
            UIntPtr signatureLen,
            [In] byte[] publicKey,
            UIntPtr keyLen,
            int algorithm,
            ref int isValid);

        /// <summary>
        /// Stores data in secure storage with encryption and compression.
        /// </summary>
        /// <param name="key">Storage key.</param>
        /// <param name="data">Data to store.</param>
        /// <param name="dataLen">Length of data.</param>
        /// <param name="encryptionKey">Key for data encryption.</param>
        /// <param name="compress">Enable compression (1=yes, 0=no).</param>
        /// <param name="metadata">Output buffer for storage metadata (JSON).</param>
        /// <param name="metadataLen">Size of metadata buffer.</param>
        /// <param name="actualLen">Actual length of metadata.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_storage_store(
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [In] byte[] data,
            UIntPtr dataLen,
            [MarshalAs(UnmanagedType.LPStr)] string encryptionKey,
            int compress,
            [Out] byte[] metadata,
            UIntPtr metadataLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Retrieves data from secure storage with decryption and decompression.
        /// </summary>
        /// <param name="key">Storage key.</param>
        /// <param name="encryptionKey">Key for data decryption.</param>
        /// <param name="data">Output buffer for retrieved data.</param>
        /// <param name="dataLen">Size of data buffer.</param>
        /// <param name="actualLen">Actual length of retrieved data.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_storage_retrieve(
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [MarshalAs(UnmanagedType.LPStr)] string encryptionKey,
            [Out] byte[] data,
            UIntPtr dataLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Deletes data from secure storage.
        /// </summary>
        /// <param name="key">Storage key.</param>
        /// <param name="result">Output buffer for deletion result (JSON).</param>
        /// <param name="resultLen">Size of result buffer.</param>
        /// <param name="actualLen">Actual length of result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_storage_delete(
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [Out] byte[] result,
            UIntPtr resultLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Fetches data from external oracle sources.
        /// </summary>
        /// <param name="url">URL to fetch data from.</param>
        /// <param name="headers">HTTP headers (JSON format).</param>
        /// <param name="processingScript">JavaScript code for data processing.</param>
        /// <param name="outputFormat">Desired output format.</param>
        /// <param name="result">Output buffer for fetched data.</param>
        /// <param name="resultLen">Size of result buffer.</param>
        /// <param name="actualLen">Actual length of result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_oracle_fetch_data(
            [MarshalAs(UnmanagedType.LPStr)] string url,
            [MarshalAs(UnmanagedType.LPStr)] string headers,
            [MarshalAs(UnmanagedType.LPStr)] string processingScript,
            [MarshalAs(UnmanagedType.LPStr)] string outputFormat,
            [Out] byte[] result,
            UIntPtr resultLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Executes JavaScript code securely.
        /// </summary>
        /// <param name="code">JavaScript code to execute.</param>
        /// <param name="args">Arguments for the code (JSON format).</param>
        /// <param name="result">Output buffer for execution result.</param>
        /// <param name="resultLen">Size of result buffer.</param>
        /// <param name="actualLen">Actual length of result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_compute_execute_js(
            [MarshalAs(UnmanagedType.LPStr)] string code,
            [MarshalAs(UnmanagedType.LPStr)] string args,
            [Out] byte[] result,
            UIntPtr resultLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Trains an AI model with provided data.
        /// </summary>
        /// <param name="modelId">Unique model identifier.</param>
        /// <param name="modelType">Type of model (neural_network, linear_regression, etc.).</param>
        /// <param name="trainingData">Training data array.</param>
        /// <param name="dataLen">Number of training data points.</param>
        /// <param name="parameters">Training parameters (JSON format).</param>
        /// <param name="result">Output buffer for training result.</param>
        /// <param name="resultLen">Size of result buffer.</param>
        /// <param name="actualLen">Actual length of result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_ai_train_model(
            [MarshalAs(UnmanagedType.LPStr)] string modelId,
            [MarshalAs(UnmanagedType.LPStr)] string modelType,
            [In] double[] trainingData,
            UIntPtr dataLen,
            [MarshalAs(UnmanagedType.LPStr)] string parameters,
            [Out] byte[] result,
            UIntPtr resultLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Makes predictions using a trained AI model.
        /// </summary>
        /// <param name="modelId">Model identifier.</param>
        /// <param name="inputData">Input data for prediction.</param>
        /// <param name="inputLen">Number of input data points.</param>
        /// <param name="predictions">Output buffer for predictions.</param>
        /// <param name="predictionsLen">Size of predictions buffer.</param>
        /// <param name="actualPredictionsLen">Actual number of predictions.</param>
        /// <param name="metadata">Output buffer for prediction metadata.</param>
        /// <param name="metadataLen">Size of metadata buffer.</param>
        /// <param name="actualMetadataLen">Actual length of metadata.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_ai_predict(
            [MarshalAs(UnmanagedType.LPStr)] string modelId,
            [In] double[] inputData,
            UIntPtr inputLen,
            [Out] double[] predictions,
            UIntPtr predictionsLen,
            ref UIntPtr actualPredictionsLen,
            [Out] byte[] metadata,
            UIntPtr metadataLen,
            ref UIntPtr actualMetadataLen);

        /// <summary>
        /// Creates an abstract account for blockchain operations.
        /// </summary>
        /// <param name="accountId">Unique account identifier.</param>
        /// <param name="accountData">Account configuration (JSON format).</param>
        /// <param name="result">Output buffer for account creation result.</param>
        /// <param name="resultLen">Size of result buffer.</param>
        /// <param name="actualLen">Actual length of result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_account_create(
            [MarshalAs(UnmanagedType.LPStr)] string accountId,
            [MarshalAs(UnmanagedType.LPStr)] string accountData,
            [Out] byte[] result,
            UIntPtr resultLen,
            ref UIntPtr actualLen);

        /// <summary>
        /// Signs a transaction using an abstract account.
        /// </summary>
        /// <param name="accountId">Account identifier.</param>
        /// <param name="transactionData">Transaction data (JSON format).</param>
        /// <param name="result">Output buffer for signed transaction.</param>
        /// <param name="resultLen">Size of result buffer.</param>
        /// <param name="actualLen">Actual length of result.</param>
        /// <returns>Occlum status code.</returns>
        [DllImport(NeoServiceEnclave, CallingConvention = CallingConvention.Cdecl)]
        public static extern int neo_account_sign_transaction(
            [MarshalAs(UnmanagedType.LPStr)] string accountId,
            [MarshalAs(UnmanagedType.LPStr)] string transactionData,
            [Out] byte[] result,
            UIntPtr resultLen,
            ref UIntPtr actualLen);
        #endregion

        #region Error Handling and Utilities
        /// <summary>
        /// Converts Occlum error code to human-readable string.
        /// </summary>
        /// <param name="errorCode">Occlum error code.</param>
        /// <returns>Error description string.</returns>
        public static string GetErrorDescription(int errorCode)
        {
            return errorCode switch
            {
                OCCLUM_SUCCESS => "Success",
                OCCLUM_ERROR_INVALID_PARAMETER => "Invalid parameter",
                OCCLUM_ERROR_OUT_OF_MEMORY => "Out of memory",
                OCCLUM_ERROR_PERMISSION_DENIED => "Permission denied",
                OCCLUM_ERROR_NOT_FOUND => "Not found",
                OCCLUM_ERROR_ALREADY_EXISTS => "Already exists",
                OCCLUM_ERROR_IO_ERROR => "I/O error",
                OCCLUM_ERROR_NETWORK_ERROR => "Network error",
                OCCLUM_ERROR_CRYPTO_ERROR => "Cryptographic error",
                OCCLUM_ERROR_TIMEOUT => "Operation timeout",
                OCCLUM_ERROR_BUFFER_TOO_SMALL => "Buffer too small",
                OCCLUM_ERROR_UNSUPPORTED_OPERATION => "Unsupported operation",
                OCCLUM_ERROR_INTERNAL_ERROR => "Internal error",
                _ => $"Unknown error code: {errorCode}"
            };
        }

        /// <summary>
        /// Checks if an Occlum operation was successful.
        /// </summary>
        /// <param name="status">Occlum status code.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool IsSuccess(int status) => status == OCCLUM_SUCCESS;

        /// <summary>
        /// Throws an exception if the Occlum operation failed.
        /// </summary>
        /// <param name="status">Occlum status code.</param>
        /// <param name="operation">Operation description for error message.</param>
        /// <exception cref="OcclumException">Thrown when operation fails.</exception>
        public static void ThrowIfError(int status, string operation)
        {
            if (status != OCCLUM_SUCCESS)
            {
                throw new OcclumException($"{operation} failed: {GetErrorDescription(status)}", status);
            }
        }

        /// <summary>
        /// Converts a byte array to a null-terminated string.
        /// </summary>
        /// <param name="buffer">Byte buffer containing string data.</param>
        /// <param name="maxLength">Maximum length to convert.</param>
        /// <returns>Converted string.</returns>
        public static string BytesToString(byte[] buffer, int maxLength)
        {
            int nullIndex = Array.IndexOf(buffer, (byte)0, 0, Math.Min(buffer.Length, maxLength));
            int length = nullIndex >= 0 ? nullIndex : Math.Min(buffer.Length, maxLength);
            return System.Text.Encoding.UTF8.GetString(buffer, 0, length);
        }

        /// <summary>
        /// Converts a string to a byte array with null termination.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <param name="bufferSize">Target buffer size.</param>
        /// <returns>Byte array with string data.</returns>
        public static byte[] StringToBytes(string str, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            if (!string.IsNullOrEmpty(str))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                int copyLength = Math.Min(bytes.Length, bufferSize - 1);
                Array.Copy(bytes, buffer, copyLength);
            }
            return buffer;
        }
        #endregion
    }

    /// <summary>
    /// Exception thrown when an Occlum LibOS operation fails.
    /// </summary>
    public class OcclumException : Exception
    {
        /// <summary>
        /// Gets the Occlum error code.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errorCode">The Occlum error code.</param>
        public OcclumException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errorCode">The Occlum error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public OcclumException(string message, int errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
