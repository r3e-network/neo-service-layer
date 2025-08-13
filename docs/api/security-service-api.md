# Security Service API Documentation

## Overview

The SecurityService provides comprehensive security functionality including input validation, encryption, authentication, and threat detection. This service addresses critical security vulnerabilities identified in the system review.

**Version**: 2.0.0  
**Namespace**: `NeoServiceLayer.Infrastructure.Security`  
**Interface**: `ISecurityService`

## Core Features

- **Input Validation**: SQL injection, XSS, and code injection detection
- **Encryption**: AES-256-GCM authenticated encryption
- **Password Security**: PBKDF2 hashing with 100,000 iterations
- **Rate Limiting**: Sliding window rate limiting
- **Token Generation**: Cryptographically secure token generation
- **Security Metrics**: Comprehensive security monitoring

## API Reference

### Input Validation

#### ValidateInputAsync

Validates input against multiple security threats.

```csharp
Task<SecurityValidationResult> ValidateInputAsync(
    string input, 
    SecurityValidationOptions options)
```

**Parameters**:
- `input` (string): The input to validate
- `options` (SecurityValidationOptions): Validation configuration

**Returns**: `SecurityValidationResult` containing validation results

**Example**:
```csharp
var options = new SecurityValidationOptions
{
    CheckSqlInjection = true,
    CheckXss = true,
    CheckCodeInjection = true,
    MaxInputSize = 1024
};

var result = await securityService.ValidateInputAsync(userInput, options);

if (result.HasSecurityThreats)
{
    // Handle threats detected in result.ThreatTypes
    Console.WriteLine($"Threats detected: {string.Join(", ", result.ThreatTypes)}");
}
```

**Threat Detection**:
- **SQL Injection**: Detects union, boolean, time-based, and error-based attacks
- **XSS**: Identifies script tags, event handlers, and JavaScript protocols  
- **Code Injection**: Catches reflection, file system, and process execution attempts

### Encryption

#### EncryptDataAsync

Encrypts data using AES-256-GCM authenticated encryption.

```csharp
Task<EncryptionResult> EncryptDataAsync(byte[] data)
```

**Parameters**:
- `data` (byte[]): Data to encrypt

**Returns**: `EncryptionResult` with encrypted data, key, and nonce

**Example**:
```csharp
var plaintext = Encoding.UTF8.GetBytes("Sensitive data");
var result = await securityService.EncryptDataAsync(plaintext);

if (result.Success)
{
    // Store result.EncryptedData, result.Key, result.Nonce securely
    Console.WriteLine($"Encrypted {plaintext.Length} bytes to {result.EncryptedData.Length} bytes");
}
```

#### DecryptDataAsync

Decrypts AES-256-GCM encrypted data.

```csharp
Task<DecryptionResult> DecryptDataAsync(
    byte[] encryptedData, 
    byte[] key, 
    byte[] nonce)
```

**Parameters**:
- `encryptedData` (byte[]): Encrypted data
- `key` (byte[]): 256-bit encryption key
- `nonce` (byte[]): 96-bit nonce/IV

**Returns**: `DecryptionResult` with decrypted data

**Example**:
```csharp
var result = await securityService.DecryptDataAsync(
    encryptedData, key, nonce);

if (result.Success)
{
    var plaintext = Encoding.UTF8.GetString(result.DecryptedData);
    Console.WriteLine($"Decrypted: {plaintext}");
}
```

### Password Security

#### HashPasswordAsync

Creates secure password hash using PBKDF2.

```csharp
Task<PasswordHashResult> HashPasswordAsync(string password)
```

**Parameters**:
- `password` (string): Password to hash

**Returns**: `PasswordHashResult` with hash, salt, and algorithm details

**Example**:
```csharp
var result = await securityService.HashPasswordAsync(password);

if (result.Success)
{
    // Store result.Hash and result.Salt in database
    Console.WriteLine($"Hash algorithm: {result.Algorithm}");
    Console.WriteLine($"Iterations: {result.Iterations}");
}
```

#### VerifyPasswordAsync

Verifies password against stored hash.

```csharp
Task<PasswordVerificationResult> VerifyPasswordAsync(
    string password, 
    string hash, 
    string salt)
```

**Parameters**:
- `password` (string): Password to verify
- `hash` (string): Stored password hash
- `salt` (string): Password salt

**Returns**: `PasswordVerificationResult` indicating if password is valid

