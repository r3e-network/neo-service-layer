using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Tee.Shared.Functions
{
    /// <summary>
    /// Represents a function that can be executed.
    /// </summary>
    public class FunctionInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the function.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the function.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the function.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the code of the function.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the hash of the function code.
        /// </summary>
        public string CodeHash { get; set; }

        /// <summary>
        /// Gets or sets the runtime of the function.
        /// </summary>
        [Required]
        public FunctionRuntime Runtime { get; set; }

        /// <summary>
        /// Gets or sets the entry point of the function.
        /// </summary>
        [Required]
        public string EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the gas limit for this function execution.
        /// </summary>
        public ulong GasLimit { get; set; } = 1000000;

        /// <summary>
        /// Gets or sets the timeout for this function execution in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the memory limit for this function execution in bytes.
        /// </summary>
        public ulong MemoryLimit { get; set; } = 128 * 1024 * 1024; // 128 MB

        /// <summary>
        /// Gets or sets the status of the function.
        /// </summary>
        public FunctionStatus Status { get; set; } = FunctionStatus.Draft;

        /// <summary>
        /// Gets or sets the owner of the function.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last deployment timestamp.
        /// </summary>
        public DateTime? DeployedAt { get; set; }

        /// <summary>
        /// Gets or sets the last execution timestamp.
        /// </summary>
        public DateTime? LastExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets the execution count.
        /// </summary>
        public ulong ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the total gas used.
        /// </summary>
        public ulong TotalGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the average gas used.
        /// </summary>
        public ulong AverageGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the average execution time in milliseconds.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the tags for the function.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the metadata for the function.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents the status of a function.
    /// </summary>
    public enum FunctionStatus
    {
        /// <summary>
        /// The function is in draft status.
        /// </summary>
        Draft,

        /// <summary>
        /// The function is active.
        /// </summary>
        Active,

        /// <summary>
        /// The function is inactive.
        /// </summary>
        Inactive,

        /// <summary>
        /// The function is deprecated.
        /// </summary>
        Deprecated,

        /// <summary>
        /// The function is archived.
        /// </summary>
        Archived
    }

    /// <summary>
    /// Represents the runtime of a function.
    /// </summary>
    public enum FunctionRuntime
    {
        /// <summary>
        /// JavaScript runtime.
        /// </summary>
        JavaScript,

        /// <summary>
        /// TypeScript runtime.
        /// </summary>
        TypeScript,

        /// <summary>
        /// WebAssembly runtime.
        /// </summary>
        WebAssembly
    }

    /// <summary>
    /// Represents the context for function execution.
    /// </summary>
    public class FunctionExecutionContext
    {
        /// <summary>
        /// Gets or sets the ID of the execution.
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the function.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the version of the function.
        /// </summary>
        public string FunctionVersion { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the function code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the entry point.
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets the runtime.
        /// </summary>
        public FunctionRuntime Runtime { get; set; }

        /// <summary>
        /// Gets or sets the input data.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets.
        /// </summary>
        public Dictionary<string, string> Secrets { get; set; }

        /// <summary>
        /// Gets or sets the gas limit.
        /// </summary>
        public ulong GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the memory limit in bytes.
        /// </summary>
        public ulong MemoryLimit { get; set; }

        /// <summary>
        /// Gets or sets the result of the execution.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if execution failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

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
        /// Gets or sets the start time of the execution.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the execution.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the logs from the execution.
        /// </summary>
        public List<string> Logs { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the metadata for the execution.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionExecutionContext"/> class.
        /// </summary>
        public FunctionExecutionContext()
        {
            ExecutionId = Guid.NewGuid().ToString();
            StartTime = DateTime.UtcNow;
            Secrets = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionExecutionContext"/> class.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="code">The function code.</param>
        /// <param name="input">The input data.</param>
        /// <param name="secrets">The secrets.</param>
        /// <param name="gasLimit">The gas limit.</param>
        public FunctionExecutionContext(string functionId, string userId, string code, string input, Dictionary<string, string> secrets, ulong gasLimit)
            : this()
        {
            FunctionId = functionId;
            UserId = userId;
            Code = code;
            Input = input;
            Secrets = secrets ?? new Dictionary<string, string>();
            GasLimit = gasLimit;
        }

        /// <summary>
        /// Sets the result of the execution.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="gasUsed">The gas used.</param>
        public void SetResult(string result, ulong gasUsed)
        {
            Result = result;
            GasUsed = gasUsed;
            Success = true;
            EndTime = DateTime.UtcNow;
            ExecutionTimeMs = (long)(EndTime - StartTime).TotalMilliseconds;
        }

        /// <summary>
        /// Sets the error of the execution.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="gasUsed">The gas used.</param>
        public void SetError(string error, ulong gasUsed)
        {
            Error = error;
            GasUsed = gasUsed;
            Success = false;
            EndTime = DateTime.UtcNow;
            ExecutionTimeMs = (long)(EndTime - StartTime).TotalMilliseconds;
        }

        /// <summary>
        /// Adds a log message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public void AddLog(string message)
        {
            Logs.Add(message);
        }
    }

    /// <summary>
    /// Represents a record of a function execution.
    /// </summary>
    public class FunctionExecutionRecord
    {
        /// <summary>
        /// Gets or sets the ID of the execution.
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the function.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the version of the function.
        /// </summary>
        public string FunctionVersion { get; set; }

        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

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
        /// Gets or sets the start time of the execution.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the execution.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the execution.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents gas usage information for a function or user.
    /// </summary>
    public class FunctionGasUsage
    {
        /// <summary>
        /// Gets or sets the ID of the function or user.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the total gas used.
        /// </summary>
        public ulong TotalGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the total execution count.
        /// </summary>
        public ulong ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the average gas used per execution.
        /// </summary>
        public ulong AverageGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the gas usage by day.
        /// </summary>
        public Dictionary<DateTime, ulong> GasUsageByDay { get; set; } = new Dictionary<DateTime, ulong>();

        /// <summary>
        /// Gets or sets the gas usage by function.
        /// </summary>
        public Dictionary<string, ulong> GasUsageByFunction { get; set; } = new Dictionary<string, ulong>();

        /// <summary>
        /// Gets or sets the gas usage by user.
        /// </summary>
        public Dictionary<string, ulong> GasUsageByUser { get; set; } = new Dictionary<string, ulong>();
    }
}
