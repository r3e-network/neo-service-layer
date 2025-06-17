# Neo Service Layer - Coding Standards

## Overview

This document defines the coding standards, conventions, and best practices for the Neo Service Layer project. All contributors must follow these guidelines to ensure code consistency, maintainability, and professional quality.

## General Principles

### 1. Code Quality
- **Readability**: Code should be self-documenting and easy to understand
- **Maintainability**: Write code that is easy to modify and extend
- **Performance**: Consider performance implications of design decisions
- **Security**: Follow secure coding practices
- **Testability**: Write code that is easy to test

### 2. SOLID Principles
- **Single Responsibility**: Each class should have one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Clients should not depend on interfaces they don't use
- **Dependency Inversion**: Depend on abstractions, not concretions

### 3. DRY (Don't Repeat Yourself)
- Avoid code duplication
- Extract common functionality into reusable components
- Use configuration for environment-specific values

---

## C# Coding Standards

### Naming Conventions

#### Classes and Interfaces
```csharp
// Classes: PascalCase
public class KeyManagementService { }
public class BlockchainClientFactory { }

// Interfaces: PascalCase with 'I' prefix
public interface IKeyManagementService { }
public interface IBlockchainClient { }

// Abstract classes: PascalCase with 'Base' suffix
public abstract class BaseController { }
public abstract class BaseService { }
```

#### Methods and Properties
```csharp
// Methods: PascalCase, descriptive verbs
public async Task<KeyMetadata> GenerateKeyAsync(GenerateKeyRequest request) { }
public bool ValidateSignature(string signature, string data) { }

// Properties: PascalCase, descriptive nouns
public string KeyId { get; set; }
public DateTime CreatedAt { get; set; }
public bool IsValid { get; private set; }
```

#### Fields and Variables
```csharp
// Private fields: camelCase with underscore prefix
private readonly ILogger<KeyManagementService> _logger;
private readonly IConfiguration _configuration;

// Local variables: camelCase
var keyMetadata = new KeyMetadata();
string publicKeyHex = GeneratePublicKey();

// Constants: PascalCase
public const int MaxKeyCount = 10000;
private const string DefaultKeyType = "Secp256k1";
```

#### Parameters and Arguments
```csharp
// Parameters: camelCase, descriptive names
public async Task<bool> SignDataAsync(string keyId, byte[] data, string algorithm)
{
    // Implementation
}

// Avoid abbreviations
public void ProcessTransaction(TransactionRequest request) // Good
public void ProcTx(TxReq req) // Bad
```

### File Organization

#### File Structure
```
src/
├── Api/
│   └── NeoServiceLayer.Api/
│       ├── Controllers/
│       ├── Middleware/
│       ├── Models/
│       └── Program.cs
├── Core/
│   └── NeoServiceLayer.Core/
│       ├── Abstractions/
│       ├── Models/
│       └── Extensions/
└── Services/
    └── NeoServiceLayer.Services.KeyManagement/
        ├── Models/
        ├── Services/
        └── Configuration/
```

#### File Naming
- Use PascalCase for file names
- Match file name with primary class name
- Use descriptive, meaningful names

```
KeyManagementService.cs          // Good
KMS.cs                          // Bad
KeyMgmtSvc.cs                   // Bad
```

### Code Organization

#### Using Statements
```csharp
// System namespaces first
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Third-party namespaces
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

// Project namespaces
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Services.KeyManagement;
```

#### Class Structure
```csharp
public class KeyManagementService : IKeyManagementService
{
    // 1. Constants
    private const int MaxRetryAttempts = 3;
    
    // 2. Private fields
    private readonly ILogger<KeyManagementService> _logger;
    private readonly IConfiguration _configuration;
    
    // 3. Constructor
    public KeyManagementService(
        ILogger<KeyManagementService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    // 4. Public properties
    public bool IsInitialized { get; private set; }
    
    // 5. Public methods
    public async Task<KeyMetadata> GenerateKeyAsync(GenerateKeyRequest request)
    {
        // Implementation
    }
    
    // 6. Private methods
    private bool ValidateRequest(GenerateKeyRequest request)
    {
        // Implementation
    }
}
```

### Documentation

#### XML Documentation
```csharp
/// <summary>
/// Generates a new cryptographic key for the specified blockchain.
/// </summary>
/// <param name="request">The key generation request containing key specifications.</param>
/// <param name="blockchainType">The target blockchain type.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the generated key metadata.</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when key generation fails.</exception>
public async Task<KeyMetadata> GenerateKeyAsync(GenerateKeyRequest request, BlockchainType blockchainType)
{
    // Implementation
}
```

