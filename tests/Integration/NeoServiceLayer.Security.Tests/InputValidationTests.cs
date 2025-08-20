using System;
using Xunit;
using NeoServiceLayer.Core.Security;

namespace NeoServiceLayer.Security.Tests;

/// <summary>
/// Comprehensive tests for input validation security implementations.
/// </summary>
public class InputValidationTests
{
    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", "ETHEREUM", true)]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", "ETH", true)]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb", "ETH", false)] // Too short
    [InlineData("0xG42d35Cc6634C0532925a3b844Bc9e7595f0bEb0", "ETH", false)] // Invalid character
    [InlineData("742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", "ETH", false)] // Missing 0x prefix
    [InlineData("NKuyBkoGdZZSLyPbJEetheRhMjeznFZszf", "NEO", true)]
    [InlineData("NKuyBkoGdZZSLyPbJEetheRhMjeznFZsz", "NEO", false)] // Too short
    [InlineData("MKuyBkoGdZZSLyPbJEetheRhMjeznFZszf", "NEO", false)] // Wrong prefix
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", "BITCOIN", true)]
    [InlineData("bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq", "BTC", true)]
    [InlineData("", "ETH", false)]
    [InlineData(null, "ETH", false)]
    public void IsValidBlockchainAddress_ShouldValidateCorrectly(string address, string blockchainType, bool expected)
    {
        // Act
        var result = InputValidation.IsValidBlockchainAddress(address, blockchainType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("SELECT * FROM users", false)]
    [InlineData("'; DROP TABLE users; --", false)]
    [InlineData("1' OR '1'='1", false)]
    [InlineData("admin' --", false)]
    [InlineData("UNION SELECT password FROM users", false)]
    [InlineData("normalInput123", true)]
    [InlineData("user@example.com", true)]
    [InlineData("Product Name (2024)", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    public void IsSafeSqlInput_ShouldDetectSqlInjection(string input, bool expected)
    {
        // Act
        var result = InputValidation.IsSafeSqlInput(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>", false)]
    [InlineData("javascript:alert('XSS')", false)]
    [InlineData("<img src=x onerror=alert('XSS')>", false)]
    [InlineData("<iframe src='evil.com'></iframe>", false)]
    [InlineData("<svg onload=alert('XSS')>", false)]
    [InlineData("Normal text content", true)]
    [InlineData("Hello <World>", true)] // Angle brackets alone are OK
    [InlineData("", true)]
    [InlineData(null, true)]
    public void IsSafeHtmlInput_ShouldDetectXss(string input, bool expected)
    {
        // Act
        var result = InputValidation.IsSafeHtmlInput(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeInput_ShouldRemoveControlCharacters()
    {
        // Arrange
        var input = "Hello\x00World\x1F\x7FTest";
        
        // Act
        var result = InputValidation.SanitizeInput(input);
        
        // Assert
        Assert.DoesNotContain("\x00", result);
        Assert.DoesNotContain("\x1F", result);
        Assert.DoesNotContain("\x7F", result);
    }

    [Fact]
    public void SanitizeInput_ShouldTruncateToMaxLength()
    {
        // Arrange
        var input = new string('a', 2000);
        
        // Act
        var result = InputValidation.SanitizeInput(input, maxLength: 100);
        
        // Assert
        Assert.Equal(100, result.Length);
    }

    [Fact]
    public void SanitizeInput_ShouldHtmlEncodeSpecialCharacters()
    {
        // Arrange
        var input = "<script>alert('test')</script>";
        
        // Act
        var result = InputValidation.SanitizeInput(input);
        
        // Assert
        Assert.Contains("&lt;script&gt;", result);
        Assert.Contains("&lt;/script&gt;", result);
        Assert.DoesNotContain("<script>", result);
    }

    [Theory]
    [InlineData(50, 0, 100, true)]
    [InlineData(0, 0, 100, true)]
    [InlineData(100, 0, 100, true)]
    [InlineData(-1, 0, 100, false)]
    [InlineData(101, 0, 100, false)]
    public void IsValidNumericRange_ShouldValidateCorrectly(decimal value, decimal min, decimal max, bool expected)
    {
        // Act
        var result = InputValidation.IsValidNumericRange(value, min, max);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://example.com", null, true)]
    [InlineData("http://example.com", null, true)]
    [InlineData("ftp://example.com", null, false)] // Only HTTP/HTTPS allowed
    [InlineData("javascript:alert('XSS')", null, false)]
    [InlineData("https://api.binance.com", new[] { "binance.com" }, true)]
    [InlineData("https://sub.binance.com", new[] { "binance.com" }, true)]
    [InlineData("https://evil.com", new[] { "binance.com" }, false)]
    [InlineData("", null, false)]
    [InlineData(null, null, false)]
    public void IsValidUrl_ShouldValidateCorrectly(string url, string[] allowedDomains, bool expected)
    {
        // Act
        var result = InputValidation.IsValidUrl(url, allowedDomains);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("user.name+tag@example.co.uk", true)]
    [InlineData("invalid.email", false)]
    [InlineData("@example.com", false)]
    [InlineData("user@", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_ShouldValidateCorrectly(string email, bool expected)
    {
        // Act
        var result = InputValidation.IsValidEmail(email);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", "ETH", true)]
    [InlineData("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcde", "ETH", false)] // Too short
    [InlineData("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", "BTC", true)]
    [InlineData("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", "NEO", true)]
    [InlineData("", "ETH", false)]
    [InlineData(null, "ETH", false)]
    public void IsValidTransactionHash_ShouldValidateCorrectly(string hash, string blockchainType, bool expected)
    {
        // Act
        var result = InputValidation.IsValidTransactionHash(hash, blockchainType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BlockchainAddressValidation_ShouldBeCaseInsensitive()
    {
        // Arrange
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0";

        // Act & Assert
        Assert.True(InputValidation.IsValidBlockchainAddress(address, "ethereum"));
        Assert.True(InputValidation.IsValidBlockchainAddress(address, "ETHEREUM"));
        Assert.True(InputValidation.IsValidBlockchainAddress(address, "Ethereum"));
        Assert.True(InputValidation.IsValidBlockchainAddress(address, "eth"));
        Assert.True(InputValidation.IsValidBlockchainAddress(address, "ETH"));
    }

    [Fact]
    public void SqlInjectionDetection_ShouldHandleComplexPatterns()
    {
        // Arrange
        var injectionPatterns = new[]
        {
            "1; EXEC sp_configure 'show advanced options', 1",
            "1'; EXECUTE IMMEDIATE 'SELECT * FROM users'; --",
            "admin' OR 1=1 --",
            "' OR ''='",
            "1 UNION ALL SELECT NULL, NULL, NULL--",
            "1' AND (SELECT * FROM (SELECT(SLEEP(5)))a)--",
            "'; WAITFOR DELAY '00:00:05'--"
        };

        // Act & Assert
        foreach (var pattern in injectionPatterns)
        {
            Assert.False(InputValidation.IsSafeSqlInput(pattern), 
                $"Pattern should be detected as SQL injection: {pattern}");
        }
    }

    [Fact]
    public void XssDetection_ShouldHandleVariousAttackVectors()
    {
        // Arrange
        var xssPatterns = new[]
        {
            "<script>alert('XSS')</script>",
            "<img src=x onerror=alert('XSS')>",
            "<svg/onload=alert('XSS')>",
            "<body onload=alert('XSS')>",
            "<iframe src=javascript:alert('XSS')>",
            "<object data='data:text/html,<script>alert(1)</script>'>",
            "<embed src='data:text/html,<script>alert(1)</script>'>"
        };

        // Act & Assert
        foreach (var pattern in xssPatterns)
        {
            Assert.False(InputValidation.IsSafeHtmlInput(pattern), 
                $"Pattern should be detected as XSS: {pattern}");
        }
    }
}