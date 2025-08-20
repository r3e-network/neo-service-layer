# NeoServiceLayer Examples

This directory contains comprehensive examples demonstrating how to use the NeoServiceLayer services in real-world scenarios.

## Example Files

### 1. BasicUsageExamples.cs
Basic usage examples for all major services including:
- **Storage Service**: Data storage, retrieval, updating, and searching
- **Backup Service**: Creating backups, scheduling, validation, and restoration
- **Monitoring Service**: Metrics collection, alerting, health monitoring
- **Cross-Chain Service**: Bridge registration, transfers, messaging
- **Complete Workflows**: End-to-end integration examples
- **Error Handling**: Robust error handling patterns
- **Performance Monitoring**: Tracing and performance analysis

### 2. AdvancedWorkflowExamples.cs
Advanced real-world workflow scenarios:
- **E-Commerce Platform**: Order processing with cross-chain payments
- **DeFi Liquidity Pool**: Liquidity management and arbitrage
- **IoT Data Pipeline**: High-volume sensor data processing
- **Disaster Recovery**: Complete disaster recovery simulation

### 3. SecurityTestingExamples.cs
Security testing and edge case scenarios:
- **Input Validation**: SQL injection, XSS, path traversal prevention
- **Cryptographic Security**: Encryption, key management, hashing
- **Access Control**: RBAC, privilege escalation prevention
- **Network Security**: MITM, replay attacks, DDoS protection

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- NeoServiceLayer packages
- Visual Studio or VS Code

### Running the Examples

1. **Basic Usage Examples**:
   ```bash
   dotnet run --project BasicUsageExamples.cs
   ```

2. **Advanced Workflow Examples**:
   ```bash
   dotnet run --project AdvancedWorkflowExamples.cs
   ```

3. **Security Testing Examples**:
   ```bash
   dotnet run --project SecurityTestingExamples.cs
   ```

## Key Concepts Demonstrated

### Service Configuration
```csharp
private IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    // Core services
    services.AddLogging(builder => builder.AddConsole());
    services.AddSingleton<IHttpClientService, HttpClientService>();
    services.AddSingleton<IBlockchainClientFactory, BlockchainClientFactory>();
    
    // Business services
    services.AddTransient<StorageService>();
    services.AddTransient<BackupService>();
    services.AddTransient<MonitoringService>();
    services.AddTransient<CrossChainService>();
    
    return services.BuildServiceProvider();
}
```

### Data Storage Patterns
```csharp
var storeResult = await storageService.StoreDataAsync(new StorageRequest
{
    Key = $"user:{userId}",
    Data = JsonSerializer.SerializeToUtf8Bytes(userData),
    Metadata = new Dictionary<string, object>
    {
        { "type", "user_profile" },
        { "version", "1.0" },
        { "encrypted", false }
    }
});
```

### Monitoring Integration
```csharp
await monitoringService.RecordMetricAsync(new MetricData
{
    Name = "api_response_time",
    Value = responseTimeMs,
    Tags = new Dictionary<string, string>
    {
        { "endpoint", "/api/users" },
        { "method", "GET" }
    },
    Timestamp = DateTime.UtcNow
});
```

### Cross-Chain Operations
```csharp
var transferResult = await crossChainService.InitiateCrossChainTransferAsync(new CrossChainTransferRequest
{
    TransferId = Guid.NewGuid().ToString(),
    SourceChain = BlockchainType.NeoN3,
    DestinationChain = BlockchainType.NeoX,
    AssetType = "GAS",
    Amount = 100.0m,
    SourceAddress = sourceAddress,
    DestinationAddress = destinationAddress
});
```

## Best Practices Demonstrated

### 1. Error Handling
- Comprehensive try-catch blocks
- Meaningful error messages
- Graceful degradation
- Retry mechanisms with exponential backoff

### 2. Security
- Input validation and sanitization
- Secure data storage patterns
- Authentication and authorization
- Audit logging

### 3. Performance
- Asynchronous operations
- Batch processing
- Performance monitoring
- Resource optimization

### 4. Monitoring & Observability
- Structured logging
- Metrics collection
- Health checks
- Performance tracking

### 5. Testing
- Unit test patterns
- Integration testing
- Security testing
- Performance testing

## Example Scenarios

### E-Commerce Order Processing
1. Create customer profile and order
2. Process cross-chain payment
3. Create transaction backup
4. Monitor payment status
5. Fulfill order upon confirmation
6. Generate analytics

### DeFi Liquidity Pool Management
1. Create liquidity pool
2. Add liquidity providers
3. Monitor pool metrics
4. Execute cross-chain arbitrage
5. Calculate and distribute rewards

### IoT Data Pipeline
1. Ingest sensor data from multiple devices
2. Process and aggregate data
3. Create hourly backups
4. Generate analytics and alerts
5. Handle high-volume data streams

### Disaster Recovery
1. Setup critical system state
2. Create comprehensive backups
3. Simulate disaster scenarios
4. Execute recovery procedures
5. Validate recovery completeness

## Configuration Examples

### Service Configuration
Examples show how to properly configure dependency injection, logging, and service registration.

### Environment-Specific Settings
Demonstrate how to handle different environments (development, staging, production) with appropriate security and performance settings.

### Monitoring Configuration
Show how to set up comprehensive monitoring with metrics, alerts, and health checks.

## Security Considerations

### Input Validation
- SQL injection prevention
- XSS protection
- Path traversal prevention
- Buffer overflow protection

### Data Protection
- Encryption at rest
- Secure key management
- Data integrity verification
- Access control

### Network Security
- Man-in-the-middle prevention
- Replay attack protection
- DDoS mitigation
- SSL/TLS security

## Performance Optimization

### Concurrent Operations
Examples demonstrate proper use of async/await patterns and concurrent execution.

### Resource Management
Proper disposal of resources and memory management.

### Caching Strategies
When and how to implement caching for improved performance.

### Batch Processing
Efficient handling of bulk operations.

## Troubleshooting

### Common Issues
- Service configuration errors
- Network connectivity issues
- Authentication failures
- Performance bottlenecks

### Debugging Techniques
- Structured logging
- Performance profiling
- Error tracking
- Health monitoring

## Contributing

When adding new examples:
1. Follow the established patterns
2. Include comprehensive error handling
3. Add monitoring and logging
4. Document the scenario clearly
5. Provide usage instructions

## Additional Resources

- [NeoServiceLayer Documentation](../docs/)
- [API Reference](../docs/api/)
- [Testing Guide](../tests/)
- [Performance Benchmarks](../benchmarks/)