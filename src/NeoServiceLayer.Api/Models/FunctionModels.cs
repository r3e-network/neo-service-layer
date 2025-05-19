using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Tee.Shared.Functions;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Request model for creating a function.
    /// </summary>
    public class CreateFunctionRequest
    {
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
        /// Gets or sets the code of the function.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the entry point of the function.
        /// </summary>
        [Required]
        public string EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets the runtime of the function.
        /// </summary>
        [Required]
        public FunctionRuntime Runtime { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the gas limit for this function execution.
        /// </summary>
        public ulong? GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the timeout for this function execution in milliseconds.
        /// </summary>
        public int? TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the memory limit for this function execution in bytes.
        /// </summary>
        public ulong? MemoryLimit { get; set; }

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
    /// Request model for updating a function.
    /// </summary>
    public class UpdateFunctionRequest
    {
        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the function.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the code of the function.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the entry point of the function.
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets the runtime of the function.
        /// </summary>
        public FunctionRuntime? Runtime { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; }

        /// <summary>
        /// Gets or sets the gas limit for this function execution.
        /// </summary>
        public ulong? GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the timeout for this function execution in milliseconds.
        /// </summary>
        public int? TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the memory limit for this function execution in bytes.
        /// </summary>
        public ulong? MemoryLimit { get; set; }

        /// <summary>
        /// Gets or sets the tags for the function.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the function.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
    }

    /// <summary>
    /// Request model for executing a function.
    /// </summary>
    public class ExecuteFunctionRequest
    {
        /// <summary>
        /// Gets or sets the input data as a JSON string.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the gas limit for this function execution.
        /// </summary>
        public ulong? GasLimit { get; set; }
    }

    /// <summary>
    /// Response model for a function.
    /// </summary>
    public class FunctionResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the function.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the function.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the function.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the hash of the function code.
        /// </summary>
        public string CodeHash { get; set; }

        /// <summary>
        /// Gets or sets the runtime of the function.
        /// </summary>
        public string Runtime { get; set; }

        /// <summary>
        /// Gets or sets the entry point of the function.
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets the list of required secret names that this function needs access to.
        /// </summary>
        public List<string> RequiredSecrets { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the gas limit for this function execution.
        /// </summary>
        public ulong GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the timeout for this function execution in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the memory limit for this function execution in bytes.
        /// </summary>
        public ulong MemoryLimit { get; set; }

        /// <summary>
        /// Gets or sets the status of the function.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the owner of the function.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

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
    /// Response model for a function execution.
    /// </summary>
    public class ExecutionResponse
    {
        /// <summary>
        /// Gets or sets the ID of the execution.
        /// </summary>
        public string ExecutionId { get; set; }

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
    /// Response model for a function execution record.
    /// </summary>
    public class ExecutionRecordResponse
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
    /// Response model for gas usage.
    /// </summary>
    public class GasUsageResponse
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
        public Dictionary<string, ulong> GasUsageByDay { get; set; } = new Dictionary<string, ulong>();

        /// <summary>
        /// Gets or sets the gas usage by function.
        /// </summary>
        public Dictionary<string, ulong> GasUsageByFunction { get; set; } = new Dictionary<string, ulong>();

        /// <summary>
        /// Gets or sets the gas usage by user.
        /// </summary>
        public Dictionary<string, ulong> GasUsageByUser { get; set; } = new Dictionary<string, ulong>();
    }

    /// <summary>
    /// Response model for paginated results.
    /// </summary>
    /// <typeparam name="T">The type of items in the response.</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// Gets or sets the items in the response.
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total count of items.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages { get; set; }
    }
}
