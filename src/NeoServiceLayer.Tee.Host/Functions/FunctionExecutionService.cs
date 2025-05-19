using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Implementation of the function execution service that delegates execution to the enclave.
    /// </summary>
    public class FunctionExecutionService : IFunctionExecutionService
    {
        private readonly ILogger<FunctionExecutionService> _logger;
        private readonly IStorageManager _storageManager;
        private readonly IFunctionExecutionEnclave _enclave;
        private readonly SemaphoreSlim _semaphore;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the FunctionExecutionService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageManager">The storage manager.</param>
        /// <param name="enclave">The function execution enclave.</param>
        public FunctionExecutionService(
            ILogger<FunctionExecutionService> logger,
            IStorageManager storageManager,
            IFunctionExecutionEnclave enclave)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _enclave = enclave ?? throw new ArgumentNullException(nameof(enclave));
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

                    // Initialize the enclave
                    if (!await _enclave.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize function execution enclave");
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
                    _logger.LogInformation("Function execution service initialized successfully");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing function execution service");
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

                // Execute the function in the enclave
                var result = await _enclave.ExecuteFunctionAsync(
                    context.FunctionId,
                    context.Code,
                    context.EntryPoint,
                    context.Runtime,
                    context.Input,
                    context.Secrets,
                    context.UserId,
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

                // Calculate the code hash in the enclave
                function.CodeHash = await _enclave.CalculateCodeHashAsync(function.Code);

                // Set timestamps
                function.CreatedAt = DateTime.UtcNow;
                function.UpdatedAt = DateTime.UtcNow;

                // Validate the function in the enclave
                var validationErrors = await _enclave.ValidateFunctionAsync(function.Code, function.EntryPoint, function.Runtime);
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

                // Calculate the code hash in the enclave
                function.CodeHash = await _enclave.CalculateCodeHashAsync(function.Code);

                // Set timestamps
                function.CreatedAt = existingFunction.CreatedAt;
                function.UpdatedAt = DateTime.UtcNow;

                // Validate the function in the enclave
                var validationErrors = await _enclave.ValidateFunctionAsync(function.Code, function.EntryPoint, function.Runtime);
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

        /// <inheritdoc/>
        public async Task<(IReadOnlyList<FunctionInfo> Functions, int TotalCount)> ListFunctionsAsync(
            string userId,
            FunctionStatus? status = null,
            int page = 1,
            int pageSize = 10)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            try
            {
                // Get all functions
                var storageProvider = _storageManager.GetProvider("functions");
                var keys = await storageProvider.GetAllKeysAsync();

                // Get the latest version of each function
                var functionIds = new HashSet<string>();
                foreach (var key in keys)
                {
                    var parts = key.Split('_');
                    if (parts.Length >= 2)
                    {
                        functionIds.Add(parts[0]);
                    }
                }

                // Get the latest version of each function
                var functions = new List<FunctionInfo>();
                foreach (var functionId in functionIds)
                {
                    var function = await GetFunctionAsync(functionId);
                    if (function != null && function.OwnerId == userId && (!status.HasValue || function.Status == status.Value))
                    {
                        functions.Add(function);
                    }
                }

                // Apply pagination
                var totalCount = functions.Count;
                var pagedFunctions = functions
                    .OrderByDescending(f => f.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedFunctions, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing functions for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeployFunctionAsync(string functionId, string version)
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
                // Get the function version
                var function = await GetFunctionVersionAsync(functionId, version);
                if (function == null)
                {
                    _logger.LogWarning("Function version not found for deployment: {FunctionId} {Version}", functionId, version);
                    return false;
                }

                // Update the function status
                function.Status = FunctionStatus.Active;
                function.DeployedAt = DateTime.UtcNow;
                function.UpdatedAt = DateTime.UtcNow;

                // Save the function
                await SaveFunctionAsync(function);

                _logger.LogInformation("Deployed function {FunctionId} version {Version}", functionId, version);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying function {FunctionId} version {Version}", functionId, version);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> ValidateFunctionAsync(FunctionInfo function)
        {
            CheckDisposed();
            CheckInitialized();

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                // Validate the function in the enclave
                var errors = await _enclave.ValidateFunctionAsync(function.Code, function.EntryPoint, function.Runtime);

                // Add additional validation errors
                var allErrors = new List<string>(errors);

                if (string.IsNullOrEmpty(function.Name))
                {
                    allErrors.Add("Function name is required");
                }

                if (string.IsNullOrEmpty(function.EntryPoint))
                {
                    allErrors.Add("Function entry point is required");
                }

                if (string.IsNullOrEmpty(function.Code))
                {
                    allErrors.Add("Function code is required");
                }

                if (function.GasLimit == 0)
                {
                    allErrors.Add("Gas limit must be greater than zero");
                }

                if (function.TimeoutMs == 0)
                {
                    allErrors.Add("Timeout must be greater than zero");
                }

                if (function.MemoryLimit == 0)
                {
                    allErrors.Add("Memory limit must be greater than zero");
                }

                return allErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating function");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(IReadOnlyList<FunctionExecutionRecord> Records, int TotalCount)> GetExecutionHistoryAsync(
            string functionId,
            int page = 1,
            int pageSize = 10)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            try
            {
                // Get all execution records for the function
                var storageProvider = _storageManager.GetProvider("function_executions");
                var keys = await storageProvider.GetAllKeysAsync();
                var functionKeys = keys.Where(k => k.StartsWith($"{functionId}_")).ToList();

                // Get all execution records
                var records = new List<FunctionExecutionRecord>();
                foreach (var key in functionKeys)
                {
                    var data = await storageProvider.ReadAsync(key);
                    if (data != null && data.Length > 0)
                    {
                        var json = Encoding.UTF8.GetString(data);
                        var record = JsonSerializer.Deserialize<FunctionExecutionRecord>(json);
                        records.Add(record);
                    }
                }

                // Apply pagination
                var totalCount = records.Count;
                var pagedRecords = records
                    .OrderByDescending(r => r.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedRecords, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for function {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(IReadOnlyList<FunctionExecutionRecord> Records, int TotalCount)> GetUserExecutionHistoryAsync(
            string userId,
            int page = 1,
            int pageSize = 10)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            try
            {
                // Get all execution records
                var storageProvider = _storageManager.GetProvider("function_executions");
                var keys = await storageProvider.GetAllKeysAsync();

                // Get all execution records for the user
                var records = new List<FunctionExecutionRecord>();
                foreach (var key in keys)
                {
                    var data = await storageProvider.ReadAsync(key);
                    if (data != null && data.Length > 0)
                    {
                        var json = Encoding.UTF8.GetString(data);
                        var record = JsonSerializer.Deserialize<FunctionExecutionRecord>(json);
                        if (record.UserId == userId)
                        {
                            records.Add(record);
                        }
                    }
                }

                // Apply pagination
                var totalCount = records.Count;
                var pagedRecords = records
                    .OrderByDescending(r => r.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedRecords, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionExecutionRecord> GetExecutionRecordAsync(string executionId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(executionId))
            {
                throw new ArgumentException("Execution ID cannot be null or empty", nameof(executionId));
            }

            try
            {
                // Get the execution record
                var storageProvider = _storageManager.GetProvider("function_executions");
                var data = await storageProvider.ReadAsync(executionId);
                if (data == null || data.Length == 0)
                {
                    _logger.LogWarning("Execution record not found: {ExecutionId}", executionId);
                    return null;
                }

                // Deserialize the execution record
                var json = Encoding.UTF8.GetString(data);
                var record = JsonSerializer.Deserialize<FunctionExecutionRecord>(json);

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution record {ExecutionId}", executionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionGasUsage> GetFunctionGasUsageAsync(string functionId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            try
            {
                // Get all execution records for the function
                var (records, _) = await GetExecutionHistoryAsync(functionId, 1, int.MaxValue);

                // Calculate gas usage
                var gasUsage = new FunctionGasUsage
                {
                    Id = functionId,
                    TotalGasUsed = (ulong)records.Sum(r => (long)r.GasUsed),
                    ExecutionCount = (ulong)records.Count
                };

                if (gasUsage.ExecutionCount > 0)
                {
                    gasUsage.AverageGasUsed = gasUsage.TotalGasUsed / gasUsage.ExecutionCount;
                }

                // Calculate gas usage by day
                foreach (var record in records)
                {
                    var day = record.StartTime.Date;
                    if (gasUsage.GasUsageByDay.TryGetValue(day, out var dailyGasUsed))
                    {
                        gasUsage.GasUsageByDay[day] = dailyGasUsed + record.GasUsed;
                    }
                    else
                    {
                        gasUsage.GasUsageByDay[day] = record.GasUsed;
                    }
                }

                // Calculate gas usage by user
                foreach (var record in records)
                {
                    if (gasUsage.GasUsageByUser.TryGetValue(record.UserId, out var userGasUsed))
                    {
                        gasUsage.GasUsageByUser[record.UserId] = userGasUsed + record.GasUsed;
                    }
                    else
                    {
                        gasUsage.GasUsageByUser[record.UserId] = record.GasUsed;
                    }
                }

                return gasUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gas usage for function {FunctionId}", functionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionGasUsage> GetUserGasUsageAsync(string userId)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            try
            {
                // Get all execution records for the user
                var (records, _) = await GetUserExecutionHistoryAsync(userId, 1, int.MaxValue);

                // Calculate gas usage
                var gasUsage = new FunctionGasUsage
                {
                    Id = userId,
                    TotalGasUsed = (ulong)records.Sum(r => (long)r.GasUsed),
                    ExecutionCount = (ulong)records.Count
                };

                if (gasUsage.ExecutionCount > 0)
                {
                    gasUsage.AverageGasUsed = gasUsage.TotalGasUsed / gasUsage.ExecutionCount;
                }

                // Calculate gas usage by day
                foreach (var record in records)
                {
                    var day = record.StartTime.Date;
                    if (gasUsage.GasUsageByDay.TryGetValue(day, out var dailyGasUsed))
                    {
                        gasUsage.GasUsageByDay[day] = dailyGasUsed + record.GasUsed;
                    }
                    else
                    {
                        gasUsage.GasUsageByDay[day] = record.GasUsed;
                    }
                }

                // Calculate gas usage by function
                foreach (var record in records)
                {
                    if (gasUsage.GasUsageByFunction.TryGetValue(record.FunctionId, out var functionGasUsed))
                    {
                        gasUsage.GasUsageByFunction[record.FunctionId] = functionGasUsed + record.GasUsed;
                    }
                    else
                    {
                        gasUsage.GasUsageByFunction[record.FunctionId] = record.GasUsed;
                    }
                }

                return gasUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gas usage for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> CalculateCodeHashAsync(string code)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            try
            {
                // Calculate the code hash in the enclave
                return await _enclave.CalculateCodeHashAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating code hash");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyCodeHashAsync(string code, string hash)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
            }

            try
            {
                // Verify the code hash in the enclave
                return await _enclave.VerifyCodeHashAsync(code, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying code hash");
                throw;
            }
        }

        /// <summary>
        /// Saves a function to storage.
        /// </summary>
        /// <param name="function">The function to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SaveFunctionAsync(FunctionInfo function)
        {
            var storageProvider = _storageManager.GetProvider("functions");
            var key = $"{function.Id}_{function.Version}";
            var json = JsonSerializer.Serialize(function);
            var data = Encoding.UTF8.GetBytes(json);
            await storageProvider.WriteAsync(key, data);
        }

        /// <summary>
        /// Records a function execution.
        /// </summary>
        /// <param name="context">The function execution context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RecordExecutionAsync(FunctionExecutionContext context)
        {
            var storageProvider = _storageManager.GetProvider("function_executions");

            // Create the execution record
            var record = new FunctionExecutionRecord
            {
                ExecutionId = context.ExecutionId,
                FunctionId = context.FunctionId,
                FunctionVersion = context.FunctionVersion,
                FunctionName = await GetFunctionNameAsync(context.FunctionId),
                UserId = context.UserId,
                Success = context.Success,
                Error = context.Error,
                GasUsed = context.GasUsed,
                ExecutionTimeMs = context.ExecutionTimeMs,
                MemoryUsed = context.MemoryUsed,
                StartTime = context.StartTime,
                EndTime = context.EndTime,
                Metadata = context.Metadata
            };

            // Save the execution record
            var json = JsonSerializer.Serialize(record);
            var data = Encoding.UTF8.GetBytes(json);
            await storageProvider.WriteAsync(context.ExecutionId, data);
        }

        /// <summary>
        /// Updates function statistics based on an execution.
        /// </summary>
        /// <param name="context">The function execution context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateFunctionStatisticsAsync(FunctionExecutionContext context)
        {
            // Get the function
            var function = await GetFunctionAsync(context.FunctionId);
            if (function == null)
            {
                return;
            }

            // Update statistics
            function.LastExecutedAt = DateTime.UtcNow;
            function.ExecutionCount++;
            function.TotalGasUsed += context.GasUsed;
            function.AverageGasUsed = function.TotalGasUsed / function.ExecutionCount;
            function.AverageExecutionTimeMs = ((function.AverageExecutionTimeMs * (function.ExecutionCount - 1)) + context.ExecutionTimeMs) / function.ExecutionCount;

            // Save the function
            await SaveFunctionAsync(function);
        }

        /// <summary>
        /// Gets the name of a function.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <returns>The name of the function, or null if not found.</returns>
        private async Task<string> GetFunctionNameAsync(string functionId)
        {
            var function = await GetFunctionAsync(functionId);
            return function?.Name;
        }

        /// <summary>
        /// Increments a version string.
        /// </summary>
        /// <param name="version">The version string to increment.</param>
        /// <returns>The incremented version string.</returns>
        private string IncrementVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return "1.0.0";
            }

            var parts = version.Split('.');
            if (parts.Length != 3)
            {
                return "1.0.0";
            }

            if (!int.TryParse(parts[2], out var patch))
            {
                return "1.0.0";
            }

            patch++;
            return $"{parts[0]}.{parts[1]}.{patch}";
        }

        /// <summary>
        /// Checks if the service is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Function execution service is not initialized");
            }
        }

        /// <summary>
        /// Checks if the service is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FunctionExecutionService));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the service.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _semaphore.Dispose();
                    _enclave.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the service.
        /// </summary>
        ~FunctionExecutionService()
        {
            Dispose(false);
        }
    }
}
