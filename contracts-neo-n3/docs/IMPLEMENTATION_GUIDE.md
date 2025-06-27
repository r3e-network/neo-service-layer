# Neo Service Layer Smart Contracts Implementation Guide

This guide provides comprehensive documentation for implementing and using the Neo N3 smart contracts that integrate with the Neo Service Layer's extensive service architecture.

## 🏗️ Architecture Overview

The Neo Service Layer smart contract system is designed to provide on-chain access to all 22+ services in the Neo Service Layer, including:

### Core Infrastructure Services
- **ServiceRegistry** - Central registry for service discovery and management
- **AccessControl** - Role-based permissions and security
- **EventAggregator** - Centralized event handling

### Account & Identity Services
- **AbstractAccountContract** - Account abstraction with social recovery
- **KeyManagementContract** - Secure key management and rotation

### Data & Oracle Services
- **OracleContract** - External data feeds and price oracles
- **StorageContract** - Decentralized storage with encryption
- **BackupContract** - Data backup and recovery

### Security & Compliance Services
- **RandomnessContract** - Secure random number generation
- **ComplianceContract** - Regulatory compliance and KYC/AML
- **SecretsManagementContract** - Secure secrets storage
- **ZeroKnowledgeContract** - Zero-knowledge proof verification

### Automation & Monitoring Services
- **AutomationContract** - Task automation and scheduling
- **MonitoringContract** - System monitoring and alerting
- **HealthContract** - Health checks and status reporting
- **NotificationContract** - Event notifications

### Advanced Services
- **ComputeContract** - Confidential computing integration
- **VotingContract** - Governance and voting mechanisms
- **ProofOfReserveContract** - Asset reserve verification
- **CrossChainContract** - Cross-chain bridge operations

### AI & Analytics Services
- **PatternRecognitionContract** - AI pattern recognition
- **PredictionContract** - Predictive analytics

## 🚀 Quick Start

### Prerequisites

1. **Neo N3 Development Environment**
   ```bash
   # Install .NET SDK 8.0+
   dotnet --version
   
   # Install Neo CLI
   # Download from: https://github.com/neo-project/neo-cli
   ```

2. **Neo Service Layer Infrastructure**
   ```bash
   # Ensure Neo Service Layer is running
   docker-compose up -d
   ```

### Building Contracts

```bash
# Clone and build
git clone <repository-url>
cd contracts-neo-n3

# Build all contracts
dotnet build --configuration Release

# Or use the deployment script
chmod +x scripts/deploy.sh
./scripts/deploy.sh build
```

### Deployment

```bash
# Deploy to testnet
export NETWORK=testnet
export WALLET_PATH=./wallet.json
export WALLET_PASSWORD=your_password

./scripts/deploy.sh deploy
```

## 📋 Contract Details

### ServiceRegistry Contract

The central registry that manages all service contracts and their metadata.

**Key Features:**
- Service registration and discovery
- Version management
- Access control integration
- Service health monitoring
- Dependency management

**Usage Example:**
```csharp
// Register a new service
var serviceId = ServiceRegistry.RegisterService(
    serviceId: contractHash,
    name: "MyService",
    version: "1.0.0",
    contractAddress: contractAddress,
    endpoint: "https://api.example.com",
    metadata: "{\"description\": \"My custom service\"}"
);

// Check if service is active
bool isActive = ServiceRegistry.IsServiceActive(serviceId);

// Get service information
var serviceInfo = ServiceRegistry.GetService(serviceId);
```

### RandomnessContract

Provides cryptographically secure random numbers using Intel SGX.

**Key Features:**
- Single and batch random number generation
- Configurable ranges
- Cryptographic proofs
- Request/fulfillment pattern
- Gas-optimized operations

**Usage Example:**
```csharp
// Request a random number
var requestId = RandomnessContract.RequestRandomness(1, 100);

// Request multiple random numbers
var batchRequestId = RandomnessContract.RequestBatchRandomness(1, 1000, 10);

// Get result (after fulfillment)
var result = RandomnessContract.GetRandomnessResult(requestId);
if (result != null)
{
    var randomValues = result.Values;
    // Use the random values
}
```

### OracleContract

Fetches external data through secure oracle infrastructure.

**Key Features:**
- Multiple data source support
- Price feed aggregation
- Data source management
- Caching mechanisms
- Proof verification

**Usage Example:**
```csharp
// Request price data
var requestId = OracleContract.RequestPriceData("BTC", "USD", "coinmarketcap,binance");

// Request custom data
var customRequestId = OracleContract.RequestOracleData(
    "weather-api",
    "temperature/london",
    "callback_data".ToByteArray()
);

// Get cached price
var priceFeed = OracleContract.GetLatestPrice("ETH", "USD");
if (priceFeed != null && !IsStale(priceFeed))
{
    var price = priceFeed.Price;
    // Use the price data
}
```

