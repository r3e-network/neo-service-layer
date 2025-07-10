# Neo Service Layer Microservices Integration Tests

This directory contains comprehensive integration tests for the Neo Service Layer microservices architecture.

## Testing Approaches

We provide multiple testing approaches to accommodate different scenarios:

1. **In-Memory Tests** - Tests that don't require any external infrastructure
2. **Mocked Tests** - Tests using mocked HTTP responses  
3. **Container Tests** - Tests using Testcontainers (when properly configured)
4. **Conditional Tests** - Tests that run only when infrastructure is available
5. **Full Integration Tests** - Original tests requiring complete infrastructure

## Overview

These tests verify the proper functioning of:
- Service-to-service communication
- API Gateway routing and policies
- Rate limiting and authentication
- Circuit breakers and resilience patterns
- Service discovery and health checks
- Distributed tracing
- Event-driven communication

## Test Categories

### 1. **MicroservicesIntegrationTests**
Basic integration tests for core functionality:
- Gateway health checks
- Service discovery
- Authentication flow
- Rate limiting enforcement
- Metrics collection

### 2. **ServiceCommunicationTests**
Tests for inter-service communication patterns:
- KeyManagement → Storage integration
- Backup → Storage + Notification orchestration
- Configuration propagation
- Cross-chain service coordination
- Event subscription patterns

### 3. **ResilienceTests**
Tests for fault tolerance and resilience:
- Circuit breaker activation
- Retry policies
- Bulkhead isolation
- Timeout handling
- Graceful degradation
- Load balancing distribution

## Running the Tests

### Prerequisites
- Docker and Docker Compose installed
- .NET 9.0 SDK
- At least 8GB of available RAM
- Ports 5432, 5672, 7000, 8500, 15672, 16686 available

### Quick Start

```bash
# Run all integration tests
./run-integration-tests.sh

# Run specific test class
./run-integration-tests.sh --test MicroservicesIntegrationTests

# Run specific test method
./run-integration-tests.sh --test "MicroservicesIntegrationTests.GatewayHealthCheck_ShouldReturnHealthy"

# Skip Docker image building (if already built)
./run-integration-tests.sh --skip-build

# Keep containers running after tests (for debugging)
./run-integration-tests.sh --keep-running
```

### Manual Setup

If you prefer to run the tests manually:

```bash
# Start the test environment
docker-compose -f docker-compose.test.yml up -d

# Wait for services to be ready (check http://localhost:8500 for Consul)
# Then run the tests
dotnet test ../NeoServiceLayer.Integration.Tests.csproj \
    --filter "Namespace~Microservices" \
    --configuration Release

# Cleanup
docker-compose -f docker-compose.test.yml down -v
```

## Test Environment

The test environment includes:

### Infrastructure Services
- **Consul**: Service discovery and configuration (port 8500)
- **RabbitMQ**: Message broker (ports 5672, 15672)
- **Jaeger**: Distributed tracing (port 16686)
- **PostgreSQL**: Test database (port 5432)

### Microservices
- **API Gateway**: Entry point with rate limiting (port 7000)
- **Auth Service**: JWT token generation
- **Notification Service**: Email/SMS/Push notifications
- **Storage Service**: Distributed storage
- **Configuration Service**: Dynamic configuration
- **Health Service**: Health monitoring
- **Monitoring Service**: Metrics and telemetry
- **KeyManagement Service**: Cryptographic key management
- **Backup Service**: Backup orchestration
- **EventSubscription Service**: Event streaming
- **Compliance Service**: Regulatory compliance
- **CrossChain Service**: Cross-blockchain operations

## Debugging Failed Tests

### View Service Logs
```bash
# View all logs
docker-compose -f docker-compose.test.yml logs

# View specific service logs
docker-compose -f docker-compose.test.yml logs notification-test

# Follow logs in real-time
docker-compose -f docker-compose.test.yml logs -f gateway-test
```

### Access Running Services
- **Consul UI**: http://localhost:8500
- **RabbitMQ Management**: http://localhost:15672 (user: test, pass: test123)
- **Jaeger UI**: http://localhost:16686
- **API Gateway**: http://localhost:7000

### Common Issues

1. **Port Conflicts**: Ensure required ports are free
   ```bash
   # Check port usage
   lsof -i :8500  # Consul
   lsof -i :7000  # Gateway
   ```

2. **Service Registration**: Services need time to register
   ```bash
   # Check registered services
   curl http://localhost:8500/v1/catalog/services
   ```

3. **Database Connection**: Ensure PostgreSQL is ready
   ```bash
   # Check database logs
   docker-compose -f docker-compose.test.yml logs postgres-test
   ```

## Writing New Tests

### Test Structure
```csharp
[Fact(Skip = "Requires running microservices infrastructure")]
public async Task YourTest_ShouldDoSomething()
{
    // Arrange
    var request = new { /* request data */ };
    
    // Act
    var response = await _httpClient.PostAsJsonAsync("/api/service/endpoint", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ResultType>();
    result.Should().NotBeNull();
    // Additional assertions...
}
```

### Best Practices
1. Use `[Fact(Skip = "...")]` to prevent tests from running in CI without infrastructure
2. Include proper cleanup in test disposal
3. Use realistic timeouts for async operations
4. Log important information for debugging
5. Group related tests in the same class
6. Use descriptive test names following the pattern: `Method_Scenario_ExpectedResult`

## CI/CD Integration

For CI/CD pipelines, you can run these tests using:

```yaml
# GitHub Actions example
- name: Run Integration Tests
  run: |
    cd tests/Integration/NeoServiceLayer.Integration.Tests/Microservices
    ./run-integration-tests.sh
  timeout-minutes: 30
```

## Performance Considerations

- The full test suite takes approximately 10-15 minutes to run
- Docker images are built in parallel for faster setup
- Services are health-checked before running tests
- Use `--skip-build` to save time when images haven't changed

## Troubleshooting

### Reset Test Environment
```bash
# Complete cleanup
docker-compose -f docker-compose.test.yml down -v
docker system prune -f
```

### Check Service Health
```bash
# Check all service health endpoints
for port in 8081 8082 8083 8084 8085; do
    echo "Checking port $port:"
    curl -s http://localhost:$port/health | jq .
done
```

### Export Test Results
Test results are saved in TRX format:
```bash
# View test results
dotnet tool install -g trx2junit
trx2junit microservices-integration-test-results.trx
```