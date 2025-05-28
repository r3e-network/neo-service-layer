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
    /// Sends notifications to multiple channels simultaneously.
    /// </summary>
    /// <param name="request">The multi-channel notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The multi-channel notification result.</returns>
    Task<MultiChannelNotificationResult> SendMultiChannelNotificationAsync(MultiChannelNotificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Registers a new notification channel.
    /// </summary>
    /// <param name="request">The channel registration request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The channel registration result.</returns>
    Task<ChannelRegistrationResult> RegisterChannelAsync(RegisterChannelRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a notification.
    /// </summary>
    /// <param name="request">The notification status request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification status.</returns>
    Task<NotificationStatusResult> GetNotificationStatusAsync(NotificationStatusRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a notification template.
    /// </summary>
    /// <param name="request">The template creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The template creation result.</returns>
    Task<TemplateResult> CreateTemplateAsync(CreateTemplateRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sends a notification using a template.
    /// </summary>
    /// <param name="request">The template notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification result.</returns>
    Task<NotificationResult> SendTemplateNotificationAsync(SendTemplateNotificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets notification history.
    /// </summary>
    /// <param name="request">The notification history request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification history.</returns>
    Task<NotificationHistoryResult> GetNotificationHistoryAsync(NotificationHistoryRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Subscribes to notifications for specific events.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<Models.SubscriptionResult> SubscribeToNotificationsAsync(SubscribeToNotificationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Unsubscribes from notifications.
    /// </summary>
    /// <param name="request">The unsubscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The unsubscription result.</returns>
    Task<Models.SubscriptionResult> UnsubscribeFromNotificationsAsync(Models.UnsubscribeFromNotificationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets available notification channels.
    /// </summary>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The available channels.</returns>
    Task<AvailableChannelsResult> GetAvailableChannelsAsync(BlockchainType blockchainType);
}