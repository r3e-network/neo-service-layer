using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.JavaScript
{
    /// <summary>
    /// Interface for a manager that handles JavaScript execution.
    /// </summary>
    public interface IJavaScriptManager : IDisposable
    {
        /// <summary>
        /// Initializes the JavaScript manager.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Executes JavaScript code.
        /// </summary>
        /// <param name="context">The JavaScript execution context.</param>
        /// <returns>True if execution was successful, false otherwise.</returns>
        Task<bool> ExecuteAsync(JavaScriptExecutionContext context);

        /// <summary>
        /// Executes JavaScript code with the specified parameters.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasLimit">The gas limit for the execution.</param>
        /// <returns>The JavaScript execution context with the result.</returns>
        Task<JavaScriptExecutionContext> ExecuteAsync(string code, string input, string secrets, string functionId, string userId, ulong gasLimit);

        /// <summary>
        /// Verifies the hash of JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code to verify.</param>
        /// <param name="hash">The expected hash of the code.</param>
        /// <returns>True if the hash matches, false otherwise.</returns>
        Task<bool> VerifyCodeHashAsync(string code, string hash);

        /// <summary>
        /// Calculates the hash of JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code to hash.</param>
        /// <returns>The hash of the code.</returns>
        Task<string> CalculateCodeHashAsync(string code);

        /// <summary>
        /// Gets the JavaScript engine.
        /// </summary>
        /// <returns>The JavaScript engine.</returns>
        IJavaScriptEngine GetEngine();

        /// <summary>
        /// Gets the JavaScript engine factory.
        /// </summary>
        /// <returns>The JavaScript engine factory.</returns>
        IJavaScriptEngineFactory GetEngineFactory();

        /// <summary>
        /// Gets the available JavaScript APIs.
        /// </summary>
        /// <returns>A list of available JavaScript APIs.</returns>
        IReadOnlyList<string> GetAvailableApis();

        /// <summary>
        /// Gets the JavaScript engine name.
        /// </summary>
        /// <returns>The JavaScript engine name.</returns>
        string GetEngineName();

        /// <summary>
        /// Gets the JavaScript engine version.
        /// </summary>
        /// <returns>The JavaScript engine version.</returns>
        string GetEngineVersion();
    }
}
