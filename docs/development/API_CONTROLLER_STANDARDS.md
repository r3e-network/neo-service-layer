# API Controller Standards

This document defines the coding standards and patterns for API controllers in the Neo Service Layer project.

## Controller Structure

All API controllers must follow this standard structure:

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for [feature] operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Tags("FeatureName")]
public class FeatureController : BaseApiController
{
    private readonly IFeatureService _featureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureController"/> class.
    /// </summary>
    public FeatureController(IFeatureService featureService, ILogger<FeatureController> logger)
        : base(logger)
    {
        _featureService = featureService ?? throw new ArgumentNullException(nameof(featureService));
    }
}
```

## Key Standards

### 1. Class Attributes

- **Always include** these attributes in order:
  - `[ApiController]` - Enables API-specific behaviors
  - `[ApiVersion("1.0")]` - Specifies API version
  - `[Route("api/v{version:apiVersion}/[controller]")]` - Standard route pattern
  - `[Authorize]` - Class-level authorization (always required)
  - `[Tags("FeatureName")]` - Groups endpoints in Swagger

### 2. Route Patterns

- Use `[controller]` placeholder in route attribute unless you need hyphens
- For hyphenated routes: `[Route("api/v{version:apiVersion}/cross-chain")]`
- Method routes should follow RESTful conventions:
  - `GET /` - List resources
  - `GET /{id}` - Get single resource
  - `POST /` - Create resource
  - `PUT /{id}` - Update resource
  - `DELETE /{id}` - Delete resource

### 3. Authorization

- Always include class-level `[Authorize]` attribute
- Add role-specific authorization at method level when needed:
  ```csharp
  [Authorize(Roles = "Admin,SpecificRole")]
  ```

### 4. Blockchain Type Parameter

Standardize on query parameter with default value:

```csharp
[FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3
```

### 5. Method Structure

```csharp
/// <summary>
/// [Action description].
/// </summary>
/// <response code="200">Success response description.</response>
/// <response code="400">Bad request description.</response>
[HttpPost]
[Authorize(Roles = "Admin,SpecificRole")]
[ProducesResponseType(typeof(ApiResponse<ResultType>), 200)]
[ProducesResponseType(typeof(ApiResponse<object>), 400)]
[ProducesResponseType(typeof(ApiResponse<object>), 401)]
public async Task<IActionResult> CreateResource(
    [FromBody] CreateRequest request,
    [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
{
    try
    {
        Logger.LogInformation("Creating resource for user {UserId}", GetCurrentUserId());
        
        var result = await _featureService.CreateAsync(request, blockchainType);
        return Ok(CreateResponse(result, "Resource created successfully"));
    }
    catch (Exception ex)
    {
        return HandleException(ex, "creating resource");
    }
}
```

### 6. Error Handling

- Use the base `HandleException` method for standard error handling
- Only add custom error handling for specific scenarios (e.g., hardware failures)
- Always include meaningful context in error messages

### 7. Response Types

- Use `ApiResponse<T>` for all responses
- Use `PaginatedResponse<T>` for paginated data
- Always include `ProducesResponseType` attributes for:
  - Success response (200/201/204)
  - Bad request (400)
  - Unauthorized (401)
  - Not found (404) - when applicable
  - Service unavailable (503) - when applicable

### 8. Logging

- Log at the start of each operation with relevant context
- Include user ID when available: `GetCurrentUserId()`
- Use structured logging with named parameters

### 9. Dependency Injection

- Use constructor injection
- Always null-check dependencies with null-coalescing throw:
  ```csharp
  _service = service ?? throw new ArgumentNullException(nameof(service));
  ```

### 10. XML Documentation

- Include `<summary>` for all public members
- Add `<response>` tags for different HTTP status codes
- Document parameters with `<param>` tags when not obvious

## Examples

### Standard CRUD Controller

See `BackupController.cs` for a complete example following these standards.

### Special Cases

1. **File Upload**: Use `IFormFile` with appropriate size limits
2. **Streaming**: Return `FileStreamResult` for large files
3. **Long-Running Operations**: Consider returning 202 Accepted with status endpoint

## Migration Checklist

When updating existing controllers:

- [ ] Add `[ApiController]` attribute if missing
- [ ] Add class-level `[Authorize]` attribute
- [ ] Standardize blockchain parameter to query parameter
- [ ] Update null-checking in constructor
- [ ] Ensure consistent error handling
- [ ] Add missing XML documentation
- [ ] Verify route patterns follow standards
- [ ] Check response type attributes

## Testing

All controllers should have corresponding integration tests that verify:
- Authorization requirements
- Input validation
- Error handling
- Response formats
- Blockchain type handling