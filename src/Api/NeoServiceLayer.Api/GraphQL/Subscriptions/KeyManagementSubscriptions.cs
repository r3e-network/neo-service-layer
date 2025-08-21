using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.KeyManagement;

namespace NeoServiceLayer.Api.GraphQL.Subscriptions;

/// <summary>
/// GraphQL subscriptions for key management events.
/// </summary>
[ExtendObjectType(typeof(Subscription))]
public class KeyManagementSubscriptions
{
    /// <summary>
    /// Subscribes to key creation events.
    /// </summary>
    /// <param name="blockchainType">Optional blockchain type filter.</param>
    /// <param name="receiver">The event receiver.</param>
    /// <returns>A stream of key creation events.</returns>
    [Subscribe]
    [Topic]
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Subscribes to key creation events")]
    public async IAsyncEnumerable<KeyEvent> OnKeyCreated(
        [EventMessage] KeyEvent keyEvent,
        BlockchainType? blockchainType = null)
    {
        if (blockchainType == null || keyEvent.BlockchainType == blockchainType)
        {
            yield return keyEvent;
        }
    }

    /// <summary>
    /// Subscribes to key rotation events.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <returns>A stream of key rotation events.</returns>
    [Subscribe]
    [Topic]
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Subscribes to key rotation events")]
    public async IAsyncEnumerable<KeyRotationEvent> OnKeyRotated(
        [EventMessage] KeyRotationEvent rotationEvent)
    {
        yield return rotationEvent;
    }

    /// <summary>
    /// Subscribes to key expiry warning events.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <returns>A stream of key expiry warnings.</returns>
    [Subscribe]
    [Topic("key-expiry-warning")]
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Subscribes to key expiry warning events")]
    public async IAsyncEnumerable<KeyExpiryWarning> OnKeyExpiryWarning(
        [EventMessage] KeyExpiryWarning warning)
    {
        yield return warning;
    }
}

/// <summary>
/// Key event model.
/// </summary>
public class KeyEvent
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the key metadata.
    /// </summary>
    public KeyMetadata KeyMetadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Key rotation event model.
/// </summary>
public class KeyRotationEvent
{
    /// <summary>
    /// Gets or sets the old key ID.
    /// </summary>
    public string OldKeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the new key metadata.
    /// </summary>
    public KeyMetadata NewKeyMetadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Key expiry warning model.
/// </summary>
public class KeyExpiryWarning
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the expiry date.
    /// </summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Gets or sets the hours until expiry.
    /// </summary>
    public double HoursUntilExpiry { get; set; }
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets the warning level.
    /// </summary>
    public string WarningLevel { get; set; } = "Warning";
}