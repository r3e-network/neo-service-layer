using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for tracking GAS usage.
    /// </summary>
    public interface IGasAccountingService
    {
        /// <summary>
        /// Records GAS usage for a function execution.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="functionId">The ID of the function.</param>
        /// <param name="executionId">The ID of the execution.</param>
        /// <param name="gasUsed">The amount of GAS used.</param>
        /// <returns>The updated GAS accounting record.</returns>
        Task<GasAccounting> RecordGasUsageAsync(string userId, string functionId, string executionId, long gasUsed);

        /// <summary>
        /// Gets the GAS accounting record for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The GAS accounting record.</returns>
        Task<GasAccounting> GetUserGasAccountingAsync(string userId);

        /// <summary>
        /// Gets the GAS accounting record for a function.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <returns>The GAS accounting record.</returns>
        Task<GasAccounting> GetFunctionGasAccountingAsync(string functionId);

        /// <summary>
        /// Gets the GAS usage records for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of GAS usage records.</returns>
        Task<(List<GasUsageRecord> Records, int TotalCount)> GetUserGasUsageRecordsAsync(
            string userId, 
            int page = 1, 
            int pageSize = 10);

        /// <summary>
        /// Gets the GAS usage records for a function.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of GAS usage records.</returns>
        Task<(List<GasUsageRecord> Records, int TotalCount)> GetFunctionGasUsageRecordsAsync(
            string functionId, 
            int page = 1, 
            int pageSize = 10);

        /// <summary>
        /// Checks if a user has enough GAS to execute a function.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="estimatedGas">The estimated GAS usage.</param>
        /// <returns>True if the user has enough GAS, false otherwise.</returns>
        Task<bool> HasEnoughGasAsync(string userId, long estimatedGas);

        /// <summary>
        /// Sets the GAS limit for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="gasLimit">The GAS limit.</param>
        /// <returns>The updated GAS accounting record.</returns>
        Task<GasAccounting> SetUserGasLimitAsync(string userId, long gasLimit);
    }
}
