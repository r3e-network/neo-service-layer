using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Interfaces
{
    /// <summary>
    /// Interface for interacting with the Trusted Execution Environment (TEE).
    /// </summary>
    public interface ITeeService
    {
        /// <summary>
        /// Initializes the TEE.
        /// </summary>
        /// <returns>True if the initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Gets the status of the TEE.
        /// </summary>
        /// <returns>The status of the TEE as a JSON string.</returns>
        Task<string> GetStatusAsync();

        /// <summary>
        /// Creates a JavaScript context in the TEE.
        /// </summary>
        /// <returns>The context ID.</returns>
        Task<ulong> CreateJavaScriptContextAsync();

        /// <summary>
        /// Destroys a JavaScript context in the TEE.
        /// </summary>
        /// <param name="contextId">The context ID.</param>
        /// <returns>True if the context was destroyed, false otherwise.</returns>
        Task<bool> DestroyJavaScriptContextAsync(ulong contextId);

        /// <summary>
        /// Executes JavaScript code in the TEE.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function being executed.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution as a JSON string.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Stores a user secret in the TEE.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="secretValue">The value of the secret.</param>
        /// <returns>True if the secret was stored, false otherwise.</returns>
        Task<bool> StoreUserSecretAsync(string userId, string secretName, string secretValue);

        /// <summary>
        /// Gets a user secret from the TEE.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <returns>The value of the secret.</returns>
        Task<string> GetUserSecretAsync(string userId, string secretName);

        /// <summary>
        /// Deletes a user secret from the TEE.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <returns>True if the secret was deleted, false otherwise.</returns>
        Task<bool> DeleteUserSecretAsync(string userId, string secretName);

        /// <summary>
        /// Generates random bytes in the TEE.
        /// </summary>
        /// <param name="length">The number of random bytes to generate.</param>
        /// <returns>The random bytes.</returns>
        Task<byte[]> GenerateRandomBytesAsync(int length);

        /// <summary>
        /// Signs data in the TEE.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <returns>The signature.</returns>
        Task<byte[]> SignDataAsync(byte[] data);

        /// <summary>
        /// Verifies a signature in the TEE.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> VerifySignatureAsync(byte[] data, byte[] signature);

        /// <summary>
        /// Seals data in the TEE.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        Task<byte[]> SealDataAsync(byte[] data);

        /// <summary>
        /// Unseals data in the TEE.
        /// </summary>
        /// <param name="sealedData">The sealed data.</param>
        /// <returns>The unsealed data.</returns>
        Task<byte[]> UnsealDataAsync(byte[] sealedData);

        /// <summary>
        /// Initializes the JavaScript executor in the TEE.
        /// </summary>
        /// <returns>True if the initialization was successful, false otherwise.</returns>
        Task<bool> InitializeJavaScriptExecutorAsync();

        /// <summary>
        /// Executes JavaScript code using the new executor in the TEE.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="filename">The filename for error reporting.</param>
        /// <returns>The result of the JavaScript execution as a string.</returns>
        Task<string> ExecuteJavaScriptCodeAsync(string code, string filename);

        /// <summary>
        /// Executes a JavaScript function using the new executor in the TEE.
        /// </summary>
        /// <param name="functionName">The name of the function to execute.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The result of the JavaScript function execution as a string.</returns>
        Task<string> ExecuteJavaScriptFunctionAsync(string functionName, IEnumerable<string> args);

        /// <summary>
        /// Collects JavaScript garbage in the TEE.
        /// </summary>
        /// <returns>True if the garbage collection was successful, false otherwise.</returns>
        Task<bool> CollectJavaScriptGarbageAsync();

        /// <summary>
        /// Shuts down the JavaScript executor in the TEE.
        /// </summary>
        /// <returns>True if the shutdown was successful, false otherwise.</returns>
        Task<bool> ShutdownJavaScriptExecutorAsync();
    }
}
