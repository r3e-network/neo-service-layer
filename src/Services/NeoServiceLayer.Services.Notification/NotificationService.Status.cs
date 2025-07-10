using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Status and history management operations for the Notification Service.
/// </summary>
public partial class NotificationService
{
    /// <inheritdoc/>
    public async Task<NotificationStatusResult> GetNotificationStatusAsync(NotificationStatusRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            lock (_cacheLock)
            {
                if (_notificationHistory.TryGetValue(request.NotificationId, out var notification))
                {
                    return new NotificationStatusResult
                    {
                        NotificationId = request.NotificationId,
                        Status = notification.Status,
                        DeliveryAttempts = 1, // Simplified for demo
                        LastAttemptAt = notification.SentAt,
                        NextRetryAt = notification.Status == DeliveryStatus.Failed ? DateTime.UtcNow.AddMinutes(5) : null,
                        Details = new DeliveryDetails
                        {
                            Channel = notification.Channel,
                            Recipient = notification.Metadata.GetValueOrDefault("recipient", "unknown").ToString() ?? "unknown",
                            SentAt = notification.SentAt,
                            DeliveredAt = notification.DeliveredAt,
                            DeliveryResponse = notification.Success ? "Success" : notification.ErrorMessage,
                            Metadata = new Dictionary<string, object>(notification.Metadata)
                        },
                        Success = true
                    };
                }
            }

            return new NotificationStatusResult
            {
                NotificationId = request.NotificationId,
                Success = false,
                ErrorMessage = "Notification not found"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get notification status for {NotificationId}", request.NotificationId);

            return new NotificationStatusResult
            {
                NotificationId = request.NotificationId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<object?> GetNotificationStatusAsync(string notificationId, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            lock (_cacheLock)
            {
                if (_notificationHistory.TryGetValue(notificationId, out var notification))
                {
                    return new
                    {
                        NotificationId = notificationId,
                        Status = notification.Status.ToString(),
                        DeliveryAttempts = 1,
                        LastAttemptAt = notification.SentAt,
                        NextRetryAt = notification.Status == DeliveryStatus.Failed ? DateTime.UtcNow.AddMinutes(5) : (DateTime?)null,
                        Details = new
                        {
                            Channel = notification.Channel.ToString(),
                            Recipient = notification.Metadata.GetValueOrDefault("recipient", "unknown").ToString() ?? "unknown",
                            SentAt = notification.SentAt,
                            DeliveredAt = notification.DeliveredAt,
                            DeliveryResponse = notification.Success ? "Success" : notification.ErrorMessage,
                            Metadata = notification.Metadata
                        },
                        Success = true
                    };
                }
            }

            return new
            {
                NotificationId = notificationId,
                Success = false,
                ErrorMessage = "Notification not found"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get notification status for {NotificationId}", notificationId);
            return new
            {
                NotificationId = notificationId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<object?> GetNotificationStatusAsync(object request, BlockchainType blockchainType)
    {
        // Try to extract NotificationId from the request object
        try
        {
            var notificationId = GetNotificationIdFromRequest(request);
            if (string.IsNullOrEmpty(notificationId))
            {
                return new
                {
                    Success = false,
                    ErrorMessage = "NotificationId is required"
                };
            }

            return await GetNotificationStatusAsync(notificationId, blockchainType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get notification status from request object");
            return new
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Sends a batch of notifications.
    /// </summary>
    public async Task<object> SendBatchNotificationsAsync(object request, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            // Extract properties from dynamic request
            var requestType = request.GetType();
            var recipients = requestType.GetProperty("Recipients")?.GetValue(request) as IEnumerable<string>;
            var subject = requestType.GetProperty("Subject")?.GetValue(request) as string;
            var message = requestType.GetProperty("Message")?.GetValue(request) as string;
            var channel = requestType.GetProperty("Channel")?.GetValue(request);
            var priority = requestType.GetProperty("Priority")?.GetValue(request);
            var metadata = requestType.GetProperty("Metadata")?.GetValue(request) as Dictionary<string, object>;

            if (recipients == null || !recipients.Any())
            {
                return new NotificationResult
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Success = false,
                    Status = DeliveryStatus.Failed,
                    ErrorMessage = "Recipients list is required",
                    SentAt = DateTime.UtcNow
                };
            }

            var notificationChannel = channel is NotificationChannel nc ? nc : NotificationChannel.Email;
            var notificationPriority = priority is NotificationPriority np ? np : NotificationPriority.Normal;

            var tasks = new List<Task<NotificationResult>>();
            var batchId = Guid.NewGuid().ToString();

            foreach (var recipient in recipients)
            {
                var notificationRequest = new SendNotificationRequest
                {
                    Recipient = recipient,
                    Subject = subject ?? "Batch Notification",
                    Message = message ?? "Batch notification message",
                    Channel = notificationChannel,
                    Priority = notificationPriority,
                    Metadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>())
                    {
                        ["batch_id"] = batchId,
                        ["batch_notification"] = true
                    }
                };

                tasks.Add(SendNotificationAsync(notificationRequest, blockchainType));
            }

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r.Success);

            Logger.LogInformation("Bulk notification completed: {SuccessCount}/{TotalCount} successful",
                successCount, results.Length);

            // Return a single result for the batch
            return new NotificationResult
            {
                NotificationId = batchId,
                Success = successCount == results.Length,
                Status = successCount == results.Length ? DeliveryStatus.Delivered :
                        successCount > 0 ? DeliveryStatus.PartiallyDelivered : DeliveryStatus.Failed,
                SentAt = DateTime.UtcNow,
                Channel = notificationChannel,
                Metadata = new Dictionary<string, object>
                {
                    ["batch_id"] = batchId,
                    ["total_recipients"] = results.Length,
                    ["successful_deliveries"] = successCount,
                    ["failed_deliveries"] = results.Length - successCount,
                    ["batch_type"] = metadata?.GetValueOrDefault("batch_type", "general") ?? "general"
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send batch notifications");
            return new NotificationResult
            {
                NotificationId = Guid.NewGuid().ToString(),
                Success = false,
                Status = DeliveryStatus.Failed,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Extracts notification ID from various request object types.
    /// </summary>
    private string? GetNotificationIdFromRequest(object request)
    {
        if (request == null) return null;

        // Try to get NotificationId property using reflection
        var requestType = request.GetType();
        var notificationIdProperty = requestType.GetProperty("NotificationId");

        if (notificationIdProperty != null && notificationIdProperty.PropertyType == typeof(string))
        {
            return notificationIdProperty.GetValue(request) as string;
        }

        // Try other common property names
        var idProperty = requestType.GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(string))
        {
            return idProperty.GetValue(request) as string;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<NotificationHistoryResult> GetNotificationHistoryAsync(NotificationHistoryRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var allNotifications = new List<NotificationResult>();

            lock (_cacheLock)
            {
                allNotifications.AddRange(_notificationHistory.Values);
            }

            // Apply filters
            var filteredNotifications = allNotifications.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Recipient))
            {
                filteredNotifications = filteredNotifications.Where(n =>
                    n.Metadata.GetValueOrDefault("recipient", "").ToString()?.Contains(request.Recipient, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (request.Channel.HasValue)
            {
                filteredNotifications = filteredNotifications.Where(n => n.Channel == request.Channel.Value);
            }

            if (request.Status.HasValue)
            {
                filteredNotifications = filteredNotifications.Where(n => n.Status == request.Status.Value);
            }

            if (request.StartTime.HasValue)
            {
                filteredNotifications = filteredNotifications.Where(n => n.SentAt >= request.StartTime.Value);
            }

            if (request.EndTime.HasValue)
            {
                filteredNotifications = filteredNotifications.Where(n => n.SentAt <= request.EndTime.Value);
            }

            var totalCount = filteredNotifications.Count();
            var pagedNotifications = filteredNotifications
                .OrderByDescending(n => n.SentAt)
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToArray();

            var entries = pagedNotifications.Select(n => new NotificationHistoryEntry
            {
                NotificationId = n.NotificationId,
                Channel = n.Channel,
                Recipient = n.Metadata.GetValueOrDefault("recipient", "unknown").ToString() ?? "unknown",
                Subject = n.Metadata.GetValueOrDefault("subject", "").ToString() ?? "",
                Status = n.Status,
                SentAt = n.SentAt,
                DeliveredAt = n.DeliveredAt,
                Priority = Enum.TryParse<NotificationPriority>(n.Metadata.GetValueOrDefault("priority", "Normal").ToString(), out var priority) ? priority : NotificationPriority.Normal,
                Metadata = new Dictionary<string, object>(n.Metadata)
            }).ToArray();

            return new NotificationHistoryResult
            {
                Entries = entries,
                TotalCount = totalCount,
                HasMore = request.Offset + request.Limit < totalCount,
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["filtered_count"] = totalCount,
                    ["returned_count"] = entries.Length,
                    ["offset"] = request.Offset,
                    ["limit"] = request.Limit
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get notification history");

            return new NotificationHistoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
