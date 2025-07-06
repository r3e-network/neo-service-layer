# API Consistency Review Report

## Overview
This report identifies consistency issues across all API controllers and endpoints in the Neo Service Layer project.

## Key Findings

### 1. API Naming Conventions and Route Patterns

#### Inconsistent Route Patterns
- **Api project controllers**: Use versioned routes `[Route("api/v{version:apiVersion}/[controller]")]` or specific names like `[Route("api/v{version:apiVersion}/notifications")]`
- **Web project controllers**: Mix of patterns:
  - Some use `[Route("api/[controller]")]` without versioning
  - BaseApiController uses `[Route("api/v{version:apiVersion}/[controller]")]`
- **API folder controllers**: Use `[Route("api/[controller]")]` without versioning

#### Examples of Inconsistency:
```csharp
// Api project (GOOD - versioned)
[Route("api/v{version:apiVersion}/notifications")]

// Web project (INCONSISTENT - no version)
[Route("api/[controller]")]

// API folder (INCONSISTENT - no version)
[Route("api/[controller]")]
```

#### Blockchain Type in Routes
- Some controllers include blockchain type in route: `/api/v1/oracle/datasources/{blockchainType}`
- Others use query parameters: `?blockchain=neo-n3`
- No consistent pattern across services

### 2. Response Format Consistency

#### Different Response Wrapper Classes
- **Api and Web projects**: Use `ApiResponse<T>` and `PaginatedResponse<T>` from BaseApiController
- **API folder (SocialRecoveryController)**: Uses custom response models like `GuardianEnrollmentResponse`, `ErrorResponse`

#### Inconsistent Error Response Formats
```csharp
// Api/Web projects (using BaseApiController)
return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));

// API folder
return StatusCode(500, new ErrorResponse { Error = "Failed to enroll guardian", Details = ex.Message });
```

### 3. Authorization and Authentication Patterns

#### Inconsistent Role Definitions
- **Api project**: Uses `[Authorize(Roles = "Admin,ServiceUser")]`
- **Web project**: Uses `[Authorize(Roles = "Admin,KeyManager,KeyUser")]`
- **API folder**: Uses plain `[Authorize]` without roles

#### Missing Consistent Authorization Patterns
- Some endpoints have `[AllowAnonymous]` for health/stats
- Others require authentication even for status checks
- No consistent pattern for which operations require which roles

### 4. Error Handling Consistency

#### Different Exception Handling Patterns
- **Api/Web projects**: Use `HandleException` method from BaseApiController
- **API folder**: Direct try-catch with custom error responses
- Different status codes for similar errors across controllers

#### Example Inconsistency:
```csharp
// Api project (using HandleException)
catch (Exception ex)
{
    return HandleException(ex, "GetData");
}

// API folder (direct handling)
catch (Exception ex)
{
    _logger.LogError(ex, "Error enrolling guardian");
    return StatusCode(500, new ErrorResponse { Error = "Failed to enroll guardian", Details = ex.Message });
}
```

### 5. HTTP Status Code Usage

#### Inconsistent Status Codes
- Not Found responses vary between controllers
- Some return 501 (Not Implemented), others return custom messages
- Success responses sometimes use `Ok()` directly, sometimes use `CreateResponse()`

### 6. API Versioning Consistency

#### Missing Version Configuration
- Api project has `[ApiVersion("1.0")]` attributes
- Web and API folder controllers lack versioning attributes
- No consistent versioning strategy across the solution

### 7. Missing Endpoints or Controllers

#### Incomplete Implementations
- VotingController in Web project has all methods commented out
- Several controllers return 501 (Not Implemented) for certain operations
- No consistent pattern for handling unimplemented features

#### Missing Standard Endpoints
- Not all controllers implement health/status/metrics endpoints
- Some controllers missing standard CRUD operations
- Inconsistent pagination support

### 8. API Documentation and Tags

#### Inconsistent Tag Usage
- **Api project**: Uses `[Tags("Service Name")]` attribute on controllers
- **Web project**: Uses `[Tags("Service Name")]` on some controllers
- **API folder**: No tags used
- Tags help organize endpoints in Swagger/OpenAPI documentation

#### Missing Documentation
- Not all controllers have XML documentation comments
- Some endpoints lack proper response type attributes
- Inconsistent use of `[ProducesResponseType]` attributes

