using HotChocolate;
using HotChocolate.Subscriptions;

namespace NeoServiceLayer.Api.GraphQL.Subscriptions;

/// <summary>
/// Root subscription type for GraphQL schema.
/// </summary>
public class Subscription
{
    /// <summary>
    /// Placeholder subscription to establish the root type.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <returns>A stream of heartbeat messages.</returns>
    [Subscribe]
    [GraphQLDescription("Heartbeat subscription for connection testing")]
    public async IAsyncEnumerable<HeartbeatMessage> OnHeartbeat(
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var stream = await receiver.SubscribeAsync<HeartbeatMessage>("heartbeat", cancellationToken);
        
        await foreach (var message in stream.WithCancellation(cancellationToken))
        {
            yield return message;
        }
    }
}

/// <summary>
/// Heartbeat message model.
/// </summary>
public class HeartbeatMessage
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = "Heartbeat";
}