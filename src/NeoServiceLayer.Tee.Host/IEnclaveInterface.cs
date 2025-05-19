using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Interface for enclave operations
    /// </summary>
    public interface IEnclaveInterface : IDisposable
    {
        /// <summary>
        /// Gets the enclave ID
        /// </summary>
        /// <returns>The enclave ID</returns>
        IntPtr GetEnclaveId();

        /// <summary>
        /// Gets the MRENCLAVE value
        /// </summary>
        /// <returns>The MRENCLAVE value</returns>
        byte[] GetMrEnclave();

        /// <summary>
        /// Gets the MRSIGNER value
        /// </summary>
        /// <returns>The MRSIGNER value</returns>
        byte[] GetMrSigner();

        /// <summary>
        /// Executes JavaScript code in the enclave
        /// </summary>
        /// <param name="code">The JavaScript code to execute</param>
        /// <param name="input">The input data as JSON</param>
        /// <param name="secrets">The secrets as JSON</param>
        /// <param name="functionId">The function ID</param>
        /// <param name="userId">The user ID</param>
        /// <returns>The result as JSON</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Gets random bytes from the enclave
        /// </summary>
        /// <param name="length">The number of random bytes to get</param>
        /// <returns>The random bytes</returns>
        byte[] GetRandomBytes(int length);

        /// <summary>
        /// Signs data using the enclave's private key
        /// </summary>
        /// <param name="data">The data to sign</param>
        /// <returns>The signature</returns>
        byte[] SignData(byte[] data);

        /// <summary>
        /// Verifies a signature using the enclave's public key
        /// </summary>
        /// <param name="data">The data that was signed</param>
        /// <param name="signature">The signature to verify</param>
        /// <returns>True if the signature is valid, false otherwise</returns>
        bool VerifySignature(byte[] data, byte[] signature);

        /// <summary>
        /// Seals data using the enclave's sealing key
        /// </summary>
        /// <param name="data">The data to seal</param>
        /// <returns>The sealed data</returns>
        byte[] SealData(byte[] data);

        /// <summary>
        /// Unseals data using the enclave's sealing key
        /// </summary>
        /// <param name="sealedData">The sealed data</param>
        /// <returns>The unsealed data</returns>
        byte[] UnsealData(byte[] sealedData);

        /// <summary>
        /// Records execution metrics
        /// </summary>
        /// <param name="functionId">The function ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="gasUsed">The amount of gas used</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed);

        /// <summary>
        /// Records execution failure
        /// </summary>
        /// <param name="functionId">The function ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage);
    }
}
