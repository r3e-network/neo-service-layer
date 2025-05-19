using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.JavaScript;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Shared.Events;
using NeoServiceLayer.Tee.Shared.JavaScript;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Manager for event triggers.
    /// </summary>
    public class EventTriggerManager : IEventTriggerManager
    {
        private readonly ILogger<EventTriggerManager> _logger;
        private readonly IStorageManager _storageManager;
        private readonly IJavaScriptManager _jsManager;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, EventTriggerInfo> _triggers;
        private readonly ConcurrentDictionary<EventTriggerType, List<string>> _triggersByType;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the EventTriggerManager class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageManager">The storage manager.</param>
        /// <param name="jsManager">The JavaScript manager.</param>
        public EventTriggerManager(
            ILogger<EventTriggerManager> logger,
            IStorageManager storageManager,
            IJavaScriptManager jsManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _jsManager = jsManager ?? throw new ArgumentNullException(nameof(jsManager));
            _semaphore = new SemaphoreSlim(1, 1);
            _triggers = new ConcurrentDictionary<string, EventTriggerInfo>();
            _triggersByType = new ConcurrentDictionary<EventTriggerType, List<string>>();
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

                    // Initialize storage provider for triggers
                    var storageProvider = _storageManager.GetProvider("triggers");
                    if (storageProvider == null)
                    {
                        // Create a new storage provider for triggers
                        storageProvider = await _storageManager.CreateProviderAsync(
                            "triggers",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "triggers" });

                        if (storageProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for triggers");
                            return false;
                        }
                    }

                    // Load triggers from storage
                    if (!await LoadTriggersAsync())
                    {
                        _logger.LogError("Failed to load triggers from storage");
                        return false;
                    }

                    _initialized = true;
                    _logger.LogInformation("Event trigger manager initialized with {TriggerCount} triggers", _triggers.Count);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing event trigger manager");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterTriggerAsync(EventTriggerInfo trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            if (string.IsNullOrEmpty(trigger.Id) || string.IsNullOrEmpty(trigger.FunctionId))
            {
                throw new ArgumentException("Trigger ID and function ID cannot be null or empty");
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if trigger already exists
                    if (_triggers.ContainsKey(trigger.Id))
                    {
                        return false;
                    }

                    // Save trigger to storage
                    if (!await SaveTriggerAsync(trigger))
                    {
                        return false;
                    }

                    // Add trigger to in-memory maps
                    _triggers[trigger.Id] = trigger;
                    _triggersByType.AddOrUpdate(
                        trigger.Type,
                        new List<string> { trigger.Id },
                        (key, list) =>
                        {
                            list.Add(trigger.Id);
                            return list;
                        });

                    _logger.LogInformation("Registered trigger {TriggerId} of type {TriggerType}", trigger.Id, trigger.Type);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering trigger {TriggerId}", trigger.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UnregisterTriggerAsync(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if trigger exists
                    if (!_triggers.TryGetValue(triggerId, out var trigger))
                    {
                        return false;
                    }

                    // Remove trigger from storage
                    var storageProvider = _storageManager.GetProvider("triggers");
                    if (storageProvider == null)
                    {
                        _logger.LogError("Storage provider for triggers not found");
                        return false;
                    }

                    if (!await storageProvider.DeleteAsync(triggerId))
                    {
                        _logger.LogError("Failed to delete trigger {TriggerId} from storage", triggerId);
                        return false;
                    }

                    // Remove trigger from in-memory maps
                    _triggers.TryRemove(triggerId, out _);
                    if (_triggersByType.TryGetValue(trigger.Type, out var triggerList))
                    {
                        triggerList.Remove(triggerId);
                    }

                    _logger.LogInformation("Unregistered trigger {TriggerId}", triggerId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering trigger {TriggerId}", triggerId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<EventTriggerInfo> GetTriggerAsync(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }

            CheckDisposed();
            CheckInitialized();

            if (_triggers.TryGetValue(triggerId, out var trigger))
            {
                return trigger;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<EventTriggerInfo>> ListTriggersAsync()
        {
            CheckDisposed();
            CheckInitialized();

            return _triggers.Values.ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<EventTriggerInfo>> ListTriggersByTypeAsync(EventTriggerType triggerType)
        {
            CheckDisposed();
            CheckInitialized();

            if (_triggersByType.TryGetValue(triggerType, out var triggerIds))
            {
                var triggers = new List<EventTriggerInfo>();
                foreach (var triggerId in triggerIds)
                {
                    if (_triggers.TryGetValue(triggerId, out var trigger))
                    {
                        triggers.Add(trigger);
                    }
                }
                return triggers;
            }

            return new List<EventTriggerInfo>();
        }

        /// <inheritdoc/>
        public async Task<bool> EnableTriggerAsync(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if trigger exists
                    if (!_triggers.TryGetValue(triggerId, out var trigger))
                    {
                        return false;
                    }

                    // Update trigger
                    trigger.Enabled = true;
                    trigger.Status = EventTriggerStatus.Active;
                    trigger.UpdatedAt = DateTime.UtcNow;

                    // Save trigger to storage
                    if (!await SaveTriggerAsync(trigger))
                    {
                        return false;
                    }

                    _logger.LogInformation("Enabled trigger {TriggerId}", triggerId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling trigger {TriggerId}", triggerId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DisableTriggerAsync(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if trigger exists
                    if (!_triggers.TryGetValue(triggerId, out var trigger))
                    {
                        return false;
                    }

                    // Update trigger
                    trigger.Enabled = false;
                    trigger.Status = EventTriggerStatus.Inactive;
                    trigger.UpdatedAt = DateTime.UtcNow;

                    // Save trigger to storage
                    if (!await SaveTriggerAsync(trigger))
                    {
                        return false;
                    }

                    _logger.LogInformation("Disabled trigger {TriggerId}", triggerId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling trigger {TriggerId}", triggerId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ProcessScheduledTriggersAsync(ulong currentTime)
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                int processedCount = 0;

                // Get scheduled triggers
                var scheduledTriggers = await ListTriggersByTypeAsync(EventTriggerType.Schedule);
                foreach (var trigger in scheduledTriggers)
                {
                    // Skip disabled triggers
                    if (!trigger.Enabled || trigger.Status != EventTriggerStatus.Active)
                    {
                        continue;
                    }

                    // Check if it's time to execute
                    if (currentTime >= trigger.NextExecutionTime)
                    {
                        try
                        {
                            // Execute trigger
                            await ExecuteTriggerAsync(trigger.Id, "{}");
                            processedCount++;

                            // Update next execution time
                            trigger.NextExecutionTime = currentTime + trigger.IntervalSeconds;
                            trigger.UpdatedAt = DateTime.UtcNow;

                            // Save trigger to storage
                            await SaveTriggerAsync(trigger);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing scheduled trigger {TriggerId}", trigger.Id);
                        }
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled triggers");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ProcessBlockchainEventAsync(string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                throw new ArgumentException("Event data cannot be null or empty", nameof(eventData));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                int processedCount = 0;

                // Get blockchain triggers
                var blockchainTriggers = await ListTriggersByTypeAsync(EventTriggerType.Blockchain);
                foreach (var trigger in blockchainTriggers)
                {
                    // Skip disabled triggers
                    if (!trigger.Enabled || trigger.Status != EventTriggerStatus.Active)
                    {
                        continue;
                    }

                    try
                    {
                        // Check if the event matches the condition
                        if (EventMatchesCondition(eventData, trigger.Condition))
                        {
                            // Execute trigger
                            await ExecuteTriggerAsync(trigger.Id, eventData);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing blockchain trigger {TriggerId}", trigger.Id);
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing blockchain event");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ProcessStorageEventAsync(string key, string operation)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(operation))
            {
                throw new ArgumentException("Key and operation cannot be null or empty");
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                int processedCount = 0;

                // Create event data
                var eventData = JsonSerializer.Serialize(new
                {
                    key,
                    operation
                });

                // Get storage triggers
                var storageTriggers = await ListTriggersByTypeAsync(EventTriggerType.Storage);
                foreach (var trigger in storageTriggers)
                {
                    // Skip disabled triggers
                    if (!trigger.Enabled || trigger.Status != EventTriggerStatus.Active)
                    {
                        continue;
                    }

                    try
                    {
                        // Check if the event matches the condition
                        if (EventMatchesCondition(eventData, trigger.Condition))
                        {
                            // Execute trigger
                            await ExecuteTriggerAsync(trigger.Id, eventData);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing storage trigger {TriggerId}", trigger.Id);
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing storage event");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ProcessExternalEventAsync(string eventType, string eventData)
        {
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(eventData))
            {
                throw new ArgumentException("Event type and data cannot be null or empty");
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                int processedCount = 0;

                // Get external triggers
                var externalTriggers = await ListTriggersByTypeAsync(EventTriggerType.External);
                foreach (var trigger in externalTriggers)
                {
                    // Skip disabled triggers
                    if (!trigger.Enabled || trigger.Status != EventTriggerStatus.Active)
                    {
                        continue;
                    }

                    try
                    {
                        // Check if the event type matches the condition
                        if (trigger.Condition == eventType)
                        {
                            // Execute trigger
                            await ExecuteTriggerAsync(trigger.Id, eventData);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing external trigger {TriggerId}", trigger.Id);
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing external event");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExecuteTriggerAsync(string triggerId, string eventData)
        {
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }

            if (string.IsNullOrEmpty(eventData))
            {
                throw new ArgumentException("Event data cannot be null or empty", nameof(eventData));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get trigger
                var trigger = await GetTriggerAsync(triggerId);
                if (trigger == null)
                {
                    throw new ArgumentException($"Trigger {triggerId} not found");
                }

                // Skip disabled triggers
                if (!trigger.Enabled || trigger.Status != EventTriggerStatus.Active)
                {
                    throw new InvalidOperationException($"Trigger {triggerId} is not active");
                }

                // Create execution context
                var context = new JavaScriptExecutionContext
                {
                    FunctionId = trigger.FunctionId,
                    UserId = trigger.UserId,
                    Code = trigger.Code,
                    GasLimit = trigger.GasLimit
                };

                // Combine input JSON with event data
                try
                {
                    var input = string.IsNullOrEmpty(trigger.InputJson)
                        ? new Dictionary<string, object>()
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(trigger.InputJson);

                    var eventObj = JsonSerializer.Deserialize<Dictionary<string, object>>(eventData);

                    // Add event data to input
                    input["event"] = eventObj;

                    // Apply input mapping
                    if (trigger.InputMapping != null && trigger.InputMapping.Count > 0)
                    {
                        foreach (var mapping in trigger.InputMapping)
                        {
                            // TODO: Implement JSONPath extraction
                            // For now, just copy the value if it exists
                            if (eventObj.TryGetValue(mapping.Value, out var value))
                            {
                                input[mapping.Key] = value;
                            }
                        }
                    }

                    context.Input = JsonSerializer.Serialize(input);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing JSON for trigger {TriggerId}", triggerId);
                    throw new InvalidOperationException($"Error parsing JSON: {ex.Message}");
                }

                // Execute JavaScript
                if (!await _jsManager.ExecuteAsync(context))
                {
                    _logger.LogError("Error executing trigger {TriggerId}: {Error}", triggerId, context.Error);
                    throw new InvalidOperationException($"Error executing trigger: {context.Error}");
                }

                // Update trigger
                trigger.LastExecutionTime = DateTime.UtcNow;
                trigger.ExecutionCount++;
                trigger.LastExecutionResult = context.Result;
                trigger.UpdatedAt = DateTime.UtcNow;

                // Save trigger to storage
                await SaveTriggerAsync(trigger);

                _logger.LogInformation("Executed trigger {TriggerId}, gas used: {GasUsed}", triggerId, context.GasUsed);
                return context.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing trigger {TriggerId}", triggerId);
                throw;
            }
        }

        /// <summary>
        /// Checks if an event matches a condition.
        /// </summary>
        /// <param name="eventData">The event data as a JSON string.</param>
        /// <param name="condition">The condition to check.</param>
        /// <returns>True if the event matches the condition, false otherwise.</returns>
        private bool EventMatchesCondition(string eventData, string condition)
        {
            // TODO: Implement proper condition matching
            // For now, just check if the condition is contained in the event data
            return string.IsNullOrEmpty(condition) || eventData.Contains(condition);
        }

        /// <summary>
        /// Saves a trigger to storage.
        /// </summary>
        /// <param name="trigger">The trigger to save.</param>
        /// <returns>True if the trigger was saved successfully, false otherwise.</returns>
        private async Task<bool> SaveTriggerAsync(EventTriggerInfo trigger)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("triggers");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for triggers not found");
                    return false;
                }

                var json = JsonSerializer.Serialize(trigger);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                return await storageProvider.WriteAsync(trigger.Id, bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving trigger {TriggerId} to storage", trigger.Id);
                return false;
            }
        }

        /// <summary>
        /// Loads triggers from storage.
        /// </summary>
        /// <returns>True if triggers were loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadTriggersAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("triggers");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for triggers not found");
                    return false;
                }

                var keys = await storageProvider.GetAllKeysAsync();
                foreach (var key in keys)
                {
                    try
                    {
                        var bytes = await storageProvider.ReadAsync(key);
                        if (bytes == null || bytes.Length == 0)
                        {
                            continue;
                        }

                        var json = System.Text.Encoding.UTF8.GetString(bytes);
                        var trigger = JsonSerializer.Deserialize<EventTriggerInfo>(json);

                        // Add trigger to in-memory maps
                        _triggers[trigger.Id] = trigger;
                        _triggersByType.AddOrUpdate(
                            trigger.Type,
                            new List<string> { trigger.Id },
                            (_, list) =>
                            {
                                list.Add(trigger.Id);
                                return list;
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading trigger {TriggerId} from storage", key);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading triggers from storage");
                return false;
            }
        }

        /// <summary>
        /// Checks if the manager is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Event trigger manager is not initialized");
            }
        }

        /// <summary>
        /// Checks if the manager is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventTriggerManager));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the manager.
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
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the manager.
        /// </summary>
        ~EventTriggerManager()
        {
            Dispose(false);
        }
    }
}