**Example**:
```csharp
var result = await securityService.VerifyPasswordAsync(
    password, storedHash, storedSalt);

if (result.Success && result.IsValid)
{
    // Password is correct
    Console.WriteLine("Authentication successful");
}
```

### Rate Limiting

#### CheckRateLimitAsync

Enforces rate limiting using sliding window algorithm.

```csharp
Task<RateLimitResult> CheckRateLimitAsync(
    string key, 
    int maxRequests, 
    TimeSpan timeWindow)
```

**Parameters**:
- `key` (string): Identifier for rate limiting (e.g., user ID, IP address)
- `maxRequests` (int): Maximum requests allowed
- `timeWindow` (TimeSpan): Time window for rate limiting

**Returns**: `RateLimitResult` indicating if request is allowed

**Example**:
```csharp
var result = await securityService.CheckRateLimitAsync(
    userId, 100, TimeSpan.FromMinutes(1));

if (!result.IsAllowed)
{
    // Rate limit exceeded
    Console.WriteLine($"Rate limited. Try again in {result.RetryAfter}");
    return TooManyRequestsResult();
}
```

### Token Generation

#### GenerateSecureTokenAsync

Generates cryptographically secure random tokens.

```csharp
Task<string> GenerateSecureTokenAsync(int tokenSize = 32)
```

**Parameters**:
- `tokenSize` (int): Token size in bytes (default: 32)

**Returns**: Base64-encoded secure token

**Example**:
```csharp
// Generate 256-bit (32-byte) token
var token = await securityService.GenerateSecureTokenAsync(32);
Console.WriteLine($"Generated token: {token}");

// Generate 128-bit (16-byte) token
var smallToken = await securityService.GenerateSecureTokenAsync(16);
```

### Security Metrics

#### GetSecurityMetricsAsync

Retrieves comprehensive security metrics.

```csharp
Task<SecurityMetrics> GetSecurityMetricsAsync()
```

**Returns**: `SecurityMetrics` with operation counts and performance data

**Example**:
```csharp
var metrics = await securityService.GetSecurityMetricsAsync();

Console.WriteLine($"Total validations: {metrics.TotalValidations}");
Console.WriteLine($"Threats detected: {metrics.ThreatsDetected}");
Console.WriteLine($"Encryptions performed: {metrics.TotalEncryptions}");
Console.WriteLine($"Hash operations: {metrics.TotalHashOperations}");
Console.WriteLine($"Tokens generated: {metrics.TokensGenerated}");
```

## Data Models

### SecurityValidationOptions

Configuration for input validation.

```csharp
public class SecurityValidationOptions
{
    public bool CheckSqlInjection { get; set; } = true;
    public bool CheckXss { get; set; } = true;
    public bool CheckCodeInjection { get; set; } = true;
    public int MaxInputSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] CustomPatterns { get; set; } = Array.Empty<string>();
}
```

### SecurityValidationResult

Result of input validation operation.

```csharp
public class SecurityValidationResult
{
    public bool IsValid { get; set; }
    public bool HasSecurityThreats { get; set; }
    public List<string> ThreatTypes { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public double RiskScore { get; set; }
    public DateTime ValidatedAt { get; set; }
}
```

### EncryptionResult

Result of encryption operation.

```csharp
public class EncryptionResult
{
    public bool Success { get; set; }
    public byte[] EncryptedData { get; set; } = Array.Empty<byte>();
    public byte[] Key { get; set; } = Array.Empty<byte>();
    public byte[] Nonce { get; set; } = Array.Empty<byte>();
    public string Algorithm { get; set; } = "AES-256-GCM";
    public string? ErrorMessage { get; set; }
}
```

### PasswordHashResult

Result of password hashing operation.

```csharp
public class PasswordHashResult
{
    public bool Success { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "PBKDF2";
    public int Iterations { get; set; } = 100000;
    public string? ErrorMessage { get; set; }
}
```

### RateLimitResult

Result of rate limit check.

```csharp
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan RetryAfter { get; set; }
    public DateTime WindowReset { get; set; }
}
```

### SecurityMetrics

Comprehensive security metrics.

