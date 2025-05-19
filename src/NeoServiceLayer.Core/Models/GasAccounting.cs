using System;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents GAS accounting information for a user or function.
    /// </summary>
    public class GasAccounting
    {
        /// <summary>
        /// Gets or sets the unique identifier for the GAS accounting record.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID associated with this GAS accounting record.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the function ID associated with this GAS accounting record.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the total GAS used.
        /// </summary>
        public long TotalGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the GAS used in the current billing period.
        /// </summary>
        public long CurrentPeriodGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the GAS limit for the user or function.
        /// </summary>
        public long GasLimit { get; set; }

        /// <summary>
        /// Gets or sets the start of the current billing period.
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the end of the current billing period.
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a GAS usage record for a specific function execution.
    /// </summary>
    public class GasUsageRecord
    {
        /// <summary>
        /// Gets or sets the unique identifier for the GAS usage record.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID associated with this GAS usage record.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the function ID associated with this GAS usage record.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the execution ID associated with this GAS usage record.
        /// </summary>
        public string ExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the amount of GAS used in this execution.
        /// </summary>
        public long GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the execution.
        /// </summary>
        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    }
}
