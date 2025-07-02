# Neo Service Layer Error Codes Reference

This document provides a comprehensive list of error codes used in the Neo Service Layer SDK and backend services.

## Error Code Format

All errors follow a consistent format:
```javascript
{
    name: 'SDKError',
    message: 'Human-readable error description',
    code: 'ERROR_CODE',
    timestamp: '2024-01-30T12:00:00Z',
    originalError: { /* Original error details if applicable */ }
}
```

## SDK Error Codes

### Connection Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `INIT_ERROR` | SDK initialization failed | Invalid configuration, network issues | Check configuration and network connectivity |
| `NETWORK_ERROR` | Network request failed | RPC endpoint down, network timeout | Retry request, check RPC status |
| `WEBSOCKET_ERROR` | WebSocket connection failed | WebSocket endpoint unavailable | Check WebSocket URL and firewall settings |
| `WEBSOCKET_NOT_CONNECTED` | WebSocket operation attempted without connection | WebSocket not initialized | Call `connectWebSocket()` first |
| `CONNECTION_POOL_EXHAUSTED` | All connections in pool are busy or failed | High load, multiple connection failures | Increase pool size or wait for connections |

### Wallet Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `NO_WALLET` | No wallet connected | User hasn't connected wallet | Call `connectWallet()` first |
| `WALLET_CONNECTION_FAILED` | Failed to connect wallet | Wallet extension not installed, user rejected | Install wallet extension, retry connection |
| `NEOLINE_NOT_FOUND` | NeoLine wallet not detected | Extension not installed | Install NeoLine extension |
| `O3_NOT_FOUND` | O3 wallet not detected | Extension not installed | Install O3 wallet |
| `ONEGATE_NOT_FOUND` | OneGate wallet not detected | Extension not installed | Install OneGate wallet |
| `NO_WALLET_FOUND` | No compatible wallet found | No supported wallet installed | Install NeoLine, O3, or OneGate |

### Contract Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `CONTRACT_NOT_FOUND` | Smart contract not found | Invalid service name, contract not deployed | Check service name and contract deployment |
| `INVALID_PARAMS` | Invalid parameters provided | Missing required params, wrong types | Review method documentation |
| `INSUFFICIENT_GAS` | Not enough GAS for transaction | Low GAS balance | Add GAS to wallet |
| `CONTRACT_ERROR` | Smart contract execution failed | Contract logic error, invalid state | Check contract requirements |

### Blockchain Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `INVALID_BLOCKCHAIN_TYPE` | Invalid blockchain type specified | Wrong blockchain name | Use 'NeoN3' or 'NeoX' |
| `BLOCKCHAIN_SYNC_ERROR` | Blockchain not synchronized | RPC node out of sync | Wait for sync or use different RPC |
| `BLOCK_NOT_FOUND` | Requested block not found | Invalid block height/hash | Verify block exists |
| `TRANSACTION_NOT_FOUND` | Transaction not found | Invalid transaction ID | Check transaction ID |

### Operation Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `INVALID_OPERATION` | Invalid operation requested | Unsupported method, wrong parameters | Check API documentation |
| `BATCH_ERROR` | Batch operation failed | One or more operations failed | Check individual operation results |
| `BATCH_SIZE_EXCEEDED` | Too many operations in batch | Batch > 50 operations | Split into smaller batches |
| `INVALID_BATCH_OPERATIONS` | Invalid batch operations format | Wrong format, empty array | Provide valid operations array |
| `PARALLEL_BATCH_ERROR` | Parallel batch execution failed | Multiple operation failures | Check individual results |

### Data Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `INVALID_ADDRESS` | Invalid blockchain address | Malformed address | Verify address format |
| `INVALID_KEY` | Invalid storage key | Key too long, invalid characters | Check key requirements |
| `DATA_NOT_FOUND` | Requested data not found | Key doesn't exist | Verify key exists |
| `ENCODING_ERROR` | Data encoding/decoding failed | Invalid data format | Check data format |

### Authentication/Authorization Errors

| Code | Description | Possible Causes | Resolution |
|------|-------------|-----------------|------------|
| `UNAUTHORIZED` | Unauthorized access | Missing or invalid credentials | Provide valid credentials |
| `FORBIDDEN` | Access forbidden | Insufficient permissions | Check user permissions |
| `TOKEN_EXPIRED` | Authentication token expired | JWT token expired | Refresh authentication |
| `INVALID_SIGNATURE` | Invalid cryptographic signature | Wrong key, data tampered | Verify signature data |

