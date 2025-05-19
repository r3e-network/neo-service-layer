using System;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Interface for interacting with a Trusted Execution Environment (TEE).
    /// </summary>
    public interface ITeeInterface : IDisposable
    {
        /// <summary>
        /// Gets the MRENCLAVE measurement of the enclave.
        /// </summary>
        byte[] MrEnclave { get; }

        /// <summary>
        /// Gets the MRSIGNER measurement of the enclave.
        /// </summary>
        byte[] MrSigner { get; }

        /// <summary>
        /// Gets the product ID of the enclave.
        /// </summary>
        int ProductId { get; }

        /// <summary>
        /// Gets the security version of the enclave.
        /// </summary>
        int SecurityVersion { get; }

        /// <summary>
        /// Gets the attributes of the enclave.
        /// </summary>
        int Attributes { get; }

        /// <summary>
        /// Initializes the interface.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        Task<JavaScriptExecutionResult> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Gets an attestation report for the enclave.
        /// </summary>
        /// <param name="reportData">Optional data to include in the report.</param>
        /// <returns>The attestation report.</returns>
        byte[] GetAttestationReport(byte[] reportData);

        /// <summary>
        /// Seals data using the enclave's sealing key.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        byte[] SealData(byte[] data);

        /// <summary>
        /// Unseals data using the enclave's sealing key.
        /// </summary>
        /// <param name="sealedData">The sealed data to unseal.</param>
        /// <returns>The unsealed data.</returns>
        byte[] UnsealData(byte[] sealedData);
    }
}
