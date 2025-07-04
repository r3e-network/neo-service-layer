# Neo Express Integration Guide for Neo Service Layer

## Overview

This guide demonstrates how to integrate Neo Express (a private Neo blockchain for development) with the Neo Service Layer. Neo Express provides a fast, lightweight blockchain environment perfect for development and testing.

## Prerequisites

- .NET 9.0 SDK
- Neo Service Layer project
- Neo Express CLI tool

## Installation

Neo Express is already installed in this environment:
```bash
dotnet tool install -g Neo.Express --version 3.8.2.1
```

## Setup and Configuration

### 1. Create a Neo Express Instance

```bash
cd neo-express-test
neoxp create
```

### 2. Create Test Wallets

```bash
# Create wallets
neoxp wallet create alice
neoxp wallet create bob

# Transfer assets for testing
neoxp transfer 100 GAS genesis alice
neoxp transfer 1000 NEO genesis alice
```

### 3. Deploy a Smart Contract

```bash
# Compile the contract
dotnet build SimpleContract.csproj

# Deploy to Neo Express
neoxp contract deploy SimpleContract.nef alice
```

### 4. Configure Neo Service Layer

Add Neo Express configuration to your `appsettings.json`:

```json
{
  "Blockchain": {
    "Neo": {
      "Network": "neo-express-local",
      "RpcUrl": "http://localhost:50012",
      "WebSocketUrl": "ws://localhost:50013",
      "TestMode": true,
      "Contracts": {
        "SimpleContract": {
          "Hash": "0x918dc5e53f237015fae0dad532655efff9834cbd",
          "Name": "SimpleContract"
        }
      }
    }
  }
}
```

## Testing Smart Contracts

### Invoke Contract Methods

```bash
# Create invocation file
cat > hello-invoke.json << EOF
{
  "contract": "0x918dc5e53f237015fae0dad532655efff9834cbd",
  "operation": "hello",
  "args": ["Neo Express"]
}
EOF

# Invoke the method
neoxp contract invoke hello-invoke.json alice
```

### Test Results from This Session

1. **Hello Method**: Successfully invoked, returned "Hello, Neo Express!"
2. **Add Method**: Successfully computed 10 + 25 = 35
3. **Storage**: Successfully stored and retrieved "Hello from Neo Express!"
4. **Events**: DataStored event emitted correctly

## Integration with Neo Service Layer

### Using the SmartContracts API

```csharp
// Example: Invoking a contract through the service layer
[HttpPost("invoke")]
public async Task<IActionResult> InvokeContract([FromBody] InvokeRequest request)
{
    var result = await _smartContractsService.InvokeContract(
        request.ContractHash,
        request.Method,
        request.Parameters
    );
    
    return Ok(result);
}
```

### Monitoring Contract Events

```csharp
// Example: Getting contract events
[HttpGet("{contractHash}/events")]
public async Task<IActionResult> GetContractEvents(string contractHash)
{
    var events = await _smartContractsService.GetContractEvents(contractHash);
    return Ok(events);
}
```

## Best Practices

1. **Use Test Mode**: Always set `TestMode: true` in configuration for Neo Express
2. **Wallet Management**: Store test wallets securely, even in development
3. **Contract Versioning**: Track contract hashes when redeploying
4. **Event Monitoring**: Use the service layer's event monitoring for real-time updates
5. **Gas Management**: Ensure test wallets have sufficient GAS for operations

## Troubleshooting

### Common Issues

1. **RPC Connection Failed**
   - Ensure Neo Express is running: `neoxp run`
   - Check the RPC port (default: 50012)

2. **Transaction Failed**
   - Check wallet balance: `neoxp wallet show alice`
   - Verify contract is deployed: `neoxp contract get <hash>`

3. **Events Not Appearing**
   - Wait for block confirmation
   - Check application logs: `neoxp show tx <txid>`

## Advanced Features

### Multi-Contract Deployment

Deploy multiple contracts for complex scenarios:

```bash
neoxp contract deploy TokenContract.nef alice
neoxp contract deploy GovernanceContract.nef alice
```

### Integration Testing

Use the provided integration test:

```bash
cd /home/ubuntu/neo-service-layer
dotnet test tests/Integration/NeoExpressIntegrationTest.cs
```

## Summary

Neo Express provides an excellent development environment for the Neo Service Layer:

- ✅ Fast blockchain for development (1-second blocks)
- ✅ Easy wallet and asset management
- ✅ Simple contract deployment
- ✅ Full RPC compatibility
- ✅ Integrated with Neo Service Layer APIs
- ✅ Support for all Neo N3 features

The integration allows developers to:
- Test smart contracts locally
- Develop without mainnet/testnet delays
- Iterate quickly on contract logic
- Test the full service layer stack
- Validate SGX integration in a controlled environment