# Neo Service Layer - Event Subscription Service

## Overview

The Event Subscription Service enables blockchain applications to subscribe to and receive events from the Neo N3 and NeoX blockchains. It provides a reliable way to monitor blockchain events and trigger actions in response, with support for filtering, batching, and retry mechanisms.

## Features

- **Event Subscription**: Subscribe to blockchain events such as new blocks, transactions, and smart contract events.
- **Event Filtering**: Filter events based on various criteria such as contract address, event name, and parameter values.
- **Event Delivery**: Deliver events to subscribers via webhooks or other callback mechanisms.
- **Retry Mechanism**: Retry event delivery in case of failures, with configurable retry policies.
- **Event Batching**: Batch multiple events for efficient delivery.
- **Multiple Blockchain Support**: Support for both Neo N3 and NeoX blockchains.
- **Event History**: Store and retrieve historical events.
- **Event Acknowledgement**: Acknowledge event receipt to prevent duplicate delivery.

## Architecture

The Event Subscription Service consists of the following components:

### Service Layer

- **IEventSubscriptionService**: Interface defining the Event Subscription service operations.
- **EventSubscriptionService**: Implementation of the Event Subscription service, inheriting from EnclaveBlockchainServiceBase.

### Enclave Layer

- **Enclave Implementation**: C++ code running within Occlum LibOS enclaves to securely process events.
- **Secure Communication**: Encrypted communication between the service layer and the enclave.

### Event Processing

- **Event Listener**: Listens for events from the blockchain.
- **Event Processor**: Processes events according to subscription rules.
- **Event Dispatcher**: Dispatches events to subscribers.
- **Retry Manager**: Manages retry attempts for failed event deliveries.

### Blockchain Integration

- **Neo N3 Integration**: Integration with the Neo N3 blockchain.
- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible).

## Data Flow

1. **Subscription Creation**: A client creates a subscription for specific blockchain events.
2. **Event Monitoring**: The service monitors the blockchain for events matching the subscription criteria.
3. **Event Detection**: When a matching event is detected, the service processes it within the enclave.
4. **Event Delivery**: The service delivers the event to the subscriber via the configured callback mechanism.
5. **Delivery Confirmation**: The subscriber acknowledges receipt of the event.
6. **Retry Handling**: If delivery fails, the service retries according to the configured retry policy.

## API Reference

### IEventSubscriptionService Interface

```csharp
public interface IEventSubscriptionService : IEnclaveService, IBlockchainService
{
    Task<Subscription> CreateSubscriptionAsync(SubscriptionRequest request, BlockchainType blockchainType);
    Task<bool> UpdateSubscriptionAsync(string subscriptionId, SubscriptionRequest request, BlockchainType blockchainType);
    Task<bool> DeleteSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);
    Task<Subscription> GetSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);
    Task<IEnumerable<Subscription>> GetSubscriptionsAsync(BlockchainType blockchainType);
    Task<IEnumerable<Event>> GetEventsAsync(string subscriptionId, int skip, int take, BlockchainType blockchainType);
    Task<bool> AcknowledgeEventAsync(string eventId, BlockchainType blockchainType);
    Task<bool> EnableSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);
    Task<bool> DisableSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);
}
```

#### Methods

- **CreateSubscriptionAsync**: Creates a new subscription.
  - Parameters:
    - `request`: The subscription request.
    - `blockchainType`: The blockchain type.
  - Returns: The created subscription.

- **UpdateSubscriptionAsync**: Updates an existing subscription.
  - Parameters:
    - `subscriptionId`: The ID of the subscription to update.
    - `request`: The updated subscription request.
    - `blockchainType`: The blockchain type.
  - Returns: True if the subscription was updated successfully.

- **DeleteSubscriptionAsync**: Deletes a subscription.
  - Parameters:
    - `subscriptionId`: The ID of the subscription to delete.
    - `blockchainType`: The blockchain type.
  - Returns: True if the subscription was deleted successfully.

- **GetSubscriptionAsync**: Gets a subscription by ID.
  - Parameters:
    - `subscriptionId`: The ID of the subscription to get.
    - `blockchainType`: The blockchain type.
  - Returns: The subscription.

- **GetSubscriptionsAsync**: Gets all subscriptions.
  - Parameters:
    - `blockchainType`: The blockchain type.
  - Returns: All subscriptions.

- **GetEventsAsync**: Gets events for a subscription.
  - Parameters:
    - `subscriptionId`: The ID of the subscription to get events for.
    - `skip`: The number of events to skip.
    - `take`: The number of events to take.
    - `blockchainType`: The blockchain type.
  - Returns: Events for the subscription.

- **AcknowledgeEventAsync**: Acknowledges receipt of an event.
  - Parameters:
    - `eventId`: The ID of the event to acknowledge.
    - `blockchainType`: The blockchain type.
  - Returns: True if the event was acknowledged successfully.

- **EnableSubscriptionAsync**: Enables a subscription.
  - Parameters:
    - `subscriptionId`: The ID of the subscription to enable.
    - `blockchainType`: The blockchain type.
  - Returns: True if the subscription was enabled successfully.

- **DisableSubscriptionAsync**: Disables a subscription.
  - Parameters:
    - `subscriptionId`: The ID of the subscription to disable.
    - `blockchainType`: The blockchain type.
  - Returns: True if the subscription was disabled successfully.

