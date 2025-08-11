using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Delivery operations for the Notification Service.
/// </summary>
public partial class NotificationService
{
    /// <inheritdoc/>
    public async Task<NotificationResult> SendNotificationAsync(SendNotificationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Notification service is not running");
        }

        try
        {
            var notificationId = Guid.NewGuid().ToString();
            Logger.LogDebug("Sending notification {NotificationId} via {Channel} to {Recipient}",
                notificationId, request.Channel, request.Recipient);

            // Check if recipient is empty
            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                return new NotificationResult
                {
                    NotificationId = notificationId,
                    Success = false,
                    Status = DeliveryStatus.Failed,
                    ErrorMessage = "Recipient is required",
                    SentAt = DateTime.UtcNow,
                    Channel = request.Channel
                };
            }

            // Check if channel is enabled
            if (!_registeredChannels.ContainsKey(request.Channel.ToString()) || 
                !_registeredChannels[request.Channel.ToString()].IsEnabled)
            {
                return new NotificationResult
                {
                    NotificationId = notificationId,
                    Success = false,
                    Status = DeliveryStatus.Failed,
                    ErrorMessage = $"Channel {request.Channel} is not enabled",
                    SentAt = DateTime.UtcNow,
                    Channel = request.Channel
                };
            }

            // Validate recipient format
            if (!ValidateRecipient(request.Channel, request.Recipient))
            {
                return new NotificationResult
                {
                    NotificationId = notificationId,
                    Success = false,
                    Status = DeliveryStatus.Failed,
                    ErrorMessage = $"Invalid recipient format for channel {request.Channel}",
                    SentAt = DateTime.UtcNow,
                    Channel = request.Channel
                };
            }

            // Check if scheduled for later
            if (request.ScheduledAt.HasValue && request.ScheduledAt.Value > DateTime.UtcNow)
            {
                var result = new NotificationResult
                {
                    NotificationId = notificationId,
                    Success = true,
                    Status = DeliveryStatus.Scheduled,
                    SentAt = DateTime.UtcNow,
                    Channel = request.Channel,
                    Metadata = new Dictionary<string, object>
                    {
                        ["scheduled_at"] = request.ScheduledAt.Value,
                        ["priority"] = request.Priority.ToString(),
                        ["category"] = request.Category
                    }
                };

                lock (_cacheLock)
                {
                    _notificationHistory[notificationId] = result;
                }

                Logger.LogInformation("Notification {NotificationId} scheduled for {ScheduledAt}",
                    notificationId, request.ScheduledAt.Value);

                return result;
            }

            // Process notification with privacy-preserving operations
            var privacyResult = await ProcessNotificationWithPrivacyAsync(request, notificationId);
            
            Logger.LogDebug("Privacy-preserving notification processing completed: NotificationId={NotificationId}, DeliveryProof={Proof}", 
                privacyResult.NotificationId, privacyResult.DeliveryProof.Proof);

            // Simulate sending notification
            var deliveryResult = await SimulateNotificationDelivery(request.Channel, request.Recipient, request.Subject, request.Message);

            var notificationResult = new NotificationResult
            {
                NotificationId = notificationId,
                Success = deliveryResult.Success,
                Status = deliveryResult.Success ? DeliveryStatus.Delivered : DeliveryStatus.Failed,
                ErrorMessage = deliveryResult.ErrorMessage,
                SentAt = DateTime.UtcNow,
                DeliveredAt = deliveryResult.Success ? DateTime.UtcNow : null,
                Channel = request.Channel,
                Metadata = new Dictionary<string, object>
                {
                    ["priority"] = request.Priority.ToString(),
                    ["category"] = request.Category,
                    ["attachments_count"] = request.Attachments.Length,
                    ["delivery_time_ms"] = deliveryResult.DeliveryTimeMs,
                    ["privacy_proof_id"] = privacyResult.NotificationId,
                    ["recipient_hash"] = privacyResult.DeliveryProof.RecipientHash,
                    ["channel_hash"] = privacyResult.DeliveryProof.ChannelHash,
                    ["delivery_proof"] = privacyResult.DeliveryProof.Proof
                }
            };

            lock (_cacheLock)
            {
                _notificationHistory[notificationId] = notificationResult;
            }

            Logger.LogInformation("Notification {NotificationId} {Status} via {Channel}",
                notificationId, notificationResult.Status, request.Channel);
            
