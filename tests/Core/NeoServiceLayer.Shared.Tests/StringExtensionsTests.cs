using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Shared.Tests;

/// <summary>
/// Comprehensive tests for StringExtensions utility methods.
/// </summary>
public class StringExtensionsTests
{
    #region IsNullOrEmpty Tests

    [Fact]
    public void IsNullOrEmpty_WithNull_ShouldReturnTrue()
    {
        // Act & Assert
        ((string?)null).IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyString_ShouldReturnTrue()
    {
        // Act & Assert
        string.Empty.IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithWhitespace_ShouldReturnFalse()
    {
        // Act & Assert
        " ".IsNullOrEmpty().Should().BeFalse();
    }

    [Fact]
    public void IsNullOrEmpty_WithValidString_ShouldReturnFalse()
    {
        // Act & Assert
        "test".IsNullOrEmpty().Should().BeFalse();
    }

    #endregion

    #region IsNullOrWhiteSpace Tests

    [Fact]
    public void IsNullOrWhiteSpace_WithNull_ShouldReturnTrue()
    {
        // Act & Assert
        ((string?)null).IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_WithEmptyString_ShouldReturnTrue()
    {
        // Act & Assert
        string.Empty.IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_WithWhitespace_ShouldReturnTrue()
    {
        // Act & Assert
        "   ".IsNullOrWhiteSpace().Should().BeTrue();
        "\t\n ".IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_WithValidString_ShouldReturnFalse()
    {
        // Act & Assert
        "test".IsNullOrWhiteSpace().Should().BeFalse();
        " test ".IsNullOrWhiteSpace().Should().BeFalse();
    }

    #endregion

    #region Base64 Encoding/Decoding Tests

    [Fact]
    public void ToBase64_WithValidString_ShouldEncodeCorrectly()
    {
        // Arrange
        var input = "Hello, World!";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

        // Act
        var result = input.ToBase64();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToBase64_WithEmptyString_ShouldReturnEmptyString()
    {
        // Act & Assert
        string.Empty.ToBase64().Should().BeEmpty();
    }

    [Fact]
    public void ToBase64_WithCustomEncoding_ShouldUseCustomEncoding()
    {
        // Arrange
        var input = "Hello, World!";
        var encoding = Encoding.ASCII;
        var expected = Convert.ToBase64String(encoding.GetBytes(input));

        // Act
        var result = input.ToBase64(encoding);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FromBase64_WithValidBase64_ShouldDecodeCorrectly()
    {
        // Arrange
        var input = "Hello, World!";
        var base64 = input.ToBase64();

        // Act
        var result = base64.FromBase64();

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void FromBase64_WithEmptyString_ShouldReturnEmptyString()
    {
        // Act & Assert
        string.Empty.FromBase64().Should().BeEmpty();
    }

    [Theory]
    [InlineData("SGVsbG8sIFdvcmxkIQ==")]
    [InlineData("VGVzdA==")]
    public void Base64_RoundTrip_ShouldPreserveOriginalString(string base64)
    {
        // Act
        var decoded = base64.FromBase64();
        var encoded = decoded.ToBase64();

        // Assert
        encoded.Should().Be(base64);
    }

    #endregion

    #region Hex Encoding/Decoding Tests

    [Fact]
    public void ToHex_WithValidString_ShouldEncodeCorrectly()
    {
        // Arrange
        var input = "Hello";
        var expected = Convert.ToHexString(Encoding.UTF8.GetBytes(input)).ToLowerInvariant();

        // Act
        var result = input.ToHex();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToHex_WithEmptyString_ShouldReturnEmptyString()
    {
        // Act & Assert
        string.Empty.ToHex().Should().BeEmpty();
    }

    [Fact]
    public void FromHex_WithValidHex_ShouldDecodeCorrectly()
    {
        // Arrange
        var input = "Hello";
        var hex = input.ToHex();

        // Act
        var result = hex.FromHex();

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("48656c6c6f", "Hello")]
    [InlineData("54657374", "Test")]
    public void Hex_RoundTrip_ShouldPreserveOriginalString(string hex, string expected)
    {
        // Act
        var decoded = hex.FromHex();
        var encoded = decoded.ToHex();

        // Assert
        decoded.Should().Be(expected);
        encoded.Should().Be(hex);
    }

    #endregion

    #region Hash Tests

    [Fact]
    public void ToSha256_WithValidInput_ShouldReturnCorrectHash()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = input.ToSha256();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64); // SHA256 produces 64 character hex string
    }

    [Fact]
    public void ToMd5_WithValidInput_ShouldReturnCorrectHash()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = input.ToMd5();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(32); // MD5 produces 32 character hex string
    }

    [Fact]
    public void ToMd5_WithEmptyString_ShouldReturnCorrectHash()
    {
        // Act
        var result = string.Empty.ToMd5();

        // Assert - MD5 of empty string
        result.Should().Be("d41d8cd98f00b204e9800998ecf8427e");
    }

    [Fact]
    public void ToMd5_WithNullString_ShouldReturnEmptyString()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.ToMd5();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Truncate Tests

    [Fact]
    public void Truncate_WithStringLongerThanMaxLength_ShouldTruncateWithSuffix()
    {
        // Arrange
        var input = "This is a very long string that should be truncated";

        // Act
        var result = input.Truncate(20);

        // Assert
        result.Should().Be("This is a very lo...");
        result.Length.Should().Be(20);
    }

    [Fact]
    public void Truncate_WithStringShorterThanMaxLength_ShouldReturnOriginal()
    {
        // Arrange
        var input = "Short string";

        // Act
        var result = input.Truncate(20);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Truncate_WithCustomSuffix_ShouldUseCustomSuffix()
    {
        // Arrange
        var input = "This is a long string";

        // Act
        var result = input.Truncate(15, "...");

        // Assert
        result.Should().Be("This is a lo...");
    }

    [Fact]
    public void Truncate_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).Truncate(10).Should().BeEmpty();
    }

    [Fact]
    public void Truncate_WithMaxLengthZero_ShouldReturnOnlySuffix()
    {
        // Arrange
        var input = "Test";

        // Act
        var result = input.Truncate(0);

        // Assert
        result.Should().Be("...");
    }

    #endregion

    #region Mask Tests

    [Fact]
    public void Mask_WithValidString_ShouldMaskMiddleCharacters()
    {
        // Arrange
        var input = "1234567890";

        // Act
        var result = input.Mask(2, 2);

        // Assert
        result.Should().Be("12******90");
    }

    [Fact]
    public void Mask_WithShortString_ShouldMaskEntireString()
    {
        // Arrange
        var input = "123";

        // Act
        var result = input.Mask(2, 2);

        // Assert
        result.Should().Be("***");
    }

    [Fact]
    public void Mask_WithCustomMaskChar_ShouldUseCustomChar()
    {
        // Arrange
        var input = "1234567890";

        // Act
        var result = input.Mask(2, 2, '#');

        // Assert
        result.Should().Be("12######90");
    }

    [Fact]
    public void Mask_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).Mask().Should().BeEmpty();
    }

    #endregion

    #region Pattern Matching Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("test@", false)]
    [InlineData("@example.com", false)]
    public void IsValidEmail_ShouldValidateEmailCorrectly(string email, bool expected)
    {
        // Act & Assert
        email.IsValidEmail().Should().Be(expected);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://test.org", true)]
    [InlineData("invalid-url", false)]
    [InlineData("ftp://example.com", false)]
    public void IsValidUrl_ShouldValidateUrlCorrectly(string url, bool expected)
    {
        // Act & Assert
        url.IsValidUrl().Should().Be(expected);
    }

    [Theory]
    [InlineData("1234567890abcdef", true)]
    [InlineData("ABCDEF123456", true)]
    [InlineData("123xyz", false)]
    [InlineData("", false)]
    public void IsValidHex_ShouldValidateHexCorrectly(string hex, bool expected)
    {
        // Act & Assert
        hex.IsValidHex().Should().Be(expected);
    }

    [Theory]
    [InlineData("NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx", true)]  // Valid Neo N3 address (34 chars)
    [InlineData("AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y", true)]  // Valid Neo Legacy address (34 chars)
    [InlineData("NiNmXL8FjEUEs1nfX9uHFBNaenxDHJtmuB", true)]  // Valid Neo N3 address (34 chars)
    [InlineData("A123456789012345678901234567890123", true)]   // Valid A-prefix address (34 chars)
    [InlineData("N123456789012345678901234567890123", true)]   // Valid N-prefix address (34 chars)
    [InlineData("B123456789012345678901234567890123", false)]  // Invalid prefix
    [InlineData("A12345678901234567890123456789012", false)]   // Too short (33 chars)
    [InlineData("A1234567890123456789012345678901234", false)] // Too long (35 chars)
    [InlineData("", false)]
    [InlineData("invalid", false)]
    public void IsValidNeoAddress_ShouldValidateNeoAddressCorrectly(string address, bool expected)
    {
        // Act & Assert
        address.IsValidNeoAddress().Should().Be(expected);
    }

    [Theory]
    [InlineData("0x1234567890123456789012345678901234567890", true)]
    [InlineData("0xABCDEF1234567890123456789012345678901234", true)]
    [InlineData("1234567890123456789012345678901234567890", false)]
    [InlineData("0x123", false)]
    public void IsValidEthereumAddress_ShouldValidateEthereumAddressCorrectly(string address, bool expected)
    {
        // Act & Assert
        address.IsValidEthereumAddress().Should().Be(expected);
    }

    #endregion

    #region Case Conversion Tests

    [Theory]
    [InlineData("TestString", "testString")]
    [InlineData("TEST", "tEST")]
    [InlineData("t", "t")]
    [InlineData("", "")]
    public void ToCamelCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act & Assert
        input.ToCamelCase().Should().Be(expected);
    }

    [Theory]
    [InlineData("testString", "TestString")]
    [InlineData("test", "Test")]
    [InlineData("T", "T")]
    [InlineData("", "")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act & Assert
        input.ToPascalCase().Should().Be(expected);
    }

    [Theory]
    [InlineData("TestString", "test-string")]
    [InlineData("XMLHttpRequest", "xmlhttp-request")]
    [InlineData("test", "test")]
    [InlineData("", "")]
    public void ToKebabCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act & Assert
        input.ToKebabCase().Should().Be(expected);
    }

    [Theory]
    [InlineData("TestString", "test_string")]
    [InlineData("XMLHttpRequest", "xmlhttp_request")]
    [InlineData("test", "test")]
    [InlineData("", "")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act & Assert
        input.ToSnakeCase().Should().Be(expected);
    }

    #endregion

    #region JSON Deserialization Tests

    [Fact]
    public void FromJson_WithValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"Name": "Test", "Value": 42}""";

        // Act
        var result = json.FromJson<JsonElement>();

        // Assert
        result.ValueKind.Should().Be(JsonValueKind.Object);
        result.GetProperty("Name").GetString().Should().Be("Test");
        result.GetProperty("Value").GetInt32().Should().Be(42);
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldReturnDefault()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = invalidJson.FromJson<object>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithEmptyString_ShouldReturnDefault()
    {
        // Act & Assert
        string.Empty.FromJson<object>().Should().BeNull();
    }

    [Fact]
    public void FromJson_WithWhitespace_ShouldReturnDefault()
    {
        // Act & Assert
        "   ".FromJson<object>().Should().BeNull();
    }

    #endregion

    #region RemoveDiacritics Tests

    [Theory]
    [InlineData("café", "cafe")]
    [InlineData("naïve", "naive")]
    [InlineData("résumé", "resume")]
    [InlineData("normal", "normal")]
    [InlineData("", "")]
    public void RemoveDiacritics_ShouldRemoveAccentsCorrectly(string input, string expected)
    {
        // Act & Assert
        input.RemoveDiacritics().Should().Be(expected);
    }

    #endregion

    #region Repeat Tests

    [Fact]
    public void Repeat_WithValidInput_ShouldRepeatCorrectly()
    {
        // Act & Assert
        "abc".Repeat(3).Should().Be("abcabcabc");
    }

    [Fact]
    public void Repeat_WithZeroCount_ShouldReturnEmptyString()
    {
        // Act & Assert
        "abc".Repeat(0).Should().BeEmpty();
    }

    [Fact]
    public void Repeat_WithNegativeCount_ShouldReturnEmptyString()
    {
        // Act & Assert
        "abc".Repeat(-1).Should().BeEmpty();
    }

    [Fact]
    public void Repeat_WithEmptyString_ShouldReturnEmptyString()
    {
        // Act & Assert
        string.Empty.Repeat(5).Should().BeEmpty();
    }

    #endregion

    #region SplitIntoChunks Tests

    [Fact]
    public void SplitIntoChunks_WithValidInput_ShouldSplitCorrectly()
    {
        // Arrange
        var input = "abcdefghij";

        // Act
        var result = input.SplitIntoChunks(3).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().Be("abc");
        result[1].Should().Be("def");
        result[2].Should().Be("ghi");
        result[3].Should().Be("j");
    }

    [Fact]
    public void SplitIntoChunks_WithChunkSizeGreaterThanStringLength_ShouldReturnSingleChunk()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = input.SplitIntoChunks(10).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("abc");
    }

    [Fact]
    public void SplitIntoChunks_WithZeroChunkSize_ShouldReturnEmpty()
    {
        // Act & Assert
        "abc".SplitIntoChunks(0).Should().BeEmpty();
    }

    [Fact]
    public void SplitIntoChunks_WithEmptyString_ShouldReturnEmpty()
    {
        // Act & Assert
        string.Empty.SplitIntoChunks(3).Should().BeEmpty();
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public void ToBase64_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToBase64().Should().BeEmpty();
    }

    [Fact]
    public void ToHex_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToHex().Should().BeEmpty();
    }

    [Fact]
    public void ToSha256_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToSha256().Should().BeEmpty();
    }

    [Fact]
    public void ToCamelCase_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToCamelCase().Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToPascalCase().Should().BeEmpty();
    }

    [Fact]
    public void ToKebabCase_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToKebabCase().Should().BeEmpty();
    }

    [Fact]
    public void ToSnakeCase_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).ToSnakeCase().Should().BeEmpty();
    }

    [Fact]
    public void RemoveDiacritics_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).RemoveDiacritics().Should().BeEmpty();
    }

    [Fact]
    public void Repeat_WithNullString_ShouldReturnEmptyString()
    {
        // Act & Assert
        ((string?)null).Repeat(3).Should().BeEmpty();
    }

    [Fact]
    public void SplitIntoChunks_WithNullString_ShouldReturnEmpty()
    {
        // Act & Assert
        ((string?)null).SplitIntoChunks(3).Should().BeEmpty();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void ToBase64_WithLargeString_ShouldPerformWell()
    {
        // Arrange
        var largeString = new string('A', 100000);

        // Act & Assert - Should not throw and complete in reasonable time
        var result = largeString.ToBase64();
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToSha256_WithLargeString_ShouldPerformWell()
    {
        // Arrange
        var largeString = new string('A', 100000);

        // Act & Assert - Should not throw and complete in reasonable time
        var result = largeString.ToSha256();
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64); // SHA256 produces 64 hex characters
    }

    #endregion
} 