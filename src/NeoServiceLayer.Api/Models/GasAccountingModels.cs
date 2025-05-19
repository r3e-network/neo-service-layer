using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Response model for GAS accounting information.
    /// </summary>
    public class GasAccountingResponse
    {
        /// <summary>
        /// Gets or sets the total GAS used.
        /// </summary>
        public long TotalGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the GAS used in the current billing period.
        /// </summary>
        public long CurrentPeriodGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the GAS limit.
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
    }

    /// <summary>
    /// Response model for function GAS usage information.
    /// </summary>
    public class FunctionGasUsageResponse
    {
        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the total GAS used by the function.
        /// </summary>
        public long TotalGasUsed { get; set; }

        /// <summary>
        /// Gets or sets the average GAS used per execution.
        /// </summary>
        public long AverageGasPerExecution { get; set; }

        /// <summary>
        /// Gets or sets the number of executions.
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the GAS used in the last execution.
        /// </summary>
        public long LastExecutionGas { get; set; }
    }

    /// <summary>
    /// Response model for a GAS usage record.
    /// </summary>
    public class GasUsageRecordResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the GAS usage record.
        /// </summary>
        public string Id { get; set; }

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
        public DateTime ExecutionTime { get; set; }
    }

    /// <summary>
    /// Response model for a list of GAS usage records.
    /// </summary>
    public class GasUsageRecordListResponse
    {
        /// <summary>
        /// Gets or sets the list of GAS usage records.
        /// </summary>
        public List<GasUsageRecordResponse> Records { get; set; }

        /// <summary>
        /// Gets or sets the total count of GAS usage records.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// Request model for setting a GAS limit.
    /// </summary>
    public class SetGasLimitRequest
    {
        /// <summary>
        /// Gets or sets the GAS limit.
        /// </summary>
        public long GasLimit { get; set; }
    }
}
