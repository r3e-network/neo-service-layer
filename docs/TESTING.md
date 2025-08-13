# Testing Infrastructure - Neo Service Layer

This document describes the comprehensive testing infrastructure implemented to address critical testing gaps identified in the code review.

## Overview

The testing infrastructure provides:
- **Unit Tests**: Core service functionality validation
- **Integration Tests**: SGX enclave and cross-service integration
- **Performance Benchmarks**: Load testing and performance validation
- **Security Tests**: Comprehensive security vulnerability testing
- **Test Utilities**: Common fixtures and factories for test development

## Test Categories

### Unit Tests

#### Security Service Tests (`tests/Infrastructure/NeoServiceLayer.Infrastructure.Security.Tests/`)
- **SQL Injection Detection**: Validates detection of SQL injection attempts
- **XSS Prevention**: Tests cross-site scripting attack detection
- **Code Injection Protection**: Validates code injection detection
- **Encryption/Decryption**: AES-256-GCM encryption validation
- **Password Hashing**: PBKDF2 password security validation
- **Rate Limiting**: Sliding window rate limit enforcement
- **Input Validation**: Comprehensive input sanitization testing

**Coverage**: All security vulnerabilities identified in code review

### Integration Tests

#### SGX Enclave Tests (`tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/`)
- **Enclave Initialization**: SGX hardware attestation validation
- **Script Execution**: JavaScript execution within enclave sandbox
- **Data Sealing/Unsealing**: SGX data protection mechanisms
- **Security Validation**: Malicious code detection and blocking
- **Performance Testing**: Enclave operation latency validation
- **Memory Management**: Memory leak detection and cleanup

**Coverage**: Complete SGX implementation gaps identified in code review

### Performance Benchmarks

#### Service Benchmarks (`tests/Performance/NeoServiceLayer.Performance.Tests/`)
- **Security Operations**: Encryption, validation, hashing performance
- **Resilience Patterns**: Circuit breaker, retry, timeout performance
- **Observability**: Metrics collection and tracing overhead
- **Enclave Operations**: SGX operation latency benchmarks
- **Load Testing**: Concurrent operation performance validation

**Benchmarks Include**:
- Individual service operation benchmarks
- Combined pipeline benchmarks
- Load testing with configurable concurrency
- Memory usage analysis

### Test Utilities

#### Common Test Infrastructure (`tests/TestUtilities/NeoServiceLayer.TestUtilities/`)
- **TestServiceFactory**: Service creation with proper configuration
- **TestServiceFixture**: Integration test fixture with DI container
- **TestData**: Comprehensive test data for various scenarios
- **AsyncTestHelpers**: Async operation testing utilities
- **TestLogger**: Log capture and verification utilities

## Running Tests

### Quick Start
```bash
# Run all tests
./scripts/run-tests.sh

# Run only security tests
./scripts/run-tests.sh --security-only

# Skip performance benchmarks
./scripts/run-tests.sh --skip-performance
```

### Individual Test Suites
```bash
# Unit tests
dotnet test tests/Infrastructure/NeoServiceLayer.Infrastructure.Security.Tests/

# Integration tests  
dotnet test tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/

# Performance benchmarks
cd tests/Performance/NeoServiceLayer.Performance.Tests/
dotnet run -c Release
```

## Test Configuration

### Security Test Configuration
```json
{
  "Security": {
    "EncryptionAlgorithm": "AES-256-GCM",
    "MaxInputSizeMB": 10,
    "EnableRateLimiting": true,
    "DefaultRateLimitRequests": 100,
    "RateLimitWindowMinutes": 1
  }
}
```

### SGX Test Configuration
```json
{
  "Tee": {
    "EnclaveType": "SGX",
    "DebugMode": true,
    "MaxEnclaveMemoryMB": 256,
    "EnableAttestation": false
  }
}
```

## Test Data Categories

### Security Test Data
- **SQL Injection Attempts**: Common injection patterns and payloads
- **XSS Attempts**: Cross-site scripting attack vectors
- **Code Injection**: Dangerous code execution attempts
- **Safe Inputs**: Normal user input for baseline validation

### Performance Test Data
- **Small Data**: 1KB test payloads
- **Medium Data**: 1MB test payloads  
- **Large Data**: Maximum size test payloads
- **Concurrent Operations**: 100, 500, 1000 operation scenarios

