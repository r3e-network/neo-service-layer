using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Interface for the Notification Service that provides multi-channel notification capabilities.
/// </summary>
public interface INotificationService : IService
{
    /// <summary>
    /// Sends a notification through the specified channel.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification result.</returns>
    Task<NotificationResult> SendNotificationAsync(SendNotificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sends a batch of notifications.
    /// </summary>
    /// <param name="request">The batch notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The batch result.</returns>
    Task<object> SendBatchNotificationsAsync(object request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a notification.
    /// </summary>
    /// <param name="notificationId">The notification ID.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification status.</returns>
    Task<object?> GetNotificationStatusAsync(string notificationId, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a notification using a request object.
    /// </summary>
    /// <param name="request">The notification status request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification status.</returns>
    Task<object?> GetNotificationStatusAsync(object request, BlockchainType blockchainType);

    /// <summary>
    /// Gets available notification channels.
    /// </summary>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The available channels.</returns>
    Task<AvailableChannelsResult> GetAvailableChannelsAsync(BlockchainType blockchainType);
} 