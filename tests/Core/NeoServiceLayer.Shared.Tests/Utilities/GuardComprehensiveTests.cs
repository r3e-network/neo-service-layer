using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Shared.Tests.Utilities
{
    /// <summary>
    /// Comprehensive unit tests for Guard utility class.
    /// Tests all guard conditions, edge cases, and exception scenarios.
    /// </summary>
    public class GuardComprehensiveTests
    {
        [Fact]
        public void NotNull_WithNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            object? nullValue = null;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                Guard.NotNull(nullValue, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void NotNull_WithValidValue_ShouldReturnValue()
        {
            // Arrange
            var validValue = "test";
            string paramName = "testParam";

            // Act
            var result = Guard.NotNull(validValue, paramName);

            // Assert
            result.Should().Be(validValue);
        }

        [Fact]
        public void NotNull_WithEmptyString_ShouldReturnString()
        {
            // Arrange
            var emptyString = "";
            string paramName = "testParam";

            // Act
            var result = Guard.NotNull(emptyString, paramName);

            // Assert
            result.Should().Be(emptyString);
        }

        [Fact]
        public void NotNull_WithZeroInteger_ShouldReturnInteger()
        {
            // Arrange
            int? zeroValue = 0;
            string paramName = "testParam";

            // Act
            var result = Guard.NotNull(zeroValue, paramName);

            // Assert
            result.Should().Be(zeroValue.Value);
        }

        [Theory]
        [InlineData("validString")]
        [InlineData("another value")]
        [InlineData("123")]
        public void NotNull_WithVariousValidStrings_ShouldReturnString(string value)
        {
            // Act
            var result = Guard.NotNull(value, "param");

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void NotNullOrEmpty_WithNullString_ShouldThrowArgumentException()
        {
            // Arrange
            string? nullString = null;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrEmpty(nullString, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void NotNullOrEmpty_WithEmptyString_ShouldThrowArgumentException()
        {
            // Arrange
            string emptyString = string.Empty;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrEmpty(emptyString, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void NotNullOrEmpty_WithValidString_ShouldReturnString()
        {
            // Arrange
            string validString = "test";
            string paramName = "testParam";

            // Act
            var result = Guard.NotNullOrEmpty(validString, paramName);

            // Assert
            result.Should().Be(validString);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void NotNullOrEmpty_WithWhitespaceString_ShouldReturnString(string whitespace)
        {
            // Act
            var result = Guard.NotNullOrEmpty(whitespace, "param");

            // Assert
            result.Should().Be(whitespace);
        }

        [Fact]
        public void NotNullOrWhiteSpace_WithNullString_ShouldThrowArgumentException()
        {
            // Arrange
            string? nullString = null;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrWhiteSpace(nullString, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void NotNullOrWhiteSpace_WithEmptyString_ShouldThrowArgumentException()
        {
            // Arrange
            string emptyString = string.Empty;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrWhiteSpace(emptyString, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("   \t\r\n   ")]
        public void NotNullOrWhiteSpace_WithWhitespaceString_ShouldThrowArgumentException(string whitespace)
        {
            // Arrange
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrWhiteSpace(whitespace, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData("valid")]
        [InlineData("a")]
        [InlineData("test string with spaces")]
        [InlineData("123456")]
        public void NotNullOrWhiteSpace_WithValidString_ShouldReturnString(string validString)
        {
            // Act
            var result = Guard.NotNullOrWhiteSpace(validString, "param");

            // Assert
            result.Should().Be(validString);
        }

        [Fact]
        public void AgainstNegative_WithNegativeValue_ShouldThrowArgumentException()
        {
            // Arrange
            int negativeValue = -1;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.AgainstNegative(negativeValue, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void AgainstNegative_WithNonNegativeValue_ShouldReturnValue(int value)
        {
            // Act
            var result = Guard.AgainstNegative(value, "param");

            // Assert
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void AgainstNegative_WithVariousNegativeValues_ShouldThrowArgumentException(int negativeValue)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                Guard.AgainstNegative(negativeValue, "param"));
        }

        [Fact]
        public void AgainstZeroOrNegative_WithZeroValue_ShouldThrowArgumentException()
        {
            // Arrange
            int zeroValue = 0;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.AgainstZeroOrNegative(zeroValue, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void AgainstZeroOrNegative_WithPositiveValue_ShouldReturnValue(int value)
        {
            // Act
            var result = Guard.AgainstZeroOrNegative(value, "param");

            // Assert
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void AgainstZeroOrNegative_WithNegativeValue_ShouldThrowArgumentException(int negativeValue)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                Guard.AgainstZeroOrNegative(negativeValue, "param"));
        }

        [Fact]
        public void Against_WithTrueCondition_ShouldThrowArgumentException()
        {
            // Arrange
            bool condition = true;
            string paramName = "testParam";
            string message = "Test message";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.Against(condition, paramName, message));
            exception.ParamName.Should().Be(paramName);
            exception.Message.Should().Contain(message);
        }

        [Fact]
        public void Against_WithFalseCondition_ShouldNotThrow()
        {
            // Arrange
            bool condition = false;
            string paramName = "testParam";
            string message = "Test message";

            // Act & Assert
            var action = () => Guard.Against(condition, paramName, message);
            action.Should().NotThrow();
        }

        [Theory]
        [InlineData(true, "Should throw")]
        [InlineData(true, "Another message")]
        public void Against_WithTrueConditionVariousMessages_ShouldThrowWithMessage(bool condition, string message)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.Against(condition, "param", message));
            exception.Message.Should().Contain(message);
        }

        [Theory]
        [InlineData(false)]
        public void Against_WithFalseConditionVariousScenarios_ShouldNotThrow(bool condition)
        {
            // Act & Assert
            var action = () => Guard.Against(condition, "param", "message");
            action.Should().NotThrow();
        }

        [Fact]
        public void AgainstOutOfRange_WithValueBelowMinimum_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            int value = 5;
            int min = 10;
            int max = 20;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
                Guard.AgainstOutOfRange(value, min, max, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void AgainstOutOfRange_WithValueAboveMaximum_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            int value = 25;
            int min = 10;
            int max = 20;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
                Guard.AgainstOutOfRange(value, min, max, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(20)]
        public void AgainstOutOfRange_WithValueInRange_ShouldReturnValue(int value)
        {
            // Arrange
            int min = 10;
            int max = 20;

            // Act
            var result = Guard.AgainstOutOfRange(value, min, max, "param");

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void AgainstInvalidEnumValue_WithInvalidEnumValue_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidEnum = (DayOfWeek)999;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.AgainstInvalidEnumValue(invalidEnum, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Friday)]
        public void AgainstInvalidEnumValue_WithValidEnumValue_ShouldReturnValue(DayOfWeek validEnum)
        {
            // Act
            var result = Guard.AgainstInvalidEnumValue(validEnum, "param");

            // Assert
            result.Should().Be(validEnum);
        }

        [Fact]
        public void AgainstEmptyCollection_WithEmptyCollection_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyCollection = new List<string>();
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.AgainstEmptyCollection(emptyCollection, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void AgainstEmptyCollection_WithNullCollection_ShouldThrowArgumentNullException()
        {
            // Arrange
            List<string>? nullCollection = null;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                Guard.AgainstEmptyCollection(nullCollection, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void AgainstEmptyCollection_WithNonEmptyCollection_ShouldReturnCollection(int itemCount)
        {
            // Arrange
            var collection = Enumerable.Range(1, itemCount).Select(i => $"item{i}").ToList();

            // Act
            var result = Guard.AgainstEmptyCollection(collection, "param");

            // Assert
            result.Should().BeEquivalentTo(collection);
        }

        [Fact]
        public void AgainstNullOrEmptyCollection_WithNullCollection_ShouldThrowArgumentException()
        {
            // Arrange
            List<string>? nullCollection = null;
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrEmptyCollection(nullCollection, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Fact]
        public void AgainstNullOrEmptyCollection_WithEmptyCollection_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyCollection = new List<string>();
            string paramName = "testParam";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                Guard.NotNullOrEmptyCollection(emptyCollection, paramName));
            exception.ParamName.Should().Be(paramName);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void AgainstNullOrEmptyCollection_WithValidCollection_ShouldReturnCollection(int itemCount)
        {
            // Arrange
            var collection = Enumerable.Range(1, itemCount).Select(i => $"item{i}").ToList();

            // Act
            var result = Guard.NotNullOrEmptyCollection(collection, "param");

            // Assert
            result.Should().BeEquivalentTo(collection);
        }
    }
}