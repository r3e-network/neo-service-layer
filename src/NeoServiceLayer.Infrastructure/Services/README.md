# Neo Service Layer - Infrastructure Services

This directory contains the implementation of the infrastructure services for the Neo Service Layer.

## Neo N3 Blockchain Integration

The Neo N3 blockchain integration consists of the following components:

### NeoN3BlockchainService

The `NeoN3BlockchainService` provides methods for interacting with the Neo N3 blockchain:

- Getting the blockchain height
- Retrieving transaction details
- Invoking smart contracts
- Test invoking smart contracts
- Getting contract events

### NeoN3EventListenerService

The `NeoN3EventListenerService` is a background service that listens for events on the Neo N3 blockchain:

- Subscribing to contract events
- Polling for new events
- Processing events and sending them to callback URLs

## Configuration

The Neo N3 blockchain integration is configured in the `appsettings.json` file:

```json
"Neo": {
  "RpcUrl": "http://localhost:10332",
  "WalletPath": "neo-wallet.json",
  "WalletPassword": "password"
}
```

## Usage

### Invoking a Smart Contract

```csharp
// Inject the INeoN3BlockchainService
private readonly INeoN3BlockchainService _blockchainService;

public MyService(INeoN3BlockchainService blockchainService)
{
    _blockchainService = blockchainService;
}

// Invoke a smart contract
public async Task<string> InvokeContract()
{
    string scriptHash = "0x1234567890abcdef";
    string operation = "transfer";
    object[] args = new object[] { "address1", "address2", 100 };
    
    return await _blockchainService.InvokeContractAsync(scriptHash, operation, args);
}
```

### Subscribing to Contract Events

```csharp
// Inject the NeoN3EventListenerService
private readonly NeoN3EventListenerService _eventListenerService;

public MyService(NeoN3EventListenerService eventListenerService)
{
    _eventListenerService = eventListenerService;
}

// Subscribe to a contract event
public string SubscribeToEvent()
{
    string scriptHash = "0x1234567890abcdef";
    string eventName = "Transfer";
    string callbackUrl = "https://myservice.com/callback";
    uint startBlock = 1000;
    
    return _eventListenerService.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);
}
```

## API Endpoints

The Neo N3 blockchain integration exposes the following API endpoints:

- `GET /api/v1/neo/height` - Get the blockchain height
- `GET /api/v1/neo/transaction/{txHash}` - Get a transaction by its hash
- `POST /api/v1/neo/invoke` - Invoke a smart contract
- `POST /api/v1/neo/testinvoke` - Test invoke a smart contract
- `GET /api/v1/neo/events/{scriptHash}` - Get events emitted by a smart contract
- `POST /api/v1/neo/subscribe` - Subscribe to a contract event
- `DELETE /api/v1/neo/unsubscribe/{subscriptionId}` - Unsubscribe from a contract event

## Dependencies

The Neo N3 blockchain integration depends on the following packages:

- `Neo.Network.RPC` - For interacting with the Neo N3 RPC server
- `Neo.SmartContract` - For building and executing smart contract scripts
- `Neo.Wallets` - For managing Neo N3 wallets and accounts