### 9. Parameter Validation

#### Inconsistent Validation Approaches
- Some controllers use data annotations (`[Required]`, `[StringLength]`, `[Range]`)
- Others rely on manual validation in the action methods
- ModelState validation is used inconsistently

#### Examples:
```csharp
// API folder (using data annotations)
[Required]
[StringLength(100, MinimumLength = 1)]
public string KeyId { get; set; }

// Some controllers (manual validation)
if (skip < 0 || take <= 0 || take > 100)
{
    return BadRequest(CreateErrorResponse("Invalid pagination parameters"));
}
```

## Recommendations

### 1. Standardize Route Patterns
- All controllers should use versioned routes: `[Route("api/v{version:apiVersion}/[controller]")]`
- Standardize blockchain type handling (prefer route parameter over query string)
- Example: `/api/v1/service/{blockchainType}/operation`

### 2. Unify Response Models
- Move all controllers to use `ApiResponse<T>` from a shared location
- Remove custom response models in favor of standardized ones
- Ensure all controllers inherit from BaseApiController

### 3. Implement Consistent Authorization
- Define standard roles: `Admin`, `ServiceUser`, `ReadOnly`
- Document which operations require which roles
- Apply consistent authorization attributes across all controllers

### 4. Standardize Error Handling
- All controllers should use `HandleException` method
- Define standard error response format
- Map exceptions to appropriate HTTP status codes consistently

### 5. Apply Consistent Versioning
- Add `[ApiVersion("1.0")]` to all controllers
- Configure API versioning in all projects
- Consider header-based versioning as alternative

### 6. Complete Missing Implementations
- Either implement missing endpoints or remove them
- Use feature flags for endpoints under development
- Document API completeness in each controller

### 7. Add Standard Endpoints to All Controllers
- Every service controller should have:
  - `GET /health` - Health check (anonymous)
  - `GET /status` - Detailed status (authenticated)
  - `GET /metrics` - Performance metrics (authenticated)

### 8. Standardize API Documentation
- Add `[Tags("Service Name")]` to all controllers
- Ensure all endpoints have XML documentation comments
- Add `[ProducesResponseType]` attributes for all possible responses
- Include example requests/responses in documentation

### 9. Implement Consistent Validation
- Use data annotations for request model validation
- Create custom validation attributes for common patterns
- Always check ModelState.IsValid before processing
- Return consistent validation error responses

### 10. Create Shared API Standards Document
- Document naming conventions
- Define standard HTTP status code usage
- Provide controller template for new services
- Include validation patterns and examples

## Priority Actions

1. **High Priority**: Fix route versioning inconsistency
2. **High Priority**: Standardize error handling and response formats
3. **Medium Priority**: Implement consistent authorization patterns
4. **Medium Priority**: Complete missing endpoint implementations
5. **Low Priority**: Add standard health/status/metrics endpoints

## Controller-Specific Issues

### Controllers Needing Major Updates:
1. **SocialRecoveryController** (API folder)
   - Missing API versioning
   - Custom error response format
   - No inheritance from BaseApiController
   - Missing [Tags] attribute

2. **VotingController** (Web project)
   - All methods commented out
   - Missing API versioning in route
   - No proper implementation

3. **VotingController** (API folder)
   - Duplicate of Web controller
   - Also missing implementation
   - No versioning

### Controllers with Good Patterns (Use as Examples):
1. **OracleController** (Api project)
   - Proper versioning
   - Consistent error handling
   - Good documentation
   - Proper use of BaseApiController

2. **KeyManagementController** (both projects)
   - Good parameter validation
   - Consistent response formats
   - Proper authorization

## Implementation Plan

1. Create a shared API standards library with:
   - BaseApiController (already exists, needs to be shared)
   - Standard response models
   - Common exception handling
   - Authorization policies

2. Update all controllers to:
   - Inherit from shared BaseApiController
   - Use consistent route patterns
   - Apply standard authorization
   - Implement standard endpoints

3. Add integration tests to verify:
   - Route consistency
   - Response format consistency
   - Error handling consistency
   - Authorization consistency

4. Consolidate duplicate controllers:
   - Merge VotingController implementations
   - Move SocialRecoveryController to main Api project
   - Remove redundant controller files