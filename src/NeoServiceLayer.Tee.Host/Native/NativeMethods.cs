using System;
using System.Runtime.InteropServices;

namespace NeoServiceLayer.Tee.Host.Native
{
    /// <summary>
    /// Native methods for interacting with the Open Enclave enclave.
    /// </summary>
    internal static class NativeMethods
    {
        private const string LibraryName = "NeoServiceLayerHost";

        /// <summary>
        /// Initializes the enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave.</param>
        /// <param name="flags">The initialization flags.</param>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_initialize_enclave", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_initialize_enclave(
            [MarshalAs(UnmanagedType.LPStr)] string enclavePath,
            uint flags,
            out IntPtr enclaveHandle);

        /// <summary>
        /// Cleans up the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_cleanup_enclave", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_cleanup_enclave(IntPtr enclaveHandle);

        /// <summary>
        /// Processes a message in the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <param name="message">The message to process.</param>
        /// <param name="messageSize">The size of the message.</param>
        /// <param name="response">The response from the enclave.</param>
        /// <param name="responseSize">The size of the response.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_process_message", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_process_message(
            IntPtr enclaveHandle,
            [In] byte[] message,
            int messageSize,
            out IntPtr response,
            out int responseSize);

        /// <summary>
        /// Frees a response from the enclave.
        /// </summary>
        /// <param name="response">The response to free.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_free_response", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_free_response(IntPtr response);

        /// <summary>
        /// Generates random bytes in the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <param name="buffer">The buffer to fill with random bytes.</param>
        /// <param name="bufferSize">The size of the buffer.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_generate_random_bytes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_generate_random_bytes(
            IntPtr enclaveHandle,
            [Out] byte[] buffer,
            int bufferSize);

        /// <summary>
        /// Signs data in the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <param name="data">The data to sign.</param>
        /// <param name="dataSize">The size of the data.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="signatureSize">The size of the signature.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_sign_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_sign_data(
            IntPtr enclaveHandle,
            [In] byte[] data,
            int dataSize,
            out IntPtr signature,
            out int signatureSize);

        /// <summary>
        /// Verifies a signature in the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <param name="data">The data that was signed.</param>
        /// <param name="dataSize">The size of the data.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="signatureSize">The size of the signature.</param>
        /// <param name="isValid">Whether the signature is valid.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_verify_signature", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_verify_signature(
            IntPtr enclaveHandle,
            [In] byte[] data,
            int dataSize,
            [In] byte[] signature,
            int signatureSize,
            [MarshalAs(UnmanagedType.Bool)] ref bool isValid);

        /// <summary>
        /// Seals data in the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <param name="data">The data to seal.</param>
        /// <param name="dataSize">The size of the data.</param>
        /// <param name="sealedData">The sealed data.</param>
        /// <param name="sealedDataSize">The size of the sealed data.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_seal_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_seal_data(
            IntPtr enclaveHandle,
            [In] byte[] data,
            int dataSize,
            out IntPtr sealedData,
            out int sealedDataSize);

        /// <summary>
        /// Unseals data in the enclave.
        /// </summary>
        /// <param name="enclaveHandle">The enclave handle.</param>
        /// <param name="sealedData">The sealed data.</param>
        /// <param name="sealedDataSize">The size of the sealed data.</param>
        /// <param name="data">The unsealed data.</param>
        /// <param name="dataSize">The size of the unsealed data.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        [DllImport(LibraryName, EntryPoint = "host_unseal_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int host_unseal_data(
            IntPtr enclaveHandle,
            [In] byte[] sealedData,
            int sealedDataSize,
            out IntPtr data,
            out int dataSize);
    }
}
