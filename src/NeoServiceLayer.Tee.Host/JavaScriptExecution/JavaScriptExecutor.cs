using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.JavaScriptExecution
{
    /// <summary>
    /// Provides functionality to execute JavaScript code in an enclave.
    /// </summary>
    public class JavaScriptExecutor : IJavaScriptExecutor
    {
        private readonly ILogger<JavaScriptExecutor> _logger;
        private readonly IOcclumInterface _occlumInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for executing JavaScript code.</param>
        public JavaScriptExecutor(ILogger<JavaScriptExecutor> logger, IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
        }

        /// <summary>
        /// Executes JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        public async Task<string> ExecuteAsync(string code, string input, string functionId, string userId)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (input == null)
            {
                input = "{}";
            }

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            _logger.LogDebug("Executing JavaScript code for function {FunctionId} for user {UserId}", functionId, userId);

            try
            {
                // Execute the JavaScript code using the Occlum interface
                string result = await _occlumInterface.ExecuteJavaScriptAsync(code, input, "{}", functionId, userId);
                _logger.LogDebug("JavaScript code executed successfully for function {FunctionId}", functionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId}", functionId);
                throw new JavaScriptExecutionException("Error executing JavaScript code", ex);
            }
        }

        /// <summary>
        /// Executes JavaScript code with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution and the gas used.</returns>
        public async Task<(string Result, ulong GasUsed)> ExecuteWithGasAsync(string code, string input, string functionId, string userId)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (input == null)
            {
                input = "{}";
            }

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            _logger.LogDebug("Executing JavaScript code with gas accounting for function {FunctionId} for user {UserId}", functionId, userId);

            try
            {
                // Execute the JavaScript code with gas accounting using the Occlum interface
                ulong gasUsed = 0;
                string result = await _occlumInterface.ExecuteJavaScriptWithGasAsync(code, input, "{}", functionId, userId, out gasUsed);
                _logger.LogDebug("JavaScript code executed successfully with gas accounting for function {FunctionId}. Gas used: {GasUsed}", functionId, gasUsed);
                return (result, gasUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code with gas accounting for function {FunctionId}", functionId);
                throw new JavaScriptExecutionException("Error executing JavaScript code with gas accounting", ex);
            }
        }
    }
}
