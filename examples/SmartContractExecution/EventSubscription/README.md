# Event Subscription Example

This example demonstrates how to create automated workflows on the Neo N3 blockchain using the Neo Service Layer (NSL) for event monitoring and action execution.

## Overview

The Event Subscription contract allows for:

1. Creating subscriptions to blockchain events
2. Automating actions when events occur
3. Managing subscriptions (creating, cancelling)
4. Supporting different types of automated actions

## How It Works

### Subscription Creation

Users can create subscriptions to blockchain events:
- Specify the source contract to monitor
- Define the event name to listen for
- Choose the action type to execute
- Provide action-specific data

### Event Monitoring

The Neo Service Layer monitors the blockchain for events:
- Listens for events from the specified contracts
- Filters events based on subscription criteria
- Triggers the appropriate actions when events occur

### Action Execution

When a matching event is detected:
- The NSL calls back to the contract to execute the action
- The contract performs the action based on the action type
- The action execution is recorded on the blockchain

## Supported Action Types

The contract supports various types of automated actions:

1. **ContractCall**: Call another smart contract with event data
2. **Notification**: Simply notify subscribers of the event

## Architecture

1. **Smart Contract**: The on-chain component that manages subscriptions and executes actions
2. **Neo Service Layer**: The off-chain component that monitors events and triggers actions
3. **Client**: The user interface that creates and manages subscriptions

## Deployment

### Prerequisites

- Neo N3 private net or testnet
- Neo Service Layer running
- Neo-CLI or Neo-GUI

### Steps

1. Deploy the EventSubscription contract:
   ```
   neo-cli deploy EventSubscription.nef
   ```

2. Initialize the contract with the Neo Service Layer address:
   ```
   neo-cli invoke <contract-hash> initialize <service-layer-address>
   ```

3. (Optional) Set the fee for subscription creation:
   ```
   neo-cli invoke <contract-hash> setFee <fee-amount>
   ```

## Usage

### Create a Subscription

```
neo-cli invoke <contract-hash> createSubscription <source-contract> <event-name> <action-type> <action-data>
```

Example for ContractCall action:
```
neo-cli invoke <contract-hash> createSubscription 0x1234567890abcdef Transfer ContractCall 0xabcdef1234567890,processTransfer
```

Example for Notification action:
```
neo-cli invoke <contract-hash> createSubscription 0x1234567890abcdef Transfer Notification ""
```

### Cancel a Subscription

```
neo-cli invoke <contract-hash> cancelSubscription <subscription-id>
```

### Get Subscription Information

```
neo-cli invoke <contract-hash> getSubscription <subscription-id>
```

## Integration with Neo Service Layer

The contract interacts with the Neo Service Layer to:

1. Register event subscriptions:
   - The NSL starts monitoring the specified contract for events
   - The NSL filters events based on the subscription criteria

2. Execute actions when events occur:
   - The NSL detects matching events on the blockchain
   - The NSL calls back to the contract to execute the action
   - The contract performs the action based on the action type

## Use Cases

- **DeFi**: Automate trading strategies based on price events
- **Gaming**: Trigger in-game actions when specific events occur
- **Supply Chain**: Automate workflows when shipments change status
- **DAO**: Execute proposals automatically when voting ends
- **NFT**: Trigger royalty payments when NFTs are transferred

## Security Considerations

- The Neo Service Layer must be trusted to monitor events correctly
- The NSL uses Intel SGX to provide integrity guarantees
- Only the NSL can trigger action execution in the contract
- The contract includes a fee mechanism to prevent spam subscriptions
- Subscription owners can cancel their subscriptions at any time
