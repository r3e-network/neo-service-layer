using System;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Request model for JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionRequest
    {
        /// <summary>
        /// Gets or sets the JavaScript code to execute.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the input data for the JavaScript code.
        /// </summary>
        [Required]
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets for the JavaScript code.
        /// </summary>
        public string Secrets { get; set; }

        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }
    }

    /// <summary>
    /// Response model for JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionResponse
    {
        /// <summary>
        /// Gets or sets the result of the JavaScript execution.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the gas used.
        /// </summary>
        public long GasUsed { get; set; }
    }

    /// <summary>
    /// Response model for TEE information.
    /// </summary>
    public class TeeInfoResponse
    {
        /// <summary>
        /// Gets or sets the Open Enclave version.
        /// </summary>
        public string OpenEnclaveVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Occlum support is enabled.
        /// </summary>
        public bool OcclumSupportEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether simulation mode is enabled.
        /// </summary>
        public bool SimulationMode { get; set; }

        /// <summary>
        /// Gets or sets the enclave configuration.
        /// </summary>
        public string EnclaveConfiguration { get; set; }
    }
}
