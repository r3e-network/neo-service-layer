using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Shared.JavaScript
{
    /// <summary>
    /// Context for JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionContext
    {
        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript code to execute.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the input data as a JSON string.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets data as a JSON string.
        /// </summary>
        public string Secrets { get; set; }

        /// <summary>
        /// Gets or sets the gas limit for the execution.
        /// </summary>
        public ulong GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the gas used by the execution.
        /// </summary>
        public ulong GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the result of the execution as a JSON string.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the execution failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the execution start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the execution end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the execution duration in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the execution metadata.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the JavaScriptExecutionContext class.
        /// </summary>
        public JavaScriptExecutionContext()
        {
            StartTime = DateTime.UtcNow;
            Success = false;
            GasUsed = 0;
        }

        /// <summary>
        /// Initializes a new instance of the JavaScriptExecutionContext class.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="gasLimit">The gas limit for the execution.</param>
        public JavaScriptExecutionContext(string functionId, string userId, string code, string input, string secrets, ulong gasLimit)
        {
            FunctionId = functionId;
            UserId = userId;
            Code = code;
            Input = input;
            Secrets = secrets;
            GasLimit = gasLimit;
            StartTime = DateTime.UtcNow;
            Success = false;
            GasUsed = 0;
        }

        /// <summary>
        /// Sets the execution result.
        /// </summary>
        /// <param name="result">The result of the execution as a JSON string.</param>
        /// <param name="gasUsed">The gas used by the execution.</param>
        public void SetResult(string result, ulong gasUsed)
        {
            Result = result;
            GasUsed = gasUsed;
            Success = true;
            EndTime = DateTime.UtcNow;
            DurationMs = (long)(EndTime - StartTime).TotalMilliseconds;
        }

        /// <summary>
        /// Sets the execution error.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="gasUsed">The gas used by the execution.</param>
        public void SetError(string error, ulong gasUsed)
        {
            Error = error;
            GasUsed = gasUsed;
            Success = false;
            EndTime = DateTime.UtcNow;
            DurationMs = (long)(EndTime - StartTime).TotalMilliseconds;
        }
    }
}
