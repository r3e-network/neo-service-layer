# Performance Testing Infrastructure

This document provides a comprehensive guide to the performance testing infrastructure implemented for the Neo Service Layer platform.

## Overview

The performance testing infrastructure uses BenchmarkDotNet for micro-benchmarks and automated regression detection to ensure consistent performance across releases.

## Architecture

```
tests/Performance/NeoServiceLayer.Performance.Tests/
├── SimpleCachingBenchmarks.cs          # Core caching performance tests
├── RegressionTests/
│   ├── PerformanceRegressionDetector.cs # Automated regression detection
│   └── AutomatedRegressionTests.cs      # Regression test cases
├── baseline-metrics.json               # Performance baseline metrics
└── NeoServiceLayer.Performance.Tests.csproj
```

## Key Components

### 1. BenchmarkDotNet Benchmarks

Located in `SimpleCachingBenchmarks.cs`, these benchmarks test:

- **Memory Cache Operations**: Set, Get, Batch operations
- **Cache Statistics**: Performance of statistics collection
- **Concurrent Load**: Multi-threaded cache performance

Example benchmark:
```csharp
[Benchmark]
public async Task MemoryCache_Set()
{
    for (int i = 0; i < ItemCount; i++)
        await _memoryCache.SetAsync(_cacheKeys[i], _testData[i]).ConfigureAwait(false);
}
```

### 2. Regression Detection System

The `PerformanceRegressionDetector` automatically:

- Compares current performance against baseline metrics
- Detects performance regressions using configurable thresholds
- Categorizes regressions by severity (Warning, Critical)
- Generates detailed analysis reports

### 3. Baseline Metrics

Stored in `baseline-metrics.json` with established performance benchmarks:

```json
{
  "version": "1.0.0",
  "timestamp": "2025-08-12T23:39:00.000Z",
  "metrics": {
    "SimpleCachingBenchmarks.MemoryCache_Set": {
      "averageResponseTimeMs": 8.5,
      "throughputPerSecond": 11764,
      "memoryUsageMB": 1.2
    }
  }
}
```

## Running Performance Tests

### Local Development

1. **Run all benchmarks:**
   ```bash
   cd tests/Performance/NeoServiceLayer.Performance.Tests
   dotnet run --configuration Release
   ```

2. **Run specific benchmark:**
   ```bash
   dotnet run --configuration Release -- --filter "*MemoryCache_Set*"
   ```

3. **Run regression tests:**
   ```bash
   dotnet test --configuration Release --filter "FullyQualifiedName~AutomatedRegressionTests"
   ```

### CI/CD Integration

Performance tests are automatically integrated into the CI/CD pipeline:

#### Main CI/CD Pipeline (`ci-cd.yml`)

- **Triggers**: Push to `master` branch
- **Components**:
  - BenchmarkDotNet execution with detailed reporting
  - Automated regression detection
  - Performance summary generation
  - Build failure on critical regressions

#### Performance Monitoring (`performance-monitoring.yml`)

- **Schedule**: Daily at 2 AM UTC
- **Purpose**: Track performance trends over time
- **Features**:
  - Comprehensive benchmark execution
  - Baseline metrics updates
  - Trend analysis and reporting
  - Automated issue creation for tracking

## Performance Budgets

Current performance thresholds:

| Metric | Warning Threshold | Critical Threshold |
|--------|-------------------|-------------------|
| Response Time | +10% | +20% |
| Throughput | -10% | -20% |
| Memory Usage | +15% | +30% |

### Caching Performance Targets

| Operation | Target Time | Target Throughput |
|-----------|-------------|-------------------|
| Memory Cache Set | <10ms | >10,000 ops/sec |
| Memory Cache Get | <5ms | >20,000 ops/sec |
| Batch Operations | <15ms | >5,000 ops/sec |

## Adding New Benchmarks

### 1. Create Benchmark Class

