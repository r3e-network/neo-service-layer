using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Models;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Trigger management functionality for the OcclumInterface.
    /// </summary>
    public partial class OcclumInterface
    {
        /// <inheritdoc/>
        public async Task<string> RegisterTriggerAsync(string eventType, string functionId, string userId, string condition)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
            }
            
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }
            
            _logger.LogInformation("Registering trigger for event type {EventType}, function {FunctionId}, and user {UserId}", 
                eventType, functionId, userId);
            
            try
            {
                // Generate a unique trigger ID
                string triggerId = Guid.NewGuid().ToString("N");
                
                // Create a trigger registration object
                var trigger = new TriggerRegistration
                {
                    TriggerId = triggerId,
                    EventType = eventType,
                    FunctionId = functionId,
                    UserId = userId,
                    Condition = condition,
                    CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                // Serialize the trigger to JSON
                string triggerJson = System.Text.Json.JsonSerializer.Serialize(trigger);
                
                // Store the trigger in persistent storage
                await StorePersistentDataAsync($"trigger:{triggerId}", Encoding.UTF8.GetBytes(triggerJson));
                
                // Also store a reference by event type for faster lookup
                string eventTriggerKey = $"event:{eventType}:trigger:{triggerId}";
                await StorePersistentDataAsync(eventTriggerKey, Array.Empty<byte>());
                
                _logger.LogInformation("Trigger {TriggerId} registered successfully", triggerId);
                
                return triggerId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering trigger for event type {EventType}, function {FunctionId}, and user {UserId}",
                    eventType, functionId, userId);
                throw new EnclaveOperationException("Failed to register trigger", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UnregisterTriggerAsync(string triggerId)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }
            
            _logger.LogInformation("Unregistering trigger {TriggerId}", triggerId);
            
            try
            {
                // First, get the trigger info to find the event type
                string triggerJson = await GetTriggerInfoAsync(triggerId);
                
                if (string.IsNullOrEmpty(triggerJson))
                {
                    _logger.LogWarning("Trigger {TriggerId} not found", triggerId);
                    return false;
                }
                
                // Deserialize the trigger
                var trigger = System.Text.Json.JsonSerializer.Deserialize<TriggerRegistration>(triggerJson);
                
                if (trigger == null)
                {
                    _logger.LogWarning("Failed to deserialize trigger {TriggerId}", triggerId);
                    return false;
                }
                
                // Delete the event type reference
                string eventTriggerKey = $"event:{trigger.EventType}:trigger:{triggerId}";
                await DeletePersistentDataAsync(eventTriggerKey);
                
                // Delete the trigger itself
                await DeletePersistentDataAsync($"trigger:{triggerId}");
                
                _logger.LogInformation("Trigger {TriggerId} unregistered successfully", triggerId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering trigger {TriggerId}", triggerId);
                throw new EnclaveOperationException("Failed to unregister trigger", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string[]> GetTriggersForEventAsync(string eventType)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
            }
            
            _logger.LogInformation("Getting triggers for event type {EventType}", eventType);
            
            try
            {
                // List all keys that match the event type pattern
                string eventPattern = $"event:{eventType}:trigger:";
                var allKeys = await ListPersistentDataAsync();
                var triggerKeys = allKeys.Where(k => k.StartsWith(eventPattern)).ToArray();
                
                // Extract the trigger IDs from the keys
                var triggerIds = new List<string>();
                foreach (var key in triggerKeys)
                {
                    string triggerId = key.Substring(eventPattern.Length);
                    triggerIds.Add(triggerId);
                }
                
                _logger.LogInformation("Found {TriggerCount} triggers for event type {EventType}", triggerIds.Count, eventType);
                
                return triggerIds.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting triggers for event type {EventType}", eventType);
                throw new EnclaveOperationException("Failed to get triggers for event", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetTriggerInfoAsync(string triggerId)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(triggerId))
            {
                throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
            }
            
            _logger.LogInformation("Getting info for trigger {TriggerId}", triggerId);
            
            try
            {
                // Get the trigger data
                byte[] triggerData = await RetrievePersistentDataAsync($"trigger:{triggerId}");
                
                if (triggerData == null || triggerData.Length == 0)
                {
                    _logger.LogWarning("Trigger {TriggerId} not found", triggerId);
                    return null;
                }
                
                // Convert the binary data to JSON
                string triggerJson = Encoding.UTF8.GetString(triggerData);
                
                return triggerJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting info for trigger {TriggerId}", triggerId);
                throw new EnclaveOperationException("Failed to get trigger info", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<int> ProcessBlockchainEventAsync(string eventData)
        {
            CheckDisposed();
            
            if (string.IsNullOrEmpty(eventData))
            {
                throw new ArgumentException("Event data cannot be null or empty", nameof(eventData));
            }
            
            _logger.LogInformation("Processing blockchain event");
            
            try
            {
                // Parse the event data
                var eventInfo = System.Text.Json.JsonSerializer.Deserialize<BlockchainEvent>(eventData);
                
                if (eventInfo == null)
                {
                    _logger.LogWarning("Failed to parse blockchain event data");
                    return 0;
                }
                
                // Get all triggers for this event type
                string[] triggerIds = await GetTriggersForEventAsync(eventInfo.EventType);
                
                if (triggerIds.Length == 0)
                {
                    _logger.LogInformation("No triggers found for event type {EventType}", eventInfo.EventType);
                    return 0;
                }
                
                int processedCount = 0;
                
                // Process each trigger
                foreach (string triggerId in triggerIds)
                {
                    string triggerJson = await GetTriggerInfoAsync(triggerId);
                    
                    if (string.IsNullOrEmpty(triggerJson))
                    {
                        continue;
                    }
                    
                    var trigger = System.Text.Json.JsonSerializer.Deserialize<TriggerRegistration>(triggerJson);
                    
                    if (trigger == null)
                    {
                        continue;
                    }
                    
                    // Check if the condition matches
                    bool conditionMet = await EvaluateTriggerConditionAsync(trigger.Condition, eventInfo);
                    
                    if (conditionMet)
                    {
                        // Execute the function
                        await ExecuteTriggerFunctionAsync(trigger, eventInfo);
                        processedCount++;
                    }
                }
                
                _logger.LogInformation("Processed {ProcessedCount} triggers for blockchain event", processedCount);
                
                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing blockchain event");
                throw new EnclaveOperationException("Failed to process blockchain event", ex);
            }
        }

        /// <summary>
        /// Evaluates whether a trigger condition is met for a specific event.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="eventInfo">The event information.</param>
        /// <returns>True if the condition is met, false otherwise.</returns>
        private async Task<bool> EvaluateTriggerConditionAsync(string condition, BlockchainEvent eventInfo)
        {
            if (string.IsNullOrEmpty(condition))
            {
                // No condition means always trigger
                return true;
            }
            
            try
            {
                // For simple conditions, we can parse them directly
                // For complex conditions, we might need a JavaScript evaluation
                if (condition.StartsWith("js:"))
                {
                    // Extract the JavaScript code
                    string code = condition.Substring(3);
                    
                    // Prepare the input with event data
                    string input = System.Text.Json.JsonSerializer.Serialize(eventInfo);
                    
                    // Execute the JavaScript condition
                    string result = await ExecuteJavaScriptAsync(
                        code,
                        input,
                        "{}",
                        "condition-evaluator",
                        "system"
                    );
                    
                    // Parse the result (should be a boolean)
                    using JsonDocument document = JsonDocument.Parse(result);
                    bool conditionResult = false;
                    
                    // Try to get the boolean value directly
                    if (document.RootElement.ValueKind == JsonValueKind.True)
                    {
                        conditionResult = true;
                    }
                    else if (document.RootElement.ValueKind == JsonValueKind.False)
                    {
                        conditionResult = false;
                    }
                    else if (document.RootElement.ValueKind == JsonValueKind.String)
                    {
                        // Try to parse a string as boolean
                        string stringValue = document.RootElement.GetString();
                        bool.TryParse(stringValue, out conditionResult);
                    }
                    
                    return conditionResult;
                }
                else
                {
                    // For simple conditions, implement a basic expression parser
                    // This is a placeholder for demonstration
                    return condition == "*" || eventInfo.Data.Contains(condition);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating trigger condition");
                return false;
            }
        }

        /// <summary>
        /// Executes a function in response to a triggered event.
        /// </summary>
        /// <param name="trigger">The trigger registration.</param>
        /// <param name="eventInfo">The event information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ExecuteTriggerFunctionAsync(TriggerRegistration trigger, BlockchainEvent eventInfo)
        {
            try
            {
                _logger.LogInformation("Executing function {FunctionId} for trigger {TriggerId}", 
                    trigger.FunctionId, trigger.TriggerId);
                
                // Get the function code
                byte[] functionData = await RetrievePersistentDataAsync($"function:{trigger.FunctionId}");
                
                if (functionData == null || functionData.Length == 0)
                {
                    _logger.LogWarning("Function {FunctionId} not found", trigger.FunctionId);
                    return;
                }
                
                string functionCode = Encoding.UTF8.GetString(functionData);
                
                // Prepare the input with event data
                string input = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Event = eventInfo,
                    Trigger = trigger
                });
                
                // Execute the function
                await ExecuteJavaScriptAsync(
                    functionCode,
                    input,
                    "{}",
                    trigger.FunctionId,
                    trigger.UserId
                );
                
                _logger.LogInformation("Function {FunctionId} executed successfully for trigger {TriggerId}", 
                    trigger.FunctionId, trigger.TriggerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionId} for trigger {TriggerId}", 
                    trigger.FunctionId, trigger.TriggerId);
            }
        }

        /// <inheritdoc/>
        public Task<int> ProcessScheduledTriggersAsync(ulong currentTime)
        {
            // Implement processing scheduled triggers
            throw new NotImplementedException("Processing scheduled triggers is not implemented");
        }
    }
} 