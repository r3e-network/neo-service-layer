using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;

namespace NeoServiceLayer.Core.Services
{
    /// <summary>
    /// Service for executing JavaScript code in the TEE.
    /// </summary>
    public class JavaScriptService : IJavaScriptService
    {
        private readonly ILogger<JavaScriptService> _logger;
        private readonly ITeeService _teeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="teeService">The TEE service.</param>
        public JavaScriptService(ILogger<JavaScriptService> logger, ITeeService teeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _teeService = teeService ?? throw new ArgumentNullException(nameof(teeService));
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptAsync(string code, object input, string functionId, string userId)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            try
            {
                _logger.LogInformation("Executing JavaScript function {FunctionId} for user {UserId}", functionId, userId);

                string inputJson = input != null
                    ? JsonSerializer.Serialize(input)
                    : "{}";

                string secretsJson = "{}"; // No secrets for now

                string result = await _teeService.ExecuteJavaScriptAsync(code, inputJson, secretsJson, functionId, userId);

                _logger.LogInformation("JavaScript function {FunctionId} executed successfully", functionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript function {FunctionId} for user {UserId}", functionId, userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeJavaScriptExecutorAsync()
        {
            try
            {
                _logger.LogInformation("Initializing JavaScript executor");
                bool result = await _teeService.InitializeJavaScriptExecutorAsync();
                _logger.LogInformation("JavaScript executor initialization {Result}", result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing JavaScript executor");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptCodeAsync(string code, string filename)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = "<eval>";
            }

            try
            {
                _logger.LogInformation("Executing JavaScript code from {Filename}", filename);
                string result = await _teeService.ExecuteJavaScriptCodeAsync(code, filename);
                _logger.LogInformation("JavaScript code execution completed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code from {Filename}", filename);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptFunctionAsync(string functionName, IEnumerable<string> args)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be null or empty", nameof(functionName));
            }

            try
            {
                _logger.LogInformation("Executing JavaScript function {FunctionName}", functionName);
                string result = await _teeService.ExecuteJavaScriptFunctionAsync(functionName, args);
                _logger.LogInformation("JavaScript function {FunctionName} execution completed", functionName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript function {FunctionName}", functionName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CollectJavaScriptGarbageAsync()
        {
            try
            {
                _logger.LogInformation("Collecting JavaScript garbage");
                bool result = await _teeService.CollectJavaScriptGarbageAsync();
                _logger.LogInformation("JavaScript garbage collection {Result}", result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting JavaScript garbage");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ShutdownJavaScriptExecutorAsync()
        {
            try
            {
                _logger.LogInformation("Shutting down JavaScript executor");
                bool result = await _teeService.ShutdownJavaScriptExecutorAsync();
                _logger.LogInformation("JavaScript executor shutdown {Result}", result ? "succeeded" : "failed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down JavaScript executor");
                throw;
            }
        }
    }
}
