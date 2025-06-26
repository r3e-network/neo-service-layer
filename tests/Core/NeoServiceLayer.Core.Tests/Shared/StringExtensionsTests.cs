using System.Text;
using System.Text.Json;
using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Shared;

/// <summary>
/// Tests for StringExtensions utility class to verify string operation behaviors.
/// </summary>
public class StringExtensionsTests
{
    #region Null/Empty/Whitespace Tests

    [Fact]
    public void IsNullOrEmpty_WithNullString_ShouldReturnTrue()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyString_ShouldReturnTrue()
    {
        // Arrange
        var emptyString = "";

        // Act
        var result = emptyString.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithValidString_ShouldReturnFalse()
    {
        // Arrange
        var validString = "test";

        // Act
        var result = validString.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNullOrWhiteSpace_WithWhitespaceString_ShouldReturnTrue()
    {
        // Arrange
        var whitespaceString = "   ";

        // Act
        var result = whitespaceString.IsNullOrWhiteSpace();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_WithValidString_ShouldReturnFalse()
    {
        // Arrange
        var validString = "test";

        // Act
        var result = validString.IsNullOrWhiteSpace();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Base64 Encoding Tests

    [Fact]
    public void ToBase64_WithValidString_ShouldReturnBase64()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.ToBase64();

        // Assert
        result.Should().Be("SGVsbG8gV29ybGQ=");
    }

    [Fact]
    public void ToBase64_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.ToBase64();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FromBase64_WithValidBase64_ShouldReturnOriginalString()
    {
        // Arrange
        var input = "SGVsbG8gV29ybGQ=";

        // Act
        var result = input.FromBase64();

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void FromBase64_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.FromBase64();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToBase64_FromBase64_RoundTrip_ShouldReturnOriginal()
    {
        // Arrange
        var original = "Test String with Special Characters: äöü@#$%";

        // Act
        var encoded = original.ToBase64();
        var decoded = encoded.FromBase64();

        // Assert
        decoded.Should().Be(original);
    }

    #endregion

    #region Hex Encoding Tests

    [Fact]
    public void ToHex_WithValidString_ShouldReturnHex()
    {
        // Arrange
        var input = "Hello";

        // Act
        var result = input.ToHex();

        // Assert
        result.Should().Be("48656c6c6f");
    }

    [Fact]
    public void ToHex_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.ToHex();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FromHex_WithValidHex_ShouldReturnOriginalString()
    {
        // Arrange
        var input = "48656c6c6f";

        // Act
        var result = input.FromHex();

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public void FromHex_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.FromHex();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToHex_FromHex_RoundTrip_ShouldReturnOriginal()
    {
        // Arrange
        var original = "Test123";

        // Act
        var hex = original.ToHex();
        var decoded = hex.FromHex();

        // Assert
        decoded.Should().Be(original);
    }

    #endregion

    #region Hash Tests

    [Fact]
    public void ToSha256_WithValidString_ShouldReturnConsistentHash()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.ToSha256();

        // Assert
        result.Should().Be("a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e");
        result.Should().HaveLength(64); // SHA256 is 64 hex characters
    }

    [Fact]
    public void ToSha256_WithNullString_ShouldReturnEmpty()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.ToSha256();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToMd5_WithValidString_ShouldReturnConsistentHash()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.ToMd5();

        // Assert
        result.Should().Be("b10a8db164e0754105b7a99be72e3fe5");
        result.Should().HaveLength(32); // MD5 is 32 hex characters
    }

    [Fact]
    public void ToMd5_WithNullString_ShouldReturnEmpty()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.ToMd5();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region String Manipulation Tests

    [Fact]
    public void Truncate_WithShortString_ShouldReturnOriginal()
    {
        // Arrange
        var input = "Short";

        // Act
        var result = input.Truncate(10);

        // Assert
        result.Should().Be("Short");
    }

    [Fact]
    public void Truncate_WithLongString_ShouldTruncateWithSuffix()
    {
        // Arrange
        var input = "This is a very long string that needs truncation";

        // Act
        var result = input.Truncate(20);

        // Assert
        result.Should().Be("This is a very lo...");
        result.Should().HaveLength(20);
    }

    [Fact]
    public void Truncate_WithCustomSuffix_ShouldUseCustomSuffix()
    {
        // Arrange
        var input = "Long string for testing";

        // Act
        var result = input.Truncate(10, " [more]");

        // Assert
        result.Should().Be("Lon [more]");
    }

    [Fact]
    public void Truncate_WithNullString_ShouldReturnEmpty()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.Truncate(10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Mask_WithValidString_ShouldMaskCorrectly()
    {
        // Arrange
        var input = "1234567890";

        // Act
        var result = input.Mask(2, 2);

        // Assert
        result.Should().Be("12******90");
    }

    [Fact]
    public void Mask_WithShortString_ShouldReturnAllMasked()
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

    #endregion

    #region Case Conversion Tests

    [Fact]
    public void ToCamelCase_WithValidString_ShouldConvertCorrectly()
    {
        // Arrange
        var input = "TestString";

        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be("testString");
    }

    [Fact]
    public void ToCamelCase_WithSingleChar_ShouldReturnLowercase()
    {
        // Arrange
        var input = "T";

        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be("t");
    }

    [Fact]
    public void ToCamelCase_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_WithValidString_ShouldConvertCorrectly()
    {
        // Arrange
        var input = "testString";

        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be("TestString");
    }

    [Fact]
    public void ToKebabCase_WithValidString_ShouldConvertCorrectly()
    {
        // Arrange
        var input = "TestStringValue";

        // Act
        var result = input.ToKebabCase();

        // Assert
        result.Should().Be("test-string-value");
    }

    [Fact]
    public void ToSnakeCase_WithValidString_ShouldConvertCorrectly()
    {
        // Arrange
        var input = "TestStringValue";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be("test_string_value");
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Matches_WithValidPattern_ShouldReturnTrue()
    {
        // Arrange
        var input = "test123";
        var pattern = @"^[a-z]+\d+$";

        // Act
        var result = input.Matches(pattern);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_WithInvalidPattern_ShouldReturnFalse()
    {
        // Arrange
        var input = "TEST123";
        var pattern = @"^[a-z]+\d+$";

        // Act
        var result = input.Matches(pattern);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var input = "";
        var pattern = @"^[a-z]+$";

        // Act
        var result = input.Matches(pattern);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidEmail_WithValidEmail_ShouldReturnTrue()
    {
        // Arrange
        var input = "test@example.com";

        // Act
        var result = input.IsValidEmail();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidEmail_WithInvalidEmail_ShouldReturnFalse()
    {
        // Arrange
        var input = "invalid-email";

        // Act
        var result = input.IsValidEmail();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidUrl_WithValidUrl_ShouldReturnTrue()
    {
        // Arrange
        var input = "https://example.com";

        // Act
        var result = input.IsValidUrl();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidUrl_WithInvalidUrl_ShouldReturnFalse()
    {
        // Arrange
        var input = "not-a-url";

        // Act
        var result = input.IsValidUrl();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidHex_WithValidHex_ShouldReturnTrue()
    {
        // Arrange
        var input = "abcdef123456";

        // Act
        var result = input.IsValidHex();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidHex_WithInvalidHex_ShouldReturnFalse()
    {
        // Arrange
        var input = "xyz123";

        // Act
        var result = input.IsValidHex();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidNeoAddress_WithValidNeoAddress_ShouldReturnTrue()
    {
        // Arrange - Example Neo address format
        var input = "NdxKRTQWgH5rFNnpVhfCktKb8WqCZVhprz";

        // Act
        var result = input.IsValidNeoAddress();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidNeoAddress_WithInvalidNeoAddress_ShouldReturnFalse()
    {
        // Arrange
        var input = "invalid-neo-address";

        // Act
        var result = input.IsValidNeoAddress();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidEthereumAddress_WithValidEthereumAddress_ShouldReturnTrue()
    {
        // Arrange
        var input = "0x742d35Cc6634C0532925a3b8D400ebBF70Ab8e8F";

        // Act
        var result = input.IsValidEthereumAddress();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidEthereumAddress_WithInvalidEthereumAddress_ShouldReturnFalse()
    {
        // Arrange
        var input = "invalid-ethereum-address";

        // Act
        var result = input.IsValidEthereumAddress();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region JSON Tests

    [Fact]
    public void FromJson_WithValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"Name":"Test","Value":123}""";

        // Act
        var result = json.FromJson<TestObject>();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(123);
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldReturnDefault()
    {
        // Arrange
        var invalidJson = "invalid json";

        // Act
        var result = invalidJson.FromJson<TestObject>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithEmptyString_ShouldReturnDefault()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = emptyJson.FromJson<TestObject>();

        // Assert
        result.Should().BeNull();
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    #endregion

    #region Utility Tests

    [Fact]
    public void RemoveDiacritics_WithAccentedString_ShouldRemoveAccents()
    {
        // Arrange
        var input = "Héllo Wörld";

        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void RemoveDiacritics_WithNormalString_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void RemoveDiacritics_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Repeat_WithValidParameters_ShouldRepeatCorrectly()
    {
        // Arrange
        var input = "Hello";

        // Act
        var result = input.Repeat(3);

        // Assert
        result.Should().Be("HelloHelloHello");
    }

    [Fact]
    public void Repeat_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var input = "Hello";

        // Act
        var result = input.Repeat(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Repeat_WithNegativeCount_ShouldReturnEmpty()
    {
        // Arrange
        var input = "Hello";

        // Act
        var result = input.Repeat(-1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SplitIntoChunks_WithValidParameters_ShouldSplitCorrectly()
    {
        // Arrange
        var input = "123456789";

        // Act
        var result = input.SplitIntoChunks(3).ToArray();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("123");
        result[1].Should().Be("456");
        result[2].Should().Be("789");
    }

    [Fact]
    public void SplitIntoChunks_WithUnevenSplit_ShouldHandleRemainder()
    {
        // Arrange
        var input = "1234567";

        // Act
        var result = input.SplitIntoChunks(3).ToArray();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("123");
        result[1].Should().Be("456");
        result[2].Should().Be("7");
    }

    [Fact]
    public void SplitIntoChunks_WithZeroChunkSize_ShouldReturnEmpty()
    {
        // Arrange
        var input = "123456";

        // Act
        var result = input.SplitIntoChunks(0).ToArray();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SplitIntoChunks_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.SplitIntoChunks(3).ToArray();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Encoding Tests with Different Encodings

    [Fact]
    public void ToBase64_WithCustomEncoding_ShouldUseSpecifiedEncoding()
    {
        // Arrange
        var input = "Hello";
        var encoding = Encoding.ASCII;

        // Act
        var result = input.ToBase64(encoding);

        // Assert
        result.Should().Be("SGVsbG8=");
    }

    [Fact]
    public void ToHex_WithCustomEncoding_ShouldUseSpecifiedEncoding()
    {
        // Arrange
        var input = "Hello";
        var encoding = Encoding.ASCII;

        // Act
        var result = input.ToHex(encoding);

        // Assert
        result.Should().Be("48656c6c6f");
    }

    [Fact]
    public void ToSha256_WithCustomEncoding_ShouldUseSpecifiedEncoding()
    {
        // Arrange
        var input = "Hello";
        var encoding = Encoding.ASCII;

        // Act
        var result = input.ToSha256(encoding);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveLength(64);
    }

    #endregion
}
