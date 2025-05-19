using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.JavaScriptExecution
{
    /// <summary>
    /// Interface for executing JavaScript code in a trusted execution environment.
    /// </summary>
    public interface IJavaScriptExecution
    {
        /// <summary>
        /// Gets the ID of the current function being executed.
        /// </summary>
        string CurrentFunctionId { get; }

        /// <summary>
        /// Gets the ID of the current user executing the function.
        /// </summary>
        string CurrentUserId { get; }

        /// <summary>
        /// Initializes the JavaScript execution environment.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);
        
        /// <summary>
        /// Executes JavaScript code in the enclave with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>A tuple containing the result of the JavaScript execution and the gas used.</returns>
        Task<(string Result, ulong GasUsed)> ExecuteJavaScriptWithGasAsync(string code, string input, string secrets, string functionId, string userId);
    }
} 