            if (notificationResult.Success)
            {
                Logger.LogInformation("Notification sent successfully");
            }

            return notificationResult;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send notification via {Channel}", request.Channel);

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
    public async Task<MultiChannelNotificationResult> SendMultiChannelNotificationAsync(MultiChannelNotificationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            var batchId = Guid.NewGuid().ToString();
            Logger.LogDebug("Sending multi-channel notification {BatchId} to {ChannelCount} channels",
                batchId, request.Channels.Length);

            var results = new List<NotificationResult>();
            var tasks = new List<Task<NotificationResult>>();

            foreach (var channel in request.Channels)
            {
                if (request.Recipients.TryGetValue(channel, out var recipients))
                {
                    foreach (var recipient in recipients)
                    {
                        var notificationRequest = new SendNotificationRequest
                        {
                            Channel = channel,
                            Recipient = recipient,
                            Subject = request.Subject,
                            Message = request.Message,
                            Priority = request.Priority,
                            Metadata = new Dictionary<string, object>(request.Metadata)
                            {
                                ["batch_id"] = batchId,
                                ["multi_channel"] = true
                            }
                        };

                        tasks.Add(SendNotificationAsync(notificationRequest, blockchainType));

                        // Stop on failure if requested
                        if (request.StopOnFailure && tasks.Count > 0)
                        {
                            var lastResult = await tasks.Last();
                            if (!lastResult.Success)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            results.AddRange(await Task.WhenAll(tasks));

            var successfulCount = results.Count(r => r.Success);
            var failedCount = results.Count - successfulCount;

            Logger.LogInformation("Multi-channel notification {BatchId} completed: {SuccessfulCount} successful, {FailedCount} failed",
                batchId, successfulCount, failedCount);

            return new MultiChannelNotificationResult
            {
                BatchId = batchId,
                Results = results.ToArray(),
                AllSuccessful = failedCount == 0,
                SuccessfulCount = successfulCount,
                FailedCount = failedCount,
                Metadata = new Dictionary<string, object>
                {
                    ["total_notifications"] = results.Count,
                    ["channels_used"] = request.Channels.Length,
                    ["stop_on_failure"] = request.StopOnFailure
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send multi-channel notification");

            return new MultiChannelNotificationResult
            {
                BatchId = Guid.NewGuid().ToString(),
                AllSuccessful = false,
                SuccessfulCount = 0,
                FailedCount = 0
            };
        }
    }

    /// <summary>
    /// Simulates notification delivery.
    /// </summary>
    /// <param name="channel">The notification channel.</param>
    /// <param name="recipient">The recipient.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="message">The message.</param>
    /// <returns>The delivery result.</returns>
    private static async Task<DeliverySimulationResult> SimulateNotificationDelivery(NotificationChannel channel, string recipient, string subject, string message)
    {
        // Simulate network delay
        await Task.Delay(Random.Shared.Next(100, 500));

        // For testing purposes, always succeed
        var success = true; // Random.Shared.NextDouble() > 0.05;
        var deliveryTimeMs = Random.Shared.Next(50, 1000);

        return new DeliverySimulationResult
        {
            Success = success,
            ErrorMessage = success ? null : $"Simulated delivery failure for {channel}",
            DeliveryTimeMs = deliveryTimeMs
        };
    }

    /// <summary>
    /// Checks delivery status for pending notifications.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void CheckDeliveryStatus(object? state)
    {
        try
        {
            Logger.LogDebug("Checking delivery status for pending notifications");

            var pendingNotifications = new List<NotificationResult>();

            lock (_cacheLock)
            {
                pendingNotifications.AddRange(_notificationHistory.Values.Where(n =>
                    n.Status == DeliveryStatus.Pending || n.Status == DeliveryStatus.Sending));
            }

            foreach (var notification in pendingNotifications)
            {
                // Simulate status updates
                if (notification.Status == DeliveryStatus.Pending)
                {
                    notification.Status = DeliveryStatus.Sending;
                }
                else if (notification.Status == DeliveryStatus.Sending)
                {
                    notification.Status = Random.Shared.NextDouble() > 0.1 ? DeliveryStatus.Delivered : DeliveryStatus.Failed;
                    if (notification.Status == DeliveryStatus.Delivered)
                    {
                        notification.DeliveredAt = DateTime.UtcNow;
                    }
                }
            }

            Logger.LogDebug("Updated status for {NotificationCount} pending notifications", pendingNotifications.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during delivery status check");
        }
    }
}