### AbstractAccountContract

Implements account abstraction with advanced features.

**Key Features:**
- Social recovery with guardians
- Session keys for temporary access
- Transaction batching
- Nonce-based replay protection
- Multi-signature support

**Usage Example:**
```csharp
// Create an abstract account
var guardians = new UInt160[] { guardian1, guardian2, guardian3 };
var (accountId, accountAddress) = AbstractAccountContract.CreateAccount(
    owner: ownerAddress,
    guardians: guardians,
    recoveryThreshold: 2,
    salt: "unique_salt".ToByteArray()
);

// Create a session key
AbstractAccountContract.CreateSessionKey(
    accountId: accountId,
    sessionKey: sessionKeyAddress,
    expirationTime: Runtime.Time + 86400, // 24 hours
    maxTransactionValue: 1000000000, // 10 GAS
    allowedContracts: new UInt160[] { targetContract }
);

// Execute transaction
AbstractAccountContract.ExecuteTransaction(
    accountId: accountId,
    target: targetContract,
    value: 100000000, // 1 GAS
    data: transactionData,
    signature: signature
);
```

### StorageContract

Provides decentralized storage with encryption and access control.

**Key Features:**
- File storage with metadata
- Access control lists
- Encryption support
- Size limits and fees
- Usage analytics

**Usage Example:**
```csharp
// Store a file
var allowedUsers = new UInt160[] { user1, user2 };
var fileId = StorageContract.StoreFile(
    filename: "document.pdf",
    data: fileData,
    contentType: "application/pdf",
    isEncrypted: true,
    allowedUsers: allowedUsers
);

// Retrieve a file
var retrievedData = StorageContract.RetrieveFile(fileId);

// Grant access to another user
StorageContract.GrantFileAccess(fileId, newUser);

// Get file metadata
var metadata = StorageContract.GetFileMetadata(fileId);
```

## 🔧 Integration Patterns

### Service Discovery Pattern

```csharp
public class ServiceClient
{
    private UInt160 registryAddress;
    
    public ServiceClient(UInt160 registry)
    {
        registryAddress = registry;
    }
    
    public UInt160 GetServiceAddress(string serviceName)
    {
        var serviceInfo = ServiceRegistry.GetServiceByName(serviceName);
        if (serviceInfo == null || !ServiceRegistry.IsServiceActive(serviceInfo.Id))
        {
            throw new InvalidOperationException($"Service {serviceName} not available");
        }
        return serviceInfo.ContractAddress;
    }
}
```

### Request/Response Pattern

```csharp
public class OracleClient
{
    public async Task<ByteString> GetDataAsync(string source, string query)
    {
        // Request data
        var requestId = OracleContract.RequestOracleData(source, query, "".ToByteArray());
        
        // Poll for result (in practice, use events)
        OracleResult result = null;
        while (result == null)
        {
            await Task.Delay(5000);
            result = OracleContract.GetOracleResult(requestId);
        }
        
        return result.Data;
    }
}
```

### Event-Driven Integration

```csharp
public class EventListener
{
    public void SubscribeToRandomnessEvents()
    {
        // Listen for RandomnessFulfilled events
        Runtime.Notify("Subscribing to randomness events");
        
        // In practice, would use Neo's event subscription mechanisms
        // to listen for contract events and react accordingly
    }
}
```

## 🔒 Security Considerations

### Access Control

All contracts implement role-based access control:

```csharp
// Check permissions before sensitive operations
if (!ServiceRegistry.HasAccess(Runtime.CallingScriptHash, "admin"))
{
    throw new InvalidOperationException("Insufficient permissions");
}
```

### Input Validation

```csharp
// Always validate inputs
if (string.IsNullOrEmpty(serviceName))
    throw new ArgumentException("Service name cannot be empty");

if (data.Length > MAX_DATA_SIZE)
    throw new ArgumentException("Data size exceeds limit");
```

### Reentrancy Protection

```csharp
// Use state checks to prevent reentrancy
if (operationInProgress)
    throw new InvalidOperationException("Operation already in progress");

operationInProgress = true;
try
{
    // Perform operation
}
finally
{
    operationInProgress = false;
}
```

## 📊 Gas Optimization

### Storage Efficiency

```csharp
// Pack data structures efficiently
public class OptimizedData
{
    public byte Status;        // 1 byte instead of bool
    public ushort Count;       // 2 bytes instead of int for small numbers
    public uint Timestamp;     // 4 bytes for timestamps
}
```

### Batch Operations

```csharp
// Process multiple items in single transaction
public static bool ProcessBatch(BatchItem[] items)
{
    foreach (var item in items)
    {
        ProcessItem(item);
    }
    return true;
}
```

