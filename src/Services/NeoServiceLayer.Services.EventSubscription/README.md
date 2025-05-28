# Event Subscription Service

## Overview

The Event Subscription Service is a secure, enclave-based service for subscribing to and receiving events from the blockchain. It provides a framework for defining event subscriptions, receiving events, and delivering them to subscribers through callbacks.

## Features

- **Event Subscriptions**: Define subscriptions for various event types, including blocks, transactions, contracts, and tokens.
- **Event Filtering**: Filter events based on criteria such as address, contract, or token.
- **Event Delivery**: Deliver events to subscribers through callbacks.
- **Retry Policies**: Define retry policies for failed event deliveries.
- **Event Acknowledgement**: Acknowledge received events to prevent duplicate deliveries.
- **Event History**: Maintain a history of events for audit purposes.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Event Subscription Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure event subscription and delivery. The service consists of the following components:

- **IEventSubscriptionService**: The interface that defines the operations supported by the service.
- **EventSubscriptionService**: The implementation of the service that uses the enclave to manage subscriptions and events.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IEventSubscriptionService, EventSubscriptionService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Creating a Subscription

```csharp
// Create a new subscription
var subscription = new EventSubscription
{
    Name = "New Block Subscription",
    Description = "Subscribe to new blocks",
    EventType = "Block",
    EventFilter = "",
    CallbackUrl = "https://example.com/callback",
    CallbackAuthHeader = "Bearer token123",
    RetryPolicy = new RetryPolicy
    {
        MaxRetries = 3,
        InitialRetryDelaySeconds = 5,
        RetryBackoffFactor = 2.0,
        MaxRetryDelaySeconds = 60
    }
};

string subscriptionId = await eventSubscriptionService.CreateSubscriptionAsync(
    subscription,
    BlockchainType.NeoN3);
```

### Getting Events

```csharp
// Get events for a subscription
var events = await eventSubscriptionService.GetEventsAsync(
    subscriptionId,
    0,
    10,
    BlockchainType.NeoN3);

foreach (var eventData in events)
{
    Console.WriteLine($"Event ID: {eventData.EventId}, Type: {eventData.EventType}, Data: {eventData.Data}");
}
```

### Acknowledging an Event

```csharp
// Acknowledge an event
bool success = await eventSubscriptionService.AcknowledgeEventAsync(
    subscriptionId,
    eventId,
    BlockchainType.NeoN3);
```

### Triggering a Test Event

```csharp
// Trigger a test event
var eventData = new EventData
{
    EventType = "Test",
    Data = "Test event data"
};

string eventId = await eventSubscriptionService.TriggerTestEventAsync(
    subscriptionId,
    eventData,
    BlockchainType.NeoN3);
```

## Security Considerations

- All subscription and event data is managed within the secure enclave.
- Event delivery is authenticated using the provided authentication header.
- All operations are logged for audit purposes.

## API Reference

### CreateSubscriptionAsync

Creates a subscription.

```csharp
Task<string> CreateSubscriptionAsync(
    EventSubscription subscription,
    BlockchainType blockchainType);
```

### GetSubscriptionAsync

Gets a subscription.

```csharp
Task<EventSubscription> GetSubscriptionAsync(
    string subscriptionId,
    BlockchainType blockchainType);
```

### UpdateSubscriptionAsync

Updates a subscription.

```csharp
Task<bool> UpdateSubscriptionAsync(
    EventSubscription subscription,
    BlockchainType blockchainType);
```

### DeleteSubscriptionAsync

Deletes a subscription.

```csharp
Task<bool> DeleteSubscriptionAsync(
    string subscriptionId,
    BlockchainType blockchainType);
```

### ListSubscriptionsAsync

Lists subscriptions.

```csharp
Task<IEnumerable<EventSubscription>> ListSubscriptionsAsync(
    int skip,
    int take,
    BlockchainType blockchainType);
```

### GetEventsAsync

Gets events for a subscription.

```csharp
Task<IEnumerable<EventData>> GetEventsAsync(
    string subscriptionId,
    int skip,
    int take,
    BlockchainType blockchainType);
```

### AcknowledgeEventAsync

Acknowledges an event.

```csharp
Task<bool> AcknowledgeEventAsync(
    string subscriptionId,
    string eventId,
    BlockchainType blockchainType);
```

### TriggerTestEventAsync

Triggers a test event.

```csharp
Task<string> TriggerTestEventAsync(
    string subscriptionId,
    EventData eventData,
    BlockchainType blockchainType);
```
