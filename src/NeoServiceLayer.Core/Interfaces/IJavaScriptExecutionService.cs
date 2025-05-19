using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;
using System.Text.Json;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for executing JavaScript functions in the TEE.
    /// </summary>
    public interface IJavaScriptExecutionService
    {
        /// <summary>
        /// Executes a JavaScript function in the TEE.
        /// </summary>
        /// <param name="functionId">The ID of the JavaScript function to execute.</param>
        /// <param name="input">The input data for the function.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the function execution.</returns>
        Task<JsonDocument> ExecuteFunctionAsync(string functionId, JsonDocument input, string userId);

        /// <summary>
        /// Creates a new JavaScript function.
        /// </summary>
        /// <param name="function">The JavaScript function to create.</param>
        /// <returns>The created JavaScript function.</returns>
        Task<JavaScriptFunction> CreateFunctionAsync(JavaScriptFunction function);

        /// <summary>
        /// Gets a JavaScript function by ID.
        /// </summary>
        /// <param name="functionId">The ID of the JavaScript function to get.</param>
        /// <returns>The JavaScript function.</returns>
        Task<JavaScriptFunction> GetFunctionAsync(string functionId);

        /// <summary>
        /// Updates a JavaScript function.
        /// </summary>
        /// <param name="function">The JavaScript function to update.</param>
        /// <returns>The updated JavaScript function.</returns>
        Task<JavaScriptFunction> UpdateFunctionAsync(JavaScriptFunction function);

        /// <summary>
        /// Deletes a JavaScript function.
        /// </summary>
        /// <param name="functionId">The ID of the JavaScript function to delete.</param>
        /// <returns>True if the function was deleted, false otherwise.</returns>
        Task<bool> DeleteFunctionAsync(string functionId);

        /// <summary>
        /// Lists all JavaScript functions for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of JavaScript functions.</returns>
        Task<(List<JavaScriptFunction> Functions, int TotalCount)> ListFunctionsAsync(
            string userId, 
            JavaScriptFunctionStatus? status = null, 
            int page = 1, 
            int pageSize = 10);
    }
}
