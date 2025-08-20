using System.Text.Json;
using System.Text;
using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Shared.Tests.Extensions;

/// <summary>
/// Comprehensive tests for StringExtensions covering all methods, edge cases, and error scenarios.
/// </summary>
public class StringExtensionsTests
{
    #region Null/Empty/Whitespace Tests

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("test", false)]
    [InlineData(" ", false)]
    public void IsNullOrEmpty_ShouldReturnExpectedResult(string? input, bool expected)
    {
        // Act & Assert
        input.IsNullOrEmpty().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("\t\n\r", true)]
    [InlineData("test", false)]
    [InlineData(" test ", false)]
    public void IsNullOrWhiteSpace_ShouldReturnExpectedResult(string? input, bool expected)
    {
        // Act & Assert
        input.IsNullOrWhiteSpace().Should().Be(expected);
    }

    #endregion

    #region Base64 Encoding/Decoding Tests

    [Theory]
    [InlineData("Hello World", "SGVsbG8gV29ybGQ=")]
    [InlineData("The quick brown fox", "VGhlIHF1aWNrIGJyb3duIGZveA==")]
    [InlineData("", "")]
    [InlineData("A", "QQ==")]
    public void ToBase64_ShouldEncodeCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToBase64();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=", "Hello World")]
    [InlineData("VGhlIHF1aWNrIGJyb3duIGZveA==", "The quick brown fox")]
    [InlineData("", "")]
    [InlineData("QQ==", "A")]
    public void FromBase64_ShouldDecodeCorrectly(string input, string expected)
    {
        // Act
        var result = input.FromBase64();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToBase64_WithCustomEncoding_ShouldEncodeCorrectly()
    {
        // Arrange
        var input = "Hello 世界";
        var encoding = Encoding.UTF8;

        // Act
        var result = input.ToBase64(encoding);

        // Assert
        var decoded = result.FromBase64(encoding);
        decoded.Should().Be(input);
    }

    [Fact]
    public void FromBase64_WithInvalidInput_ShouldThrowFormatException()
    {
        // Arrange
        var invalidBase64 = "InvalidBase64!@#";

        // Act & Assert
        Assert.Throws<FormatException>(() => invalidBase64.FromBase64());
    }

    #endregion

    #region Hex Encoding/Decoding Tests

    [Theory]
    [InlineData("Hello", "48656c6c6f")]
    [InlineData("World", "576f726c64")]
    [InlineData("", "")]
    [InlineData("A", "41")]
    public void ToHex_ShouldEncodeCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToHex();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("48656c6c6f", "Hello")]
    [InlineData("576f726c64", "World")]
    [InlineData("", "")]
    [InlineData("41", "A")]
    public void FromHex_ShouldDecodeCorrectly(string input, string expected)
    {
        // Act
        var result = input.FromHex();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToHex_WithCustomEncoding_ShouldEncodeCorrectly()
    {
        // Arrange
        var input = "Hello 世界";
        var encoding = Encoding.UTF8;

        // Act
        var result = input.ToHex(encoding);

        // Assert
        var decoded = result.FromHex(encoding);
        decoded.Should().Be(input);
    }

    [Fact]
    public void FromHex_WithInvalidInput_ShouldThrowFormatException()
    {
        // Arrange
        var invalidHex = "InvalidHex!@#";

        // Act & Assert
        Assert.Throws<FormatException>(() => invalidHex.FromHex());
    }

    #endregion

    #region Hash Tests

    [Theory]
    [InlineData("Hello World", "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e")]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("Test", "532eaabd9574880dbf76b9b8cc00832c20a6ec113d682299550d7a6e0f345e25")]
    public void ToSha256_ShouldGenerateCorrectHash(string input, string expectedHash)
    {
        // Act
        var result = input.ToSha256();

        // Assert
        result.Should().Be(expectedHash);
    }

    [Theory]
    [InlineData("Hello World", "b10a8db164e0754105b7a99be72e3fe5")]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("Test", "0cbc6611f5540bd0809a388dc95a615b")]
    public void ToMd5_ShouldGenerateCorrectHash(string input, string expectedHash)
    {
        // Act
        var result = input.ToMd5();

        // Assert
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void ToSha256_WithCustomEncoding_ShouldHashCorrectly()
    {
        // Arrange
        var input = "Hello 世界";
        var encoding = Encoding.UTF8;

        // Act
        var result = input.ToSha256(encoding);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64); // SHA256 produces 64 character hex string
    }

    [Fact]
    public void ToSha256_WithEmptyString_ShouldReturnCorrectHash()
    {
        // Act
        var result = string.Empty.ToSha256();

        // Assert - SHA256 of empty string
        result.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public void ToSha256_WithNullString_ShouldReturnEmptyString()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.ToSha256();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToSha256_WithTestString_ShouldReturnCorrectHash()
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

    [Theory]
    [InlineData("Hello World", 5, "...", "He...")]
    [InlineData("Hello World", 11, "...", "Hello World")]
    [InlineData("Hello World", 15, "...", "Hello World")]
    [InlineData("Hello World", 8, " [more]", "H [more]")]
    [InlineData("", 5, "...", "")]
    public void Truncate_ShouldTruncateCorrectly(string input, int maxLength, string suffix, string expected)
    {
        // Act
        var result = input.Truncate(maxLength, suffix);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_WithZeroLength_ShouldReturnSuffix()
    {
        // Arrange
        var input = "Hello World";
        var suffix = "...";

        // Act
        var result = input.Truncate(0, suffix);

        // Assert
        result.Should().Be(suffix);
    }

    #endregion

    #region Mask Tests

    [Theory]
    [InlineData("1234567890", 2, 2, '*', "12******90")]
    [InlineData("abc", 1, 1, 'X', "aXc")]
    [InlineData("12", 1, 1, '*', "**")]
    [InlineData("1", 2, 2, '*', "*")]
    [InlineData("", 2, 2, '*', "")]
    public void Mask_ShouldMaskCorrectly(string input, int visibleStart, int visibleEnd, char maskChar, string expected)
    {
        // Act
        var result = input.Mask(visibleStart, visibleEnd, maskChar);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Mask_WithCreditCardNumber_ShouldMaskSensitiveData()
    {
        // Arrange
        var creditCard = "1234567890123456";

        // Act
        var result = creditCard.Mask(4, 4);

        // Assert
        result.Should().Be("1234********3456");
    }

    #endregion

    #region Pattern Matching Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("user@domain", false)]
    [InlineData("user@domain.co.uk", true)]
    [InlineData("", false)]
    public void IsValidEmail_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = input.IsValidEmail();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://test.org", true)]
    [InlineData("ftp://invalid.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void IsValidUrl_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = input.IsValidUrl();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", false)] // Empty string is not valid hex
    [InlineData("123abc", true)]
    [InlineData("ABCDEF", true)]
    [InlineData("123XYZ", false)]
    [InlineData("0x123abc", false)] // Should not include 0x prefix
    public void IsValidHex_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = input.IsValidHex();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx", true)]  // Valid Neo N3 address
    [InlineData("AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y", true)]  // Valid Neo Legacy address
    [InlineData("NiNmXL8FjEUEs1nfX9uHFBNaenxDHJtmuB", true)]  // Valid Neo N3 address
    [InlineData("AbL9qXdkBrF8nrXw2JcN5R7H6eK6x8m4", false)]    // Too short
    [InlineData("BTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx", false)]  // Invalid prefix
    [InlineData("", false)]
    [InlineData("invalid", false)]
    public void IsValidNeoAddress_ShouldValidateNeoAddressCorrectly(string address, bool expected)
    {
        // Act & Assert
        address.IsValidNeoAddress().Should().Be(expected);
    }

    [Theory]
    [InlineData("0x742d35cc6570c38c0b6d96b1c93f63bfaacf67d2", true)]
    [InlineData("0x1234567890abcdef1234567890abcdef12345678", true)]
    [InlineData("742d35cc6570c38c0b6d96b1c93f63bfaacf67d2", false)] // Missing 0x prefix
    [InlineData("0xinvalid", false)]
    [InlineData("", false)]
    public void IsValidEthereumAddress_ShouldValidateCorrectly(string input, bool expected)
    {
        // Act
        var result = input.IsValidEthereumAddress();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Case Conversion Tests

    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("PascalCase", "pascalCase")]
    [InlineData("A", "a")]
    [InlineData("", "")]
    [InlineData("already_lowercase", "already_lowercase")]
    public void ToCamelCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("helloWorld", "HelloWorld")]
    [InlineData("camelCase", "CamelCase")]
    [InlineData("a", "A")]
    [InlineData("", "")]
    [InlineData("Already_uppercase", "Already_uppercase")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("PascalCase", "pascal-case")]
    [InlineData("already-kebab", "already-kebab")]
    [InlineData("", "")]
    public void ToKebabCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToKebabCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("PascalCase", "pascal_case")]
    [InlineData("already_snake", "already_snake")]
    [InlineData("", "")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region JSON Deserialization Tests

    [Fact]
    public void FromJson_WithValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"Name": "Test", "Value": 42}""";

        // Act
        var result = json.FromJson<TestModel>();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldReturnDefault()
    {
        // Arrange
        var invalidJson = "invalid json";

        // Act
        var result = invalidJson.FromJson<TestModel>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithEmptyString_ShouldReturnDefault()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = emptyJson.FromJson<TestModel>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithWhitespace_ShouldReturnDefault()
    {
        // Arrange
        var whitespaceJson = "   ";

        // Act
        var result = whitespaceJson.FromJson<TestModel>();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Diacritics Tests

    [Theory]
    [InlineData("café", "cafe")]
    [InlineData("naïve", "naive")]
    [InlineData("résumé", "resume")]
    [InlineData("Zürich", "Zurich")]
    [InlineData("no diacritics", "no diacritics")]
    [InlineData("", "")]
    public void RemoveDiacritics_ShouldRemoveAccents(string input, string expected)
    {
        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Repeat Tests

    [Theory]
    [InlineData("ab", 3, "ababab")]
    [InlineData("x", 5, "xxxxx")]
    [InlineData("", 3, "")]
    [InlineData("test", 0, "")]
    [InlineData("test", -1, "")]
    public void Repeat_ShouldRepeatCorrectly(string input, int count, string expected)
    {
        // Act
        var result = input.Repeat(count);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Split Into Chunks Tests

    [Fact]
    public void SplitIntoChunks_ShouldSplitCorrectly()
    {
        // Arrange
        var input = "abcdefghij";
        var chunkSize = 3;

        // Act
        var result = input.SplitIntoChunks(chunkSize).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().Be("abc");
        result[1].Should().Be("def");
        result[2].Should().Be("ghi");
        result[3].Should().Be("j");
    }

    [Fact]
    public void SplitIntoChunks_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";
        var chunkSize = 3;

        // Act
        var result = input.SplitIntoChunks(chunkSize).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SplitIntoChunks_WithZeroChunkSize_ShouldReturnEmpty()
    {
        // Arrange
        var input = "test";
        var chunkSize = 0;

        // Act
        var result = input.SplitIntoChunks(chunkSize).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Classes

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    #endregion
}

public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
