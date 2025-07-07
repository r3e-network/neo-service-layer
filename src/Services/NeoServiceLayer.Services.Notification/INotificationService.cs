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

    /// <summary>
    /// Subscribes to notifications.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<Models.SubscriptionResult> SubscribeAsync(SubscribeRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Unsubscribes from notifications.
    /// </summary>
    /// <param name="request">The unsubscribe request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The unsubscribe result.</returns>
    Task<UnsubscribeResult> UnsubscribeAsync(UnsubscribeRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets subscriptions for a user or address.
    /// </summary>
    /// <param name="address">The address to get subscriptions for.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The list of subscriptions.</returns>
    Task<IEnumerable<NotificationSubscription>> GetSubscriptionsAsync(string address, BlockchainType blockchainType);

    /// <summary>
    /// Creates a notification template.
    /// </summary>
    /// <param name="request">The template creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The created template.</returns>
    Task<NotificationTemplate> CreateTemplateAsync(CreateTemplateRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Updates a notification template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="request">The template update request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The updated template.</returns>
    Task<NotificationTemplate> UpdateTemplateAsync(string templateId, UpdateTemplateRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteTemplateAsync(string templateId, BlockchainType blockchainType);

    /// <summary>
    /// Gets all notification templates.
    /// </summary>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The list of templates.</returns>
    Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(BlockchainType blockchainType);

    /// <summary>
    /// Gets notification history.
    /// </summary>
    /// <param name="request">The history request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification history.</returns>
    Task<NotificationHistory> GetNotificationHistoryAsync(GetHistoryRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Broadcasts a notification to multiple recipients.
    /// </summary>
    /// <param name="request">The broadcast request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The broadcast result.</returns>
    Task<BroadcastResult> BroadcastNotificationAsync(BroadcastRequest request, BlockchainType blockchainType);
}
