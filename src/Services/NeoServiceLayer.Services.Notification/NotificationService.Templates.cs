using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Template and subscription management operations for the Notification Service.
/// </summary>
public partial class NotificationService
{
    /// <summary>
    /// Creates a template and returns a TemplateResult.
    /// </summary>
    public async Task<TemplateResult> CreateTemplateWithResultAsync(CreateTemplateRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var templateId = Guid.NewGuid().ToString();

            var template = new InternalNotificationTemplate
            {
                TemplateId = templateId,
                TemplateName = request.TemplateName,
                Subject = request.Subject,
                Body = request.Body,
                Variables = request.Variables,
                SupportedChannels = request.SupportedChannels,
                Category = request.Category,
                Description = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>(request.Metadata)
            };

            lock (_cacheLock)
            {
                _templates[templateId] = template;
            }

            Logger.LogInformation("Created notification template {TemplateName} with ID {TemplateId}",
                request.TemplateName, templateId);

            return new TemplateResult
            {
                TemplateId = templateId,
                Success = true,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["template_name"] = request.TemplateName,
                    ["variables_count"] = request.Variables.Length,
                    ["supported_channels"] = request.SupportedChannels.Length
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create template {TemplateName}", request.TemplateName);

            return new TemplateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate> CreateTemplateAsync(CreateTemplateRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var templateId = Guid.NewGuid().ToString();

            var template = new InternalNotificationTemplate
            {
                TemplateId = templateId,
                TemplateName = request.Name ?? request.TemplateName,
                Subject = request.SubjectTemplate ?? request.Subject,
                Body = request.BodyTemplate ?? request.Body,
                Variables = request.Variables,
                SupportedChannels = request.SupportedChannels ?? new[] { request.Channel },
                Category = request.Category,
                Description = request.Description ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>(request.Metadata ?? new Dictionary<string, object>())
            };

            lock (_cacheLock)
            {
                _templates[templateId] = template;
            }

            Logger.LogInformation("Created notification template {TemplateName} with ID {TemplateId}",
                template.TemplateName, templateId);

            // Convert internal template to public model
            var publicTemplate = new NotificationTemplate
            {
                TemplateId = template.TemplateId,
                TemplateName = template.TemplateName,
                Subject = template.Subject,
                Body = template.Body,
                Variables = template.Variables,
                SupportedChannels = template.SupportedChannels,
                Category = template.Category,
                IsActive = template.IsActive,
                Description = template.Description,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Metadata = template.Metadata
            };

            return await Task.FromResult(publicTemplate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create template {TemplateName}", request.Name ?? request.TemplateName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationResult> SendTemplateNotificationAsync(SendTemplateNotificationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            InternalNotificationTemplate? template;
            lock (_cacheLock)
            {
                _templates.TryGetValue(request.TemplateId, out template);
            }

            if (template == null)
            {
                return new NotificationResult
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Success = false,
                    Status = DeliveryStatus.Failed,
                    ErrorMessage = $"Template {request.TemplateId} not found",
                    SentAt = DateTime.UtcNow,
                    Channel = request.Channel
                };
            }

            // Process template variables
            var processedSubject = ProcessTemplate(template.Subject, request.Variables);
            var processedBody = ProcessTemplate(template.Body, request.Variables);

            var notificationRequest = new SendNotificationRequest
            {
                Channel = request.Channel,
                Recipient = request.Recipient,
                Subject = processedSubject,
                Message = processedBody,
                Priority = request.Priority,
                Metadata = new Dictionary<string, object>(request.Metadata)
                {
                    ["template_id"] = request.TemplateId,
                    ["template_name"] = template.TemplateName,
                    ["template_category"] = template.Category
                }
            };

            return await SendNotificationAsync(notificationRequest, blockchainType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send template notification {TemplateId}", request.TemplateId);

            return new NotificationResult
            {
                NotificationId = Guid.NewGuid().ToString(),
                Success = false,
                Status = DeliveryStatus.Failed,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow,
                Channel = request.Channel
            };
        }
    }

    /// <inheritdoc/>
    public async Task<Models.SubscriptionResult> SubscribeToNotificationsAsync(SubscribeToNotificationsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var subscriptionId = Guid.NewGuid().ToString();

            var subscription = new NotificationSubscription
            {
                Id = subscriptionId,
                SubscriberId = request.SubscriberId,
                Categories = request.Categories,
                Channels = request.Channels,
                Preferences = request.Preferences,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>(request.Metadata)
            };

            lock (_cacheLock)
            {
                _subscriptions[subscriptionId] = subscription;
            }

            var activeCount = _subscriptions.Values.Count(s => s.SubscriberId == request.SubscriberId && s.IsActive);

            Logger.LogInformation("Created subscription {SubscriptionId} for subscriber {SubscriberId} with {CategoryCount} categories",
                subscriptionId, request.SubscriberId, request.Categories.Length);

            return new Models.SubscriptionResult
            {
                SubscriptionId = subscriptionId,
                Success = true,
                Timestamp = DateTime.UtcNow,
                ActiveSubscriptionsCount = activeCount,
                Metadata = new Dictionary<string, object>
                {
                    ["subscriber_id"] = request.SubscriberId,
                    ["categories_count"] = request.Categories.Length,
                    ["channels_count"] = request.Channels.Length
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create subscription for {SubscriberId}", request.SubscriberId);

            return new Models.SubscriptionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<Models.SubscriptionResult> UnsubscribeFromNotificationsAsync(Models.UnsubscribeFromNotificationsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var removedCount = 0;

            lock (_cacheLock)
            {
                var subscriptionsToRemove = _subscriptions.Values
                    .Where(s => s.SubscriberId == request.SubscriberId && s.IsActive)
                    .Where(s => request.Categories.Length == 0 || s.Categories.Any(et => request.Categories.Contains(et)))
                    .ToArray();

                foreach (var subscription in subscriptionsToRemove)
                {
                    if (request.Categories.Length == 0)
                    {
                        // Remove entire subscription
                        subscription.IsActive = false;
                        removedCount++;
                    }
                    else
                    {
                        // Remove specific categories
                        subscription.Categories = subscription.Categories.Except(request.Categories).ToArray();
                        if (subscription.Categories.Length == 0)
                        {
                            subscription.IsActive = false;
                        }
                        removedCount++;
                    }
                }
            }

            var activeCount = _subscriptions.Values.Count(s => s.SubscriberId == request.SubscriberId && s.IsActive);

            Logger.LogInformation("Unsubscribed {RemovedCount} subscriptions for subscriber {SubscriberId}",
                removedCount, request.SubscriberId);

            return new Models.SubscriptionResult
            {
                SubscriptionId = string.Empty, // Not applicable for unsubscribe
                Success = true,
                Timestamp = DateTime.UtcNow,
                ActiveSubscriptionsCount = activeCount,
                Metadata = new Dictionary<string, object>
                {
                    ["subscriber_id"] = request.SubscriberId,
                    ["removed_count"] = removedCount,
                    ["categories"] = request.Categories
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unsubscribe {SubscriberId}", request.SubscriberId);

            return new Models.SubscriptionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate> UpdateTemplateAsync(string templateId, UpdateTemplateRequest request, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            InternalNotificationTemplate? template;
            lock (_cacheLock)
            {
                _templates.TryGetValue(templateId, out template);
            }

            if (template == null)
            {
                throw new ArgumentException($"Template {templateId} not found", nameof(templateId));
            }

            // Update template properties
            if (!string.IsNullOrEmpty(request.Name))
                template.TemplateName = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
                template.Description = request.Description;
            if (!string.IsNullOrEmpty(request.SubjectTemplate))
                template.Subject = request.SubjectTemplate;
            if (!string.IsNullOrEmpty(request.BodyTemplate))
                template.Body = request.BodyTemplate;
            if (request.Variables != null)
                template.Variables = request.Variables;

            template.UpdatedAt = DateTime.UtcNow;

            Logger.LogInformation("Updated notification template {TemplateId}", templateId);

            // Convert internal template to public model
            var publicTemplate = new NotificationTemplate
            {
                TemplateId = template.TemplateId,
                TemplateName = template.TemplateName,
                Subject = template.Subject,
                Body = template.Body,
                Variables = template.Variables,
                SupportedChannels = template.SupportedChannels,
                Category = template.Category,
                IsActive = template.IsActive,
                Description = template.Description,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Metadata = template.Metadata
            };

            return await Task.FromResult(publicTemplate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating notification template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteTemplateAsync(string templateId, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            bool removed;
            InternalNotificationTemplate? removedTemplate;
            lock (_cacheLock)
            {
                removed = _templates.TryRemove(templateId, out removedTemplate);
            }

            if (removed)
            {
                Logger.LogInformation("Deleted notification template {TemplateId}", templateId);
            }
            else
            {
                Logger.LogWarning("Template {TemplateId} not found for deletion", templateId);
            }

            return await Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting notification template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            List<NotificationTemplate> templates;
            lock (_cacheLock)
            {
                templates = _templates.Values
                    .Where(t => t.IsActive)
                    .Select(t => new NotificationTemplate
                    {
                        TemplateId = t.TemplateId,
                        TemplateName = t.TemplateName,
                        Subject = t.Subject,
                        Body = t.Body,
                        Variables = t.Variables,
                        SupportedChannels = t.SupportedChannels,
                        Category = t.Category,
                        IsActive = t.IsActive,
                        Description = t.Description,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        Metadata = t.Metadata
                    })
                    .ToList();
            }

            Logger.LogDebug("Retrieved {Count} active notification templates", templates.Count);

            return await Task.FromResult<IEnumerable<NotificationTemplate>>(templates);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting notification templates");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationHistory> GetNotificationHistoryAsync(GetHistoryRequest request, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            var history = _notificationHistory.Values
                .Where(n => (string.IsNullOrEmpty(request.Recipient) || n.Recipient == request.Recipient) &&
                           (request.StartDate == null || n.SentAt >= request.StartDate) &&
                           (request.EndDate == null || n.SentAt <= request.EndDate))
                .OrderByDescending(n => n.SentAt)
                .Take(request.PageSize)
                .ToList();

            Logger.LogDebug("Retrieved {Count} notification history records", history.Count);

            return await Task.FromResult(new NotificationHistory
            {
                Notifications = history.ToArray(),
                TotalCount = history.Count,
                PageSize = request.PageSize,
                HasMore = false,
                Success = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting notification history");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BroadcastResult> BroadcastNotificationAsync(BroadcastRequest request, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            Logger.LogInformation("Broadcasting notification to {Channel} channel", request.Channel);

            var broadcastId = Guid.NewGuid().ToString();
            var tasks = new List<Task<NotificationResult>>();

            // Get all active subscriptions for the channel
            var targetSubscriptions = _subscriptions.Values
                .Where(s => s.IsActive && s.Channels.Contains(request.Channel))
                .ToList();

            foreach (var subscription in targetSubscriptions)
            {
                var notificationRequest = new SendNotificationRequest
                {
                    Recipient = subscription.SubscriberId,
                    Subject = request.Subject,
                    Message = request.Message,
                    Channel = request.Channel,
                    Priority = request.Priority,
                    Metadata = new Dictionary<string, object>(request.Metadata)
                    {
                        ["BroadcastId"] = broadcastId
                    }
                };

                tasks.Add(ProcessNotificationAsync(notificationRequest));
            }

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Length - successCount;

            Logger.LogInformation("Broadcast {BroadcastId} completed: {SuccessCount} successful, {FailureCount} failed",
                broadcastId, successCount, failureCount);

            return new BroadcastResult
            {
                BroadcastId = broadcastId,
                Success = successCount > 0,
                TotalRecipients = targetSubscriptions.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Channel = request.Channel,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error broadcasting notification");
            throw;
        }
    }

    /// <summary>
    /// Processes template with variables.
    /// </summary>
    /// <param name="template">The template string.</param>
    /// <param name="variables">The variables to substitute.</param>
    /// <returns>The processed template.</returns>
    private static string ProcessTemplate(string template, Dictionary<string, object> variables)
    {
        var result = template;
        foreach (var variable in variables)
        {
            var placeholder = $"{{{variable.Key}}}";
            result = result.Replace(placeholder, variable.Value?.ToString() ?? string.Empty);
        }
        return result;
    }
}
