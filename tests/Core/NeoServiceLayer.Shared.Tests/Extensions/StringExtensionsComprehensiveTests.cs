using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Shared.Tests.Extensions
{
    /// <summary>
    /// Comprehensive unit tests for StringExtensions.
    /// Tests all string extension methods, edge cases, and validation scenarios.
    /// </summary>
    public class StringExtensionsComprehensiveTests
    {
        #pragma warning disable CA5350 // Do not use weak cryptographic algorithms in tests
        private readonly SHA1 sha1 = SHA1.Create();
        #pragma warning restore CA5350
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
            string emptyString = string.Empty;

            // Act
            var result = emptyString.IsNullOrEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("a")]
        public void IsNullOrEmpty_WithNonEmptyString_ShouldReturnFalse(string value)
        {
            // Act
            var result = value.IsNullOrEmpty();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNullOrWhiteSpace_WithNullString_ShouldReturnTrue()
        {
            // Arrange
            string? nullString = null;

            // Act
            var result = nullString.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullOrWhiteSpace_WithEmptyString_ShouldReturnTrue()
        {
            // Arrange
            string emptyString = string.Empty;

            // Act
            var result = emptyString.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("   \t\r\n   ")]
        public void IsNullOrWhiteSpace_WithWhitespaceString_ShouldReturnTrue(string whitespace)
        {
            // Act
            var result = whitespace.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        [InlineData("hello world")]
        [InlineData("123")]
        public void IsNullOrWhiteSpace_WithValidString_ShouldReturnFalse(string value)
        {
            // Act
            var result = value.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("World", "WORLD")]
        [InlineData("TeSt", "TEST")]
        [InlineData("", "")]
        [InlineData("123", "123")]
        public void ToUpperInvariant_ShouldReturnUppercaseString(string input, string expected)
        {
            // Act
            var result = input.ToUpperInvariant();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("HELLO", "hello")]
        [InlineData("WORLD", "world")]
        [InlineData("TeSt", "test")]
        [InlineData("", "")]
        [InlineData("123", "123")]
        public void ToLowerInvariant_ShouldReturnLowercaseString(string input, string expected)
        {
            // Act
            var result = input.ToLowerInvariant();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello world", "Hello World")]
        [InlineData("TEST STRING", "Test String")]
        [InlineData("a", "A")]
        [InlineData("", "")]
        [InlineData("multiple word string", "Multiple Word String")]
        public void ToTitleCase_ShouldReturnTitleCaseString(string input, string expected)
        {
            // Act
            var result = input.ToTitleCase();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", "Hello")]
        [InlineData("world", "World")]
        [InlineData("t", "T")]
        [InlineData("", "")]
        [InlineData("UPPERCASE", "UPPERCASE")]
        public void Capitalize_ShouldCapitalizeFirstLetter(string input, string expected)
        {
            // Act
            var result = input.Capitalize();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello world", 5, "hello")]
        [InlineData("test string", 4, "test")]
        [InlineData("short", 10, "short")]
        [InlineData("", 5, "")]
        [InlineData("a", 1, "a")]
        public void Truncate_ShouldTruncateToSpecifiedLength(string input, int maxLength, string expected)
        {
            // Act
            var result = input.Truncate(maxLength);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello world", 5, "he...")]
        [InlineData("test string", 7, "test...")]
        [InlineData("short", 10, "short")]
        [InlineData("", 5, "")]
        [InlineData("a", 4, "a")]
        public void TruncateWithEllipsis_ShouldTruncateWithEllipsis(string input, int maxLength, string expected)
        {
            // Act
            var result = input.TruncateWithEllipsis(maxLength);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", "ell", true)]
        [InlineData("world", "WORLD", true)]
        [InlineData("Test", "test", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData("", "", true)]
        public void ContainsIgnoreCase_ShouldPerformCaseInsensitiveContains(string input, string value, bool expected)
        {
            // Act
            var result = input.ContainsIgnoreCase(value);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", "HELLO", true)]
        [InlineData("world", "World", true)]
        [InlineData("Test", "test", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData("", "", true)]
        public void EqualsIgnoreCase_ShouldPerformCaseInsensitiveEquality(string input, string value, bool expected)
        {
            // Act
            var result = input.EqualsIgnoreCase(value);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", "HELLO", true)]
        [InlineData("world", "Wor", true)]
        [InlineData("Test", "TES", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData("", "", true)]
        public void StartsWithIgnoreCase_ShouldPerformCaseInsensitiveStartsWith(string input, string value, bool expected)
        {
            // Act
            var result = input.StartsWithIgnoreCase(value);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", "LLO", true)]
        [InlineData("world", "RLD", true)]
        [InlineData("Test", "est", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData("", "", true)]
        public void EndsWithIgnoreCase_ShouldPerformCaseInsensitiveEndsWith(string input, string value, bool expected)
        {
            // Act
            var result = input.EndsWithIgnoreCase(value);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToBase64_WithValidString_ShouldReturnBase64EncodedString()
        {
            // Arrange
            string input = "Hello World";
            string expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

            // Act
            var result = input.ToBase64();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToBase64_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            string input = string.Empty;

            // Act
            var result = input.ToBase64();

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void FromBase64_WithValidBase64String_ShouldReturnDecodedString()
        {
            // Arrange
            string originalString = "Hello World";
            string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalString));

            // Act
            var result = base64String.FromBase64();

            // Assert
            result.Should().Be(originalString);
        }

        [Fact]
        public void FromBase64_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            string input = string.Empty;

            // Act
            var result = input.FromBase64();

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void FromBase64_WithInvalidBase64String_ShouldThrowFormatException()
        {
            // Arrange
            string invalidBase64 = "Invalid Base64!@#";

            // Act & Assert
            Assert.Throws<FormatException>(() => invalidBase64.FromBase64());
        }

        [Theory]
        [InlineData("hello@example.com", true)]
        [InlineData("user.name+tag@domain.co.uk", true)]
        [InlineData("test123@test.org", true)]
        [InlineData("invalid.email", false)]
        [InlineData("@domain.com", false)]
        [InlineData("user@", false)]
        [InlineData("", false)]
        public void IsValidEmail_ShouldValidateEmailFormat(string email, bool expected)
        {
            // Act
            var result = email.IsValidEmail();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("https://www.example.com", true)]
        [InlineData("http://test.org", true)]
        [InlineData("ftp://ftp.example.com", false)]
        [InlineData("invalid-url", false)]
        [InlineData("www.example.com", false)]
        [InlineData("", false)]
        public void IsValidUrl_ShouldValidateUrlFormat(string url, bool expected)
        {
            // Act
            var result = url.IsValidUrl();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("12345", true)]
        [InlineData("123.45", true)]
        [InlineData("-123", true)]
        [InlineData("abc", false)]
        [InlineData("12a34", false)]
        [InlineData("", false)]
        public void IsNumeric_ShouldValidateNumericString(string value, bool expected)
        {
            // Act
            var result = value.IsNumeric();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToMD5Hash_WithValidString_ShouldReturnMD5Hash()
        {
            // Arrange
            string input = "Hello World";
            string expected = "b10a8db164e0754105b7a99be72e3fe5";

            // Act
            var result = input.ToMD5Hash();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToSHA1Hash_WithValidString_ShouldReturnSHA1Hash()
        {
            // Arrange
            string input = "Hello World";
            byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            string expected = Convert.ToHexString(hashBytes).ToLowerInvariant();

            // Act
            var result = input.ToSHA1Hash();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToSHA256Hash_WithValidString_ShouldReturnSHA256Hash()
        {
            // Arrange
            string input = "Hello World";
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            string expected = Convert.ToHexString(hashBytes).ToLowerInvariant();

            // Act
            var result = input.ToSHA256Hash();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("  hello  ", "hello")]
        [InlineData("\thello\t", "hello")]
        [InlineData("\nhello\n", "hello")]
        [InlineData(" \t\r\n hello \t\r\n ", "hello")]
        [InlineData("hello", "hello")]
        public void TrimSafe_ShouldTrimWhitespaceCharacters(string input, string expected)
        {
            // Act
            var result = input.TrimSafe();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void TrimSafe_WithNullString_ShouldReturnEmptyString()
        {
            // Arrange
            string? nullString = null;

            // Act
            var result = nullString.TrimSafe();

            // Assert
            result.Should().Be(string.Empty);
        }

        [Theory]
        [InlineData("hello world", "-", "hello-world")]
        [InlineData("test string example", "_", "test_string_example")]
        [InlineData("single", "-", "single")]
        [InlineData("", "-", "")]
        public void ReplaceSpaces_ShouldReplaceSpacesWithSpecifiedCharacter(string input, string replacement, string expected)
        {
            // Act
            var result = input.ReplaceSpaces(replacement);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", 10, "hello     ")]
        [InlineData("test", 8, "test    ")]
        [InlineData("toolong", 3, "too")]
        [InlineData("", 5, "     ")]
        public void PadRightToLength_ShouldPadOrTruncateToSpecifiedLength(string input, int length, string expected)
        {
            // Act
            var result = input.PadRightToLength(length);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", 10, "     hello")]
        [InlineData("test", 8, "    test")]
        [InlineData("toolong", 3, "too")]
        [InlineData("", 5, "     ")]
        public void PadLeftToLength_ShouldPadOrTruncateToSpecifiedLength(string input, int length, string expected)
        {
            // Act
            var result = input.PadLeftToLength(length);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello world", 2, new[] { "hello", "world" })]
        [InlineData("one,two,three", 3, new[] { "one", "two", "three" })]
        [InlineData("single", 1, new[] { "single" })]
        [InlineData("", 0, new string[0])]
        public void SafeSplit_ShouldSplitStringCorrectly(string input, int expectedCount, string[] expected)
        {
            // Act
            var result = input.SafeSplit(' ', ',');

            // Assert
            result.Should().HaveCount(expectedCount);
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void SafeSplit_WithNullString_ShouldReturnEmptyArray()
        {
            // Arrange
            string? nullString = null;

            // Act
            var result = nullString.SafeSplit(',');

            // Assert
            result.Should().BeEmpty();
        }
    }
}