#### Inline Comments
```csharp
public async Task<bool> ValidateSignatureAsync(string signature, string data)
{
    // Validate input parameters
    if (string.IsNullOrEmpty(signature))
        throw new ArgumentException("Signature cannot be null or empty", nameof(signature));
    
    // Parse the signature format
    var signatureBytes = Convert.FromHexString(signature);
    
    // Perform cryptographic validation
    // Note: Using ECDSA with secp256k1 curve for Neo compatibility
    return await _cryptoService.VerifySignatureAsync(signatureBytes, data);
}
```

### Error Handling

#### Exception Handling
```csharp
public async Task<KeyMetadata> GenerateKeyAsync(GenerateKeyRequest request)
{
    try
    {
        // Validate input
        ValidateRequest(request);
        
        // Generate key
        var keyData = await _cryptoProvider.GenerateKeyAsync(request.KeyType);
        
        // Create metadata
        return new KeyMetadata
        {
            KeyId = request.KeyId,
            KeyType = request.KeyType,
            CreatedAt = DateTime.UtcNow
        };
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, "Invalid request parameters for key generation");
        throw;
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Cryptographic operation failed during key generation");
        throw new InvalidOperationException("Key generation failed", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during key generation");
        throw;
    }
}
```

#### Custom Exceptions
```csharp
/// <summary>
/// Exception thrown when a key operation fails.
/// </summary>
public class KeyManagementException : Exception
{
    public string KeyId { get; }
    public KeyOperation Operation { get; }
    
    public KeyManagementException(string keyId, KeyOperation operation, string message) 
        : base(message)
    {
        KeyId = keyId;
        Operation = operation;
    }
    
    public KeyManagementException(string keyId, KeyOperation operation, string message, Exception innerException) 
        : base(message, innerException)
    {
        KeyId = keyId;
        Operation = operation;
    }
}
```

### Async/Await Patterns

#### Proper Async Usage
```csharp
// Good: Async all the way
public async Task<KeyMetadata> GenerateKeyAsync(GenerateKeyRequest request)
{
    var keyData = await _cryptoProvider.GenerateKeyAsync(request.KeyType);
    var metadata = await _repository.SaveKeyAsync(keyData);
    return metadata;
}

// Bad: Blocking async calls
public KeyMetadata GenerateKey(GenerateKeyRequest request)
{
    var keyData = _cryptoProvider.GenerateKeyAsync(request.KeyType).Result; // Don't do this
    return keyData;
}

// Good: ConfigureAwait(false) in libraries
public async Task<bool> ValidateKeyAsync(string keyId)
{
    var key = await _repository.GetKeyAsync(keyId).ConfigureAwait(false);
    return key != null;
}
```

#### Cancellation Tokens
```csharp
public async Task<KeyMetadata> GenerateKeyAsync(
    GenerateKeyRequest request, 
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    var keyData = await _cryptoProvider.GenerateKeyAsync(
        request.KeyType, 
        cancellationToken).ConfigureAwait(false);
    
    return await _repository.SaveKeyAsync(keyData, cancellationToken).ConfigureAwait(false);
}
```

### Dependency Injection

#### Service Registration
```csharp
// Program.cs or Startup.cs
services.AddScoped<IKeyManagementService, KeyManagementService>();
services.AddSingleton<IBlockchainClientFactory, BlockchainClientFactory>();
services.AddTransient<ICryptoProvider, CryptoProvider>();

// Configuration binding
services.Configure<KeyManagementOptions>(configuration.GetSection("KeyManagement"));
```

#### Constructor Injection
```csharp
public class KeyManagementService : IKeyManagementService
{
    private readonly ILogger<KeyManagementService> _logger;
    private readonly IOptions<KeyManagementOptions> _options;
    private readonly ICryptoProvider _cryptoProvider;
    
    public KeyManagementService(
        ILogger<KeyManagementService> logger,
        IOptions<KeyManagementOptions> options,
        ICryptoProvider cryptoProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
    }
}
```

---

## API Design Standards

### Controller Design

