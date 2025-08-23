using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using Xunit;

namespace NeoServiceLayer.Shared.Tests.Utilities;

public class GuardUnitTests
{
    [Fact]
    public void NotNull_WithValidObject_ReturnsObject()
    {
        var testObject = "test";
        
        var result = Guard.NotNull(testObject);
        
        result.Should().Be(testObject);
    }

    [Fact]
    public void NotNull_WithNullObject_ThrowsArgumentNullException()
    {
        string testObject = null;
        
        Action act = () => Guard.NotNull(testObject);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNull_WithNullableStruct_ReturnsValue()
    {
        int? testValue = 42;
        
        var result = Guard.NotNull(testValue);
        
        result.Should().Be(42);
    }

    [Fact]
    public void NotNull_WithNullNullableStruct_ThrowsArgumentNullException()
    {
        int? testValue = null;
        
        Action act = () => Guard.NotNull(testValue);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    public void NotNullOrEmpty_WithEmptyString_ThrowsArgumentException(string input)
    {
        Action act = () => Guard.NotNullOrEmpty(input);
        
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be null or empty*");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void NotNullOrEmpty_WithWhitespaceString_ReturnsString(string input)
    {
        var result = Guard.NotNullOrEmpty(input);
        
        result.Should().Be(input);
    }

    [Fact]
    public void NotNullOrEmpty_WithValidString_ReturnsString()
    {
        var input = "valid string";
        
        var result = Guard.NotNullOrEmpty(input);
        
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_WithWhitespaceString_ThrowsArgumentException(string input)
    {
        Action act = () => Guard.NotNullOrWhiteSpace(input);
        
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be null or whitespace*");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithValidString_ReturnsString()
    {
        var input = "valid string";
        
        var result = Guard.NotNullOrWhiteSpace(input);
        
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void AgainstNegative_WithNegativeValue_ThrowsArgumentOutOfRangeException(int value)
    {
        Action act = () => Guard.AgainstNegative(value);
        
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*cannot be negative*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void AgainstNegative_WithNonNegativeValue_ReturnsValue(int value)
    {
        var result = Guard.AgainstNegative(value);
        
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AgainstZeroOrNegative_WithZeroOrNegativeValue_ThrowsArgumentOutOfRangeException(int value)
    {
        Action act = () => Guard.AgainstZeroOrNegative(value);
        
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*must be greater than zero*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void AgainstZeroOrNegative_WithPositiveValue_ReturnsValue(int value)
    {
        var result = Guard.AgainstZeroOrNegative(value);
        
        result.Should().Be(value);
    }

    [Fact]
    public void Against_WithTrueCondition_ThrowsArgumentException()
    {
        Action act = () => Guard.Against(true, "Test message");
        
        act.Should().Throw<ArgumentException>().WithMessage("Test message*");
    }

    [Fact]
    public void Against_WithFalseCondition_DoesNotThrow()
    {
        Action act = () => Guard.Against(false, "Test message");
        
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(5, 1, 10)] // Within range
    [InlineData(1, 1, 10)] // At minimum
    [InlineData(10, 1, 10)] // At maximum
    public void AgainstOutOfRange_WithValueInRange_ReturnsValue(int value, int min, int max)
    {
        var result = Guard.AgainstOutOfRange(value, min, max);
        
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0, 1, 10)] // Below minimum
    [InlineData(11, 1, 10)] // Above maximum
    public void AgainstOutOfRange_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(int value, int min, int max)
    {
        Action act = () => Guard.AgainstOutOfRange(value, min, max);
        
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AgainstInvalidEnumValue_WithValidEnum_ReturnsValue()
    {
        var enumValue = StringComparison.OrdinalIgnoreCase;
        
        var result = Guard.AgainstInvalidEnumValue(enumValue);
        
        result.Should().Be(enumValue);
    }

    [Fact]
    public void AgainstInvalidEnumValue_WithInvalidEnum_ThrowsArgumentException()
    {
        var invalidEnum = (StringComparison)999;
        
        Action act = () => Guard.AgainstInvalidEnumValue(invalidEnum);
        
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid enum value*");
    }

    [Fact]
    public void AgainstEmptyCollection_WithNonEmptyCollection_ReturnsCollection()
    {
        var collection = new List<int> { 1, 2, 3 };
        
        var result = Guard.AgainstEmptyCollection(collection);
        
        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void AgainstEmptyCollection_WithEmptyCollection_ThrowsArgumentException()
    {
        var collection = new List<int>();
        
        Action act = () => Guard.AgainstEmptyCollection(collection);
        
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Fact]
    public void AgainstEmptyCollection_WithNullCollection_ThrowsArgumentNullException()
    {
        List<int> collection = null;
        
        Action act = () => Guard.AgainstEmptyCollection(collection);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNullOrEmptyCollection_WithValidCollection_ReturnsCollection()
    {
        var collection = new List<int> { 1, 2, 3 };
        
        var result = Guard.NotNullOrEmptyCollection(collection);
        
        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void AgainstNullOrEmptyCollection_WithValidEnumerable_ReturnsEnumerable()
    {
        var enumerable = new[] { 1, 2, 3 }.AsEnumerable();
        
        var result = Guard.AgainstNullOrEmptyCollection(enumerable);
        
        result.Should().BeSameAs(enumerable);
    }

    [Fact]
    public void AgainstNullOrEmptyCollection_WithNullEnumerable_ThrowsArgumentNullException()
    {
        IEnumerable<int> enumerable = null;
        
        Action act = () => Guard.AgainstNullOrEmptyCollection(enumerable);
        
        act.Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
    }

    [Fact]
    public void AgainstNullOrEmptyCollection_WithEmptyEnumerable_ThrowsArgumentException()
    {
        var enumerable = Enumerable.Empty<int>();
        
        Action act = () => Guard.AgainstNullOrEmptyCollection(enumerable);
        
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Theory]
    [InlineData(1.5, 1.0, 2.0)]
    [InlineData(1.0, 1.0, 2.0)]
    [InlineData(2.0, 1.0, 2.0)]
    public void AgainstOutOfRange_WithDoubleInRange_ReturnsValue(double value, double min, double max)
    {
        var result = Guard.AgainstOutOfRange(value, min, max);
        
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0.9, 1.0, 2.0)]
    [InlineData(2.1, 1.0, 2.0)]
    public void AgainstOutOfRange_WithDoubleOutOfRange_ThrowsArgumentOutOfRangeException(double value, double min, double max)
    {
        Action act = () => Guard.AgainstOutOfRange(value, min, max);
        
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AgainstNegative_WithDoubleValue_WorksCorrectly()
    {
        var validValue = 1.5;
        var result = Guard.AgainstNegative(validValue);
        result.Should().Be(validValue);

        Action act = () => Guard.AgainstNegative(-1.5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AgainstZeroOrNegative_WithDoubleValue_WorksCorrectly()
    {
        var validValue = 1.5;
        var result = Guard.AgainstZeroOrNegative(validValue);
        result.Should().Be(validValue);

        Action act1 = () => Guard.AgainstZeroOrNegative(0.0);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        Action act2 = () => Guard.AgainstZeroOrNegative(-1.5);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }
}