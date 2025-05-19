using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.JavaScript;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Service for executing JavaScript code within the secure enclave.
    /// </summary>
    public class JavaScriptExecutionService
    {
        private readonly ILogger<JavaScriptExecutionService> _logger;
        private readonly JavaScriptEngine _jsEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="jsEngine">The JavaScript engine.</param>
        public JavaScriptExecutionService(
            ILogger<JavaScriptExecutionService> logger,
            JavaScriptEngine jsEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsEngine = jsEngine ?? throw new ArgumentNullException(nameof(jsEngine));
        }

        /// <summary>
        /// Executes JavaScript code within the secure enclave.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The updated execution context with results.</returns>
        public async Task<Shared.JavaScript.JavaScriptExecutionContext> ExecuteAsync(Shared.JavaScript.JavaScriptExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(context.Code))
            {
                throw new ArgumentException("JavaScript code cannot be null or empty.", nameof(context));
            }

            _logger.LogInformation(
                "Executing JavaScript function {FunctionId} for user {UserId}",
                context.FunctionId,
                context.UserId);

            try
            {
                // Convert the shared context to enclave context
                var enclaveContext = JavaScriptContextAdapter.FromSharedContext(context);
                
                // Execute the JavaScript code
                var (resultJson, gasUsed) = await _jsEngine.ExecuteAsync(context.Code, enclaveContext);
                
                // Update the shared context with the results
                JavaScriptContextAdapter.UpdateSharedContext(context, enclaveContext, resultJson, gasUsed);
                
                _logger.LogInformation(
                    "Successfully executed JavaScript function {FunctionId} for user {UserId}. Gas used: {GasUsed}",
                    context.FunctionId,
                    context.UserId,
                    gasUsed);
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing JavaScript function {FunctionId} for user {UserId}",
                    context.FunctionId,
                    context.UserId);
                
                // Update the context with the error
                context.Success = false;
                context.Error = ex.Message;
                context.EndTime = DateTime.UtcNow;
                context.DurationMs = (long)(context.EndTime - context.StartTime).TotalMilliseconds;
                
                return context;
            }
        }

        /// <summary>
        /// Executes a JavaScript function with the specified parameters.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="gasLimit">The gas limit for the execution.</param>
        /// <returns>The execution context with results.</returns>
        public async Task<Shared.JavaScript.JavaScriptExecutionContext> ExecuteFunctionAsync(
            string functionId,
            string userId,
            string code,
            string input,
            string secrets,
            ulong gasLimit = 1_000_000)
        {
            var context = new Shared.JavaScript.JavaScriptExecutionContext(
                functionId,
                userId,
                code,
                input,
                secrets,
                gasLimit);
            
            return await ExecuteAsync(context);
        }
    }
} 