using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Functions
{
    /// <summary>
    /// Interface for a service that executes functions.
    /// </summary>
    public interface IFunctionExecutionService : IDisposable
    {
        /// <summary>
        /// Initializes the function execution service.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Executes a function.
        /// </summary>
        /// <param name="context">The function execution context.</param>
        /// <returns>True if execution was successful, false otherwise.</returns>
        Task<bool> ExecuteAsync(FunctionExecutionContext context);

        /// <summary>
        /// Executes a function.
        /// </summary>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <param name="gasLimit">The gas limit for execution.</param>
        /// <returns>The function execution context containing the result.</returns>
        Task<FunctionExecutionContext> ExecuteAsync(string functionId, string input, string userId, ulong gasLimit);

        /// <summary>
        /// Creates a new function.
        /// </summary>
        /// <param name="function">The function to create.</param>
        /// <returns>The created function.</returns>
        Task<FunctionInfo> CreateFunctionAsync(FunctionInfo function);

        /// <summary>
        /// Updates an existing function.
        /// </summary>
        /// <param name="function">The function to update.</param>
        /// <returns>The updated function.</returns>
        Task<FunctionInfo> UpdateFunctionAsync(FunctionInfo function);

        /// <summary>
        /// Gets a function by ID.
        /// </summary>
        /// <param name="functionId">The ID of the function to get.</param>
        /// <returns>The function, or null if not found.</returns>
        Task<FunctionInfo> GetFunctionAsync(string functionId);

        /// <summary>
        /// Gets a function by ID and version.
        /// </summary>
        /// <param name="functionId">The ID of the function to get.</param>
        /// <param name="version">The version of the function to get.</param>
        /// <returns>The function, or null if not found.</returns>
        Task<FunctionInfo> GetFunctionVersionAsync(string functionId, string version);

        /// <summary>
        /// Gets all versions of a function.
        /// </summary>
        /// <param name="functionId">The ID of the function to get versions for.</param>
        /// <returns>A list of function versions.</returns>
        Task<IReadOnlyList<FunctionInfo>> GetFunctionVersionsAsync(string functionId);

        /// <summary>
        /// Deletes a function.
        /// </summary>
        /// <param name="functionId">The ID of the function to delete.</param>
        /// <returns>True if the function was deleted, false otherwise.</returns>
        Task<bool> DeleteFunctionAsync(string functionId);

        /// <summary>
        /// Lists all functions for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of functions and the total count.</returns>
        Task<(IReadOnlyList<FunctionInfo> Functions, int TotalCount)> ListFunctionsAsync(
            string userId,
            FunctionStatus? status = null,
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Deploys a function.
        /// </summary>
        /// <param name="functionId">The ID of the function to deploy.</param>
        /// <param name="version">The version to deploy.</param>
        /// <returns>True if the function was deployed, false otherwise.</returns>
        Task<bool> DeployFunctionAsync(string functionId, string version);

        /// <summary>
        /// Validates a function.
        /// </summary>
        /// <param name="function">The function to validate.</param>
        /// <returns>A list of validation errors, or an empty list if validation was successful.</returns>
        Task<IReadOnlyList<string>> ValidateFunctionAsync(FunctionInfo function);

        /// <summary>
        /// Gets the execution history for a function.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of execution records and the total count.</returns>
        Task<(IReadOnlyList<FunctionExecutionRecord> Records, int TotalCount)> GetExecutionHistoryAsync(
            string functionId,
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Gets the execution history for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of execution records and the total count.</returns>
        Task<(IReadOnlyList<FunctionExecutionRecord> Records, int TotalCount)> GetUserExecutionHistoryAsync(
            string userId,
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Gets the execution record for a specific execution.
        /// </summary>
        /// <param name="executionId">The ID of the execution.</param>
        /// <returns>The execution record, or null if not found.</returns>
        Task<FunctionExecutionRecord> GetExecutionRecordAsync(string executionId);

        /// <summary>
        /// Gets the gas usage for a function.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <returns>The gas usage information.</returns>
        Task<FunctionGasUsage> GetFunctionGasUsageAsync(string functionId);

        /// <summary>
        /// Gets the gas usage for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The gas usage information.</returns>
        Task<FunctionGasUsage> GetUserGasUsageAsync(string userId);

        /// <summary>
        /// Calculates the hash of a function's code.
        /// </summary>
        /// <param name="code">The function code.</param>
        /// <returns>The hash of the code.</returns>
        Task<string> CalculateCodeHashAsync(string code);

        /// <summary>
        /// Verifies the hash of a function's code.
        /// </summary>
        /// <param name="code">The function code.</param>
        /// <param name="hash">The hash to verify against.</param>
        /// <returns>True if the hash is valid, false otherwise.</returns>
        Task<bool> VerifyCodeHashAsync(string code, string hash);
    }
}