#### Base Controller
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;
    
    protected BaseApiController(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Operation completed successfully")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
    
    protected ActionResult<ApiResponse<object>> Error(string message, string errorCode = null)
    {
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Error = new ApiError { Code = errorCode },
            Timestamp = DateTime.UtcNow
        });
    }
}
```

#### Endpoint Design
```csharp
[HttpPost("keys/{blockchainType}")]
[ProducesResponseType(typeof(ApiResponse<KeyMetadata>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public async Task<ActionResult<ApiResponse<KeyMetadata>>> GenerateKey(
    [FromRoute] string blockchainType,
    [FromBody] GenerateKeyRequest request,
    CancellationToken cancellationToken = default)
{
    // Validate blockchain type
    if (!Enum.TryParse<BlockchainType>(blockchainType, true, out var blockchain))
    {
        return Error($"Invalid blockchain type: {blockchainType}", "INVALID_BLOCKCHAIN_TYPE");
    }
    
    // Validate request
    if (!ModelState.IsValid)
    {
        return Error("Invalid request parameters", "INVALID_REQUEST");
    }
    
    try
    {
        var result = await _keyManagementService.GenerateKeyAsync(request, blockchain, cancellationToken);
        return Success(result, "Key generated successfully");
    }
    catch (ArgumentException ex)
    {
        return Error(ex.Message, "INVALID_ARGUMENT");
    }
    catch (KeyManagementException ex)
    {
        return Error(ex.Message, "KEY_GENERATION_FAILED");
    }
}
```

### Model Design

#### Request Models
```csharp
/// <summary>
/// Request model for generating a new cryptographic key.
/// </summary>
public class GenerateKeyRequest
{
    /// <summary>
    /// Unique identifier for the key.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string KeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of cryptographic key to generate.
    /// </summary>
    [Required]
    [EnumDataType(typeof(KeyType))]
    public string KeyType { get; set; } = string.Empty;
    
    /// <summary>
    /// Intended usage for the key.
    /// </summary>
    [Required]
    public string KeyUsage { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the key can be exported.
    /// </summary>
    public bool Exportable { get; set; } = false;
    
    /// <summary>
    /// Optional description for the key.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}
```

#### Response Models
```csharp
/// <summary>
/// Standard API response wrapper.
/// </summary>
/// <typeparam name="T">Type of the response data.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Human-readable message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The response data.
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// Error information if the operation failed.
    /// </summary>
    public ApiError? Error { get; set; }
    
    /// <summary>
    /// Timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Unique identifier for the request.
    /// </summary>
    public string? RequestId { get; set; }
}
```

---

## Testing Standards

### Unit Test Structure

#### Test Class Organization
```csharp
public class KeyManagementServiceTests
{
    private readonly Mock<ILogger<KeyManagementService>> _loggerMock;
    private readonly Mock<ICryptoProvider> _cryptoProviderMock;
    private readonly Mock<IKeyRepository> _repositoryMock;
    private readonly KeyManagementService _service;
    
    public KeyManagementServiceTests()
    {
        _loggerMock = new Mock<ILogger<KeyManagementService>>();
        _cryptoProviderMock = new Mock<ICryptoProvider>();
        _repositoryMock = new Mock<IKeyRepository>();
        
        _service = new KeyManagementService(
            _loggerMock.Object,
            _cryptoProviderMock.Object,
            _repositoryMock.Object);
    }
    
    [Fact]
    public async Task GenerateKeyAsync_WithValidRequest_ShouldReturnKeyMetadata()
    {
        // Arrange
        var request = new GenerateKeyRequest
        {
            KeyId = "test-key",
            KeyType = "Secp256k1",
            KeyUsage = "Sign,Verify"
        };
        
        var expectedKeyData = new KeyData { /* ... */ };
        _cryptoProviderMock
            .Setup(x => x.GenerateKeyAsync(request.KeyType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedKeyData);
        
        // Act
        var result = await _service.GenerateKeyAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.KeyId, result.KeyId);
        Assert.Equal(request.KeyType, result.KeyType);
        
        _cryptoProviderMock.Verify(
            x => x.GenerateKeyAsync(request.KeyType, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
    
    [Fact]
    public async Task GenerateKeyAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GenerateKeyAsync(null));
    }
}
```

#### Test Naming Convention
```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public async Task GenerateKeyAsync_WithValidRequest_ShouldReturnKeyMetadata() { }

[Fact]
public async Task GenerateKeyAsync_WithInvalidKeyType_ShouldThrowArgumentException() { }

[Fact]
public async Task GenerateKeyAsync_WhenCryptoProviderFails_ShouldThrowKeyManagementException() { }
```

### Integration Test Structure

#### Test Base Class
```csharp
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    
    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
    
    protected async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await Client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
    
    protected async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await Client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, JsonOptions);
    }
}
```

---

## Configuration Standards

### Configuration Structure
```csharp
public class KeyManagementOptions
{
    public const string SectionName = "KeyManagement";
    
    /// <summary>
    /// Maximum number of keys that can be stored.
    /// </summary>
    [Range(1, 100000)]
    public int MaxKeyCount { get; set; } = 10000;
    
    /// <summary>
    /// Supported key types for generation.
    /// </summary>
    public List<string> SupportedKeyTypes { get; set; } = new()
    {
        "Secp256k1",
        "Ed25519",
        "RSA2048"
    };
    
    /// <summary>
    /// Default encryption algorithm for key storage.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
    
    /// <summary>
    /// Whether to enable hardware security module integration.
    /// </summary>
    public bool EnableHardwareSecurityModule { get; set; } = false;
}
```

### Configuration Validation
```csharp
public class KeyManagementOptionsValidator : IValidateOptions<KeyManagementOptions>
{
    public ValidateOptionsResult Validate(string name, KeyManagementOptions options)
    {
        var failures = new List<string>();
        
        if (options.MaxKeyCount <= 0)
        {
            failures.Add("MaxKeyCount must be greater than 0");
        }
        
        if (!options.SupportedKeyTypes.Any())
        {
            failures.Add("At least one supported key type must be specified");
        }
        
        if (string.IsNullOrEmpty(options.EncryptionAlgorithm))
        {
            failures.Add("EncryptionAlgorithm cannot be null or empty");
        }
        
        return failures.Any() 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
```

---

## Security Standards

### Input Validation
```csharp
public class GenerateKeyRequestValidator : AbstractValidator<GenerateKeyRequest>
{
    public GenerateKeyRequestValidator()
    {
        RuleFor(x => x.KeyId)
            .NotEmpty()
            .Length(1, 100)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("KeyId must contain only alphanumeric characters, hyphens, and underscores");
        
        RuleFor(x => x.KeyType)
            .NotEmpty()
            .Must(BeValidKeyType)
            .WithMessage("Invalid key type specified");
        
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
    
    private bool BeValidKeyType(string keyType)
    {
        var validTypes = new[] { "Secp256k1", "Ed25519", "RSA2048", "RSA4096" };
        return validTypes.Contains(keyType);
    }
}
```

### Secure Logging
```csharp
public class KeyManagementService
{
    public async Task<KeyMetadata> GenerateKeyAsync(GenerateKeyRequest request)
    {
        // Log request without sensitive data
        _logger.LogInformation(
            "Generating key {KeyId} of type {KeyType}", 
            request.KeyId, 
            request.KeyType);
        
        try
        {
            var result = await GenerateKeyInternal(request);
            
            // Log success without sensitive data
            _logger.LogInformation(
                "Successfully generated key {KeyId}", 
                request.KeyId);
            
            return result;
        }
        catch (Exception ex)
        {
            // Log error without exposing sensitive information
            _logger.LogError(ex, 
                "Failed to generate key {KeyId}", 
                request.KeyId);
            throw;
        }
    }
}
```

---

## Performance Standards

### Caching Patterns
```csharp
public class KeyManagementService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
    
    public async Task<KeyMetadata> GetKeyAsync(string keyId)
    {
        var cacheKey = $"key:{keyId}";
        
        if (_cache.TryGetValue(cacheKey, out KeyMetadata cachedKey))
        {
            return cachedKey;
        }
        
        var key = await _repository.GetKeyAsync(keyId);
        if (key != null)
        {
            _cache.Set(cacheKey, key, _cacheExpiration);
        }
        
        return key;
    }
}
```

### Resource Management
```csharp
public class CryptoProvider : ICryptoProvider, IDisposable
{
    private readonly ECDsa _ecdsa;
    private bool _disposed = false;
    
    public CryptoProvider()
    {
        _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }
    
    public async Task<byte[]> SignAsync(byte[] data)
    {
        ThrowIfDisposed();
        return await Task.Run(() => _ecdsa.SignData(data, HashAlgorithmName.SHA256));
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _ecdsa?.Dispose();
            _disposed = true;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CryptoProvider));
        }
    }
}
```

---

## Code Review Checklist

### Before Submitting
- [ ] Code follows naming conventions
- [ ] All public APIs have XML documentation
- [ ] Unit tests cover new functionality
- [ ] Integration tests pass
- [ ] No hardcoded values or secrets
- [ ] Error handling is comprehensive
- [ ] Logging is appropriate and secure
- [ ] Performance considerations addressed
- [ ] Security best practices followed

### Review Criteria
- [ ] Code is readable and maintainable
- [ ] Design patterns are appropriate
- [ ] Dependencies are properly injected
- [ ] Async/await patterns are correct
- [ ] Exception handling is proper
- [ ] Resource disposal is handled
- [ ] Configuration is externalized
- [ ] Tests are comprehensive and meaningful

---

## Tools and Automation

### Code Analysis
- **StyleCop**: Enforce coding style rules
- **SonarQube**: Code quality and security analysis
- **Roslyn Analyzers**: Static code analysis
- **EditorConfig**: Consistent formatting across editors

### Formatting
```xml
<!-- .editorconfig -->
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# formatting rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

### Pre-commit Hooks
```bash
#!/bin/sh
# Pre-commit hook to run code formatting and analysis

# Format code
dotnet format --verify-no-changes

# Run static analysis
dotnet build --configuration Release --verbosity quiet

# Run unit tests
dotnet test --configuration Release --logger "console;verbosity=minimal"
```

---

This coding standards document ensures consistency, maintainability, and professional quality across the entire Neo Service Layer codebase. All team members should familiarize themselves with these standards and apply them consistently in their development work. 