# Smart Contracts Service

## Overview

The Smart Contracts Service provides comprehensive smart contract lifecycle management for both Neo N3 and Neo X (EVM-compatible) blockchains. It enables secure deployment, invocation, and management of smart contracts within the Intel SGX enclave environment.

## Features

- **Multi-Chain Support**: Deploy and manage contracts on both Neo N3 and Neo X
- **Secure Deployment**: Contract deployment through SGX-protected environment
- **Template Library**: Pre-audited contract templates for common use cases
- **Automated Testing**: Built-in contract testing and verification
- **Gas Optimization**: Automatic gas estimation and optimization
- **Version Management**: Track and manage contract versions
- **Upgrade Support**: Safe contract upgrade mechanisms
- **Event Monitoring**: Real-time contract event tracking

## API Reference

### Deploy Contract

Deploys a new smart contract to the specified blockchain.

**Endpoint**: `POST /api/v1/smartcontracts/deploy/{blockchainType}`

**Request Body**:
```json
{
  "contractCode": "base64_encoded_contract",
  "contractName": "MyContract",
  "version": "1.0.0",
  "author": "Developer Name",
  "email": "dev@example.com",
  "description": "Contract description",
  "parameters": {
    "initialSupply": 1000000,
    "decimals": 8
  }
}
```

**Response**:
```json
{
  "success": true,
  "contractAddress": "0x123...",
  "transactionHash": "0xabc...",
  "gasUsed": 1500000,
  "deploymentTime": "2025-01-01T00:00:00Z"
}
```

### Invoke Contract

Invokes a method on a deployed smart contract.

**Endpoint**: `POST /api/v1/smartcontracts/invoke/{blockchainType}`

**Request Body**:
```json
{
  "contractAddress": "0x123...",
  "method": "transfer",
  "parameters": [
    "0xrecipient...",
    1000
  ],
  "gasLimit": 100000
}
```

### Get Contract Info

Retrieves information about a deployed contract.

**Endpoint**: `GET /api/v1/smartcontracts/{contractAddress}/info/{blockchainType}`

**Response**:
```json
{
  "contractAddress": "0x123...",
  "name": "MyContract",
  "version": "1.0.0",
  "deployedAt": "2025-01-01T00:00:00Z",
  "methods": ["transfer", "balanceOf", "approve"],
  "events": ["Transfer", "Approval"],
  "isVerified": true
}
```

### List Contracts

Lists all contracts deployed through the service.

**Endpoint**: `GET /api/v1/smartcontracts/list/{blockchainType}`

### Verify Contract

Verifies contract source code against deployed bytecode.

**Endpoint**: `POST /api/v1/smartcontracts/verify/{blockchainType}`

## Configuration

Add to your `appsettings.json`:

```json
{
  "SmartContractsService": {
    "Enabled": true,
    "MaxContractSize": 1048576,
    "DefaultGasLimit": 10000000,
    "EnableTemplates": true,
    "AutoVerify": true,
    "Networks": {
      "NeoN3": {
        "CompilerVersion": "3.6.2",
        "OptimizationLevel": 2
      },
      "NeoX": {
        "SolidityVersion": "0.8.19",
        "EVMVersion": "paris"
      }
    }
  }
}
```

## Contract Templates

The service includes pre-audited templates for:

1. **NEP-17 Token** (Neo N3)
   - Standard fungible token implementation
   - Includes minting, burning, and pause functions

2. **NEP-11 NFT** (Neo N3)
   - Non-fungible token implementation
   - Supports metadata and royalties

3. **ERC-20 Token** (Neo X)
   - Standard ERC-20 implementation
   - Compatible with DeFi protocols

4. **ERC-721 NFT** (Neo X)
   - Standard NFT implementation
   - Includes enumeration extension

5. **Multi-Signature Wallet**
   - Configurable signature requirements
   - Time-locked transactions

6. **Escrow Contract**
   - Secure asset holding
   - Conditional release mechanisms

## Security Features

- **Code Analysis**: Automatic security vulnerability scanning
- **Gas Limit Protection**: Prevents excessive gas consumption
- **Reentrancy Guards**: Built-in protection against reentrancy attacks
- **Access Control**: Role-based contract administration
- **Audit Trail**: Complete history of all contract interactions

## Usage Examples

### Deploy NEP-17 Token

```csharp
var client = new SmartContractsServiceClient(apiKey);

var deployRequest = new DeployContractRequest
{
    TemplateName = "NEP17Token",
    Parameters = new Dictionary<string, object>
    {
        ["name"] = "MyToken",
        ["symbol"] = "MTK",
        ["decimals"] = 8,
        ["totalSupply"] = 1000000000000000 // 1M tokens
    }
};

var result = await client.DeployContractAsync(deployRequest, BlockchainType.NeoN3);
Console.WriteLine($"Token deployed at: {result.ContractAddress}");
```

### Invoke Transfer Method

```csharp
var invokeRequest = new InvokeContractRequest
{
    ContractAddress = "0x123...",
    Method = "transfer",
    Parameters = new object[] 
    { 
        "NXjtqYERuvSWGawjVux8UerNejvwdYg7eE", // recipient
        100000000 // 1 token (8 decimals)
    }
};

var result = await client.InvokeContractAsync(invokeRequest, BlockchainType.NeoN3);
```

## Best Practices

1. **Testing**: Always test contracts on testnet before mainnet deployment
2. **Gas Estimation**: Use the estimation endpoint before invoking methods
3. **Version Control**: Maintain contract versions for upgrade paths
4. **Monitoring**: Set up event monitoring for critical contract events
5. **Security Audits**: Use the verification service for all production contracts

## Performance Considerations

- Contract deployment: ~5-10 seconds
- Method invocation: ~1-3 seconds
- Event queries: <100ms for recent events
- Verification: ~2-5 seconds depending on contract size

## Limitations

- Maximum contract size: 1MB
- Maximum method parameters: 16
- Event history retention: 90 days
- Concurrent deployments: 10 per account

## Related Services

- [Storage Service](storage-service.md) - For off-chain data storage
- [Key Management Service](key-management-service.md) - For secure key handling
- [Monitoring Service](monitoring-service.md) - For contract monitoring