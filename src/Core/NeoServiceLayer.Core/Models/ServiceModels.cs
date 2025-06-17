using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Core.Models;

#region Cross-Chain Models

/// <summary>
/// Represents a cross-chain message request.
/// </summary>
public class CrossChainMessageRequest
{
    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [Required]
    public byte[] Message { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the destination chain address.
    /// </summary>
    [Required]
    public string DestinationAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents a cross-chain transfer request.
/// </summary>
public class CrossChainTransferRequest
{
    /// <summary>
    /// Gets or sets the token address.
    /// </summary>
    [Required]
    public string TokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount to transfer.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the destination address.
    /// </summary>
    [Required]
    public string DestinationAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional transfer data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Represents a remote call request.
/// </summary>
public class RemoteCallRequest
{
    /// <summary>
    /// Gets or sets the contract address.
    /// </summary>
    [Required]
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    [Required]
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public object[] Parameters { get; set; } = Array.Empty<object>();

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public long GasLimit { get; set; }
}

/// <summary>
/// Represents cross-chain message status.
/// </summary>
public enum CrossChainMessageStatus
{
    /// <summary>
    /// Message is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Message is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Message was delivered successfully.
    /// </summary>
    Delivered,

    /// <summary>
    /// Message delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Message was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents a cross-chain message.
/// </summary>
public class CrossChainMessage
{
    /// <summary>
    /// Gets or sets the message ID.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source chain.
    /// </summary>
    public BlockchainType SourceChain { get; set; }

    /// <summary>
    /// Gets or sets the destination chain.
    /// </summary>
    public BlockchainType DestinationChain { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the message status.
    /// </summary>
    public CrossChainMessageStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a cross-chain route.
/// </summary>
public class CrossChainRoute
{
    /// <summary>
    /// Gets or sets the route ID.
    /// </summary>
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source chain.
    /// </summary>
    public BlockchainType SourceChain { get; set; }

    /// <summary>
    /// Gets or sets the destination chain.
    /// </summary>
    public BlockchainType DestinationChain { get; set; }

    /// <summary>
    /// Gets or sets the intermediate hops.
    /// </summary>
    public List<BlockchainType> IntermediateHops { get; set; } = new();

    /// <summary>
    /// Gets or sets the estimated cost.
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Gets or sets the estimated time in seconds.
    /// </summary>
    public int EstimatedTimeSeconds { get; set; }
}

/// <summary>
/// Represents cross-chain operation types.
/// </summary>
public enum CrossChainOperation
{
    /// <summary>
    /// Message transfer operation.
    /// </summary>
    MessageTransfer,

    /// <summary>
    /// Token transfer operation.
    /// </summary>
    TokenTransfer,

    /// <summary>
    /// Contract call operation.
    /// </summary>
    ContractCall,

    /// <summary>
    /// Data synchronization operation.
    /// </summary>
    DataSync
}

/// <summary>
/// Represents a supported chain.
/// </summary>
public class SupportedChain
{
    /// <summary>
    /// Gets or sets the chain type.
    /// </summary>
    public BlockchainType ChainType { get; set; }

    /// <summary>
    /// Gets or sets the chain name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the chain is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the supported operations.
    /// </summary>
    public List<CrossChainOperation> SupportedOperations { get; set; } = new();
}

/// <summary>
/// Represents a token mapping.
/// </summary>
public class TokenMapping
{
    /// <summary>
    /// Gets or sets the source token address.
    /// </summary>
    public string SourceTokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination token address.
    /// </summary>
    public string DestinationTokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source chain.
    /// </summary>
    public BlockchainType SourceChain { get; set; }

    /// <summary>
    /// Gets or sets the destination chain.
    /// </summary>
    public BlockchainType DestinationChain { get; set; }

    /// <summary>
    /// Gets or sets the conversion ratio.
    /// </summary>
    public decimal ConversionRatio { get; set; } = 1.0m;
}

#endregion

#region Notification Models

/// <summary>
/// Configuration options for notification service.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the enabled channels.
    /// </summary>
    public string[] EnabledChannels { get; set; } = { "Email", "Webhook" };

    /// <summary>
    /// Gets or sets the retry attempts.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Represents a notification result.
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the notification was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the notification was sent.
    /// </summary>
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Notification delivery status.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Notification is pending delivery.
    /// </summary>
    Pending,

    /// <summary>
    /// Notification was delivered successfully.
    /// </summary>
    Delivered,

    /// <summary>
    /// Notification delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Notification was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Notification priority levels.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority notification.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority notification.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority notification.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority notification.
    /// </summary>
    Critical
}

/// <summary>
/// Notification preferences for a user.
/// </summary>
public class NotificationPreferences
{
    /// <summary>
    /// Gets or sets the preferred channels.
    /// </summary>
    public string[] PreferredChannels { get; set; } = { "Email" };

    /// <summary>
    /// Gets or sets whether to enable notifications.
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets the quiet hours start time.
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }

    /// <summary>
    /// Gets or sets the quiet hours end time.
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }

    /// <summary>
    /// Gets or sets the minimum priority level for notifications.
    /// </summary>
    public NotificationPriority MinimumPriority { get; set; } = NotificationPriority.Normal;
}

#endregion 