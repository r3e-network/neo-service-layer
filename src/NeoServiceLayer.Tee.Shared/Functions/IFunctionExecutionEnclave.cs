using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Functions
{
    /// <summary>
    /// Interface for the function execution enclave.
    /// </summary>
    public interface IFunctionExecutionEnclave : IDisposable
    {
        /// <summary>
        /// Initializes the function execution enclave.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Executes a function in the enclave.
        /// </summary>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="functionCode">The function code.</param>
        /// <param name="entryPoint">The entry point function to call.</param>
        /// <param name="runtime">The runtime of the function.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets to make available to the function.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <param name="gasLimit">The gas limit for execution.</param>
        /// <param name="timeoutMs">The timeout in milliseconds.</param>
        /// <returns>The execution result.</returns>
        Task<FunctionExecutionResult> ExecuteFunctionAsync(
            string functionId,
            string functionCode,
            string entryPoint,
            FunctionRuntime runtime,
            string input,
            Dictionary<string, string> secrets,
            string userId,
            ulong gasLimit,
            int timeoutMs);

        /// <summary>
        /// Validates a function in the enclave.
        /// </summary>
        /// <param name="functionCode">The function code.</param>
        /// <param name="entryPoint">The entry point function to validate.</param>
        /// <param name="runtime">The runtime of the function.</param>
        /// <returns>A list of validation errors, or an empty list if validation was successful.</returns>
        Task<IReadOnlyList<string>> ValidateFunctionAsync(
            string functionCode,
            string entryPoint,
            FunctionRuntime runtime);

        /// <summary>
        /// Calculates the hash of a function's code in the enclave.
        /// </summary>
        /// <param name="functionCode">The function code.</param>
        /// <returns>The hash of the code.</returns>
        Task<string> CalculateCodeHashAsync(string functionCode);

        /// <summary>
        /// Verifies the hash of a function's code in the enclave.
        /// </summary>
        /// <param name="functionCode">The function code.</param>
        /// <param name="hash">The hash to verify against.</param>
        /// <returns>True if the hash is valid, false otherwise.</returns>
        Task<bool> VerifyCodeHashAsync(string functionCode, string hash);

        /// <summary>
        /// Gets the enclave attestation.
        /// </summary>
        /// <returns>The enclave attestation.</returns>
        Task<string> GetAttestationAsync();

        /// <summary>
        /// Gets the enclave information.
        /// </summary>
        /// <returns>The enclave information.</returns>
        Task<EnclaveInfo> GetEnclaveInfoAsync();
    }

    /// <summary>
    /// Represents the result of a function execution in the enclave.
    /// </summary>
    public class FunctionExecutionResult
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

    /// <summary>
    /// Represents information about the enclave.
    /// </summary>
    public class EnclaveInfo
    {
        /// <summary>
        /// Gets or sets the enclave ID.
        /// </summary>
        public string EnclaveId { get; set; }

        /// <summary>
        /// Gets or sets the enclave type.
        /// </summary>
        public string EnclaveType { get; set; }

        /// <summary>
        /// Gets or sets the enclave version.
        /// </summary>
        public string EnclaveVersion { get; set; }

        /// <summary>
        /// Gets or sets the security version.
        /// </summary>
        public string SecurityVersion { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the enclave is debuggable.
        /// </summary>
        public bool IsDebuggable { get; set; }

        /// <summary>
        /// Gets or sets the supported runtimes.
        /// </summary>
        public List<FunctionRuntime> SupportedRuntimes { get; set; } = new List<FunctionRuntime>();

        /// <summary>
        /// Gets or sets the JavaScript engine name.
        /// </summary>
        public string JavaScriptEngineName { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript engine version.
        /// </summary>
        public string JavaScriptEngineVersion { get; set; }
    }
}
