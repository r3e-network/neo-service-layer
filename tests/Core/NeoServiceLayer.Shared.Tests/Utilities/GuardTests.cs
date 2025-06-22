using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using Xunit;

namespace NeoServiceLayer.Shared.Tests.Utilities;

/// <summary>
/// Comprehensive tests for Guard utilities covering all validation methods and error scenarios.
/// </summary>
public class GuardTests
{
    #region NotNull Tests

    [Fact]
    public void NotNull_WithValidObject_ShouldReturnObject()
    {
        // Arrange
        var testObj = new TestClass { Value = "Test" };

        // Act
        var result = Guard.NotNull(testObj);

        // Assert
        result.Should().BeSameAs(testObj);
    }

    [Fact]
    public void NotNull_WithNullObject_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestClass? nullObj = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(nullObj));
        exception.ParamName.Should().Be("nullObj");
    }

    [Fact]
    public void NotNull_WithCustomParameterName_ShouldUseCustomName()
    {
        // Arrange
        TestClass? nullObj = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(nullObj, "customParam"));
        exception.ParamName.Should().Be("customParam");
    }

    [Fact]
    public void NotNull_WithNullableStruct_ShouldReturnValue()
    {
        // Arrange
        int? nullableInt = 42;

        // Act
        var result = Guard.NotNull(nullableInt);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void NotNull_WithNullNullableStruct_ShouldThrowArgumentNullException()
    {
        // Arrange
        int? nullableInt = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(nullableInt));
        exception.ParamName.Should().Be("nullableInt");
    }

    #endregion

    #region NotNullOrEmpty String Tests

    [Theory]
    [InlineData("valid string")]
    [InlineData("a")]
    [InlineData(" ")]
    [InlineData("   spaces   ")]
    public void NotNullOrEmpty_WithValidString_ShouldReturnString(string input)
    {
        // Act
        var result = Guard.NotNullOrEmpty(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void NotNullOrEmpty_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullString = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(nullString));
        exception.Message.Should().Contain("String cannot be null or empty");
        exception.ParamName.Should().Be("nullString");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyString = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(emptyString));
        exception.Message.Should().Contain("String cannot be null or empty");
        exception.ParamName.Should().Be("emptyString");
    }

    #endregion

    #region NotNullOrWhiteSpace Tests

    [Theory]
    [InlineData("valid string")]
    [InlineData("a")]
    [InlineData("string with spaces")]
    public void NotNullOrWhiteSpace_WithValidString_ShouldReturnString(string input)
    {
        // Act
        var result = Guard.NotNullOrWhiteSpace(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    [InlineData(" \t \n \r ")]
    public void NotNullOrWhiteSpace_WithInvalidString_ShouldThrowArgumentException(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(input));
        exception.Message.Should().Contain("String cannot be null, empty, or whitespace");
    }

    #endregion

    #region NotNullOrEmpty Collection Tests

    [Fact]
    public void NotNullOrEmpty_WithValidCollection_ShouldReturnCollection()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act
        var result = Guard.NotNullOrEmpty(collection);

        // Assert
        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void NotNullOrEmpty_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        List<int>? nullCollection = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrEmpty(nullCollection));
        exception.ParamName.Should().Be("nullCollection");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyCollection = new List<int>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(emptyCollection));
        exception.Message.Should().Contain("Collection cannot be empty");
        exception.ParamName.Should().Be("emptyCollection");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyArray_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyArray = Array.Empty<string>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(emptyArray));
        exception.Message.Should().Contain("Collection cannot be empty");
    }

    #endregion

    #region Comparison Tests - GreaterThanOrEqual

    [Theory]
    [InlineData(10, 5)]
    [InlineData(10, 10)]
    [InlineData(0, -1)]
    [InlineData(-5, -10)]
    public void GreaterThanOrEqual_WithValidValues_ShouldReturnArgument(int argument, int minimum)
    {
        // Act
        var result = Guard.GreaterThanOrEqual(argument, minimum);

        // Assert
        result.Should().Be(argument);
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(-1, 0)]
    [InlineData(-10, -5)]
    public void GreaterThanOrEqual_WithInvalidValues_ShouldThrowArgumentOutOfRangeException(int argument, int minimum)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.GreaterThanOrEqual(argument, minimum));
        exception.Message.Should().Contain($"Argument must be greater than or equal to {minimum}");
        exception.ParamName.Should().Be("argument");
    }

    [Fact]
    public void GreaterThanOrEqual_WithDateTime_ShouldWork()
    {
        // Arrange
        var now = DateTime.Now;
        var yesterday = now.AddDays(-1);

        // Act
        var result = Guard.GreaterThanOrEqual(now, yesterday);

        // Assert
        result.Should().Be(now);
    }

    [Fact]
    public void GreaterThanOrEqual_WithString_ShouldWork()
    {
        // Arrange
        var arg = "zebra";
        var min = "apple";

        // Act
        var result = Guard.GreaterThanOrEqual(arg, min);

        // Assert
        result.Should().Be(arg);
    }

    #endregion

    #region Comparison Tests - GreaterThan

    [Theory]
    [InlineData(10, 5)]
    [InlineData(1, 0)]
    [InlineData(-5, -10)]
    public void GreaterThan_WithValidValues_ShouldReturnArgument(int argument, int minimum)
    {
        // Act
        var result = Guard.GreaterThan(argument, minimum);

        // Assert
        result.Should().Be(argument);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(5, 10)]
    [InlineData(0, 0)]
    [InlineData(-5, -5)]
    public void GreaterThan_WithInvalidValues_ShouldThrowArgumentOutOfRangeException(int argument, int minimum)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.GreaterThan(argument, minimum));
        exception.Message.Should().Contain($"Argument must be greater than {minimum}");
        exception.ParamName.Should().Be("argument");
    }

    #endregion

    #region Comparison Tests - LessThanOrEqual

    [Theory]
    [InlineData(5, 10)]
    [InlineData(10, 10)]
    [InlineData(-1, 0)]
    [InlineData(-10, -5)]
    public void LessThanOrEqual_WithValidValues_ShouldReturnArgument(int argument, int maximum)
    {
        // Act
        var result = Guard.LessThanOrEqual(argument, maximum);

        // Assert
        result.Should().Be(argument);
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(1, 0)]
    [InlineData(-5, -10)]
    public void LessThanOrEqual_WithInvalidValues_ShouldThrowArgumentOutOfRangeException(int argument, int maximum)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.LessThanOrEqual(argument, maximum));
        exception.Message.Should().Contain($"Argument must be less than or equal to {maximum}");
        exception.ParamName.Should().Be("argument");
    }

    #endregion

    #region Comparison Tests - LessThan

    [Theory]
    [InlineData(5, 10)]
    [InlineData(0, 1)]
    [InlineData(-10, -5)]
    public void LessThan_WithValidValues_ShouldReturnArgument(int argument, int maximum)
    {
        // Act
        var result = Guard.LessThan(argument, maximum);

        // Assert
        result.Should().Be(argument);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 5)]
    [InlineData(0, 0)]
    [InlineData(-5, -5)]
    public void LessThan_WithInvalidValues_ShouldThrowArgumentOutOfRangeException(int argument, int maximum)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.LessThan(argument, maximum));
        exception.Message.Should().Contain($"Argument must be less than {maximum}");
        exception.ParamName.Should().Be("argument");
    }

    #endregion

    #region Range Tests

    [Theory]
    [InlineData(5, 0, 10)]
    [InlineData(0, 0, 10)]
    [InlineData(10, 0, 10)]
    [InlineData(-5, -10, 0)]
    public void InRange_WithValidValues_ShouldReturnArgument(int argument, int minimum, int maximum)
    {
        // Act
        var result = Guard.InRange(argument, minimum, maximum);

        // Assert
        result.Should().Be(argument);
    }

    [Theory]
    [InlineData(-1, 0, 10)]
    [InlineData(11, 0, 10)]
    [InlineData(-11, -10, 0)]
    [InlineData(1, -10, 0)]
    public void InRange_WithInvalidValues_ShouldThrowArgumentOutOfRangeException(int argument, int minimum, int maximum)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(argument, minimum, maximum));
        exception.Message.Should().Contain($"Argument must be between {minimum} and {maximum}");
        exception.ParamName.Should().Be("argument");
    }

    [Fact]
    public void InRange_WithDecimal_ShouldWork()
    {
        // Arrange
        decimal value = 5.5m;
        decimal min = 0m;
        decimal max = 10m;

        // Act
        var result = Guard.InRange(value, min, max);

        // Assert
        result.Should().Be(value);
    }

    #endregion

    #region Boolean Condition Tests

    [Fact]
    public void IsTrue_WithTrueCondition_ShouldNotThrow()
    {
        // Arrange
        var condition = true;

        // Act & Assert
        Guard.IsTrue(condition, "Test message");
    }

    [Fact]
    public void IsTrue_WithFalseCondition_ShouldThrowArgumentException()
    {
        // Arrange
        var condition = false;
        var message = "Custom error message";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.IsTrue(condition, message));
        exception.Message.Should().Contain(message);
        exception.ParamName.Should().Be("condition");
    }

    [Fact]
    public void IsFalse_WithFalseCondition_ShouldNotThrow()
    {
        // Arrange
        var condition = false;

        // Act & Assert
        Guard.IsFalse(condition, "Test message");
    }

    [Fact]
    public void IsFalse_WithTrueCondition_ShouldThrowArgumentException()
    {
        // Arrange
        var condition = true;
        var message = "Custom error message";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.IsFalse(condition, message));
        exception.Message.Should().Contain(message);
        exception.ParamName.Should().Be("condition");
    }

    #endregion

    #region GUID Tests

    [Fact]
    public void NotEmpty_WithValidGuid_ShouldReturnGuid()
    {
        // Arrange
        var validGuid = Guid.NewGuid();

        // Act
        var result = Guard.NotEmpty(validGuid);

        // Assert
        result.Should().Be(validGuid);
    }

    [Fact]
    public void NotEmpty_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotEmpty(emptyGuid));
        exception.Message.Should().Contain("GUID cannot be empty");
        exception.ParamName.Should().Be("emptyGuid");
    }

    #endregion

    #region Index Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void NotNegative_WithValidIndex_ShouldReturnIndex(int index)
    {
        // Act
        var result = Guard.NotNegative(index);

        // Assert
        result.Should().Be(index);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void NotNegative_WithNegativeIndex_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NotNegative(index));
        exception.Message.Should().Contain("Index cannot be negative");
        exception.ParamName.Should().Be("index");
    }

    #endregion

    #region Operation Validation Tests

    [Fact]
    public void ValidOperation_WithTrueCondition_ShouldNotThrow()
    {
        // Arrange
        var condition = true;
        var message = "Operation is valid";

        // Act & Assert
        Guard.ValidOperation(condition, message);
    }

    [Fact]
    public void ValidOperation_WithFalseCondition_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var condition = false;
        var message = "Operation is invalid";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => Guard.ValidOperation(condition, message));
        exception.Message.Should().Be(message);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void EnumDefined_WithValidEnumValue_ShouldReturnValue()
    {
        // Arrange
        var validEnum = TestEnum.Value2;

        // Act
        var result = Guard.EnumDefined(validEnum);

        // Assert
        result.Should().Be(validEnum);
    }

    [Fact]
    public void EnumDefined_WithInvalidEnumValue_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidEnum = (TestEnum)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.EnumDefined(invalidEnum));
        exception.Message.Should().Contain($"Enum value '{invalidEnum}' is not defined in {typeof(TestEnum).Name}");
        exception.ParamName.Should().Be("invalidEnum");
    }

    #endregion

    #region Type Validation Tests

    [Fact]
    public void IsAssignableTo_WithValidAssignment_ShouldReturnType()
    {
        // Arrange
        var derivedType = typeof(DerivedClass);
        var baseType = typeof(BaseClass);

        // Act
        var result = Guard.IsAssignableTo(derivedType, baseType);

        // Assert
        result.Should().Be(derivedType);
    }

    [Fact]
    public void IsAssignableTo_WithInvalidAssignment_ShouldThrowArgumentException()
    {
        // Arrange
        var unrelatedType = typeof(string);
        var baseType = typeof(BaseClass);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.IsAssignableTo(unrelatedType, baseType));
        exception.Message.Should().Contain($"Type '{unrelatedType.Name}' is not assignable to '{baseType.Name}'");
        exception.ParamName.Should().Be("unrelatedType");
    }

    [Fact]
    public void IsAssignableTo_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        Type? nullType = null;
        var baseType = typeof(BaseClass);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.IsAssignableTo(nullType!, baseType));
        exception.ParamName.Should().Be("nullType");
    }

    #endregion

    #region Pattern Matching Tests

    [Theory]
    [InlineData("test@example.com", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    [InlineData("123-456-7890", @"^\d{3}-\d{3}-\d{4}$")]
    [InlineData("ABC123", @"^[A-Z]{3}\d{3}$")]
    public void MatchesPattern_WithValidPattern_ShouldReturnString(string input, string pattern)
    {
        // Act
        var result = Guard.MatchesPattern(input, pattern);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("invalid-email", @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    [InlineData("123-45-6789", @"^\d{3}-\d{3}-\d{4}$")]
    [InlineData("abc123", @"^[A-Z]{3}\d{3}$")]
    public void MatchesPattern_WithInvalidPattern_ShouldThrowArgumentException(string input, string pattern)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.MatchesPattern(input, pattern));
        exception.Message.Should().Contain($"String does not match the required pattern: {pattern}");
        exception.ParamName.Should().Be("input");
    }

    [Fact]
    public void MatchesPattern_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var input = "invalid";
        var pattern = @"^\d+$";
        var customMessage = "Must be numeric";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.MatchesPattern(input, pattern, customMessage));
        exception.Message.Should().Contain(customMessage);
    }

    [Fact]
    public void MatchesPattern_WithNullInput_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullInput = null;
        var pattern = @"^\d+$";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.MatchesPattern(nullInput!, pattern));
        exception.ParamName.Should().Be("nullInput");
    }

    [Fact]
    public void MatchesPattern_WithEmptyPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var value = "test";
        var emptyPattern = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.MatchesPattern(value, emptyPattern, nameof(emptyPattern)));
        exception.ParamName.Should().Be("pattern");
    }

    #endregion

    #region Test Helper Classes

    private class TestClass
    {
        public string Value { get; set; } = string.Empty;
    }

    private enum TestEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    private class BaseClass
    {
        public virtual string Name { get; set; } = string.Empty;
    }

    private class DerivedClass : BaseClass
    {
        public override string Name { get; set; } = "Derived";
    }

    #endregion
}
