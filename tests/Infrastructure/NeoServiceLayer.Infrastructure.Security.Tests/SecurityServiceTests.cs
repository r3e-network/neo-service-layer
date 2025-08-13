using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Security;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Infrastructure.Security.Tests;

/// <summary>
/// Comprehensive unit tests for SecurityService addressing security vulnerabilities identified in code review.
/// </summary>
public class SecurityServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SecurityService _securityService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new NullLogger<SecurityService>();
        _securityService = new SecurityService(_logger);
    }

    [Fact]
    public async Task SecurityService_Initialization_ShouldSucceed()
    {
        // Act
        var health = await _securityService.GetHealthAsync();
        
        // Assert
        Assert.Equal(ServiceHealth.Healthy, health);
        Assert.True(_securityService.HasCapability<ISecurityService>());
    }

    [Theory]
    [InlineData("SELECT * FROM users WHERE id = 1", false)] // Safe query
    [InlineData("SELECT * FROM users WHERE id = 1; DROP TABLE users;", true)] // SQL injection
    [InlineData("SELECT * FROM users WHERE name = 'admin' OR '1'='1'", true)] // SQL injection
    [InlineData("INSERT INTO logs (message) VALUES ('Normal log entry')", false)] // Safe insert
    [InlineData("'; DELETE FROM users; --", true)] // SQL injection attempt
    [InlineData("UNION SELECT password FROM admin_users", true)] // Union-based injection
    public async Task ValidateInputAsync_SqlInjectionDetection_ShouldWork(string input, bool expectedThreat)
    {
        // Arrange
        var options = new SecurityValidationOptions
        {
            CheckSqlInjection = true
        };

        // Act
        var result = await _securityService.ValidateInputAsync(input, options);

        // Assert
        Assert.Equal(expectedThreat, result.HasSecurityThreats);
        if (expectedThreat)
        {
            Assert.Contains("SQL injection", string.Join(", ", result.ThreatTypes));
        }
    }

    [Theory]
    [InlineData("<p>Normal text</p>", false)] // Safe HTML
    [InlineData("<script>alert('xss')</script>", true)] // XSS attempt
    [InlineData("<img src=x onerror=alert(1)>", true)] // XSS in image tag
    [InlineData("javascript:alert('xss')", true)] // JavaScript protocol
    [InlineData("<iframe src='javascript:alert(1)'></iframe>", true)] // XSS in iframe
    [InlineData("onclick=\"alert('xss')\"", true)] // Event handler injection
    [InlineData("Normal text without any tags", false)] // Plain text
    public async Task ValidateInputAsync_XssDetection_ShouldWork(string input, bool expectedThreat)
    {
        // Arrange
        var options = new SecurityValidationOptions
        {
            CheckXss = true
        };

        // Act
        var result = await _securityService.ValidateInputAsync(input, options);

        // Assert
        Assert.Equal(expectedThreat, result.HasSecurityThreats);
        if (expectedThreat)
        {
            Assert.Contains("XSS", string.Join(", ", result.ThreatTypes));
        }
    }

    [Theory]
    [InlineData("Math.sqrt(16)", false)] // Safe expression
    [InlineData("System.IO.File.Delete('important.txt')", true)] // File system access
    [InlineData("Process.Start('cmd.exe')", true)] // Process execution
    [InlineData("Assembly.Load", true)] // Assembly loading
    [InlineData("typeof(System.Reflection.Assembly)", true)] // Reflection
    [InlineData("Environment.Exit(0)", true)] // Environment manipulation
    [InlineData("var result = 2 + 2;", false)] // Safe code
    public async Task ValidateInputAsync_CodeInjectionDetection_ShouldWork(string input, bool expectedThreat)
    {
        // Arrange
        var options = new SecurityValidationOptions
        {
            CheckCodeInjection = true
        };

        // Act
        var result = await _securityService.ValidateInputAsync(input, options);

        // Assert
        Assert.Equal(expectedThreat, result.HasSecurityThreats);
        if (expectedThreat)
        {
            Assert.Contains("Code injection", string.Join(", ", result.ThreatTypes));
        }
    }

    [Theory]
    [InlineData("user@example.com", 100, false)] // Normal input
    [InlineData("", 100, true)] // Empty input
    [InlineData(null, 100, true)] // Null input
    public async Task ValidateInputAsync_InputSizeValidation_ShouldWork(string input, int maxSize, bool shouldFail)
    {
        // Arrange
        var options = new SecurityValidationOptions
        {
            MaxInputSize = maxSize
        };

        // Act & Assert
        if (shouldFail)
        {
            var result = await _securityService.ValidateInputAsync(input, options);
            Assert.False(result.IsValid);
        }
        else
        {
            var result = await _securityService.ValidateInputAsync(input, options);
            Assert.True(result.IsValid);
        }
    }

    [Fact]
    public async Task ValidateInputAsync_LargeInput_ShouldBeRejected()
    {
        // Arrange
        var largeInput = new string('A', 10 * 1024 * 1024 + 1); // 10MB + 1 byte
        var options = new SecurityValidationOptions();

        // Act
        var result = await _securityService.ValidateInputAsync(largeInput, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Input size exceeds maximum allowed", result.ValidationErrors);
    }

    [Fact]
    public async Task EncryptDataAsync_ValidInput_ShouldSucceed()
    {
        // Arrange
        var plaintext = "Sensitive data that needs encryption";
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        // Act
        var result = await _securityService.EncryptDataAsync(plaintextBytes);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.EncryptedData);
        Assert.NotNull(result.Key);
        Assert.NotNull(result.Nonce);
        Assert.True(result.EncryptedData.Length > 0);
        Assert.Equal("AES-256-GCM", result.Algorithm);
    }

    [Fact]
    public async Task DecryptDataAsync_ValidInput_ShouldRecoverOriginalData()
    {
        // Arrange
        var originalData = "Test data for encryption/decryption";
        var originalBytes = Encoding.UTF8.GetBytes(originalData);

        // Encrypt first
        var encryptResult = await _securityService.EncryptDataAsync(originalBytes);
        Assert.True(encryptResult.Success);

        // Act - Decrypt
        var decryptResult = await _securityService.DecryptDataAsync(
            encryptResult.EncryptedData, 
            encryptResult.Key, 
            encryptResult.Nonce);

        // Assert
        Assert.True(decryptResult.Success);
        Assert.NotNull(decryptResult.DecryptedData);
        
        var recoveredText = Encoding.UTF8.GetString(decryptResult.DecryptedData);
        Assert.Equal(originalData, recoveredText);
    }

    [Fact]
    public async Task DecryptDataAsync_InvalidKey_ShouldFail()
    {
        // Arrange
        var originalBytes = Encoding.UTF8.GetBytes("Test data");
        var encryptResult = await _securityService.EncryptDataAsync(originalBytes);
        var invalidKey = new byte[32]; // All zeros

        // Act
        var decryptResult = await _securityService.DecryptDataAsync(
            encryptResult.EncryptedData,
            invalidKey,
            encryptResult.Nonce);

        // Assert
        Assert.False(decryptResult.Success);
        Assert.NotNull(decryptResult.ErrorMessage);
    }

    [Fact]
    public async Task HashPasswordAsync_ValidPassword_ShouldCreateSecureHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var result = await _securityService.HashPasswordAsync(password);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Hash);
        Assert.NotNull(result.Salt);
        Assert.Equal("PBKDF2", result.Algorithm);
        Assert.Equal(100000, result.Iterations);
        
        // Hash should not contain the original password
        Assert.DoesNotContain(password, result.Hash);
    }

    [Fact]
    public async Task VerifyPasswordAsync_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashResult = await _securityService.HashPasswordAsync(password);

        // Act
        var verifyResult = await _securityService.VerifyPasswordAsync(
            password, 
            hashResult.Hash, 
            hashResult.Salt);

        // Assert
        Assert.True(verifyResult.IsValid);
        Assert.True(verifyResult.Success);
    }

    [Fact]
    public async Task VerifyPasswordAsync_IncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashResult = await _securityService.HashPasswordAsync(correctPassword);

        // Act
        var verifyResult = await _securityService.VerifyPasswordAsync(
            wrongPassword, 
            hashResult.Hash, 
            hashResult.Salt);

        // Assert
        Assert.False(verifyResult.IsValid);
        Assert.True(verifyResult.Success); // Operation succeeded, but password is invalid
    }

    [Fact]
    public async Task GenerateSecureTokenAsync_ShouldCreateUniqueTokens()
    {
        // Act
        var token1 = await _securityService.GenerateSecureTokenAsync(32);
        var token2 = await _securityService.GenerateSecureTokenAsync(32);

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);
        Assert.Equal(44, token1.Length); // Base64 encoded 32 bytes = 44 characters
    }

    [Theory]
    [InlineData(16, 24)] // 16 bytes -> 24 base64 chars
    [InlineData(32, 44)] // 32 bytes -> 44 base64 chars
    [InlineData(64, 88)] // 64 bytes -> 88 base64 chars
    public async Task GenerateSecureTokenAsync_DifferentSizes_ShouldProduceCorrectLength(int tokenSize, int expectedLength)
    {
        // Act
        var token = await _securityService.GenerateSecureTokenAsync(tokenSize);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(expectedLength, token.Length);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimit_ShouldAllow()
    {
        // Arrange
        var key = "test_user_1";
        var limit = 10;
        var window = TimeSpan.FromMinutes(1);

        // Act - Make requests within limit
        for (int i = 0; i < 5; i++)
        {
            var result = await _securityService.CheckRateLimitAsync(key, limit, window);
            Assert.True(result.IsAllowed);
            Assert.True(result.RemainingRequests > 0);
        }
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsLimit_ShouldBlock()
    {
        // Arrange
        var key = "test_user_2";
        var limit = 3;
        var window = TimeSpan.FromMinutes(1);

        // Act - Exceed the limit
        for (int i = 0; i < limit; i++)
        {
            var result = await _securityService.CheckRateLimitAsync(key, limit, window);
            Assert.True(result.IsAllowed);
        }

        // This should be blocked
        var blockedResult = await _securityService.CheckRateLimitAsync(key, limit, window);

        // Assert
        Assert.False(blockedResult.IsAllowed);
        Assert.Equal(0, blockedResult.RemainingRequests);
        Assert.True(blockedResult.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task ValidateInputAsync_CombinedThreats_ShouldDetectAll()
    {
        // Arrange
        var maliciousInput = "<script>alert('xss')</script>'; DROP TABLE users; --";
        var options = new SecurityValidationOptions
        {
            CheckSqlInjection = true,
            CheckXss = true,
            CheckCodeInjection = true
        };

        // Act
        var result = await _securityService.ValidateInputAsync(maliciousInput, options);

        // Assert
        Assert.True(result.HasSecurityThreats);
        Assert.Contains("XSS", string.Join(", ", result.ThreatTypes));
        Assert.Contains("SQL injection", string.Join(", ", result.ThreatTypes));
    }

    [Fact]
    public async Task GetSecurityMetricsAsync_ShouldReturnValidMetrics()
    {
        // Arrange - Generate some activity
        await _securityService.ValidateInputAsync("test input", new SecurityValidationOptions());
        await _securityService.HashPasswordAsync("testpassword");
        await _securityService.GenerateSecureTokenAsync(32);

        // Act
        var metrics = await _securityService.GetSecurityMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalValidations >= 1);
        Assert.True(metrics.TotalEncryptions >= 0);
        Assert.True(metrics.TotalHashOperations >= 1);
        Assert.True(metrics.TokensGenerated >= 1);
        Assert.NotNull(metrics.LastUpdated);
    }

    public void Dispose()
    {
        _securityService?.Dispose();
    }
}

/// <summary>
/// Additional test class for security edge cases and performance validation.
/// </summary>
public class SecurityServicePerformanceTests : IDisposable
{
    private readonly SecurityService _securityService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityServicePerformanceTests()
    {
        _logger = new NullLogger<SecurityService>();
        _securityService = new SecurityService(_logger);
    }

    [Fact]
    public async Task EncryptionPerformance_ShouldMeetRequirements()
    {
        // Arrange
        var data = new byte[1024]; // 1KB of data
        var random = new Random();
        random.NextBytes(data);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _securityService.EncryptDataAsync(data);
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.True(result.Success);
        var duration = endTime - startTime;
        Assert.True(duration.TotalMilliseconds < 100, $"Encryption took {duration.TotalMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task ValidationPerformance_ShouldHandleReasonableLoad()
    {
        // Arrange
        var testInputs = new[]
        {
            "Normal text input",
            "SELECT * FROM users WHERE id = 1",
            "<p>HTML content</p>",
            "Math.sqrt(16)"
        };

        var options = new SecurityValidationOptions
        {
            CheckSqlInjection = true,
            CheckXss = true,
            CheckCodeInjection = true
        };

        // Act
        var startTime = DateTime.UtcNow;
        var tasks = new List<Task<SecurityValidationResult>>();

        for (int i = 0; i < 100; i++)
        {
            var input = testInputs[i % testInputs.Length];
            tasks.Add(_securityService.ValidateInputAsync(input, options));
        }

        await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        Assert.True(duration.TotalSeconds < 5, $"100 validations took {duration.TotalSeconds}s, expected < 5s");
        
        foreach (var task in tasks)
        {
            Assert.NotNull(task.Result);
        }
    }

    public void Dispose()
    {
        _securityService?.Dispose();
    }
}