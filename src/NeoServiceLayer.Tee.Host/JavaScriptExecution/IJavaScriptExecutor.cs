using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Tee.Host.JavaScriptExecution
{
    /// <summary>
    /// Interface for executing JavaScript code in an enclave.
    /// </summary>
    public interface IJavaScriptExecutor
    {
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
        /// Creates a new JavaScript context in the enclave.
        /// </summary>
        /// <returns>The ID of the created context.</returns>
        Task<ulong> CreateJavaScriptContextAsync();

        /// <summary>
        /// Destroys a JavaScript context in the enclave.
        /// </summary>
        /// <param name="contextId">The ID of the context to destroy.</param>
        Task DestroyJavaScriptContextAsync(ulong contextId);
    }
}
