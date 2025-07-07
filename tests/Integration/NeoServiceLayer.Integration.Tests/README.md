# Integration Tests

This directory contains integration tests for the Neo Service Layer API.

## Test Structure

- **Controllers/** - Integration tests for API controllers
  - `VotingControllerTests.cs` - Tests for voting endpoints
  - `AbstractAccountControllerTests.cs` - Tests for abstract account endpoints
  - Additional controller tests to be added...

- **Helpers/** - Test utilities and base classes
  - `IntegrationTestBase.cs` - Base class with JWT token generation and common utilities

- **HealthCheckTests.cs** - Tests for health check endpoints

## Running Tests

### Run all integration tests
```bash
dotnet test tests/Integration/NeoServiceLayer.Integration.Tests
```

### Run specific test category
```bash
dotnet test --filter Category=Controllers
dotnet test --filter Category=HealthChecks
```

### Run with detailed output
```bash
dotnet test -v normal
```

## Test Configuration

The integration tests use `WebApplicationFactory<Program>` to create an in-memory test server. This allows testing the full request/response pipeline without external dependencies.

### Environment Variables for Tests

Create a `test.env` file or set these environment variables:

```bash
# Required for tests
JWT_SECRET_KEY=TestSecretKeyThatIsAtLeast32CharactersLong!
SGX_MODE=SW
IAS_API_KEY=test-api-key

# Optional test configuration
ASPNETCORE_ENVIRONMENT=Test
```

## Authentication in Tests

Tests use the `IntegrationTestBase` class to generate JWT tokens:

```csharp
// Generate token with specific role
var token = GenerateJwtToken("user-id", "Admin");
SetAuthorizationHeader(token);

// Or use helper method
SetAuthorizationHeader("user-id", "User");
```

## Adding New Tests

1. Create a new test class inheriting from `IntegrationTestBase`
2. Use proper naming: `[ControllerName]ControllerTests.cs`
3. Test key scenarios:
   - Authentication/Authorization
   - Valid requests
   - Invalid input validation
   - Error handling

Example test structure:

```csharp
public class NewControllerTests : IntegrationTestBase
{
    public NewControllerTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task Endpoint_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/v1/endpoint");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_WithValidAuth_ReturnsSuccess()
    {
        SetAuthorizationHeader("user", "User");
        var response = await Client.GetAsync("/api/v1/endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```

## Best Practices

1. **Test Isolation**: Each test should be independent
2. **Clear Naming**: Test names should describe what they test
3. **Arrange-Act-Assert**: Follow AAA pattern
4. **Use Theory**: For testing multiple scenarios with different data
5. **Mock External Dependencies**: Override services in `ConfigureServices`

## Continuous Integration

These tests run automatically in the CI/CD pipeline. Ensure all tests pass before merging pull requests.

## Known Issues

- Some tests may fail if required services aren't properly mocked
- Health check tests may timeout if dependencies are slow to initialize
- JWT token validation requires proper secret key configuration

## TODO

- [ ] Add tests for all controllers
- [ ] Add performance tests
- [ ] Add stress tests for rate limiting
- [ ] Add tests for error scenarios
- [ ] Add tests for pagination
- [ ] Add tests for file upload/download endpoints