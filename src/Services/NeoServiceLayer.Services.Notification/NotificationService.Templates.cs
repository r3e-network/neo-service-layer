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
    /// <inheritdoc/>
    public async Task<TemplateResult> CreateTemplateAsync(CreateTemplateRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var templateId = Guid.NewGuid().ToString();

            var template = new NotificationTemplate
            {
                TemplateId = templateId,
                TemplateName = request.TemplateName,
                Subject = request.Subject,
                Body = request.Body,
                Variables = request.Variables,
                SupportedChannels = request.SupportedChannels,
                Category = request.Category,
                CreatedAt = DateTime.UtcNow,
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
    public async Task<NotificationResult> SendTemplateNotificationAsync(SendTemplateNotificationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            NotificationTemplate? template;
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
