# Social Recovery Service Configuration

## Overview

The Social Recovery Service requires deployed smart contract addresses to be configured before it can be used. The service will validate these addresses on startup and refuse to start if placeholder addresses are detected.

## Configuration

### 1. Deploy Social Recovery Smart Contracts

Before configuring the service, you must deploy the Social Recovery smart contracts to the target blockchains:

- **Neo N3**: Deploy the social recovery contract to Neo N3 mainnet/testnet
- **Neo X**: Deploy the social recovery contract to Neo X mainnet/testnet

### 2. Configure Contract Addresses

Update the `appsettings.json` file with the actual deployed contract addresses:

```json
{
  "SocialRecovery": {
    "ContractHash": {
      "neo-n3": "0xYOUR_ACTUAL_NEO_N3_CONTRACT_HASH_HERE",
      "neo-x": "0xYOUR_ACTUAL_NEO_X_CONTRACT_HASH_HERE"
    },
    "DefaultCacheDuration": "00:05:00",
    "MaxRecoveryAttempts": 3,
    "RecoveryAttemptWindow": "1.00:00:00"
  }
}
```

### 3. Environment-Specific Configuration

For different environments (development, staging, production), use environment-specific configuration files:

- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

Example for production:

```json
{
  "SocialRecovery": {
    "ContractHash": {
      "neo-n3": "0x1234567890abcdef1234567890abcdef12345678",
      "neo-x": "0xabcdef1234567890abcdef1234567890abcdef12"
    }
  }
}
```

## Validation

The service performs the following validations on startup:

1. **Non-empty configuration**: Ensures contract addresses are configured
2. **No placeholder addresses**: Rejects `0x0000000000000000000000000000000000000000`
3. **Valid format**: Checks that addresses start with `0x` and are 42 characters long

If validation fails, the service will throw an `InvalidOperationException` with a descriptive error message.

## Security Considerations

1. **Never commit production contract addresses to source control**
2. Use environment variables or secure configuration providers for production
3. Consider using Azure Key Vault, AWS Secrets Manager, or similar services
4. Regularly audit contract address configurations

## Troubleshooting

### Service fails to start with "Invalid or placeholder contract address"

**Cause**: The service detected placeholder addresses in the configuration.

**Solution**: Update `appsettings.json` with actual deployed contract addresses.

### Service fails with "No contract address configured for blockchain"

**Cause**: The requested blockchain is not configured.

**Solution**: Add the missing blockchain configuration to the `ContractHash` dictionary.

## Example Configuration with Environment Variables

```json
{
  "SocialRecovery": {
    "ContractHash": {
      "neo-n3": "${SOCIAL_RECOVERY_NEO_N3_CONTRACT}",
      "neo-x": "${SOCIAL_RECOVERY_NEO_X_CONTRACT}"
    }
  }
}
```

Then set environment variables:
```bash
export SOCIAL_RECOVERY_NEO_N3_CONTRACT=0x1234567890abcdef1234567890abcdef12345678
export SOCIAL_RECOVERY_NEO_X_CONTRACT=0xabcdef1234567890abcdef1234567890abcdef12
```