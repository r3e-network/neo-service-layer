using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for interacting with the Trusted Execution Environment (TEE).
    /// </summary>
    public interface ITeeService
    {
        /// <summary>
        /// Initializes the JavaScript executor in the TEE.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeJavaScriptExecutorAsync();

        /// <summary>
        /// Executes JavaScript code in the TEE.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Executes JavaScript code in the TEE.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="filename">The filename for the JavaScript code.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        Task<string> ExecuteJavaScriptCodeAsync(string code, string filename);

        /// <summary>
        /// Executes a JavaScript function in the TEE.
        /// </summary>
        /// <param name="functionName">The name of the function to execute.</param>
        /// <param name="args">The arguments for the function.</param>
        /// <returns>The result of the JavaScript function execution.</returns>
        Task<string> ExecuteJavaScriptFunctionAsync(string functionName, IEnumerable<string> args);

        /// <summary>
        /// Collects JavaScript garbage in the TEE.
        /// </summary>
        /// <returns>True if garbage collection was successful, false otherwise.</returns>
        Task<bool> CollectJavaScriptGarbageAsync();

        /// <summary>
        /// Shuts down the JavaScript executor in the TEE.
        /// </summary>
        /// <returns>True if shutdown was successful, false otherwise.</returns>
        Task<bool> ShutdownJavaScriptExecutorAsync();
    }
}
