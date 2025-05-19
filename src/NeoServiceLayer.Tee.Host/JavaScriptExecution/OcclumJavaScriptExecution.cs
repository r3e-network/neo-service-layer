using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Occlum;

namespace NeoServiceLayer.Tee.Host.JavaScriptExecution
{
    /// <summary>
    /// Executes JavaScript code in an Occlum enclave.
    /// </summary>
    public class OcclumJavaScriptExecution : IJavaScriptExecution
    {
        private readonly ILogger _logger;
        private readonly IOcclumManager _occlumManager;
        private bool _initialized;

        /// <summary>
        /// Gets the ID of the current function being executed.
        /// </summary>
        public string CurrentFunctionId { get; private set; }

        /// <summary>
        /// Gets the ID of the current user executing the function.
        /// </summary>
        public string CurrentUserId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the OcclumJavaScriptExecution class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="occlumManager">The Occlum manager.</param>
        public OcclumJavaScriptExecution(ILogger logger, IOcclumManager occlumManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumManager = occlumManager ?? throw new ArgumentNullException(nameof(occlumManager));
            _initialized = false;
        }

        /// <summary>
        /// Initializes the JavaScript execution environment.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                // Initialize the Occlum manager
                await _occlumManager.InitializeAsync();
                _initialized = true;
                _logger.LogInformation("Occlum JavaScript execution initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Occlum JavaScript execution");
                throw new JavaScriptExecutionException("Error initializing Occlum JavaScript execution", ex);
            }
        }

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The result of the JavaScript execution.</returns>
        public async Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

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

            // Set the current function and user IDs
            CurrentFunctionId = functionId;
            CurrentUserId = userId;

            try
            {
                _logger.LogInformation("Executing JavaScript code for function {FunctionId} and user {UserId}", functionId, userId);

                // Create a wrapper script that includes the user code and executes it with the input
                string wrapperScript = CreateWrapperScript(code, input, secrets, functionId, userId);

                // Create a temporary file for the wrapper script
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_js");
                Directory.CreateDirectory(tempDir);
                string tempFilePath = Path.Combine(tempDir, $"script_{Guid.NewGuid():N}.js");
                await File.WriteAllTextAsync(tempFilePath, wrapperScript);

                try
                {
                    // Create a temporary file for the result
                    string resultFilePath = Path.Combine(tempDir, $"result_{Guid.NewGuid():N}.json");

                    // Execute the JavaScript code
                    string[] args = new string[] { "--result-file", resultFilePath };
                    int exitCode = await _occlumManager.ExecuteJavaScriptFileAsync(tempFilePath, args);

                    if (exitCode != 0)
                    {
                        throw new JavaScriptExecutionException($"JavaScript execution failed with exit code {exitCode}");
                    }

                    // Read the result
                    string result = await File.ReadAllTextAsync(resultFilePath);

                    // Clean up the result file
                    try
                    {
                        File.Delete(resultFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary result file {ResultFilePath}", resultFilePath);
                    }

                    return result;
                }
                finally
                {
                    // Clean up the temporary file
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file {TempFilePath}", tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId} and user {UserId}", functionId, userId);
                throw new JavaScriptExecutionException("Error executing JavaScript code", ex);
            }
        }

        /// <summary>
        /// Executes JavaScript code in the enclave with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>A tuple containing the result of the JavaScript execution and the gas used.</returns>
        public async Task<(string Result, ulong GasUsed)> ExecuteJavaScriptWithGasAsync(string code, string input, string secrets, string functionId, string userId)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

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

            // Set the current function and user IDs
            CurrentFunctionId = functionId;
            CurrentUserId = userId;

            try
            {
                _logger.LogInformation("Executing JavaScript code with gas accounting for function {FunctionId} and user {UserId}", functionId, userId);

                // Create a wrapper script that includes the user code and executes it with the input and gas accounting
                string wrapperScript = CreateWrapperScriptWithGas(code, input, secrets, functionId, userId);

                // Create a temporary file for the wrapper script
                string tempDir = Path.Combine(Path.GetTempPath(), "occlum_js");
                Directory.CreateDirectory(tempDir);
                string tempFilePath = Path.Combine(tempDir, $"script_{Guid.NewGuid():N}.js");
                await File.WriteAllTextAsync(tempFilePath, wrapperScript);

                try
                {
                    // Create a temporary file for the result
                    string resultFilePath = Path.Combine(tempDir, $"result_{Guid.NewGuid():N}.json");

                    // Execute the JavaScript code
                    string[] args = new string[] { "--result-file", resultFilePath, "--with-gas" };
                    int exitCode = await _occlumManager.ExecuteJavaScriptFileAsync(tempFilePath, args);

                    if (exitCode != 0)
                    {
                        throw new JavaScriptExecutionException($"JavaScript execution with gas accounting failed with exit code {exitCode}");
                    }

                    // Read the result
                    string resultJson = await File.ReadAllTextAsync(resultFilePath);

                    // Parse the result to extract the result and gas used
                    using JsonDocument document = JsonDocument.Parse(resultJson);
                    JsonElement root = document.RootElement;
                    
                    if (!root.TryGetProperty("result", out JsonElement resultElement))
                    {
                        throw new JavaScriptExecutionException("Result property not found in JavaScript execution result");
                    }
                    
                    if (!root.TryGetProperty("gasUsed", out JsonElement gasUsedElement))
                    {
                        throw new JavaScriptExecutionException("GasUsed property not found in JavaScript execution result");
                    }
                    
                    string result = resultElement.GetRawText();
                    ulong gasUsed = gasUsedElement.GetUInt64();

                    // Clean up the result file
                    try
                    {
                        File.Delete(resultFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary result file {ResultFilePath}", resultFilePath);
                    }

                    return (result, gasUsed);
                }
                finally
                {
                    // Clean up the temporary file
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file {TempFilePath}", tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code with gas accounting for function {FunctionId} and user {UserId}", functionId, userId);
                throw new JavaScriptExecutionException("Error executing JavaScript code with gas accounting", ex);
            }
        }

        /// <summary>
        /// Creates a wrapper script that includes the user code and executes it with the input.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The wrapper script.</returns>
        private string CreateWrapperScript(string code, string input, string secrets, string functionId, string userId)
        {
            // Create a wrapper script that includes the user code and executes it with the input
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("const fs = require('fs');");
            sb.AppendLine("const path = require('path');");
            sb.AppendLine();
            sb.AppendLine("// User code");
            sb.AppendLine(code);
            sb.AppendLine();
            sb.AppendLine("// Execute the user code");
            sb.AppendLine("try {");
            sb.AppendLine("    // Parse the input");
            sb.AppendLine($"    const input = {input};");
            sb.AppendLine($"    const secrets = {secrets};");
            sb.AppendLine($"    const functionId = '{functionId}';");
            sb.AppendLine($"    const userId = '{userId}';");
            sb.AppendLine();
            sb.AppendLine("    // Execute the main function");
            sb.AppendLine("    const result = main(input, secrets, functionId, userId);");
            sb.AppendLine();
            sb.AppendLine("    // Write the result to the result file");
            sb.AppendLine("    const resultFile = process.argv[3];");
            sb.AppendLine("    if (resultFile) {");
            sb.AppendLine("        fs.writeFileSync(resultFile, JSON.stringify(result));");
            sb.AppendLine("    } else {");
            sb.AppendLine("        console.log(JSON.stringify(result));");
            sb.AppendLine("    }");
            sb.AppendLine("} catch (error) {");
            sb.AppendLine("    // Write the error to the result file");
            sb.AppendLine("    const resultFile = process.argv[3];");
            sb.AppendLine("    const errorResult = { error: error.message, stack: error.stack };");
            sb.AppendLine("    if (resultFile) {");
            sb.AppendLine("        fs.writeFileSync(resultFile, JSON.stringify(errorResult));");
            sb.AppendLine("    } else {");
            sb.AppendLine("        console.error(JSON.stringify(errorResult));");
            sb.AppendLine("    }");
            sb.AppendLine("    process.exit(1);");
            sb.AppendLine("}");

            return sb.ToString();
        }
        
        /// <summary>
        /// Creates a wrapper script that includes the user code and executes it with the input and gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data for the JavaScript code.</param>
        /// <param name="secrets">The secrets for the JavaScript code.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user executing the function.</param>
        /// <returns>The wrapper script with gas accounting.</returns>
        private string CreateWrapperScriptWithGas(string code, string input, string secrets, string functionId, string userId)
        {
            // Create a wrapper script that includes the user code and executes it with the input and gas accounting
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("const fs = require('fs');");
            sb.AppendLine("const path = require('path');");
            sb.AppendLine();
            
            // Add gas accounting module
            sb.AppendLine("// Gas accounting module");
            sb.AppendLine("const gasAccounting = {");
            sb.AppendLine("    gasUsed: 0n,");
            sb.AppendLine("    trackGas: function(operation, size = 1) {");
            sb.AppendLine("        // Gas costs for different operations");
            sb.AppendLine("        const gasCosts = {");
            sb.AppendLine("            'call': 1n,         // Function call");
            sb.AppendLine("            'compute': 1n,      // Computation");
            sb.AppendLine("            'memory': 1n,       // Memory allocation");
            sb.AppendLine("            'storage': 10n,     // Storage operation");
            sb.AppendLine("            'io': 100n,         // I/O operation");
            sb.AppendLine("            'crypto': 50n,      // Cryptographic operation");
            sb.AppendLine("            'default': 1n       // Default cost");
            sb.AppendLine("        };");
            sb.AppendLine("        const cost = (gasCosts[operation] || gasCosts.default) * BigInt(size);");
            sb.AppendLine("        this.gasUsed += cost;");
            sb.AppendLine("        return cost;");
            sb.AppendLine("    },");
            sb.AppendLine("    getGasUsed: function() {");
            sb.AppendLine("        return this.gasUsed;");
            sb.AppendLine("    }");
            sb.AppendLine("};");
            sb.AppendLine();
            
            // Add the user code
            sb.AppendLine("// User code");
            sb.AppendLine(code);
            sb.AppendLine();
            
            // Execute the user code with gas accounting
            sb.AppendLine("// Execute the user code with gas accounting");
            sb.AppendLine("try {");
            sb.AppendLine("    // Parse the input");
            sb.AppendLine($"    const input = {input};");
            sb.AppendLine($"    const secrets = {secrets};");
            sb.AppendLine($"    const functionId = '{functionId}';");
            sb.AppendLine($"    const userId = '{userId}';");
            sb.AppendLine();
            sb.AppendLine("    // Add gas tracking to global scope");
            sb.AppendLine("    global.trackGas = gasAccounting.trackGas.bind(gasAccounting);");
            sb.AppendLine();
            sb.AppendLine("    // Track initial gas for function call");
            sb.AppendLine("    trackGas('call');");
            sb.AppendLine();
            sb.AppendLine("    // Execute the main function");
            sb.AppendLine("    const result = main(input, secrets, functionId, userId);");
            sb.AppendLine();
            sb.AppendLine("    // Get the gas used");
            sb.AppendLine("    const gasUsed = gasAccounting.getGasUsed();");
            sb.AppendLine();
            sb.AppendLine("    // Create the result object with gas usage information");
            sb.AppendLine("    const finalResult = {");
            sb.AppendLine("        result: result,");
            sb.AppendLine("        gasUsed: gasUsed");
            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine("    // Write the result to the result file");
            sb.AppendLine("    const resultFile = process.argv[3];");
            sb.AppendLine("    if (resultFile) {");
            sb.AppendLine("        fs.writeFileSync(resultFile, JSON.stringify(finalResult));");
            sb.AppendLine("    } else {");
            sb.AppendLine("        console.log(JSON.stringify(finalResult));");
            sb.AppendLine("    }");
            sb.AppendLine("} catch (error) {");
            sb.AppendLine("    // Write the error to the result file");
            sb.AppendLine("    const resultFile = process.argv[3];");
            sb.AppendLine("    const errorResult = {");
            sb.AppendLine("        error: error.message,");
            sb.AppendLine("        stack: error.stack,");
            sb.AppendLine("        gasUsed: gasAccounting.getGasUsed()");
            sb.AppendLine("    };");
            sb.AppendLine("    if (resultFile) {");
            sb.AppendLine("        fs.writeFileSync(resultFile, JSON.stringify(errorResult));");
            sb.AppendLine("    } else {");
            sb.AppendLine("        console.error(JSON.stringify(errorResult));");
            sb.AppendLine("    }");
            sb.AppendLine("    process.exit(1);");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