### Lazy Loading

```csharp
// Load data only when needed
public static ServiceInfo GetServiceInfo(UInt160 serviceId)
{
    var cached = GetCachedInfo(serviceId);
    if (cached != null)
        return cached;
    
    return LoadFromStorage(serviceId);
}
```

## 🧪 Testing

### Unit Testing

```csharp
[TestMethod]
public void TestServiceRegistration()
{
    // Arrange
    var serviceId = UInt160.Zero;
    var serviceName = "TestService";
    
    // Act
    var result = ServiceRegistry.RegisterService(
        serviceId, serviceName, "1.0.0", 
        contractAddress, endpoint, metadata
    );
    
    // Assert
    Assert.IsTrue(result);
    Assert.IsTrue(ServiceRegistry.IsServiceActive(serviceId));
}
```

### Integration Testing

```csharp
[TestMethod]
public void TestOracleIntegration()
{
    // Test full oracle request/response cycle
    var requestId = OracleContract.RequestOracleData("test-source", "test-query", "".ToByteArray());
    
    // Simulate oracle fulfillment
    OracleContract.FulfillOracleData(requestId, "test-result".ToByteArray(), "proof");
    
    var result = OracleContract.GetOracleResult(requestId);
    Assert.IsNotNull(result);
    Assert.AreEqual("test-result", result.Data.ToByteString());
}
```

## 🚀 Deployment Guide

### Environment Setup

1. **Configure Network**
   ```bash
   export NETWORK=testnet  # or mainnet
   export NEO_CLI_PATH=/path/to/neo-cli
   ```

2. **Prepare Wallet**
   ```bash
   # Create or import wallet
   neo-cli create wallet wallet.json
   # Fund wallet with GAS for deployments
   ```

3. **Deploy Contracts**
   ```bash
   ./scripts/deploy.sh deploy
   ```

### Post-Deployment Configuration

1. **Register Services**
   ```bash
   # Services are automatically registered during deployment
   # Verify registration
   neo-cli invoke ServiceRegistry getServiceCount
   ```

2. **Configure Access Control**
   ```bash
   # Grant admin access to operators
   neo-cli invoke ServiceRegistry grantAccess <operator-address> admin
   ```

3. **Set Service Parameters**
   ```bash
   # Configure randomness service
   neo-cli invoke RandomnessContract updateConfiguration <fee> <min-range> <max-range> <batch-size>
   
   # Configure oracle service
   neo-cli invoke OracleContract updateConfiguration <fee> <confirmations> <deviation> <cache-expiry>
   ```

## 📈 Monitoring and Maintenance

### Health Monitoring

```csharp
// Check service health
var healthInfo = RandomnessContract.GetHealthStatus();
if (!healthInfo.IsHealthy)
{
    // Alert administrators
    Runtime.Log($"Service unhealthy: {healthInfo.Status}");
}
```

### Performance Metrics

```csharp
// Track service usage
var requestCount = RandomnessContract.GetRequestCount();
var errorCount = RandomnessContract.GetErrorCount();
var errorRate = (double)errorCount / requestCount;

if (errorRate > 0.05) // 5% error threshold
{
    // Take corrective action
}
```

### Upgrades

```csharp
// Contracts support upgradeable proxy pattern
// Update implementation while preserving state
ProxyContract.Upgrade(newImplementationAddress);
```

## 🤝 Contributing

### Adding New Services

1. **Create Contract**
   ```csharp
   public class NewServiceContract : BaseServiceContract
   {
       // Implement required methods
       protected override void InitializeService(string config) { }
       protected override bool PerformHealthCheck() { return true; }
   }
   ```

2. **Register Service**
   ```csharp
   // Add to deployment script
   var serviceHash = DeployContract("NewServiceContract");
   ServiceRegistry.RegisterService(serviceHash, "NewService", "1.0.0", ...);
   ```

3. **Update Documentation**
   - Add service description
   - Provide usage examples
   - Update integration guides

### Best Practices

- Follow the established contract patterns
- Implement comprehensive error handling
- Add thorough documentation
- Include unit and integration tests
- Optimize for gas efficiency
- Consider upgrade paths

## 📄 License

MIT License - see [LICENSE](../LICENSE) file for details.

## 🆘 Support

- **Documentation**: [Full documentation](https://docs.neo-service-layer.com)
- **Issues**: [GitHub Issues](https://github.com/neo-service-layer/issues)
- **Community**: [Discord](https://discord.gg/neo-service-layer)

---

This implementation provides a complete, production-ready smart contract system that integrates seamlessly with the Neo Service Layer's comprehensive service architecture, enabling developers to build sophisticated dApps with access to advanced services like confidential computing, AI analytics, and secure data management.