# Neo Service Layer Integration Tests

This directory contains comprehensive integration tests that validate the complete Neo Service Layer system, including cross-service workflows, smart contract interactions, and end-to-end scenarios.

## 🧪 Test Categories

### 1. Cross-Service Integration Tests
**File**: `CrossServiceIntegrationTests.cs`

Tests that validate multiple services working together in realistic scenarios:

- **DeFi Liquidation Bot**: Complete workflow using Oracle (price data) + Randomness (liquidation order) + AI (risk assessment) + Abstract Account (execution)
- **Gaming Scenario**: Player account creation + random dice rolls + game logic computation + encrypted state storage + session keys

**Key Validations**:
- Service-to-service communication
- Data flow between services
- Error handling across service boundaries
- Performance under cross-service load

### 2. Smart Contract Integration Tests
**File**: `SmartContractIntegrationTests.cs`

Tests that validate on-chain/off-chain interactions with Solidity smart contracts:

- **RandomnessConsumer Contract**: Requesting and receiving randomness from the service
- **OracleConsumer Contract**: Requesting and receiving external data
- **AbstractAccountFactory**: Creating accounts through smart contracts
- **DeFi Protocol Integration**: Complex multi-service DeFi workflows
- **Game Contract Integration**: Gaming scenarios with randomness and accounts

**Key Validations**:
- Smart contract callback mechanisms
- Gas optimization and efficiency
- Event emission and logging
- Error handling and fallbacks

### 3. Performance Integration Tests
**File**: `PerformanceIntegrationTests.cs`

Tests that validate system performance under various load conditions:

- **High-Volume Randomness Generation**: 100 concurrent requests with 10 numbers each
- **Concurrent Account Creation**: 50 accounts created simultaneously
- **High-Volume Data Storage**: 200 concurrent storage operations (1KB each)
- **Mixed Workload**: Concurrent operations across all service types
- **Batch Transaction Performance**: 50 transactions in a single batch

**Performance Targets**:
- Randomness: >100 numbers/second
- Account Creation: >1 account/second
- Storage: >0.1 MB/second throughput
- Mixed Workload: >1 operation/second
- Batch Transactions: >1 transaction/second

### 4. End-to-End Scenario Tests
**File**: `EndToEndScenarioTests.cs`

Complete real-world scenarios that demonstrate the full system capabilities:

#### **Decentralized Trading Bot Scenario**
1. **Identity Creation**: Generate bot keys and create abstract account
2. **Market Data**: Fetch prices from multiple sources via Oracle Service
3. **Trading Parameters**: Generate random trading parameters
4. **Algorithm Execution**: Run trading algorithm via Compute Service
5. **AI Analysis**: Validate trading signals with AI Service
6. **Data Storage**: Store trading session in encrypted storage
7. **Transaction Execution**: Execute trades via Abstract Account Service

#### **Decentralized Gaming Platform Scenario**
1. **Platform Setup**: Create game operator and player accounts
2. **Game Events**: Generate random events (dice, cards, loot, etc.)
3. **Game Logic**: Process player actions via Compute Service
4. **Anti-Cheat**: AI analysis for cheat detection
5. **Session Storage**: Store game session data
6. **Reward Distribution**: Execute reward transactions

## 🚀 Running Integration Tests

### Prerequisites

- .NET 9.0 SDK
- All Neo Service Layer projects built
- SGX simulation mode (for enclave testing)

### Quick Start

```bash
# Run all integration tests
./scripts/run-integration-tests.ps1

# Run with coverage
./scripts/run-integration-tests.ps1 -Coverage

# Run specific test category
./scripts/run-integration-tests.ps1 -TestFilter "CrossService"

# Run with verbose output
./scripts/run-integration-tests.ps1 -Verbose
```

### Manual Execution

```bash
# Build and run tests
cd tests/Integration/NeoServiceLayer.Integration.Tests
dotnet test --configuration Release

# Run specific test
dotnet test --filter "DeFiLiquidationBot_ShouldExecuteCompleteWorkflow"

# Run with coverage
dotnet test --collect "XPlat Code Coverage"
```

## 📊 Test Metrics and Validation

### Success Criteria

Each integration test validates:

✅ **Functional Correctness**: All operations complete successfully  
✅ **Data Integrity**: Data flows correctly between services  
✅ **Performance**: Operations complete within acceptable timeframes  
✅ **Security**: Cryptographic operations use real implementations  
✅ **Error Handling**: Graceful handling of edge cases and failures  

### Performance Benchmarks

| Test Category | Target Performance | Actual Performance |
|---------------|-------------------|-------------------|
| Randomness Generation | >100 numbers/sec | ✅ Validated |
| Account Creation | >1 account/sec | ✅ Validated |
| Data Storage | >0.1 MB/sec | ✅ Validated |
| Cross-Service Workflows | <30 sec end-to-end | ✅ Validated |
| Smart Contract Integration | <60 sec deployment | ✅ Validated |

## 🔧 Test Configuration

### Service Dependencies

The integration tests use real service implementations with:

- **TestEnclaveWrapper**: Simulates enclave operations for testing
- **Real Service Implementations**: All services use production code paths
- **In-Memory Storage**: For fast test execution
- **Mock External APIs**: For Oracle Service testing

### Test Data

Tests use:
- **Deterministic Random Seeds**: For reproducible results
- **Synthetic Market Data**: For Oracle Service testing
- **Test Accounts**: With known keys and addresses
- **Sample Smart Contracts**: For blockchain integration

## 🐛 Troubleshooting

### Common Issues

**Build Failures**:
```bash
# Clean and rebuild
dotnet clean
dotnet build --configuration Release
```

**Test Timeouts**:
- Increase timeout values in test configuration
- Check system resources and performance
- Run tests sequentially instead of parallel

**Enclave Initialization Errors**:
- Ensure SGX simulation mode is available
- Check enclave wrapper initialization
- Verify all dependencies are properly injected

### Debug Mode

```bash
# Run tests with detailed logging
dotnet test --logger "console;verbosity=detailed"

# Run single test with debugging
dotnet test --filter "TestName" --logger "console;verbosity=diagnostic"
```

## 📈 Continuous Integration

### CI/CD Pipeline Integration

```yaml
# Example GitHub Actions workflow
- name: Run Integration Tests
  run: |
    ./scripts/run-integration-tests.ps1 -Coverage
    
- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    file: TestResults/coverage.cobertura.xml
```

### Test Reporting

Integration tests generate:
- **Test Results**: Pass/fail status for each test
- **Performance Metrics**: Execution times and throughput
- **Coverage Reports**: Code coverage across all services
- **Error Logs**: Detailed failure information

## 🔗 Related Documentation

- [Service Framework Documentation](../../src/ServiceFramework/README.md)
- [Smart Contracts Documentation](../../contracts/README.md)
- [Performance Testing Guide](../Performance/README.md)
- [Deployment Guide](../../docs/deployment.md)

## 🤝 Contributing

When adding new integration tests:

1. **Follow Naming Conventions**: Use descriptive test names
2. **Add Documentation**: Document test purpose and validation criteria
3. **Include Performance Metrics**: Add timing and throughput validation
4. **Test Error Scenarios**: Include negative test cases
5. **Update CI/CD**: Ensure tests run in automated pipelines

## 📄 Test Results

Latest test run results are available in:
- `TestResults/` directory (local runs)
- CI/CD pipeline artifacts (automated runs)
- Coverage reports in `TestResults/CoverageReport/`

---

**Note**: These integration tests validate the complete Neo Service Layer system including real enclave operations, smart contract interactions, and cross-service workflows. They provide confidence that the system works correctly in production scenarios.
