using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Functions
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
        /// <param name="entryPoint">The entry point function to call.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets to make available to the code.</param>
        /// <param name="gasLimit">The gas limit for execution.</param>
        /// <param name="timeoutMs">The timeout in milliseconds.</param>
        /// <returns>The execution result.</returns>
        Task<JavaScriptExecutionResult> ExecuteAsync(
            string code,
            string entryPoint,
            string input,
            Dictionary<string, string> secrets,
            ulong gasLimit,
            int timeoutMs);

        /// <summary>
        /// Validates JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code to validate.</param>
        /// <param name="entryPoint">The entry point function to validate.</param>
        /// <returns>A list of validation errors, or an empty list if validation was successful.</returns>
        Task<IReadOnlyList<string>> ValidateCodeAsync(string code, string entryPoint);

        /// <summary>
        /// Calculates the hash of JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        /// <returns>The hash of the code.</returns>
        Task<string> CalculateCodeHashAsync(string code);

        /// <summary>
        /// Verifies the hash of JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        /// <param name="hash">The hash to verify against.</param>
        /// <returns>True if the hash is valid, false otherwise.</returns>
        Task<bool> VerifyCodeHashAsync(string code, string hash);

        /// <summary>
        /// Gets the current gas used.
        /// </summary>
        /// <returns>The current gas used.</returns>
        ulong GetGasUsed();

        /// <summary>
        /// Gets the current memory used in bytes.
        /// </summary>
        /// <returns>The current memory used in bytes.</returns>
        ulong GetMemoryUsed();

        /// <summary>
        /// Gets the engine name.
        /// </summary>
        /// <returns>The engine name.</returns>
        string GetEngineName();

        /// <summary>
        /// Gets the engine version.
        /// </summary>
        /// <returns>The engine version.</returns>
        string GetEngineVersion();
    }

    /// <summary>
    /// Represents the result of a JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result of the execution.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if execution failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the gas used.
        /// </summary>
        public ulong GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the memory used in bytes.
        /// </summary>
        public ulong MemoryUsed { get; set; }

        /// <summary>
        /// Gets or sets the logs from the execution.
        /// </summary>
        public List<string> Logs { get; set; } = new List<string>();
    }
}
