using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Shared;

/// <summary>
/// Tests for Guard utility class to verify defensive programming patterns.
/// </summary>
public class GuardTests
{
    #region NotNull Tests for Reference Types

    [Fact]
    public void NotNull_WithValidReferenceType_ShouldReturnValue()
    {
        // Arrange
        var testObject = "test";

        // Act
        var result = Guard.NotNull(testObject);

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void NotNull_WithNullReferenceType_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? nullString = null;

        // Act & Assert
        var action = () => Guard.NotNull(nullString);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nullString");
    }

    #endregion

    #region NotNull Tests for Value Types

    [Fact]
    public void NotNull_WithValidNullableValueType_ShouldReturnValue()
    {
        // Arrange
        int? testValue = 42;

        // Act
        var result = Guard.NotNull(testValue);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void NotNull_WithNullValueType_ShouldThrowArgumentNullException()
    {
        // Arrange
        int? nullValue = null;

        // Act & Assert
        var action = () => Guard.NotNull(nullValue);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nullValue");
    }

    #endregion

    #region NotNullOrEmpty Tests

    [Fact]
    public void NotNullOrEmpty_WithValidString_ShouldReturnValue()
    {
        // Arrange
        var testString = "valid";

        // Act
        var result = Guard.NotNullOrEmpty(testString);

        // Assert
        result.Should().Be("valid");
    }

    [Fact]
    public void NotNullOrEmpty_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullString = null;

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(nullString);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("nullString")
            .WithMessage("String cannot be null or empty. (Parameter 'nullString')");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyString = "";

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(emptyString);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("emptyString")
            .WithMessage("String cannot be null or empty. (Parameter 'emptyString')");
    }

    #endregion

    #region NotNullOrWhiteSpace Tests