```csharp
public class SecurityMetrics
{
    public long TotalValidations { get; set; }
    public long ThreatsDetected { get; set; }
    public long TotalEncryptions { get; set; }
    public long TotalHashOperations { get; set; }
    public long TokensGenerated { get; set; }
    public double AverageValidationTime { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

## Security Considerations

### Input Validation
- **Maximum Input Size**: 10MB default limit (configurable)
- **Pattern Matching**: Uses optimized regex patterns for threat detection
- **Performance**: < 50ms validation time for typical inputs
- **False Positives**: Minimal false positive rate through refined patterns

### Encryption
- **Algorithm**: AES-256-GCM (authenticated encryption)
- **Key Management**: Secure key generation using cryptographic RNG
- **Nonce Handling**: Unique nonce per encryption operation
- **Authentication**: Integrated authentication prevents tampering

### Password Security
- **Algorithm**: PBKDF2 with SHA-256
- **Iterations**: 100,000 (OWASP recommended minimum)
- **Salt**: Unique 128-bit salt per password
- **Storage**: Never store plaintext passwords

### Rate Limiting
- **Algorithm**: Sliding window with Redis-like implementation
- **Accuracy**: Precise request counting within time windows
- **Performance**: < 10ms per rate limit check
- **Scalability**: Supports millions of unique keys

## Performance Characteristics

### Throughput Targets
- **Input Validation**: 5,000 operations/second
- **Encryption**: 1,000 operations/second for 1KB data
- **Password Hashing**: 10 operations/second (intentionally slow)
- **Rate Limiting**: 10,000 checks/second

### Latency Targets
- **Validation**: < 50ms for typical input
- **Encryption**: < 100ms for 1KB data
- **Decryption**: < 50ms for 1KB data
- **Rate Limit Check**: < 10ms

## Error Handling

All operations return structured results with success indicators and error messages.

### Common Error Types
- **ValidationException**: Invalid input parameters
- **EncryptionException**: Encryption/decryption failures
- **RateLimitException**: Rate limit configuration errors
- **SecurityException**: Security policy violations

### Error Response Format
```csharp
{
    "Success": false,
    "ErrorMessage": "Descriptive error message",
    "ErrorCode": "SECURITY_001",
    "Details": { /* Additional error context */ }
}
```

## Integration Examples

### Web API Integration
```csharp
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    private readonly ISecurityService _securityService;

    public SecureController(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateInput([FromBody] string input)
    {
        var options = new SecurityValidationOptions();
        var result = await _securityService.ValidateInputAsync(input, options);
        
        if (result.HasSecurityThreats)
        {
            return BadRequest(new { Threats = result.ThreatTypes });
        }
        
        return Ok(new { Valid = result.IsValid });
    }
}
```

### Middleware Integration
```csharp
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISecurityService _securityService;

    public SecurityMiddleware(RequestDelegate next, ISecurityService securityService)
    {
        _next = next;
        _securityService = securityService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Rate limiting
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitResult = await _securityService.CheckRateLimitAsync(
            clientId, 100, TimeSpan.FromMinutes(1));

        if (!rateLimitResult.IsAllowed)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            return;
        }

        await _next(context);
    }
}
```

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task ValidateInputAsync_SqlInjection_ShouldDetectThreat()
{
    // Arrange
    var securityService = TestServiceFactory.CreateSecurityService();
    var maliciousInput = "'; DROP TABLE users; --";
    var options = new SecurityValidationOptions { CheckSqlInjection = true };

    // Act
    var result = await securityService.ValidateInputAsync(maliciousInput, options);

    // Assert
    Assert.True(result.HasSecurityThreats);
    Assert.Contains("SQL injection", result.ThreatTypes);
}
```

### Performance Test Example
```csharp
[Benchmark]
public async Task<SecurityValidationResult> ValidationPerformance()
{
    return await _securityService.ValidateInputAsync(_testInput, _options);
}
```

## Monitoring and Observability

The SecurityService integrates with the observability infrastructure:

- **Metrics**: Validation counts, threat detection rates, performance metrics
- **Tracing**: Distributed tracing for all security operations
- **Logging**: Security events with structured logging
- **Health Checks**: Service health and dependency validation

## Configuration

### appsettings.json
```json
{
  "Security": {
    "EncryptionAlgorithm": "AES-256-GCM",
    "KeyRotationIntervalHours": 24,
    "MaxInputSizeMB": 10,
    "EnableRateLimiting": true,
    "DefaultRateLimitRequests": 100,
    "RateLimitWindowMinutes": 1
  }
}
```

### Dependency Injection
```csharp
services.AddNeoServiceLayer(configuration);
// SecurityService is automatically registered
```

This SecurityService API provides comprehensive protection against the security vulnerabilities identified in the system review, ensuring production-ready security for the Neo Service Layer.