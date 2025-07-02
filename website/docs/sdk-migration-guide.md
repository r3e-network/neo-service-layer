# Neo Service Layer SDK Migration Guide (v1 to v2)

This guide helps you migrate from Neo Service Layer SDK v1 to v2, highlighting key changes and providing code examples for a smooth transition.

## Overview

SDK v2 introduces significant improvements in reliability, performance, and multi-blockchain support while maintaining backward compatibility where possible.

## Key Changes

### 1. **Multi-Blockchain Support** 🆕

**v1:**
```javascript
const sdk = new NeoServiceLayerSDK({
    network: 'testnet',
    rpcUrl: 'https://testnet1.neo.coz.io:443'
});
```

**v2:**
```javascript
const sdk = new NeoServiceLayerSDK({
    network: 'testnet',
    blockchainType: 'NeoN3', // or 'NeoX'
    rpcUrl: 'https://testnet1.neo.coz.io:443'
});

// Switch blockchain dynamically
await sdk.switchBlockchain('NeoX');
```

### 2. **Enhanced Error Handling** 🛡️

**v1:**
```javascript
try {
    await sdk.storage.store('key', 'value');
} catch (error) {
    console.error(error);
}
```

**v2:**
```javascript
try {
    await sdk.storage.store('key', 'value');
} catch (error) {
    if (error instanceof SDKError) {
        console.error(`Error Code: ${error.code}`);
        console.error(`Timestamp: ${error.timestamp}`);
        console.error(`Original Error: ${error.originalError}`);
    }
}
```

### 3. **Automatic Retry Logic** 🔄

**v1:** No automatic retries

**v2:** Built-in retry with exponential backoff
```javascript
// Global configuration
const sdk = new NeoServiceLayerSDK({
    retryAttempts: 3,
    retryDelay: 1000
});

// Per-operation override
await sdk.storage.store('key', 'value', {
    retryAttempts: 5,
    retryDelay: 2000
});
```

### 4. **Batch Operations** 📦

**v1:** Not supported

**v2:** Two batch execution modes
```javascript
// Sequential batch (atomic)
const result = await sdk.batchExecute([
    { service: 'storage', method: 'store', params: ['key1', 'value1'] },
    { service: 'storage', method: 'store', params: ['key2', 'value2'] }
]);

// Parallel batch (independent)
const results = await sdk.batchExecuteParallel([
    { service: 'storage', method: 'get', params: ['key1'] },
    { service: 'oracle', method: 'getData', params: ['request1'] }
]);
```

### 5. **Enhanced Service Methods** 🚀

#### Storage Service
**New methods in v2:**
- `beginTransaction()` - Start atomic transaction
- `commitTransaction()` - Commit changes
- `rollbackTransaction()` - Rollback changes

#### Oracle Service
**New methods in v2:**
- `requestDataBatch()` - Request multiple data sources
- `verifyData()` - Verify oracle responses
- `subscribe()` - Subscribe to data feeds
- `unsubscribe()` - Cancel subscriptions

### 6. **Improved Configuration** ⚙️

**v1:**
```javascript
const sdk = new NeoServiceLayerSDK({
    network: 'testnet',
    contracts: { storage: '0x123...' }
});
```

**v2:**
```javascript
const sdk = new NeoServiceLayerSDK({
    network: 'testnet',
    blockchainType: 'NeoN3',
    enableMetrics: true,
    enableCache: true,
    cacheTimeout: 300000,
    maxGasInvoke: 20,
    contracts: {
        testnet: { storage: '0x123...' },
        mainnet: { storage: '0x456...' }
    }
});
```

### 7. **Performance Metrics** 📊

**v1:** No built-in metrics

**v2:** Comprehensive metrics tracking
```javascript
const status = sdk.getStatus();
console.log(status.metrics);
// {
//   transactionsCount: 42,
//   gasUsed: 1.234,
//   errors: 2,
//   avgResponseTime: 345
// }
```

### 8. **Health Checks** 🏥

