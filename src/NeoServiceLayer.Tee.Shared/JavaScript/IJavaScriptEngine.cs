using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.JavaScript
{
    /// <summary>
    /// Interface for a JavaScript engine that executes JavaScript code securely.
    /// </summary>
    public interface IJavaScriptEngine : IDisposable
    {
        /// <summary>
        /// Initializes the JavaScript engine.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Executes JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The result of the execution as a JSON string.</returns>
        Task<string> ExecuteAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Executes JavaScript code with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasLimit">The gas limit for the execution.</param>
        /// <param name="gasUsed">Output parameter for the amount of gas used.</param>
        /// <returns>The result of the execution as a JSON string.</returns>
        Task<string> ExecuteWithGasAsync(string code, string input, string secrets, string functionId, string userId, ulong gasLimit, out ulong gasUsed);

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
        /// Resets the gas used counter.
        /// </summary>
        void ResetGasUsed();

        /// <summary>
        /// Gets the current gas used.
        /// </summary>
        /// <returns>The gas used.</returns>
        ulong GetGasUsed();

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
