using System;
using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using Xunit;

namespace NeoServiceLayer.Shared.Tests.Extensions;

public class StringExtensionsUnitTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("test", false)]
    [InlineData(" ", false)]
    public void IsNullOrEmpty_ReturnsExpectedResult(string input, bool expected)
    {
        var result = input.IsNullOrEmpty();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("\t\n", true)]
    [InlineData("test", false)]
    [InlineData(" test ", false)]
    public void IsNullOrWhiteSpace_ReturnsExpectedResult(string input, bool expected)
    {
        var result = input.IsNullOrWhiteSpace();
        result.Should().Be(expected);
    }

    [Fact]
    public void ToBase64_WithValidString_ReturnsEncodedString()
    {
        var input = "Hello World";
        var result = input.ToBase64();
        
        result.Should().Be("SGVsbG8gV29ybGQ=");
    }

    [Fact]
    public void ToBase64_WithEmptyString_ReturnsEmptyString()
    {
        var result = "".ToBase64();
        result.Should().BeEmpty();
    }

    [Fact]
    public void FromBase64_WithValidBase64_ReturnsOriginalString()
    {
        var base64 = "SGVsbG8gV29ybGQ=";
        var result = base64.FromBase64();
        
        result.Should().Be("Hello World");
    }

    [Fact]
    public void FromBase64_WithInvalidBase64_ThrowsFormatException()
    {
        var invalidBase64 = "InvalidBase64!@#";
        Action act = () => invalidBase64.FromBase64();
        
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name+tag@domain.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@example.com", false)]
    [InlineData("test@", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_ReturnsExpectedResult(string email, bool expected)
    {
        var result = email.IsValidEmail();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://test.org", true)]
    [InlineData("https://subdomain.example.com/path?query=value", true)]
    [InlineData("invalid-url", false)]
    [InlineData("ftp://example.com", false)] // Only http/https supported by pattern
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidUrl_ReturnsExpectedResult(string url, bool expected)
    {
        var result = url.IsValidUrl();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("NKuyBkoGdZZSLyPbJEetheRhMrGSCQx7YL", true)] // Valid Neo N3 address
    [InlineData("AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y", true)] // Valid Neo Legacy address
    [InlineData("InvalidNeoAddress123", false)]
    [InlineData("Nshort", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidNeoAddress_ReturnsExpectedResult(string address, bool expected)
    {
        var result = address.IsValidNeoAddress();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0x742d35cc6cdfb32123b95e5b4c1de7b5e7f2f28e", true)]
    [InlineData("0x742D35CC6CDFB32123B95E5B4C1DE7B5E7F2F28E", true)]
    [InlineData("742d35cc6cdfb32123b95e5b4c1de7b5e7f2f28e", false)] // Missing 0x
    [InlineData("0xInvalidEthereumAddress", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEthereumAddress_ReturnsExpectedResult(string address, bool expected)
    {
        var result = address.IsValidEthereumAddress();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1234567890", true)]
    [InlineData("ABCDEF", true)]
    [InlineData("abcdef123", true)]
    [InlineData("ghijk", false)]
    [InlineData("", true)] // Empty is valid hex
    [InlineData(null, false)]
    public void IsHexString_ReturnsExpectedResult(string input, bool expected)
    {
        var result = input.IsHexString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", 5, "Hello")]
    [InlineData("Test", 10, "Test")]
    [InlineData("", 5, "")]
    [InlineData("Test", 0, "")]
    public void Truncate_ReturnsExpectedResult(string input, int maxLength, string expected)
    {
        var result = input.Truncate(maxLength);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello", "llo", true)]
    [InlineData("Hello", "LLO", true)] // Case insensitive
    [InlineData("Hello", "xyz", false)]
    [InlineData("", "", true)]
    [InlineData(null, "", false)]
    public void ContainsIgnoreCase_ReturnsExpectedResult(string input, string value, bool expected)
    {
        var result = input.ContainsIgnoreCase(value);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello", "HEL", true)]
    [InlineData("Hello", "hel", true)]
    [InlineData("Hello", "llo", false)]
    [InlineData("", "", true)]
    [InlineData(null, "", false)]
    public void StartsWithIgnoreCase_ReturnsExpectedResult(string input, string value, bool expected)
    {
        var result = input.StartsWithIgnoreCase(value);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello", "LLO", true)]
    [InlineData("Hello", "llo", true)]
    [InlineData("Hello", "HEL", false)]
    [InlineData("", "", true)]
    [InlineData(null, "", false)]
    public void EndsWithIgnoreCase_ReturnsExpectedResult(string input, string value, bool expected)
    {
        var result = input.EndsWithIgnoreCase(value);
        result.Should().Be(expected);
    }

    [Fact]
    public void ToSha256_WithValidString_ReturnsHashString()
    {
        var input = "Hello World";
        var result = input.ToSha256();
        
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64); // SHA256 produces 64 character hex string
        result.Should().MatchRegex("^[a-fA-F0-9]{64}$");
    }

    [Fact]
    public void ToSha256_WithEmptyString_ReturnsValidHash()
    {
        var result = "".ToSha256();
        
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64);
    }

    [Fact]
    public void Sanitize_RemovesControlCharacters()
    {
        var input = "Hello\x00\x01\x02World";
        var result = input.Sanitize();
        
        result.Should().Be("HelloWorld");
    }

    [Theory]
    [InlineData("test-value", "TestValue")]
    [InlineData("multiple-word-test", "MultipleWordTest")]
    [InlineData("", "")]
    [InlineData("single", "Single")]
    public void ToPascalCase_ReturnsExpectedResult(string input, string expected)
    {
        var result = input.ToPascalCase();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("TestValue", "test-value")]
    [InlineData("MultipleWordTest", "multiple-word-test")]
    [InlineData("", "")]
    [InlineData("Single", "single")]
    public void ToKebabCase_ReturnsExpectedResult(string input, string expected)
    {
        var result = input.ToKebabCase();
        result.Should().Be(expected);
    }

    [Fact]
    public void EscapeHtml_EscapesSpecialCharacters()
    {
        var input = "<script>alert('test');</script>";
        var result = input.EscapeHtml();
        
        result.Should().Contain("&lt;").And.Contain("&gt;").And.Contain("&#39;");
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("256.1.1.1", false)]
    [InlineData("192.168.1", false)]
    [InlineData("invalid-ip", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidIPv4_ReturnsExpectedResult(string ip, bool expected)
    {
        var result = ip.IsValidIPv4();
        result.Should().Be(expected);
    }
}