**v1:** Not available

**v2:** Built-in health monitoring
```javascript
const health = await sdk.healthCheck();
console.log(health);
// {
//   isHealthy: true,
//   checks: {
//     rpcConnection: true,
//     contractsLoaded: true,
//     walletConnected: true,
//     networkSync: true
//   },
//   blockchainType: 'NeoN3',
//   timestamp: '2024-01-30T12:00:00Z'
// }
```

## Migration Steps

### Step 1: Update Initialization

Replace your SDK initialization code:

```javascript
// Old (v1)
const sdk = new NeoServiceLayerSDK({
    network: 'testnet',
    privateKey: 'your-key'
});

// New (v2)
const sdk = new NeoServiceLayerSDK({
    network: 'testnet',
    blockchainType: 'NeoN3',
    privateKey: 'your-key',
    retryAttempts: 3,
    enableMetrics: true
});
```

### Step 2: Update Service Calls

Add blockchain type to service calls that need it:

```javascript
// Old (v1)
await sdk.storage.store('key', 'value');

// New (v2) - explicit blockchain type
await sdk.storage.store('key', 'value', {
    blockchainType: 'NeoX' // Override default
});
```

### Step 3: Implement Error Handling

Update error handling to use new error structure:

```javascript
sdk.on('error', (error) => {
    if (error.code === 'NETWORK_ERROR') {
        // Handle network errors
    } else if (error.code === 'CONTRACT_NOT_FOUND') {
        // Handle missing contracts
    }
});
```

### Step 4: Leverage New Features

Take advantage of new capabilities:

```javascript
// Use batch operations for better performance
const operations = [
    { service: 'storage', method: 'store', params: ['k1', 'v1'] },
    { service: 'storage', method: 'store', params: ['k2', 'v2'] },
    { service: 'storage', method: 'store', params: ['k3', 'v3'] }
];

const result = await sdk.batchExecute(operations);

// Monitor SDK health
setInterval(async () => {
    const health = await sdk.healthCheck();
    if (!health.isHealthy) {
        console.error('SDK health check failed:', health.checks);
    }
}, 60000);
```

## Breaking Changes

1. **Contract Address Format**
   - v1: Single contract address per service
   - v2: Network-specific contract addresses

2. **Event Names**
   - v1: `'transaction-complete'`
   - v2: `'transaction-confirmed'`

3. **Error Objects**
   - v1: Plain Error objects
   - v2: SDKError with additional metadata

## Compatibility Mode

For gradual migration, you can use compatibility options:

```javascript
const sdk = new NeoServiceLayerSDK({
    // ... other config
    compatibilityMode: true // Enables v1 compatibility
});
```

## Common Migration Issues

### Issue 1: Missing Blockchain Type
**Error:** `Invalid blockchainType: must be "NeoN3" or "NeoX"`
**Solution:** Add `blockchainType` to your configuration

### Issue 2: Contract Not Found
**Error:** `Contract not found for service: storage`
**Solution:** Update contract addresses for your network

### Issue 3: Batch Size Exceeded
**Error:** `Batch size cannot exceed 50 operations`
**Solution:** Split large batches into smaller chunks

## Support

For migration assistance:
- Check the [API Reference](./api-reference.html)
- Review [example code](https://github.com/neo-service-layer/examples)
- Join our [Discord community](https://discord.gg/neo-service-layer)

## Version Compatibility

| SDK Version | Backend Version | Neo Version | Neo X Support |
|-------------|----------------|-------------|---------------|
| v1.x        | 1.0+          | N3          | ❌            |
| v2.x        | 1.5+          | N3          | ✅            |

## Deprecation Timeline

- **v1.0**: Maintenance mode (security fixes only)
- **v1.0**: End of support - June 2024
- **v2.0**: Current stable version
- **v2.1**: Upcoming - WebSocket support

---

Remember to thoroughly test your application after migration. The v2 SDK provides better error messages and debugging information to help identify any issues.