### Service-Specific Errors

#### Storage Service
| Code | Description | Resolution |
|------|-------------|------------|
| `STORAGE_LIMIT_EXCEEDED` | Storage quota exceeded | Delete old data or upgrade plan |
| `ENCRYPTION_FAILED` | Data encryption failed | Check encryption parameters |
| `TRANSACTION_CONFLICT` | Storage transaction conflict | Retry transaction |

#### Oracle Service
| Code | Description | Resolution |
|------|-------------|------------|
| `ORACLE_REQUEST_FAILED` | Oracle data request failed | Check URL and parameters |
| `INVALID_ORACLE_RESPONSE` | Invalid response from oracle | Verify data source |
| `ORACLE_TIMEOUT` | Oracle request timeout | Retry or increase timeout |

#### Cross-Chain Service
| Code | Description | Resolution |
|------|-------------|------------|
| `BRIDGE_NOT_AVAILABLE` | Bridge service unavailable | Wait for service restoration |
| `UNSUPPORTED_CHAIN` | Target chain not supported | Check supported chains |
| `BRIDGE_LIMIT_EXCEEDED` | Bridge transfer limit exceeded | Reduce amount or wait |

#### Zero Knowledge Service
| Code | Description | Resolution |
|------|-------------|------------|
| `PROOF_GENERATION_FAILED` | Failed to generate ZK proof | Check input parameters |
| `PROOF_VERIFICATION_FAILED` | Proof verification failed | Verify proof data |
| `CIRCUIT_NOT_FOUND` | ZK circuit not found | Register circuit first |

## Backend Service Error Codes

### HTTP Status Codes

| Status | Error Type | Description |
|--------|------------|-------------|
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Authentication required |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource conflict |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily down |

### Service Layer Errors

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `SVC_INIT_ERROR` | Service initialization failed | 500 |
| `SVC_NOT_READY` | Service not ready | 503 |
| `ENCLAVE_ERROR` | Intel SGX enclave error | 500 |
| `BLOCKCHAIN_CONNECTION_ERROR` | Cannot connect to blockchain | 503 |
| `DATABASE_ERROR` | Database operation failed | 500 |
| `CACHE_ERROR` | Cache operation failed | 500 |

## Error Handling Best Practices

### 1. Always Catch Errors
```javascript
try {
    await sdk.storage.store('key', 'value');
} catch (error) {
    if (error.code === 'NO_WALLET') {
        // Handle wallet connection
        await sdk.connectWallet();
    } else if (error.code === 'NETWORK_ERROR') {
        // Retry with exponential backoff
        await retryWithBackoff(() => sdk.storage.store('key', 'value'));
    }
}
```

### 2. Use Error Events
```javascript
sdk.on('error', (error) => {
    console.error(`SDK Error [${error.code}]: ${error.message}`);
    // Log to monitoring service
    logToMonitoring(error);
});
```

### 3. Implement Retry Logic
```javascript
async function retryOperation(operation, maxRetries = 3) {
    for (let i = 0; i < maxRetries; i++) {
        try {
            return await operation();
        } catch (error) {
            if (error.code === 'NETWORK_ERROR' && i < maxRetries - 1) {
                await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, i)));
                continue;
            }
            throw error;
        }
    }
}
```

### 4. User-Friendly Error Messages
```javascript
function getUserMessage(error) {
    const messages = {
        'NO_WALLET': 'Please connect your wallet to continue',
        'INSUFFICIENT_GAS': 'You need more GAS to complete this transaction',
        'NETWORK_ERROR': 'Network connection issue. Please try again',
        'CONTRACT_NOT_FOUND': 'Service temporarily unavailable'
    };
    
    return messages[error.code] || 'An unexpected error occurred';
}
```

## Monitoring and Debugging

### Enable Debug Mode
```javascript
const sdk = new NeoServiceLayerSDK({
    debug: true,
    logLevel: 'verbose'
});
```

### Track Error Metrics
```javascript
const errorCounts = new Map();

sdk.on('error', (error) => {
    const count = errorCounts.get(error.code) || 0;
    errorCounts.set(error.code, count + 1);
    
    // Alert on error spikes
    if (count > 10) {
        alertOps(`Error spike detected: ${error.code}`);
    }
});
```

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0.0 | 2024-01-30 | Initial error code standardization |
| 2.1.0 | TBD | Added WebSocket error codes |

---

For additional support, please refer to the [SDK documentation](./javascript-sdk.html) or contact support.