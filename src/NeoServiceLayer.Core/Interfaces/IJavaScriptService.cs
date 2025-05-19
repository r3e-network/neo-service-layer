using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for executing JavaScript code in the TEE.
    /// </summary>
    public interface IJavaScriptService
    {
        /// <summary>
        /// Executes JavaScript code in the TEE.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function being executed.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution as a JSON string.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, object input, string functionId, string userId);

        /// <summary>
        /// Initializes the JavaScript executor.
        /// </summary>
        /// <returns>True if the initialization was successful, false otherwise.</returns>
        Task<bool> InitializeJavaScriptExecutorAsync();

        /// <summary>
        /// Executes JavaScript code using the new executor.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="filename">The filename for error reporting.</param>
        /// <returns>The result of the JavaScript execution as a string.</returns>
        Task<string> ExecuteJavaScriptCodeAsync(string code, string filename);

        /// <summary>
        /// Executes a JavaScript function using the new executor.
        /// </summary>
        /// <param name="functionName">The name of the function to execute.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The result of the JavaScript function execution as a string.</returns>
        Task<string> ExecuteJavaScriptFunctionAsync(string functionName, IEnumerable<string> args);

        /// <summary>
        /// Collects JavaScript garbage.
        /// </summary>
        /// <returns>True if the garbage collection was successful, false otherwise.</returns>
        Task<bool> CollectJavaScriptGarbageAsync();

        /// <summary>
        /// Shuts down the JavaScript executor.
        /// </summary>
        /// <returns>True if the shutdown was successful, false otherwise.</returns>
        Task<bool> ShutdownJavaScriptExecutorAsync();
    }
}
