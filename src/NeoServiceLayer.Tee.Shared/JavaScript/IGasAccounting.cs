using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.JavaScript
{
    /// <summary>
    /// Interface for gas accounting.
    /// </summary>
    public interface IGasAccounting
    {
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
        /// Uses a specified amount of gas.
        /// </summary>
        /// <param name="amount">The amount of gas to use.</param>
        void UseGas(ulong amount);

        /// <summary>
        /// Sets the gas limit.
        /// </summary>
        /// <param name="limit">The gas limit.</param>
        void SetGasLimit(ulong limit);

        /// <summary>
        /// Gets the gas limit.
        /// </summary>
        /// <returns>The gas limit.</returns>
        ulong GetGasLimit();

        /// <summary>
        /// Checks if the gas limit is exceeded.
        /// </summary>
        /// <returns>True if the gas limit is exceeded, false otherwise.</returns>
        bool IsGasLimitExceeded();

        /// <summary>
        /// Calculates the gas cost for a specific operation.
        /// </summary>
        /// <param name="operationType">The type of operation.</param>
        /// <param name="size">The size of the operation in bytes.</param>
        /// <returns>The gas cost.</returns>
        ulong CalculateGasCost(string operationType, ulong size = 0);

        /// <summary>
        /// Records gas usage for a function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasUsed">The gas used.</param>
        void RecordGasUsage(string functionId, string userId, ulong gasUsed);

        /// <summary>
        /// Gets the average gas usage for a function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <returns>The average gas usage.</returns>
        ulong GetAverageGasUsage(string functionId);

        /// <summary>
        /// Gets the total gas usage for a function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <returns>The total gas usage.</returns>
        ulong GetTotalGasUsage(string functionId);

        /// <summary>
        /// Gets the execution count for a function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <returns>The execution count.</returns>
        ulong GetExecutionCount(string functionId);

        /// <summary>
        /// Gets the gas usage history for a function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <returns>The gas usage history.</returns>
        IReadOnlyList<GasUsageRecord> GetGasUsageHistory(string functionId);

        /// <summary>
        /// Gets the gas usage history for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The gas usage history.</returns>
        IReadOnlyList<GasUsageRecord> GetGasUsageHistoryForUser(string userId);
    }

    /// <summary>
    /// Record of gas usage.
    /// </summary>
    public class GasUsageRecord
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
        /// Gets or sets the gas used.
        /// </summary>
        public ulong GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
