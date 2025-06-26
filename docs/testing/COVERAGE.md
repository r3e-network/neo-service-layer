# Code Coverage Guidelines

This document outlines the code coverage requirements and guidelines for the Neo Service Layer project.

## Coverage Requirements

### Minimum Thresholds
- **Line Coverage**: 75%
- **Branch Coverage**: 70%
- **Method Coverage**: 75%

These thresholds are enforced by:
- CI/CD pipeline quality gates
- Pre-commit hooks (optional)
- Local development scripts

## Quality Gates

### CI/CD Pipeline
The CI/CD pipeline enforces code coverage requirements through:

1. **Automated Test Execution**: All tests run with coverage collection
2. **Coverage Report Generation**: HTML and XML reports generated for review
3. **Threshold Validation**: Build fails if coverage falls below minimums
4. **PR Comments**: Coverage results automatically posted to pull requests

### Local Development

#### Quick Coverage Check
```bash
# Run tests with coverage (bash)
./scripts/check-coverage.sh

# Run tests with coverage (PowerShell)
./scripts/check-coverage.ps1
```

#### Custom Thresholds
```bash
# Set custom thresholds
./scripts/check-coverage.sh --min-coverage 80 --min-branch-coverage 75

# Don't fail on low coverage (for development)
./scripts/check-coverage.sh --no-fail
```

#### Manual Test Execution
```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" \
  --settings tests/codecoverage.runsettings \
  --results-directory TestResults

# Generate report manually
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html;JsonSummary;Badges"
```

## Coverage Configuration

### Exclusions
The following are excluded from coverage analysis:
- Test projects (`**/tests/**`, `**/*Tests*/**`)
- Generated code (`**/*Designer.cs`, `**/Program.cs`)
- Build artifacts (`**/bin/**`, `**/obj/**`)
- Third-party libraries

### Settings File
Coverage settings are configured in `tests/codecoverage.runsettings`:

```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>opencover,cobertura,json</Format>
          <Exclude>
            [*.Tests]*
            [*TestInfrastructure]*
            [Microsoft.*]*
            [System.*]*
          </Exclude>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## Writing Testable Code

### Best Practices

1. **Dependency Injection**: Use interfaces and DI for better testability
2. **Single Responsibility**: Keep methods focused and small
3. **Avoid Static Dependencies**: Make dependencies explicit and mockable
4. **Error Handling**: Test both success and failure paths

### Example: Testable Service

```csharp
// Good: Testable with mocked dependencies
public class OracleService : IOracleService
{
    private readonly IEnclaveManager _enclaveManager;
    private readonly ILogger<OracleService> _logger;

    public OracleService(
        IEnclaveManager enclaveManager,
        ILogger<OracleService> logger)
    {
        _enclaveManager = enclaveManager;
        _logger = logger;
    }

    public async Task<string> GetDataAsync(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        
        try
        {
            return await _enclaveManager.GetDataAsync(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data from {Source}", source);
            throw;
        }
    }
}

// Corresponding test
[Fact]
public async Task GetDataAsync_ShouldReturnData_WhenSourceIsValid()
{
    // Arrange
    var enclaveManagerMock = new Mock<IEnclaveManager>();
    enclaveManagerMock.Setup(x => x.GetDataAsync("test-source"))
                     .ReturnsAsync("test-data");
    
    var service = new OracleService(
        enclaveManagerMock.Object,
        Mock.Of<ILogger<OracleService>>());

    // Act
    var result = await service.GetDataAsync("test-source");

    // Assert
    Assert.Equal("test-data", result);
}
```

## Coverage Reports

### Local Reports
- **HTML Report**: `TestResults/CoverageReport/index.html`
- **JSON Summary**: `TestResults/CoverageReport/Summary.json`
- **Badges**: `TestResults/CoverageReport/badge_*.svg`

### CI/CD Artifacts
- Coverage reports uploaded as build artifacts
- Pull request comments with coverage diff
- Quality dashboard with historical trends

## Integration with IDEs

### Visual Studio
1. Install "Fine Code Coverage" extension
2. Configure to use `tests/codecoverage.runsettings`
3. View coverage highlights in editor

### VS Code
1. Install "Coverage Gutters" extension
2. Configure to read generated coverage files
3. View coverage indicators in editor

### JetBrains Rider
1. Built-in coverage support
2. Configure coverage runner to use XPlat Code Coverage
3. View coverage results in dedicated tool window

## Troubleshooting

### Common Issues

1. **No coverage data collected**
   - Ensure `coverlet.collector` package is installed in test projects
   - Verify `tests/codecoverage.runsettings` path is correct

2. **Low coverage on interface/abstract classes**
   - These require integration tests, not just unit tests
   - Consider testing concrete implementations

3. **Coverage differs between local and CI**
   - Ensure same .NET version and settings
   - Check for platform-specific code paths

### Getting Help

For coverage-related issues:
1. Check the [GitHub Actions logs](../../actions)
2. Review the coverage report artifacts
3. Compare with previous builds
4. Create an issue with coverage details

## Continuous Improvement

### Monthly Coverage Review
- Review coverage trends and identify areas for improvement
- Update coverage targets if consistently exceeded
- Identify and address coverage debt

### Coverage Debt
Track areas with low coverage:
- Legacy code without tests
- Complex integration scenarios
- Error handling paths

### Team Guidelines
- All new features require tests
- Bug fixes should include regression tests
- Code reviews should verify test coverage
- Pair programming encouraged for complex testing scenarios