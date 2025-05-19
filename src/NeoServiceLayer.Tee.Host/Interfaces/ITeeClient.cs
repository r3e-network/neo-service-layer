using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Interfaces
{
    /// <summary>
    /// Interface for communicating with the Trusted Execution Environment (TEE).
    /// </summary>
    public interface ITeeClient
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
        /// Executes JavaScript code in the TEE with user's secrets.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <param name="functionId">The ID of the function being executed.</param>
        /// <returns>The result of the JavaScript execution as a JSON string.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string userId, string functionId);

        /// <summary>
        /// Executes JavaScript code in the TEE with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <param name="functionId">The ID of the function being executed.</param>
        /// <returns>A tuple containing the result of the JavaScript execution as a JSON string and the gas used.</returns>
        Task<(string Result, ulong GasUsed)> ExecuteJavaScriptWithGasAsync(string code, string input, string userId, string functionId);

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
        /// Lists all user secrets in the TEE.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An array of secret names.</returns>
        Task<string[]> ListUserSecretsAsync(string userId);

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
        /// Sends a message to the TEE.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response from the TEE.</returns>
        Task<string> SendMessageAsync(string message);
    }
}
