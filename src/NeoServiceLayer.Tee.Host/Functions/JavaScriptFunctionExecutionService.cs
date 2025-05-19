using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Shared.Functions;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Functions
{
    /// <summary>
    /// Implementation of the function execution service for JavaScript functions.
    /// </summary>
    public class JavaScriptFunctionExecutionService : IFunctionExecutionService
    {
        private readonly ILogger<JavaScriptFunctionExecutionService> _logger;
        private readonly IStorageManager _storageManager;
        private readonly IJavaScriptEngine _jsEngine;
        private readonly SemaphoreSlim _semaphore;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the JavaScriptFunctionExecutionService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageManager">The storage manager.</param>
        /// <param name="jsEngine">The JavaScript engine.</param>
        public JavaScriptFunctionExecutionService(
            ILogger<JavaScriptFunctionExecutionService> logger,
            IStorageManager storageManager,
            IJavaScriptEngine jsEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _jsEngine = jsEngine ?? throw new ArgumentNullException(nameof(jsEngine));
            _semaphore = new SemaphoreSlim(1, 1);
            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    // Initialize the JavaScript engine
                    if (!await _jsEngine.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize JavaScript engine");
                        return false;
                    }

                    // Initialize storage providers
                    var functionsProvider = _storageManager.GetProvider("functions");
                    if (functionsProvider == null)
                    {
                        // Create a new storage provider for functions
                        functionsProvider = await _storageManager.CreateProviderAsync(
                            "functions",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "functions" });

                        if (functionsProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for functions");
                            return false;
                        }
                    }

                    var executionsProvider = _storageManager.GetProvider("function_executions");
                    if (executionsProvider == null)
                    {
                        // Create a new storage provider for function executions
                        executionsProvider = await _storageManager.CreateProviderAsync(
                            "function_executions",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "function_executions" });

                        if (executionsProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for function executions");
                            return false;
                        }
                    }

                    _initialized = true;
                    _logger.LogInformation("JavaScript function execution service initialized successfully");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing JavaScript function execution service");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteAsync(FunctionExecutionContext context)
        {
            CheckDisposed();
            CheckInitialized();

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                _logger.LogInformation("Executing function {FunctionId} for user {UserId}", context.FunctionId, context.UserId);

                // Get the function if not provided in the context
                if (string.IsNullOrEmpty(context.Code))
                {
                    var function = await GetFunctionAsync(context.FunctionId);
                    if (function == null)
                    {
                        context.SetError($"Function not found: {context.FunctionId}", 0);
                        _logger.LogWarning("Function not found: {FunctionId}", context.FunctionId);
                        return false;
                    }

                    context.Code = function.Code;
                    context.EntryPoint = function.EntryPoint;
                    context.Runtime = function.Runtime;
                    context.TimeoutMs = function.TimeoutMs;
                    context.MemoryLimit = function.MemoryLimit;
                    context.FunctionVersion = function.Version;
                }

                // Execute the function
                var result = await _jsEngine.ExecuteAsync(
                    context.Code,
                    context.EntryPoint,
                    context.Input,
                    context.Secrets,
                    context.GasLimit,
                    context.TimeoutMs);

                // Update the context with the result
                if (result.Success)
                {
                    context.SetResult(result.Result, result.GasUsed);
                    context.MemoryUsed = result.MemoryUsed;
                    context.Logs.AddRange(result.Logs);
                }
                else
                {
                    context.SetError(result.Error, result.GasUsed);
                    context.MemoryUsed = result.MemoryUsed;
                    context.Logs.AddRange(result.Logs);
                }

                // Record the execution
                await RecordExecutionAsync(context);

                // Update function statistics
                await UpdateFunctionStatisticsAsync(context);

                _logger.LogInformation("Function {FunctionId} executed {Status} for user {UserId}, gas used: {GasUsed}, execution time: {ExecutionTimeMs}ms",
                    context.FunctionId, context.Success ? "successfully" : "with error", context.UserId, context.GasUsed, context.ExecutionTimeMs);

                return context.Success;
            }
            catch (Exception ex)
            {
                context.SetError($"Error executing function: {ex.Message}", 0);
                _logger.LogError(ex, "Error executing function {FunctionId} for user {UserId}", context.FunctionId, context.UserId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionExecutionContext> ExecuteAsync(string functionId, string input, string userId, ulong gasLimit)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            // Create the execution context
            var context = new FunctionExecutionContext
            {
                FunctionId = functionId,
                UserId = userId,
                Input = input,
                GasLimit = gasLimit
            };

            // Execute the function
            await ExecuteAsync(context);

            return context;
        }

        /// <inheritdoc/>
        public async Task<FunctionInfo> CreateFunctionAsync(FunctionInfo function)
        {
            CheckDisposed();
            CheckInitialized();

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                // Generate a new ID if not provided
                if (string.IsNullOrEmpty(function.Id))
                {
                    function.Id = Guid.NewGuid().ToString();
                }

                // Set the version if not provided
                if (string.IsNullOrEmpty(function.Version))
                {
                    function.Version = "1.0.0";
                }

                // Calculate the code hash
                function.CodeHash = await CalculateCodeHashAsync(function.Code);

                // Set timestamps
                function.CreatedAt = DateTime.UtcNow;
                function.UpdatedAt = DateTime.UtcNow;

                // Validate the function
                var validationErrors = await ValidateFunctionAsync(function);
                if (validationErrors.Count > 0)
                {
                    throw new InvalidOperationException($"Function validation failed: {string.Join(", ", validationErrors)}");
                }

                // Save the function
                await SaveFunctionAsync(function);

                _logger.LogInformation("Created function {FunctionId} version {Version} for user {UserId}", function.Id, function.Version, function.OwnerId);

                return function;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating function for user {UserId}", function.OwnerId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionInfo> UpdateFunctionAsync(FunctionInfo function)
        {
            CheckDisposed();
            CheckInitialized();

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                // Get the existing function
                var existingFunction = await GetFunctionAsync(function.Id);
                if (existingFunction == null)
                {
                    throw new InvalidOperationException($"Function not found: {function.Id}");
                }

                // Increment the version
                function.Version = IncrementVersion(existingFunction.Version);

                // Calculate the code hash
                function.CodeHash = await CalculateCodeHashAsync(function.Code);

                // Set timestamps
                function.CreatedAt = existingFunction.CreatedAt;
                function.UpdatedAt = DateTime.UtcNow;

                // Validate the function
                var validationErrors = await ValidateFunctionAsync(function);
                if (validationErrors.Count > 0)
                {
                    throw new InvalidOperationException($"Function validation failed: {string.Join(", ", validationErrors)}");
                }

                // Save the function
                await SaveFunctionAsync(function);

                _logger.LogInformation("Updated function {FunctionId} to version {Version} for user {UserId}", function.Id, function.Version, function.OwnerId);

                return function;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating function {FunctionId} for user {UserId}", function.Id, function.OwnerId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionInfo> GetFunctionAsync(string functionId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            try
            {
                // Get the latest version of the function
                var versions = await GetFunctionVersionsAsync(functionId);
                if (versions.Count == 0)
                {
                    _logger.LogWarning("Function not found: {FunctionId}", functionId);
                    return null;
                }

                // Return the latest version
                return versions.OrderByDescending(v => v.Version).First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionInfo> GetFunctionVersionAsync(string functionId, string version)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("Version cannot be null or empty", nameof(version));
            }

            try
            {
                // Get the function from storage
                var storageProvider = _storageManager.GetProvider("functions");
                var key = $"{functionId}_{version}";
                var data = await storageProvider.ReadAsync(key);
                if (data == null || data.Length == 0)
                {
                    _logger.LogWarning("Function version not found: {FunctionId} {Version}", functionId, version);
                    return null;
                }

                // Deserialize the function
                var json = Encoding.UTF8.GetString(data);
                var function = JsonSerializer.Deserialize<FunctionInfo>(json);

                return function;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function version {FunctionId} {Version}", functionId, version);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FunctionInfo>> GetFunctionVersionsAsync(string functionId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            try
            {
                // Get all keys for the function
                var storageProvider = _storageManager.GetProvider("functions");
                var keys = await storageProvider.GetAllKeysAsync();
                var functionKeys = keys.Where(k => k.StartsWith($"{functionId}_")).ToList();

                // Get all versions
                var versions = new List<FunctionInfo>();
                foreach (var key in functionKeys)
                {
                    var data = await storageProvider.ReadAsync(key);
                    if (data != null && data.Length > 0)
                    {
                        var json = Encoding.UTF8.GetString(data);
                        var function = JsonSerializer.Deserialize<FunctionInfo>(json);
                        versions.Add(function);
                    }
                }

                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function versions {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteFunctionAsync(string functionId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            try
            {
                // Get all versions of the function
                var versions = await GetFunctionVersionsAsync(functionId);
                if (versions.Count == 0)
                {
                    _logger.LogWarning("Function not found for deletion: {FunctionId}", functionId);
                    return false;
                }

                // Delete all versions
                var storageProvider = _storageManager.GetProvider("functions");
                foreach (var function in versions)
                {
                    var key = $"{functionId}_{function.Version}";
                    await storageProvider.DeleteAsync(key);
                }

                _logger.LogInformation("Deleted function {FunctionId} with {VersionCount} versions", functionId, versions.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting function {FunctionId}", functionId);
                throw;
            }
        }
    }
}
