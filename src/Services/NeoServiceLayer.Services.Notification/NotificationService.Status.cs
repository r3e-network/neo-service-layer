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