### SubscriptionRequest Class

```csharp
public class SubscriptionRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string EventType { get; set; }
    public string EventFilter { get; set; }
    public string CallbackUrl { get; set; }
    public string CallbackAuthHeader { get; set; }
    public RetryPolicy RetryPolicy { get; set; }
}
```

#### Properties

- **Name**: The name of the subscription.
- **Description**: A description of the subscription.
- **EventType**: The type of event to subscribe to (e.g., "Block", "Transaction", "ContractEvent").
- **EventFilter**: A filter expression for the events.
- **CallbackUrl**: The URL to call when an event is detected.
- **CallbackAuthHeader**: The authentication header to include in the callback request.
- **RetryPolicy**: The retry policy for failed callback attempts.

### Subscription Class

```csharp
public class Subscription
{
    public string SubscriptionId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string EventType { get; set; }
    public string EventFilter { get; set; }
    public string CallbackUrl { get; set; }
    public string CallbackAuthHeader { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public RetryPolicy RetryPolicy { get; set; }
}
```

#### Properties

- **SubscriptionId**: The ID of the subscription.
- **Name**: The name of the subscription.
- **Description**: A description of the subscription.
- **EventType**: The type of event to subscribe to.
- **EventFilter**: A filter expression for the events.
- **CallbackUrl**: The URL to call when an event is detected.
- **CallbackAuthHeader**: The authentication header to include in the callback request.
- **Enabled**: Whether the subscription is enabled.
- **CreatedAt**: When the subscription was created.
- **LastModifiedAt**: When the subscription was last modified.
- **RetryPolicy**: The retry policy for failed callback attempts.

### RetryPolicy Class

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelaySeconds { get; set; } = 5;
    public double RetryBackoffFactor { get; set; } = 2.0;
    public int MaxRetryDelaySeconds { get; set; } = 60;
}
```

#### Properties

- **MaxRetries**: The maximum number of retry attempts.
- **InitialRetryDelaySeconds**: The initial delay between retry attempts in seconds.
- **RetryBackoffFactor**: The factor by which to increase the delay between retry attempts.
- **MaxRetryDelaySeconds**: The maximum delay between retry attempts in seconds.

### Event Class

```csharp
public class Event
{
    public string EventId { get; set; }
    public string SubscriptionId { get; set; }
    public string EventType { get; set; }
    public string Data { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Acknowledged { get; set; }
    public int DeliveryAttempts { get; set; }
    public string DeliveryStatus { get; set; }
}
```

#### Properties

- **EventId**: The ID of the event.
- **SubscriptionId**: The ID of the subscription that triggered the event.
- **EventType**: The type of the event.
- **Data**: The event data.
- **Timestamp**: The timestamp of the event.
- **Acknowledged**: Whether the event has been acknowledged.
- **DeliveryAttempts**: The number of delivery attempts.
- **DeliveryStatus**: The status of the delivery (e.g., "Pending", "Delivered", "Failed").

## Smart Contract Integration

### Neo N3 Smart Contract

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;

namespace EventExample
{
    [DisplayName("EventExample")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Event Example")]
    public class EventExample : SmartContract
    {
        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount)
        {
            // Verify that the caller is authorized
            if (!Runtime.CheckWitness(from))
            {
                return false;
            }

            // Perform the transfer
            // ...

            // Emit the event
            OnTransfer(from, to, amount);

            return true;
        }
    }
}
```

### NeoX Smart Contract

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract EventExample {
    event Transfer(address indexed from, address indexed to, uint256 amount);
    
    function transfer(address to, uint256 amount) external returns (bool) {
        // Verify that the caller is authorized
        require(msg.sender != address(0), "Invalid sender");
        
        // Perform the transfer
        // ...
        
        // Emit the event
        emit Transfer(msg.sender, to, amount);
        
        return true;
    }
}
```

## Security Considerations

- **Enclave Security**: All event processing occurs within secure Occlum LibOS enclaves.
- **Callback Authentication**: Callbacks include authentication headers to ensure they are only processed by authorized recipients.
- **Event Verification**: Events are verified to ensure they are authentic and have not been tampered with.
- **Access Control**: Access to subscription management functions is restricted to authorized users.
- **Rate Limiting**: Rate limiting is applied to prevent abuse of the service.

## Performance Considerations

- **Event Batching**: Events are batched for efficient delivery.
- **Parallel Processing**: Events are processed in parallel to improve throughput.
- **Retry Backoff**: Retry attempts use exponential backoff to avoid overwhelming the callback endpoint.
- **Event Filtering**: Events are filtered at the source to reduce the number of events that need to be processed.
- **Event Pruning**: Old events are pruned to prevent the event store from growing too large.

## Deployment

The Event Subscription Service is deployed as part of the Neo Service Layer, with the following components:

- **Service Layer**: Deployed as a .NET service.
- **Enclave Layer**: Deployed within Occlum LibOS enclaves.
- **Smart Contracts**: Deployed on the Neo N3 and NeoX blockchains.

## Conclusion

The Event Subscription Service provides a secure and reliable way to monitor blockchain events and trigger actions in response. By leveraging Occlum LibOS enclaves, it ensures the confidentiality and integrity of event processing, enabling blockchain applications to react to on-chain events in a secure and timely manner.
