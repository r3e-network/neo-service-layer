using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models
{
    /// <summary>
    /// Represents the result of executing JavaScript code in an enclave.
    /// </summary>
    public class JavaScriptExecutionResult
    {
        /// <summary>
        /// Gets or sets the output of the JavaScript execution.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets the error message if the JavaScript execution failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets whether the JavaScript execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the gas used by the JavaScript execution.
        /// </summary>
        public long GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the ID of the function that was executed.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who executed the function.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the execution started.
        /// </summary>
        public DateTime ExecutionStartTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the execution ended.
        /// </summary>
        public DateTime ExecutionEndTime { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the execution.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
