# Neo Service Layer JavaScript SDK v2.0

A production-ready JavaScript SDK for interacting with Neo Service Layer smart contracts on the Neo N3 blockchain.

## Features

- **22+ Smart Contract Services**: Complete coverage of all Neo Service Layer services with proper parameter validation
- **Advanced Wallet Support**: NeoLine, O3 Wallet, OneGate with automatic failover and enhanced connection management
- **Real-time Transaction Monitoring**: Comprehensive event system with transaction confirmation tracking
- **Production Features**: Error handling with custom SDKError class, performance metrics, intelligent caching, and health checks
- **Gas Optimization**: Automatic gas estimation with optimization suggestions and batch operation support
- **Enhanced RPC Client**: Failover support across multiple endpoints with retry logic and connection pooling
- **Cross-Platform**: Works seamlessly in browsers and Node.js environments

## Installation

### Browser (Direct Include)

```html
<!-- Configuration -->
<script src="config/neo-config.js"></script>

<!-- Wallet Integration -->
<script src="src/scripts/wallet-integration.js"></script>

<!-- Neo Service Layer SDK v2 -->
<script src="src/scripts/neo-service-layer-sdk-v2.js"></script>
```

### Usage

```javascript
// Initialize SDK v2 with enhanced configuration
const neoServiceLayer = new NeoServiceLayerSDK({
    network: 'testnet',
    rpcUrl: 'https://testnet1.neo.coz.io:443',
    networkMagic: 894710606,
    enableMetrics: true,
    enableCache: true,
    timeout: 30000,
    retryAttempts: 3
});

// Connect wallet with auto-detection
const wallet = await neoServiceLayer.connectWallet('auto');

// Use services with enhanced parameters
const result = await neoServiceLayer.storage.store('mykey', 'myvalue', {
    encrypted: true,
    accessLevel: 'private'
});
console.log('Transaction ID:', result.txid);

// Monitor transaction
neoServiceLayer.on('transaction-confirmed', (tx) => {
    console.log('Transaction confirmed:', tx.txid);
});
```

## Services

### Core Services

#### Storage Service
```javascript
// Store data
await neoServiceLayer.storage.store('key', 'value', {
    encrypted: true,
    accessLevel: 'private'
});

// Retrieve data
const data = await neoServiceLayer.storage.get('key');

// Delete data
await neoServiceLayer.storage.delete('key');

// List keys
const keys = await neoServiceLayer.storage.listKeys();
```

#### Oracle Service
```javascript
// Request external data
const request = await neoServiceLayer.oracle.requestData(
    'https://api.coinbase.com/v2/exchange-rates?currency=BTC',
    '$.data.rates.USD',
    'callbackContract'
);

// Get oracle data
const data = await neoServiceLayer.oracle.getData(request.id);
```

#### Compute Service
```javascript
// Execute computation
const job = await neoServiceLayer.compute.execute(
    'hash',
    'input data',
    true // verify
);

// Get result
const result = await neoServiceLayer.compute.getResult(job.id);
```

### DeFi Services

#### Lending Service
```javascript
// Supply assets
await neoServiceLayer.lending.supply('GAS', 100.5);

// Borrow assets
await neoServiceLayer.lending.borrow('USDT', 50, 'GAS');

// Repay loan
await neoServiceLayer.lending.repay('USDT', 50);

// Get pool info
const poolInfo = await neoServiceLayer.lending.getPoolInfo('GAS');
```

#### NFT Marketplace
```javascript
// Mint NFT
const nft = await neoServiceLayer.marketplace.mint(
    'My NFT',
    'Description',
    'https://image.url',
    { trait: 'rare' }
);

// List for sale
await neoServiceLayer.marketplace.list(nft.tokenId, 10.5, 30);

// Buy NFT
await neoServiceLayer.marketplace.buy(123, 15.0);
```

#### Token Creation
```javascript
// Create new token
const token = await neoServiceLayer.tokenization.createToken(
    'MyToken',
    'MTK',
    8,
    1000000,
    ['mintable', 'burnable']
);
```

### Advanced Services

#### Zero Knowledge Proofs
```javascript
// Generate proof
const proof = await neoServiceLayer.zeroknowledge.generateProof(
    'membership',
    'private_input',
    'public_params'
);

// Verify proof
const isValid = await neoServiceLayer.zeroknowledge.verifyProof(
    proof.proof,
    proof.publicInputs
);
```

#### Smart Automation
```javascript
// Create automation job
const job = await neoServiceLayer.automation.createJob(
    'time',
    '0 0 12 * *', // Daily at noon
    'contractAddress',
    'methodName',
    ['param1', 'param2']
);
```

