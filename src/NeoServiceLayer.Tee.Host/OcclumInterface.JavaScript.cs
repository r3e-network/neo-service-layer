using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// JavaScript execution functionality for the OcclumInterface.
    /// </summary>
    public partial class OcclumInterface
    {
        /// <inheritdoc/>
        public async Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            CheckDisposed();

            try
            {
                return await _jsExecution.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code for function {FunctionId}", functionId);
                throw new JavaScriptExecutionException("Error executing JavaScript code", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<(string Result, ulong GasUsed)> ExecuteJavaScriptWithGasAsync(string code, string input, string secrets, string functionId, string userId)
        {
            CheckDisposed();

            try
            {
                var result = await _jsExecution.ExecuteJavaScriptWithGasAsync(code, input, secrets, functionId, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code with gas accounting for function {FunctionId}", functionId);
                throw new JavaScriptExecutionException("Error executing JavaScript code with gas accounting", ex);
            }
        }
        
        /// <inheritdoc/>
        public Task<string> ExecuteJavaScriptWithGasAsync(string code, string input, string secrets, string functionId, string userId, out ulong gasUsed)
        {
            // This method is included for compatibility with ITeeInterface
            throw new NotSupportedException("This method is not supported in Occlum. Use the tuple-returning overload instead.");
        }
    }
} 