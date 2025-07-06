# CrossChain Service Configuration Guide

## Overview

The CrossChain Service enables interoperability between different blockchain networks. This guide explains how to configure the service for production use.

## Configuration Structure

Add the following configuration to your `appsettings.json`:

```json
{
  "CrossChain": {
    "SupportedPairs": [
      {
        "SourceChain": "NeoN3",
        "TargetChain": "NeoX",
        "IsActive": true,
        "MinTransferAmount": 0.001,
        "MaxTransferAmount": 1000000,
        "BaseFee": 0.01,
        "EstimatedTime": 5,
        "SupportedTokens": ["GAS", "NEO", "USDT", "USDC"]
      },
      {
        "SourceChain": "NeoX",
        "TargetChain": "NeoN3",
        "IsActive": true,
        "MinTransferAmount": 0.001,
        "MaxTransferAmount": 1000000,
        "BaseFee": 0.01,
        "EstimatedTime": 5,
        "SupportedTokens": ["GAS", "NEO", "USDT", "USDC"]
      }
    ],
    "BridgeContracts": {
      "NeoN3ToNeoX": "0xYOUR_NEO_N3_BRIDGE_CONTRACT_ADDRESS",
      "NeoXToNeoN3": "0xYOUR_NEO_X_BRIDGE_CONTRACT_ADDRESS"
    },
    "Confirmations": {
      "NeoN3": 6,
      "NeoX": 12
    },
    "Fees": {
      "NeoN3": {
        "BaseFee": 0.001,
        "PerByteFee": 0.000001
      },
      "NeoX": {
        "BaseFee": 0.002,
        "PerByteFee": 0.000002
      }
    },
    "TokenMappings": {
      "GAS": {
        "NeoN3": "0xd2a4cff31913016155e38e474a2c06d08be276cf",
        "NeoX": "0xYOUR_GAS_CONTRACT_ON_NEO_X"
      },
      "NEO": {
        "NeoN3": "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5",
        "NeoX": "0xYOUR_NEO_CONTRACT_ON_NEO_X"
      },
      "USDT": {
        "NeoN3": "0xYOUR_USDT_CONTRACT_ON_NEO_N3",
        "NeoX": "0xYOUR_USDT_CONTRACT_ON_NEO_X"
      }
    },
    "MessageTimeout": "01:00:00",
    "TransferTimeout": "02:00:00",
    "MaxRetries": 3,
    "RetryDelay": "00:05:00"
  }
}
```

## Configuration Parameters

### SupportedPairs
Defines the blockchain pairs that support cross-chain operations:

- **SourceChain**: The originating blockchain
- **TargetChain**: The destination blockchain
- **IsActive**: Whether this pair is currently enabled
- **MinTransferAmount**: Minimum amount allowed for transfers
- **MaxTransferAmount**: Maximum amount allowed for transfers
- **BaseFee**: Base fee for cross-chain operations
- **EstimatedTime**: Estimated time in minutes for completion
- **SupportedTokens**: List of tokens supported for this pair

### BridgeContracts
Maps the bridge contract addresses for each blockchain pair:

```json
"BridgeContracts": {
  "{SourceChain}To{TargetChain}": "0xCONTRACT_ADDRESS"
}
```

### Confirmations
Number of block confirmations required before considering a transaction final:

```json
"Confirmations": {
  "BlockchainType": NumberOfConfirmations
}
```

### Fees
Fee configuration for each blockchain:

```json
"Fees": {
  "BlockchainType": {
    "BaseFee": decimal,      // Base fee for operations
    "PerByteFee": decimal    // Fee per byte of data
  }
}
```

The fee estimation takes into account:
- Operation type (TokenTransfer: 1x, Message: 1.5x, ContractCall: 2.5x)
- Data size (bytes * PerByteFee)
- Priority (High: +50% of base fee, Normal: +20%, Low: 0%)
- Chain-specific overrides from SupportedPairs configuration

### TokenMappings
Maps token contract addresses across different blockchains:

```json
"TokenMappings": {
  "TokenSymbol": {
    "BlockchainType": "0xTOKEN_CONTRACT_ADDRESS"
  }
}
```

## Environment-Specific Configuration

Use environment variables for sensitive data:

```bash
export CROSSCHAIN_BRIDGE_NEON3_NEOX=0x1234567890abcdef...
export CROSSCHAIN_BRIDGE_NEOX_NEON3=0xabcdef1234567890...
```

Then reference in configuration:

```json
{
  "CrossChain": {
    "BridgeContracts": {
      "NeoN3ToNeoX": "${CROSSCHAIN_BRIDGE_NEON3_NEOX}",
      "NeoXToNeoN3": "${CROSSCHAIN_BRIDGE_NEOX_NEON3}"
    }
  }
}
```

## Required Infrastructure

1. **Bridge Smart Contracts**: Deploy bridge contracts on each blockchain
2. **Relayer Service**: Set up relayer nodes to monitor and relay messages
3. **Oracle Service**: For price feeds and external data
4. **Monitoring**: Set up monitoring for bridge health and message delivery

## Security Considerations

1. **Contract Verification**: Always verify bridge contract addresses
2. **Multi-Signature**: Use multi-sig for bridge contract administration
3. **Rate Limiting**: Implement rate limits to prevent abuse
4. **Monitoring**: Monitor for suspicious cross-chain activity
5. **Emergency Pause**: Implement circuit breakers for emergencies

## Testing Configuration

For development/testing, you can use testnet contracts:

```json
{
  "CrossChain": {
    "Environment": "Testnet",
    "BridgeContracts": {
      "NeoN3ToNeoX": "0xTESTNET_BRIDGE_CONTRACT",
      "NeoXToNeoN3": "0xTESTNET_BRIDGE_CONTRACT"
    }
  }
}
```

## Monitoring and Alerts

Configure alerts for:

- Message delivery failures
- Unusual transfer volumes
- Bridge contract balance thresholds
- Confirmation timeout events

## Example Production Configuration

```json
{
  "CrossChain": {
    "SupportedPairs": [
      {
        "SourceChain": "NeoN3",
        "TargetChain": "NeoX",
        "IsActive": true,
        "MinTransferAmount": 10,
        "MaxTransferAmount": 100000,
        "BaseFee": 1,
        "EstimatedTime": 10,
        "SupportedTokens": ["GAS", "NEO"]
      }
    ],
    "BridgeContracts": {
      "NeoN3ToNeoX": "${BRIDGE_CONTRACT_N3_X}",
      "NeoXToNeoN3": "${BRIDGE_CONTRACT_X_N3}"
    },
    "Confirmations": {
      "NeoN3": 10,
      "NeoX": 20
    },
    "MessageTimeout": "02:00:00",
    "MaxRetries": 5
  }
}
```