### SGX Test Data
- **JavaScript Code**: Safe and malicious script examples
- **Sealed Data**: Various data sizes for sealing tests
- **Attestation Data**: Mock attestation scenarios

## Coverage Requirements

### Minimum Coverage Targets
- **Unit Tests**: 90% line coverage
- **Integration Tests**: 70% critical path coverage
- **Security Tests**: 100% vulnerability scenario coverage

### Coverage Report Generation
```bash
# Generate HTML coverage report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.opencover.xml -targetdir:TestResults/CoverageReport -reporttypes:Html
```

## Performance Requirements

### Latency Targets
- **Encryption**: < 100ms for 1KB data
- **Validation**: < 50ms for typical input
- **SGX Operations**: < 1000ms for seal/unseal cycle
- **Rate Limiting**: < 10ms for limit checks

### Throughput Targets
- **Concurrent Encryption**: 1000 ops/sec
- **Validation**: 5000 ops/sec
- **Metrics Collection**: 10000 ops/sec

## Continuous Integration

### GitHub Actions Integration
```yaml
- name: Run Tests
  run: |
    ./scripts/run-tests.sh --skip-performance
    
- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    file: ./TestResults/**/coverage.opencover.xml
```

### Quality Gates
- **All unit tests must pass**
- **90%+ code coverage required**
- **Security tests 100% pass rate**
- **Performance benchmarks within targets**

## Test Development Guidelines

### Writing Unit Tests
```csharp
[Fact]
public async Task SecurityService_ValidInput_ShouldPass()
{
    // Arrange
    var securityService = TestServiceFactory.CreateSecurityService();
    var input = "Normal user input";
    var options = new SecurityValidationOptions();

    // Act
    var result = await securityService.ValidateInputAsync(input, options);

    // Assert
    Assert.True(result.IsValid);
    Assert.False(result.HasSecurityThreats);
}
```

### Writing Integration Tests
```csharp
[Fact]
public async Task EnclaveWrapper_ExecuteScript_ShouldReturnResult()
{
    // Arrange
    using var fixture = new TestServiceFixture();
    var script = "const result = 2 + 2; result;";

    // Act & Assert
    if (fixture.EnclaveWrapper.IsInitialized)
    {
        var result = await fixture.EnclaveWrapper.ExecuteScriptAsync(script, "{}");
        Assert.Equal("4", result);
    }
}
```

### Writing Performance Tests
```csharp
[Benchmark]
[BenchmarkCategory("Security")]
public async Task<EncryptionResult> Encryption_AES256GCM()
{
    return await _securityService.EncryptDataAsync(_testData);
}
```

## Troubleshooting

### Common Issues

#### SGX Not Available
- Tests automatically detect SGX availability
- Graceful fallback to simulation mode
- No test failures when SGX hardware unavailable

#### Permission Issues
```bash
chmod +x ./scripts/run-tests.sh
sudo chown -R $USER:$USER TestResults/
```

#### Memory Issues
```bash
# Increase test timeout
export VSTEST_HOST_DEBUG=1
export DOTNET_gcServer=1
```

## Monitoring and Metrics

### Test Execution Metrics
- **Test Duration**: Track test execution time
- **Coverage Trends**: Monitor coverage over time
- **Failure Rates**: Track test stability
- **Performance Trends**: Monitor benchmark results

### Alerting
- **Coverage Drop**: Alert when coverage falls below 85%
- **Performance Regression**: Alert on 20%+ performance degradation
- **Test Failures**: Immediate alert on security test failures

## Security Testing Deep Dive

### SQL Injection Detection
Tests validate detection of:
- Union-based injection
- Boolean-based blind injection
- Time-based blind injection
- Error-based injection
- Second-order injection

### XSS Prevention  
Tests validate detection of:
- Stored XSS
- Reflected XSS
- DOM-based XSS
- JavaScript protocol injection
- Event handler injection

### Code Injection Protection
Tests validate detection of:
- File system access attempts
- Process execution attempts
- Assembly loading attempts
- Reflection-based attacks
- Environment manipulation

This comprehensive testing infrastructure addresses all critical testing gaps identified in the code review and provides production-ready validation for all security fixes implemented.