    [Fact]
    public void NotNullOrWhiteSpace_WithValidString_ShouldReturnValue()
    {
        // Arrange
        var testString = "valid string";

        // Act
        var result = Guard.NotNullOrWhiteSpace(testString);

        // Assert
        result.Should().Be("valid string");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullString = null;

        // Act & Assert
        var action = () => Guard.NotNullOrWhiteSpace(nullString);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("nullString")
            .WithMessage("String cannot be null, empty, or whitespace. (Parameter 'nullString')");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithWhitespaceString_ShouldThrowArgumentException()
    {
        // Arrange
        var whitespaceString = "   ";

        // Act & Assert
        var action = () => Guard.NotNullOrWhiteSpace(whitespaceString);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("whitespaceString")
            .WithMessage("String cannot be null, empty, or whitespace. (Parameter 'whitespaceString')");
    }

    #endregion

    #region NotNullOrEmpty Collection Tests

    [Fact]
    public void NotNullOrEmpty_WithValidCollection_ShouldReturnValue()
    {
        // Arrange
        var testCollection = new[] { 1, 2, 3 };

        // Act
        var result = Guard.NotNullOrEmpty(testCollection);

        // Assert
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void NotNullOrEmpty_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        int[]? nullCollection = null;

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(nullCollection);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nullCollection");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyCollection = Array.Empty<int>();

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(emptyCollection);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("emptyCollection")
            .WithMessage("Collection cannot be empty. (Parameter 'emptyCollection')");
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void GreaterThanOrEqual_WithValidValue_ShouldReturnValue()
    {
        // Arrange
        var value = 10;
        var minimum = 5;

        // Act
        var result = Guard.GreaterThanOrEqual(value, minimum);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void GreaterThanOrEqual_WithEqualValue_ShouldReturnValue()
    {
        // Arrange
        var value = 5;
        var minimum = 5;

        // Act
        var result = Guard.GreaterThanOrEqual(value, minimum);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void GreaterThanOrEqual_WithSmallerValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var value = 3;
        var minimum = 5;

        // Act & Assert
        var action = () => Guard.GreaterThanOrEqual(value, minimum);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("Argument must be greater than or equal to 5. (Parameter 'value')");
    }

    [Fact]
    public void GreaterThan_WithValidValue_ShouldReturnValue()
    {
        // Arrange
        var value = 10;
        var minimum = 5;

        // Act
        var result = Guard.GreaterThan(value, minimum);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void GreaterThan_WithEqualValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var value = 5;
        var minimum = 5;

        // Act & Assert
        var action = () => Guard.GreaterThan(value, minimum);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("Argument must be greater than 5. (Parameter 'value')");
    }

    [Fact]
    public void LessThanOrEqual_WithValidValue_ShouldReturnValue()
    {
        // Arrange
        var value = 3;
        var maximum = 5;

        // Act
        var result = Guard.LessThanOrEqual(value, maximum);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void LessThanOrEqual_WithLargerValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var value = 10;
        var maximum = 5;

        // Act & Assert
        var action = () => Guard.LessThanOrEqual(value, maximum);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("Argument must be less than or equal to 5. (Parameter 'value')");
    }

    [Fact]
    public void LessThan_WithValidValue_ShouldReturnValue()
    {
        // Arrange
        var value = 3;
        var maximum = 5;

        // Act
        var result = Guard.LessThan(value, maximum);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void LessThan_WithEqualValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var value = 5;
        var maximum = 5;

        // Act & Assert
        var action = () => Guard.LessThan(value, maximum);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("Argument must be less than 5. (Parameter 'value')");
    }

    [Fact]
    public void InRange_WithValidValue_ShouldReturnValue()
    {
        // Arrange
        var value = 5;
        var minimum = 1;
        var maximum = 10;

        // Act
        var result = Guard.InRange(value, minimum, maximum);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void InRange_WithValueBelowRange_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var value = 0;
        var minimum = 1;
        var maximum = 10;

        // Act & Assert
        var action = () => Guard.InRange(value, minimum, maximum);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("Argument must be between 1 and 10. (Parameter 'value')");
    }

    [Fact]
    public void InRange_WithValueAboveRange_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var value = 15;
        var minimum = 1;
        var maximum = 10;

        // Act & Assert
        var action = () => Guard.InRange(value, minimum, maximum);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("Argument must be between 1 and 10. (Parameter 'value')");
    }

    #endregion

    #region Boolean Condition Tests

    [Fact]
    public void IsTrue_WithTrueCondition_ShouldNotThrow()
    {
        // Arrange
        var condition = true;

        // Act & Assert
        var action = () => Guard.IsTrue(condition, "Test message");
        action.Should().NotThrow();
    }

    [Fact]
    public void IsTrue_WithFalseCondition_ShouldThrowArgumentException()
    {
        // Arrange
        var condition = false;

        // Act & Assert
        var action = () => Guard.IsTrue(condition, "Test message");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("condition")
            .WithMessage("Test message (Parameter 'condition')");
    }

    [Fact]
    public void IsFalse_WithFalseCondition_ShouldNotThrow()
    {
        // Arrange
        var condition = false;

        // Act & Assert
        var action = () => Guard.IsFalse(condition, "Test message");
        action.Should().NotThrow();
    }

    [Fact]
    public void IsFalse_WithTrueCondition_ShouldThrowArgumentException()
    {
        // Arrange
        var condition = true;

        // Act & Assert
        var action = () => Guard.IsFalse(condition, "Test message");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("condition")
            .WithMessage("Test message (Parameter 'condition')");
    }

    #endregion

    #region GUID Tests

    [Fact]
    public void NotEmpty_WithValidGuid_ShouldReturnValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = Guard.NotEmpty(guid);

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void NotEmpty_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act & Assert
        var action = () => Guard.NotEmpty(emptyGuid);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("emptyGuid")
            .WithMessage("GUID cannot be empty. (Parameter 'emptyGuid')");
    }

    #endregion

    #region Index Tests

    [Fact]
    public void NotNegative_WithPositiveIndex_ShouldReturnValue()
    {
        // Arrange
        var index = 5;

        // Act
        var result = Guard.NotNegative(index);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void NotNegative_WithZeroIndex_ShouldReturnValue()
    {
        // Arrange
        var index = 0;

        // Act
        var result = Guard.NotNegative(index);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void NotNegative_WithNegativeIndex_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var index = -1;

        // Act & Assert
        var action = () => Guard.NotNegative(index);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("index")
            .WithMessage("Index cannot be negative. (Parameter 'index')");
    }

    #endregion

    #region Operation Validation Tests

    [Fact]
    public void ValidOperation_WithTrueCondition_ShouldNotThrow()
    {
        // Arrange
        var condition = true;

        // Act & Assert
        var action = () => Guard.ValidOperation(condition, "Operation is valid");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidOperation_WithFalseCondition_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var condition = false;

        // Act & Assert
        var action = () => Guard.ValidOperation(condition, "Operation is invalid");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Operation is invalid");
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void EnumDefined_WithValidEnumValue_ShouldReturnValue()
    {
        // Arrange
        var enumValue = DayOfWeek.Monday;

        // Act
        var result = Guard.EnumDefined(enumValue);

        // Assert
        result.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void EnumDefined_WithInvalidEnumValue_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidEnumValue = (DayOfWeek)999;

        // Act & Assert
        var action = () => Guard.EnumDefined(invalidEnumValue);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("invalidEnumValue")
            .WithMessage("Enum value '999' is not defined in DayOfWeek. (Parameter 'invalidEnumValue')");
    }

    #endregion

    #region Type Tests

    [Fact]
    public void IsAssignableTo_WithValidTypes_ShouldReturnType()
    {
        // Arrange
        var type = typeof(string);
        var targetType = typeof(object);

        // Act
        var result = Guard.IsAssignableTo(type, targetType);

        // Assert
        result.Should().Be(typeof(string));
    }

    [Fact]
    public void IsAssignableTo_WithInvalidTypes_ShouldThrowArgumentException()
    {
        // Arrange
        var type = typeof(int);
        var targetType = typeof(string);

        // Act & Assert
        var action = () => Guard.IsAssignableTo(type, targetType);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("type")
            .WithMessage("Type 'Int32' is not assignable to 'String'. (Parameter 'type')");
    }

    [Fact]
    public void IsAssignableTo_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        Type? nullType = null;
        var targetType = typeof(object);

        // Act & Assert
        var action = () => Guard.IsAssignableTo(nullType!, targetType);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nullType");
    }

    [Fact]
    public void IsAssignableTo_WithNullTargetType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var type = typeof(string);
        Type? nullTargetType = null;

        // Act & Assert
        var action = () => Guard.IsAssignableTo(type, nullTargetType!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetType");
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void MatchesPattern_WithValidPattern_ShouldReturnValue()
    {
        // Arrange
        var text = "test123";
        var pattern = @"^[a-z]+\d+$";

        // Act
        var result = Guard.MatchesPattern(text, pattern);

        // Assert
        result.Should().Be("test123");
    }

    [Fact]
    public void MatchesPattern_WithInvalidPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var text = "TEST123";
        var pattern = @"^[a-z]+\d+$";

        // Act & Assert
        var action = () => Guard.MatchesPattern(text, pattern);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("text")
            .WithMessage("String does not match the required pattern: ^[a-z]+\\d+$ (Parameter 'text')");
    }

    [Fact]
    public void MatchesPattern_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var text = "invalid";
        var pattern = @"^\d+$";
        var customMessage = "Must be numeric only";

        // Act & Assert
        var action = () => Guard.MatchesPattern(text, pattern, customMessage);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("text")
            .WithMessage("Must be numeric only (Parameter 'text')");
    }

    [Fact]
    public void MatchesPattern_WithNullText_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullText = null;
        var pattern = @"^\d+$";

        // Act & Assert
        var action = () => Guard.MatchesPattern(nullText!, pattern);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("nullText");
    }

    [Fact]
    public void MatchesPattern_WithNullPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var text = "123";
        string? nullPattern = null;

        // Act & Assert
        var action = () => Guard.MatchesPattern(text, nullPattern!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("pattern");
    }

    #endregion
}