#### Governance & Voting
```javascript
// Create proposal
const proposal = await neoServiceLayer.voting.createProposal(
    'Upgrade Protocol',
    'Detailed description',
    [{ contract: '0x...', method: 'upgrade' }],
    7 // 7 days
);

// Vote on proposal
await neoServiceLayer.voting.vote(proposal.id, 'yes');

// Delegate voting power
await neoServiceLayer.voting.delegate('delegateAddress', 1000);
```

## Wallet Integration

### Supported Wallets

- **NeoLine**: Browser extension wallet
- **O3 Wallet**: Mobile and desktop wallet
- **OneGate**: Multi-platform wallet
- **Demo Mode**: For testing without real wallet

### Wallet Connection

```javascript
// Show wallet selector modal
const walletIntegration = new WalletIntegration();
walletIntegration.showWalletSelector();

// Connect specific wallet
const wallet = await walletIntegration.connectWallet('neoline');

// Sign transaction
const signedTx = await walletIntegration.signTransaction(transaction, 'neoline');
```

## Event System

```javascript
// Listen for wallet connection
neoServiceLayer.on('wallet-connected', (wallet) => {
    console.log('Wallet connected:', wallet);
});

// Listen for transactions
neoServiceLayer.on('transaction-sent', (tx) => {
    console.log('Transaction sent:', tx.txid);
});

neoServiceLayer.on('transaction-confirmed', (tx) => {
    console.log('Transaction confirmed:', tx.txid);
});

// Handle errors
neoServiceLayer.on('error', (error) => {
    console.error('SDK Error:', error);
});
```

## Configuration

### Network Configuration

```javascript
// Use configuration file
const config = window.NEO_SERVICE_LAYER_CONFIG;

// Switch networks
config.switchNetwork('mainnet');

// Get current network
const network = config.getCurrentNetwork();

// Get contract addresses
const contracts = config.getCurrentContracts();
```

### Custom Configuration

```javascript
const sdk = new NeoServiceLayerSDK({
    network: 'mainnet',
    rpcUrl: 'https://mainnet1.neo.coz.io:443',
    networkMagic: 860833102,
    contracts: {
        storage: '0x...',
        oracle: '0x...',
        // ... other contracts
    }
});
```

## Error Handling

```javascript
try {
    const result = await neoServiceLayer.storage.store('key', 'value');
    console.log('Success:', result.txid);
} catch (error) {
    if (error.message.includes('wallet')) {
        // Handle wallet errors
        console.log('Please connect your wallet');
    } else if (error.message.includes('gas')) {
        // Handle gas errors
        console.log('Insufficient GAS for transaction');
    } else {
        // Handle other errors
        console.error('Transaction failed:', error.message);
    }
}
```

## Production Deployment

### Contract Addresses

Update contract addresses in `config/neo-config.js`:

```javascript
contracts: {
    mainnet: {
        storage: '0xYOUR_MAINNET_STORAGE_CONTRACT',
        oracle: '0xYOUR_MAINNET_ORACLE_CONTRACT',
        // ... other contracts
    }
}
```

### Security Considerations

1. **Private Keys**: Never expose private keys in client-side code
2. **Contract Verification**: Always verify contract addresses
3. **Input Validation**: Validate all user inputs before sending transactions
4. **Error Handling**: Implement comprehensive error handling
5. **Gas Limits**: Set appropriate gas limits for transactions

### Performance Optimization

1. **Connection Pooling**: Reuse RPC connections
2. **Caching**: Cache contract addresses and network configuration
3. **Batch Requests**: Group multiple operations when possible
4. **Error Retry**: Implement retry logic for network failures

## Development

### Building from Source

```bash
# Clone repository
git clone https://github.com/your-org/neo-service-layer-website

# Install dependencies
npm install

# Build SDK
npm run build

# Run tests
npm test
```

### Testing

```javascript
// Unit tests
npm run test:unit

// Integration tests with testnet
npm run test:integration

// End-to-end tests
npm run test:e2e
```

## Support

- **Documentation**: [https://docs.neoservicelayer.com](https://docs.neoservicelayer.com)
- **GitHub**: [https://github.com/your-org/neo-service-layer](https://github.com/your-org/neo-service-layer)
- **Discord**: [https://discord.gg/neoservicelayer](https://discord.gg/neoservicelayer)
- **Issues**: [https://github.com/your-org/neo-service-layer/issues](https://github.com/your-org/neo-service-layer/issues)

## License

MIT License - see LICENSE file for details.