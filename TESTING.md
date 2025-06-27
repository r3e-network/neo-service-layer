# Neo Service Layer - Testing Guide

This document describes how to run tests for the Neo Service Layer project.

## Quick Start

### Quick Test Commands (From Root Directory)
```bash
# Run unit tests only (recommended for CI/CD)
./scripts/testing/run-unit-tests.sh

# Run all tests including performance tests
./scripts/testing/run-all-tests.sh

# Windows - PowerShell
./scripts/testing/run-all-tests.ps1 -Coverage
```

### Direct Script Access
```bash
# Linux/macOS - Unit tests only
./scripts/testing/run-unit-tests.sh

# Linux/macOS - All tests
./scripts/testing/run-all-tests.sh

# Windows - PowerShell
./scripts/testing/run-all-tests.ps1 -Coverage
```

## Test Scripts

### 1. `run-unit-tests.sh` (Recommended)
- **Purpose**: Runs all unit tests excluding performance tests
- **Best for**: CI/CD pipelines, regular development testing
- **Excludes**: Performance stress tests that may fail in resource-constrained environments
- **Coverage**: Includes code coverage collection and reporting

```bash
./scripts/testing/run-unit-tests.sh [Configuration] [Verbosity]

# Examples:
./scripts/testing/run-unit-tests.sh Release minimal    # Release build, minimal output
./scripts/testing/run-unit-tests.sh Debug normal       # Debug build, normal output
```

### 2. `run-all-tests.sh` (Complete Suite)
- **Purpose**: Runs all tests including performance tests
- **Best for**: Local development, comprehensive testing
- **Includes**: All unit tests + performance/stress tests
- **Note**: Performance tests may fail in resource-constrained environments

```bash
./scripts/testing/run-all-tests.sh [Configuration] [Verbosity]

# Examples:
./scripts/testing/run-all-tests.sh Release minimal     # All tests, minimal output
./scripts/testing/run-all-tests.sh Debug detailed      # All tests, detailed output
```

### 3. `run-all-tests.ps1` (PowerShell/Windows)
- **Purpose**: Cross-platform PowerShell version
- **Features**: Same functionality as bash scripts but for Windows/PowerShell

```powershell
./scripts/testing/run-all-tests.ps1 -Configuration Release -Verbosity normal -Coverage
./scripts/testing/run-all-tests.ps1 -Help  # Show help
```

## Test Results

### Output Locations
- **Test Results**: `./TestResults/` - Contains TRX files and raw test output
- **Coverage Report**: `./CoverageReport/index.html` - HTML coverage report
- **Coverage Data**: `./TestResults/**/coverage.cobertura.xml` - Raw coverage data

### Coverage Reports
The scripts automatically generate:
- **HTML Report**: Interactive coverage report (`./CoverageReport/index.html`)
- **Badges**: Coverage badges for documentation
- **Summary**: Text summary of coverage statistics
- **GitHub Markdown**: Summary for GitHub PRs/issues

## GitHub Actions Workflow

The project includes a comprehensive CI/CD pipeline (`.github/workflows/ci.yml`) that:

1. **Builds** the solution in Release configuration
2. **Runs unit tests** (excludes performance tests for reliability)
3. **Collects coverage** data using Coverlet
4. **Generates reports** using ReportGenerator
5. **Uploads artifacts** for test results and coverage
6. **Comments on PRs** with coverage summaries
7. **Runs security scans** for vulnerable packages
8. **Tests Docker builds** on main branch

### Workflow Triggers
- **Push** to `main` or `develop` branches
- **Pull requests** to `main` or `develop` branches
- **Manual dispatch** via GitHub UI

## Test Project Structure

The test suite includes:

### Unit Tests (28 projects)
- **Core Tests**: `NeoServiceLayer.Core.Tests`, `NeoServiceLayer.Shared.Tests`
- **Service Tests**: All service modules (Oracle, KeyManagement, Storage, etc.)
- **AI Tests**: `NeoServiceLayer.AI.PatternRecognition.Tests`, `NeoServiceLayer.AI.Prediction.Tests`
- **Blockchain Tests**: `NeoServiceLayer.Neo.N3.Tests`, `NeoServiceLayer.Neo.X.Tests`
- **Infrastructure Tests**: TEE, API, Integration tests

### Performance Tests (Excluded from CI)
- **Load Testing**: Using NBomber framework
- **Stress Testing**: Memory pressure, resource constraints
- **Note**: These tests are resource-intensive and may fail in CI environments

## Quality Gates

The test scripts enforce these quality gates:

1. ✅ **All unit tests must pass**
2. ✅ **Coverage data must be collected**
3. ✅ **All test projects must be executable**
4. ✅ **Build must succeed**

## Coverage Targets

| Component | Target Coverage |
|-----------|----------------|
| Core Services | 90% |
| Shared Utilities | 95% |
| AI Services | 85% |
| Advanced Services | 80% |
| Blockchain Integration | 85% |

## Troubleshooting

### Common Issues

1. **Permission Denied on TestResults**
   ```bash
   sudo rm -rf ./TestResults ./CoverageReport
   ```

2. **Missing ReportGenerator**
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   ```

3. **Performance Tests Failing**
   - Use `./scripts/testing/run-unit-tests.sh` instead of `./scripts/testing/run-all-tests.sh`
   - Performance tests are excluded from CI for reliability

4. **Coverage Collection Issues**
   - Ensure `config/coverlet.runsettings` exists
   - Check that test projects reference Coverlet packages

### Manual Test Commands

```bash
# Build only
dotnet build NeoServiceLayer.sln --configuration Release

# Run specific test project
dotnet test tests/Core/NeoServiceLayer.Core.Tests --configuration Release

# Run tests with filter
dotnet test NeoServiceLayer.sln --filter "FullyQualifiedName!~Performance"

# Generate coverage report manually
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:"Html"
```

## Integration with IDEs

### Visual Studio / VS Code
- Test Explorer automatically discovers all test projects
- Run individual tests or test classes
- Debug tests with breakpoints
- View coverage in IDE (with appropriate extensions)

### JetBrains Rider
- Built-in test runner and coverage analysis
- Integrated coverage visualization
- Performance profiling for tests

## Best Practices

1. **Use unit test script for CI/CD** - More reliable than full test suite
2. **Run full test suite locally** - Before major commits or releases
3. **Monitor coverage trends** - Aim to maintain or improve coverage
4. **Fix failing tests immediately** - Don't let technical debt accumulate
5. **Review performance test results** - On dedicated test environments

## Contributing

When adding new tests:
1. Follow existing test project naming conventions
2. Add appropriate test categories/filters
3. Ensure tests are deterministic and fast
4. Update this documentation if adding new test types