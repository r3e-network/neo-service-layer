using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Service for executing JavaScript functions in the TEE.
    /// </summary>
    public class JavaScriptExecutionService : IJavaScriptExecutionService
    {
        private readonly ITeeHostService _teeHostService;
        private readonly IUserSecretService _userSecretService;
        private readonly IGasAccountingService _gasAccountingService;
        private readonly IRepository<JavaScriptFunction, string> _functionRepository;
        private readonly ILogger<JavaScriptExecutionService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionService"/> class.
        /// </summary>
        /// <param name="teeHostService">The TEE host service.</param>
        /// <param name="userSecretService">The user secret service.</param>
        /// <param name="gasAccountingService">The GAS accounting service.</param>
        /// <param name="functionRepository">The function repository.</param>
        /// <param name="logger">The logger.</param>
        public JavaScriptExecutionService(
            ITeeHostService teeHostService,
            IUserSecretService userSecretService,
            IGasAccountingService gasAccountingService,
            IRepository<JavaScriptFunction, string> functionRepository,
            ILogger<JavaScriptExecutionService> logger)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
            _userSecretService = userSecretService ?? throw new ArgumentNullException(nameof(userSecretService));
            _gasAccountingService = gasAccountingService ?? throw new ArgumentNullException(nameof(gasAccountingService));
            _functionRepository = functionRepository ?? throw new ArgumentNullException(nameof(functionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<JavaScriptFunction> CreateFunctionAsync(JavaScriptFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            function.Id = Guid.NewGuid().ToString();
            function.CreatedAt = DateTime.UtcNow;
            function.UpdatedAt = DateTime.UtcNow;

            await _functionRepository.AddAsync(function);
            _logger.LogInformation("Created JavaScript function: {FunctionId}", function.Id);

            return function;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteFunctionAsync(string functionId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentNullException(nameof(functionId));
            }

            var function = await _functionRepository.GetByIdAsync(functionId);
            if (function == null)
            {
                _logger.LogWarning("JavaScript function not found: {FunctionId}", functionId);
                return false;
            }

            function.Status = JavaScriptFunctionStatus.Inactive;
            function.UpdatedAt = DateTime.UtcNow;

            await _functionRepository.UpdateAsync(function);
            _logger.LogInformation("Deleted JavaScript function: {FunctionId}", functionId);

            return true;
        }

        /// <inheritdoc/>
        public async Task<JsonDocument> ExecuteFunctionAsync(string functionId, JsonDocument input, string userId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentNullException(nameof(functionId));
            }

            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var function = await _functionRepository.GetByIdAsync(functionId);
            if (function == null)
            {
                _logger.LogWarning("JavaScript function not found: {FunctionId}", functionId);
                throw new InvalidOperationException($"JavaScript function not found: {functionId}");
            }

            if (function.Status != JavaScriptFunctionStatus.Active)
            {
                _logger.LogWarning("JavaScript function is not active: {FunctionId}", functionId);
                throw new InvalidOperationException($"JavaScript function is not active: {functionId}");
            }

            // Check if the user has enough GAS
            if (!await _gasAccountingService.HasEnoughGasAsync(userId, function.GasLimit))
            {
                _logger.LogWarning("User does not have enough GAS: {UserId}", userId);
                throw new InvalidOperationException($"User does not have enough GAS: {userId}");
            }

            // Get the user secrets required by the function
            Dictionary<string, string> secrets = null;
            if (function.RequiredSecrets != null && function.RequiredSecrets.Count > 0)
            {
                secrets = await _userSecretService.GetSecretValuesByNamesAsync(function.RequiredSecrets, userId);
            }

            // Create the execution request
            var executionId = Guid.NewGuid().ToString();
            var request = new TeeMessage
            {
                Id = executionId,
                Type = TeeMessageType.JavaScriptExecution,
                Data = JsonSerializer.Serialize(new
                {
                    function_id = functionId,
                    function_code = function.Code,
                    input = input,
                    secrets = secrets,
                    user_id = userId,
                    gas_limit = function.GasLimit
                }),
                CreatedAt = DateTime.UtcNow
            };

            // Send the request to the TEE
            _logger.LogInformation("Executing JavaScript function: {FunctionId}", functionId);
            var response = await _teeHostService.SendMessageAsync(request);

            // Parse the response
            var result = JsonSerializer.Deserialize<JsonDocument>(response.Data);

            // Record the GAS usage
            var gasUsed = result.RootElement.GetProperty("gas_used").GetInt64();
            await _gasAccountingService.RecordGasUsageAsync(userId, functionId, executionId, gasUsed);

            // Return the result
            return result;
        }

        /// <inheritdoc/>
        public async Task<JavaScriptFunction> GetFunctionAsync(string functionId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentNullException(nameof(functionId));
            }

            var function = await _functionRepository.GetByIdAsync(functionId);
            if (function == null)
            {
                _logger.LogWarning("JavaScript function not found: {FunctionId}", functionId);
                return null;
            }

            return function;
        }

        /// <inheritdoc/>
        public async Task<(List<JavaScriptFunction> Functions, int TotalCount)> ListFunctionsAsync(
            string userId,
            JavaScriptFunctionStatus? status = null,
            int page = 1,
            int pageSize = 10)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var query = new Dictionary<string, object>
            {
                { "OwnerId", userId }
            };

            if (status.HasValue)
            {
                query.Add("Status", status.Value);
            }

            // Convert dictionary to expression
            var functions = await _functionRepository.FindAsync(f => f.OwnerId == userId && (!status.HasValue || f.Status == status.Value));
            var functionsList = functions.ToList();
            int totalCount = functionsList.Count;

            return (functionsList, totalCount);
        }

        /// <inheritdoc/>
        public async Task<JavaScriptFunction> UpdateFunctionAsync(JavaScriptFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            var existingFunction = await _functionRepository.GetByIdAsync(function.Id);
            if (existingFunction == null)
            {
                _logger.LogWarning("JavaScript function not found: {FunctionId}", function.Id);
                return null;
            }

            existingFunction.Name = function.Name;
            existingFunction.Description = function.Description;
            existingFunction.Code = function.Code;
            existingFunction.RequiredSecrets = function.RequiredSecrets;
            existingFunction.GasLimit = function.GasLimit;
            existingFunction.Status = function.Status;
            existingFunction.UpdatedAt = DateTime.UtcNow;

            await _functionRepository.UpdateAsync(existingFunction);
            _logger.LogInformation("Updated JavaScript function: {FunctionId}", function.Id);

            return existingFunction;
        }
    }
}
