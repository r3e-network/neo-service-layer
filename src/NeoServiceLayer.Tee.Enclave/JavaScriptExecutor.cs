using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Executes JavaScript functions in the TEE.
    /// </summary>
    public class JavaScriptExecutor
    {
        private readonly ILogger<JavaScriptExecutor> _logger;
        private readonly JavaScriptEngine _jsEngine;
        private readonly UserSecretManager _userSecretManager;
        private readonly GasAccountingManager _gasAccountingManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="jsEngine">The JavaScript engine.</param>
        /// <param name="userSecretManager">The user secret manager.</param>
        /// <param name="gasAccountingManager">The GAS accounting manager.</param>
        public JavaScriptExecutor(
            ILogger<JavaScriptExecutor> logger,
            JavaScriptEngine jsEngine,
            UserSecretManager userSecretManager,
            GasAccountingManager gasAccountingManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsEngine = jsEngine ?? throw new ArgumentNullException(nameof(jsEngine));
            _userSecretManager = userSecretManager ?? throw new ArgumentNullException(nameof(userSecretManager));
            _gasAccountingManager = gasAccountingManager ?? throw new ArgumentNullException(nameof(gasAccountingManager));
        }

        /// <summary>
        /// Executes a JavaScript function.
        /// </summary>
        /// <param name="message">The TEE message containing the function execution request.</param>
        /// <returns>The TEE message containing the function execution result.</returns>
        public async Task<TeeMessage> ExecuteJavaScriptFunctionAsync(TeeMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                _logger.LogInformation("Executing JavaScript function: {MessageId}", message.Id);

                // Parse the request
                var request = JsonSerializer.Deserialize<JavaScriptExecutionRequest>(message.Data);
                if (request == null)
                {
                    throw new InvalidOperationException("Invalid JavaScript execution request");
                }

                // Initialize the GAS accounting
                _gasAccountingManager.InitializeGasTracking(request.GasLimit);

                // Prepare the execution context
                var executionContext = new JavaScriptExecutionContext
                {
                    FunctionId = request.FunctionId,
                    UserId = request.UserId,
                    Secrets = request.Secrets ?? new Dictionary<string, string>(),
                    Input = request.Input
                };

                // Execute the function
                var (result, gasUsed) = await _jsEngine.ExecuteAsync(request.FunctionCode, executionContext);

                // Create the response
                var response = new JavaScriptExecutionResponse
                {
                    Result = result,
                    GasUsed = gasUsed
                };

                return new TeeMessage
                {
                    Id = message.Id,
                    Type = TeeMessageType.JavaScriptExecution,
                    Data = JsonSerializer.Serialize(response),
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript function: {MessageId}", message.Id);

                return new TeeMessage
                {
                    Id = message.Id,
                    Type = TeeMessageType.JavaScriptExecution,
                    Data = JsonSerializer.Serialize(new JavaScriptExecutionResponse
                    {
                        Error = ex.Message,
                        GasUsed = _gasAccountingManager.GetGasUsed()
                    }),
                    CreatedAt = DateTime.UtcNow
                };
            }
        }
    }

    /// <summary>
    /// Request model for JavaScript function execution.
    /// </summary>
    public class JavaScriptExecutionRequest
    {
        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public required string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the function code.
        /// </summary>
        public required string FunctionCode { get; set; }

        /// <summary>
        /// Gets or sets the input data.
        /// </summary>
        public required JsonDocument Input { get; set; }

        /// <summary>
        /// Gets or sets the user secrets.
        /// </summary>
        public Dictionary<string, string>? Secrets { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public required string UserId { get; set; }

        /// <summary>
        /// Gets or sets the GAS limit.
        /// </summary>
        public long GasLimit { get; set; }
    }

    /// <summary>
    /// Response model for JavaScript function execution.
    /// </summary>
    public class JavaScriptExecutionResponse
    {
        /// <summary>
        /// Gets or sets the result of the function execution.
        /// </summary>
        public JsonDocument? Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if the execution failed.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the amount of GAS used.
        /// </summary>
        public long GasUsed { get; set; }
    }

    /// <summary>
    /// Context for JavaScript function execution.
    /// </summary>
    public class JavaScriptExecutionContext
    {
        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public required string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public required string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user secrets.
        /// </summary>
        public required Dictionary<string, string> Secrets { get; set; }

        /// <summary>
        /// Gets or sets the input data.
        /// </summary>
        public required JsonDocument Input { get; set; }
    }
}
