# Neo Service Layer Smart Contracts

This directory contains Solidity smart contracts that enable on-chain integration with the Neo Service Layer on the NeoX blockchain. These contracts provide a bridge between blockchain applications and the secure, confidential computing services offered by the Neo Service Layer.

## 🏗️ Architecture Overview

The smart contract system consists of several key components:

### Core Contracts

1. **ServiceRegistry** - Central registry for all Neo Service Layer services
2. **RandomnessConsumer** - Consumes secure randomness from the Randomness Service
3. **OracleConsumer** - Requests external data from the Oracle Service
4. **AbstractAccountFactory** - Creates and manages abstract accounts
5. **AbstractAccount** - Individual account abstraction implementation

### Service Integration

Each contract integrates with specific Neo Service Layer services:

- **Randomness Service** → `RandomnessConsumer`
- **Oracle Service** → `OracleConsumer`
- **Abstract Account Service** → `AbstractAccountFactory` + `AbstractAccount`
- **AI Services** → (Future implementation)
- **Compute Service** → (Future implementation)
- **Storage Service** → (Future implementation)

## 🚀 Quick Start

### Prerequisites

- Node.js 18+
- npm or yarn
- Hardhat development environment

### Installation

```bash
# Install dependencies
npm install

# Compile contracts
npm run compile

# Run tests
npm run test

# Deploy to local network
npm run deploy

# Deploy to NeoX testnet
npm run deploy:testnet
```

### Environment Setup

Create a `.env` file:

```env
PRIVATE_KEY=your_private_key_here
NEOX_API_KEY=your_neox_api_key_here
COINMARKETCAP_API_KEY=your_coinmarketcap_api_key_here
```

## 📋 Contract Details

### ServiceRegistry

Central registry that manages all Neo Service Layer services.

**Key Features:**
- Service registration and discovery
- Service metrics tracking
- Service activation/deactivation
- Access control and permissions

**Usage:**
```solidity
// Register a new service
bytes32 serviceId = serviceRegistry.registerService(
    "MyService",
    "1.0.0",
    serviceAddress,
    "https://api.example.com"
);

// Check if service is active
bool isActive = serviceRegistry.isServiceActive(serviceId);
```

### RandomnessConsumer

Provides secure random number generation using the Neo Service Layer.

**Key Features:**
- Single and batch randomness requests
- Configurable ranges
- Request fulfillment tracking
- Automatic service metrics logging

**Usage:**
```solidity
// Request a random number between 1 and 100
bytes32 requestId = randomnessConsumer.requestRandomness(1, 100);

// Check if fulfilled
(bool fulfilled, uint256 randomValue) = randomnessConsumer.getRandomnessResult(requestId);
```

### OracleConsumer

Fetches external data through the Neo Service Layer Oracle Service.

**Key Features:**
- Multiple data source support
- Price data caching
- Request/response pattern
- Data source management

**Usage:**
```solidity
// Request Bitcoin price from CoinMarketCap
bytes32 requestId = oracleConsumer.requestOracleData("coinmarketcap", "bitcoin/price");

// Get the result
(bool fulfilled, bool success, bytes memory data) = oracleConsumer.getOracleResult(requestId);
```

### AbstractAccountFactory

Creates and manages abstract accounts with advanced features.

**Key Features:**
- Deterministic account creation
- Batch account creation
- Account lifecycle management
- Integration with Neo Service Layer

**Usage:**
```solidity
// Create an abstract account
address[] memory guardians = [guardian1, guardian2];
(bytes32 accountId, address accountAddress) = factory.createAccount(
    owner,
    guardians,
    2, // recovery threshold
    salt
);
```

### AbstractAccount

Individual account abstraction with social recovery and session keys.

**Key Features:**
- Social recovery with guardians
- Session keys with permissions
- Transaction execution
- Nonce-based replay protection

**Usage:**
```solidity
// Execute a transaction
bool success = account.executeTransaction(
    targetAddress,
    value,
    data
);

// Create a session key
account.createSessionKey(
    sessionKeyAddress,
    expirationTime,
    maxTransactionValue,
    allowedContracts
);
```

## 🧪 Testing

The test suite covers all major functionality:

```bash
# Run all tests
npm run test

# Run with gas reporting
npm run gas-report

# Run with coverage
npm run coverage
```

### Test Categories

1. **Unit Tests** - Individual contract functionality
2. **Integration Tests** - Cross-contract interactions
3. **Workflow Tests** - End-to-end scenarios
4. **Security Tests** - Access control and edge cases

## 🚀 Deployment

### Local Development

```bash
# Start local Hardhat network
npx hardhat node

# Deploy to local network
npm run deploy
```

### NeoX Testnet

```bash
# Deploy to NeoX testnet
npm run deploy:testnet

# Verify contracts
npm run verify
```

### NeoX Mainnet

```bash
# Deploy to NeoX mainnet
npm run deploy:mainnet

# Verify contracts
npm run verify
```

## 🔧 Configuration

### Hardhat Configuration

The `hardhat.config.ts` file includes:

- Network configurations for NeoX
- Compiler optimization settings
- Gas reporting configuration
- Contract verification setup

### Supported Networks

- **Hardhat Local** - Development and testing
- **NeoX Testnet** - Testing deployment
- **NeoX Mainnet** - Production deployment

## 📊 Gas Optimization

All contracts are optimized for gas efficiency:

- **Compiler Optimization**: Enabled with 200 runs
- **Storage Packing**: Efficient struct layouts
- **Batch Operations**: Reduced transaction costs
- **Access Patterns**: Optimized for common use cases

## 🔒 Security Features

### Access Control
- Owner-based permissions
- Role-based access control
- Guardian-based recovery

### Protection Mechanisms
- Reentrancy guards
- Input validation
- Overflow protection
- Replay attack prevention

### Audit Considerations
- Comprehensive test coverage
- Security-focused design patterns
- OpenZeppelin library usage
- Clear upgrade paths

## 🔗 Integration Examples

### DeFi Integration

```solidity
contract DeFiProtocol {
    RandomnessConsumer randomness;
    OracleConsumer oracle;
    
    function liquidatePosition() external {
        // Get random liquidation order
        bytes32 randomId = randomness.requestRandomness(0, positions.length);
        
        // Get current price
        bytes32 priceId = oracle.requestOracleData("coinmarketcap", "ethereum/price");
        
        // Process liquidation...
    }
}
```

### Gaming Integration

```solidity
contract GameContract {
    RandomnessConsumer randomness;
    AbstractAccountFactory accountFactory;
    
    function createPlayerAccount(address player) external {
        // Create abstract account for player
        address[] memory guardians = [gameOperator];
        accountFactory.createAccount(player, guardians, 1, keccak256(abi.encode(player)));
    }
    
    function rollDice() external {
        // Get random number for dice roll
        bytes32 requestId = randomness.requestRandomness(1, 7);
        // Handle result in callback...
    }
}
```

## 📚 Documentation

- [Contract API Reference](./docs/api.md)
- [Integration Guide](./docs/integration.md)
- [Security Best Practices](./docs/security.md)
- [Deployment Guide](./docs/deployment.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

MIT License - see [LICENSE](../LICENSE) file for details.

## 🆘 Support

For support and questions:

- GitHub Issues: [Create an issue](https://github.com/neo-service-layer/issues)
- Documentation: [Full documentation](https://docs.neo-service-layer.com)
- Community: [Discord server](https://discord.gg/neo-service-layer)

---

**Note**: These contracts are designed specifically for the NeoX blockchain and integrate with the Neo Service Layer's confidential computing infrastructure. They provide a secure bridge between on-chain applications and off-chain confidential services.