```csharp
[MemoryDiagnoser]
[SimpleJob]
[MarkdownExporter]
public class NewServiceBenchmarks
{
    private IMyService _service;
    
    [GlobalSetup]
    public void Setup()
    {
        // Initialize service and test data
    }
    
    [Benchmark]
    public async Task MyService_Operation()
    {
        await _service.PerformOperationAsync().ConfigureAwait(false);
    }
}
```

### 2. Register in Performance Tests

Add the benchmark to the test project and include in CI/CD filters.

### 3. Update Baseline Metrics

Run the performance monitoring workflow or manually update baseline metrics:

```bash
# Manual baseline update (for new benchmarks)
dotnet run --configuration Release -- --filter "*NewService*" --exporters json
```

## Analyzing Results

### BenchmarkDotNet Output

Results include:
- **Mean execution time** (with confidence intervals)
- **Memory allocation patterns**
- **Throughput measurements**
- **Statistical analysis** (outliers, standard deviation)

### Regression Analysis

The regression detector provides:
- **Percentage change** from baseline
- **Severity classification** (Warning/Critical)
- **Trend analysis** over multiple runs
- **Recommendations** for investigation

### Interpretation Guidelines

1. **Response Time Increases**:
   - 0-5%: Normal variation
   - 5-10%: Monitor trend
   - 10-20%: Warning - investigate
   - >20%: Critical - requires immediate attention

2. **Memory Usage Increases**:
   - 0-10%: Acceptable
   - 10-15%: Monitor
   - 15-30%: Warning
   - >30%: Critical

3. **Throughput Decreases**:
   - Follow same thresholds as response time

## Troubleshooting

### Common Issues

1. **Benchmark Timeout**:
   - Reduce iteration count for development
   - Check for deadlocks or infinite loops

2. **Inconsistent Results**:
   - Ensure stable test environment
   - Increase warmup iterations
   - Check for background processes

3. **Memory Leaks**:
   - Review MemoryDiagnoser output
   - Check for proper disposal patterns
   - Validate cache eviction policies

### Performance Debugging

1. **Use profiling tools**:
   - dotMemory for memory analysis
   - PerfView for detailed performance traces
   - Application Insights for production monitoring

2. **Isolate components**:
   - Test individual services in isolation
   - Mock external dependencies
   - Use controlled test data sets

## Best Practices

### Benchmark Design

1. **Realistic scenarios**: Test real-world usage patterns
2. **Proper setup**: Initialize services correctly
3. **Stable environment**: Consistent test conditions
4. **Appropriate scale**: Test with realistic data volumes

### CI/CD Integration

1. **Fail fast**: Set appropriate thresholds for build failures
2. **Trend tracking**: Monitor performance over time
3. **Documentation**: Keep performance budgets updated
4. **Communication**: Alert teams to performance changes

### Baseline Management

1. **Regular updates**: Update baselines after confirmed improvements
2. **Version tracking**: Associate baselines with release versions
3. **Environment consistency**: Ensure CI environment matches expectations
4. **Backup strategy**: Maintain historical baseline data

## Monitoring and Alerting

### GitHub Actions Integration

- **Automated runs**: Daily performance monitoring
- **PR comments**: Performance impact on pull requests
- **Issue creation**: Automatic tracking of performance trends
- **Artifact storage**: Historical data preservation

### Performance Tracking

- **Trend analysis**: Long-term performance evolution
- **Regression alerts**: Immediate notification of issues
- **Baseline evolution**: Controlled performance budget updates
- **Release validation**: Performance gates for deployments

## Future Enhancements

1. **Additional Services**: Expand benchmarks to cover all core services
2. **Load Testing**: Integration with load testing frameworks
3. **Production Monitoring**: Real-time performance tracking
4. **Machine Learning**: Predictive performance analysis
5. **Cross-Platform**: Performance testing on different environments

## Support and Contact

For questions about performance testing:

1. **Documentation**: Check this README and inline code comments
2. **Issues**: Create GitHub issues for bugs or feature requests
3. **Discussions**: Use GitHub Discussions for general questions
4. **Code Review**: Include performance team in relevant PRs

---

*This performance testing infrastructure ensures the Neo Service Layer maintains optimal performance across all releases while providing early detection of performance regressions.*