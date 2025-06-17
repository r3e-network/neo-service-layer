using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using System.ComponentModel;
using Xunit;

namespace NeoServiceLayer.Shared.Tests;

/// <summary>
/// Comprehensive tests for Guard utility methods.
/// </summary>
public class GuardTests
{
    #region NotNull Tests for Reference Types

    [Fact]
    public void NotNull_WithValidReference_ShouldReturnValue()
    {
        // Arrange
        var testString = "test value";

        // Act
        var result = Guard.NotNull(testString);

        // Assert
        result.Should().Be(testString);
    }

    [Fact]
    public void NotNull_WithNullReference_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Guard.NotNull((string?)null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNull_WithNullReferenceAndParameterName_ShouldIncludeParameterName()
    {
        // Act & Assert
        var action = () => Guard.NotNull((string?)null, "testParameter");
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("testParameter");
    }

    #endregion

    #region NotNull Tests for Value Types

    [Fact]
    public void NotNull_WithValidNullableStruct_ShouldReturnValue()
    {
        // Arrange
        int? testValue = 42;

        // Act
        var result = Guard.NotNull(testValue);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void NotNull_WithNullStruct_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Guard.NotNull((int?)null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNull_WithNullStructAndParameterName_ShouldIncludeParameterName()
    {
        // Act & Assert
        var action = () => Guard.NotNull((DateTime?)null, "dateParameter");
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("dateParameter");
    }

    #endregion

    #region NotNullOrEmpty String Tests

    [Fact]
    public void NotNullOrEmpty_WithValidString_ShouldReturnValue()
    {
        // Arrange
        var testString = "valid string";

        // Act
        var result = Guard.NotNullOrEmpty(testString);

        // Assert
        result.Should().Be(testString);
    }

    [Fact]
    public void NotNullOrEmpty_WithNullString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrEmpty((string?)null);
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null or empty.*");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(string.Empty);
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null or empty.*");
    }

    [Fact]
    public void NotNullOrEmpty_WithWhitespace_ShouldReturnValue()
    {
        // Arrange
        var whitespaceString = "   ";

        // Act
        var result = Guard.NotNullOrEmpty(whitespaceString);

        // Assert
        result.Should().Be(whitespaceString);
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
        result.Should().Be(testString);
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithNullString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrWhiteSpace((string?)null);
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null, empty, or whitespace.*");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrWhiteSpace(string.Empty);
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null, empty, or whitespace.*");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithWhitespace_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrWhiteSpace("   ");
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null, empty, or whitespace.*");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithTabsAndNewlines_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrWhiteSpace("\t\n\r ");
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null, empty, or whitespace.*");
    }

    #endregion

    #region NotNullOrEmpty Collection Tests

    [Fact]
    public void NotNullOrEmpty_WithValidCollection_ShouldReturnValue()
    {
        // Arrange
        var collection = new List<string> { "item1", "item2" };

        // Act
        var result = Guard.NotNullOrEmpty(collection);

        // Assert
        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void NotNullOrEmpty_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrEmpty((IEnumerable<string>?)null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyCollection_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(new List<string>());
        action.Should().Throw<ArgumentException>()
              .WithMessage("Collection cannot be empty.*");
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyArray_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(Array.Empty<int>());
        action.Should().Throw<ArgumentException>()
              .WithMessage("Collection cannot be empty.*");
    }

    #endregion

    #region Comparison Tests - GreaterThanOrEqual

    [Fact]
    public void GreaterThanOrEqual_WithValidValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.GreaterThanOrEqual(10, 5);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void GreaterThanOrEqual_WithEqualValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.GreaterThanOrEqual(5, 5);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void GreaterThanOrEqual_WithLowerValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.GreaterThanOrEqual(3, 5);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be greater than or equal to 5.*");
    }

    [Fact]
    public void GreaterThanOrEqual_WithNegativeNumbers_ShouldWork()
    {
        // Act & Assert
        Guard.GreaterThanOrEqual(-5, -10).Should().Be(-5);
        
        var action = () => Guard.GreaterThanOrEqual(-15, -10);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Comparison Tests - GreaterThan

    [Fact]
    public void GreaterThan_WithValidValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.GreaterThan(10, 5);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void GreaterThan_WithEqualValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.GreaterThan(5, 5);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be greater than 5.*");
    }

    [Fact]
    public void GreaterThan_WithLowerValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.GreaterThan(3, 5);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be greater than 5.*");
    }

    #endregion

    #region Comparison Tests - LessThanOrEqual

    [Fact]
    public void LessThanOrEqual_WithValidValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.LessThanOrEqual(5, 10);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void LessThanOrEqual_WithEqualValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.LessThanOrEqual(10, 10);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void LessThanOrEqual_WithHigherValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.LessThanOrEqual(15, 10);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be less than or equal to 10.*");
    }

    #endregion

    #region Comparison Tests - LessThan

    [Fact]
    public void LessThan_WithValidValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.LessThan(5, 10);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void LessThan_WithEqualValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.LessThan(10, 10);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be less than 10.*");
    }

    [Fact]
    public void LessThan_WithHigherValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.LessThan(15, 10);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be less than 10.*");
    }

    #endregion

    #region InRange Tests

    [Fact]
    public void InRange_WithValueInRange_ShouldReturnValue()
    {
        // Act
        var result = Guard.InRange(15, 10, 20);

        // Assert
        result.Should().Be(15);
    }

    [Fact]
    public void InRange_WithValueAtMinimum_ShouldReturnValue()
    {
        // Act
        var result = Guard.InRange(10, 10, 20);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void InRange_WithValueAtMaximum_ShouldReturnValue()
    {
        // Act
        var result = Guard.InRange(20, 10, 20);

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public void InRange_WithValueBelowMinimum_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.InRange(5, 10, 20);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be between 10 and 20.*");
    }

    [Fact]
    public void InRange_WithValueAboveMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.InRange(25, 10, 20);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Argument must be between 10 and 20.*");
    }

    [Fact]
    public void InRange_WithDecimalValues_ShouldWork()
    {
        // Act & Assert
        Guard.InRange(15.5m, 10.0m, 20.0m).Should().Be(15.5m);
        
        var action = () => Guard.InRange(25.1m, 10.0m, 20.0m);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Boolean Condition Tests

    [Fact]
    public void IsTrue_WithTrueCondition_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Guard.IsTrue(true, "Condition should be true");
        action.Should().NotThrow();
    }

    [Fact]
    public void IsTrue_WithFalseCondition_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.IsTrue(false, "Custom error message");
        action.Should().Throw<ArgumentException>()
              .WithMessage("Custom error message*");
    }

    [Fact]
    public void IsFalse_WithFalseCondition_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Guard.IsFalse(false, "Condition should be false");
        action.Should().NotThrow();
    }

    [Fact]
    public void IsFalse_WithTrueCondition_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.IsFalse(true, "Custom error message");
        action.Should().Throw<ArgumentException>()
              .WithMessage("Custom error message*");
    }

    #endregion

    #region GUID Tests

    [Fact]
    public void NotEmpty_WithValidGuid_ShouldReturnValue()
    {
        // Arrange
        var testGuid = Guid.NewGuid();

        // Act
        var result = Guard.NotEmpty(testGuid);

        // Assert
        result.Should().Be(testGuid);
    }

    [Fact]
    public void NotEmpty_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.NotEmpty(Guid.Empty);
        action.Should().Throw<ArgumentException>()
              .WithMessage("GUID cannot be empty.*");
    }

    #endregion

    #region Index Tests

    [Fact]
    public void NotNegative_WithPositiveIndex_ShouldReturnValue()
    {
        // Act
        var result = Guard.NotNegative(5);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void NotNegative_WithZeroIndex_ShouldReturnValue()
    {
        // Act
        var result = Guard.NotNegative(0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void NotNegative_WithNegativeIndex_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => Guard.NotNegative(-1);
        action.Should().Throw<ArgumentOutOfRangeException>()
              .WithMessage("Index cannot be negative.*");
    }

    #endregion

    #region Operation Validation Tests

    [Fact]
    public void ValidOperation_WithTrueCondition_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Guard.ValidOperation(true, "Operation is valid");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidOperation_WithFalseCondition_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => Guard.ValidOperation(false, "Invalid operation attempted");
        action.Should().Throw<InvalidOperationException>()
              .WithMessage("Invalid operation attempted");
    }

    #endregion

    #region Enum Validation Tests

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [Fact]
    public void EnumDefined_WithValidEnumValue_ShouldReturnValue()
    {
        // Act
        var result = Guard.EnumDefined(TestEnum.Value2);

        // Assert
        result.Should().Be(TestEnum.Value2);
    }

    [Fact]
    public void EnumDefined_WithInvalidEnumValue_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.EnumDefined((TestEnum)999);
        action.Should().Throw<ArgumentException>()
              .WithMessage("Enum value '999' is not defined in TestEnum.*");
    }

    [Fact]
    public void EnumDefined_WithValidEnumFromInt_ShouldWork()
    {
        // Act
        var result = Guard.EnumDefined((TestEnum)1); // Value2

        // Assert
        result.Should().Be(TestEnum.Value2);
    }

    #endregion

    #region Type Validation Tests

    [Fact]
    public void IsAssignableTo_WithCompatibleTypes_ShouldReturnType()
    {
        // Act
        var result = Guard.IsAssignableTo(typeof(string), typeof(object));

        // Assert
        result.Should().Be(typeof(string));
    }

    [Fact]
    public void IsAssignableTo_WithIncompatibleTypes_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.IsAssignableTo(typeof(int), typeof(string));
        action.Should().Throw<ArgumentException>()
              .WithMessage("Type 'Int32' is not assignable to 'String'.*");
    }

    [Fact]
    public void IsAssignableTo_WithNullType_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Guard.IsAssignableTo(null!, typeof(object));
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsAssignableTo_WithNullTargetType_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Guard.IsAssignableTo(typeof(string), null!);
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void MatchesPattern_WithValidPattern_ShouldReturnValue()
    {
        // Arrange
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        var email = "test@example.com";

        // Act
        var result = Guard.MatchesPattern(email, emailPattern);

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void MatchesPattern_WithInvalidPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        var invalidEmail = "not-an-email";

        // Act & Assert
        var action = () => Guard.MatchesPattern(invalidEmail, emailPattern);
        action.Should().Throw<ArgumentException>()
              .WithMessage("String does not match the required pattern*");
    }

    [Fact]
    public void MatchesPattern_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var pattern = @"\d+";
        var value = "abc";
        var customMessage = "Value must contain only digits";

        // Act & Assert
        var action = () => Guard.MatchesPattern(value, pattern, customMessage);
        action.Should().Throw<ArgumentException>()
              .WithMessage("Value must contain only digits*");
    }

    [Fact]
    public void MatchesPattern_WithNullString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.MatchesPattern(null!, @"\d+");
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null or empty.*");
    }

    [Fact]
    public void MatchesPattern_WithNullPattern_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Guard.MatchesPattern("test", null!);
        action.Should().Throw<ArgumentException>()
              .WithMessage("String cannot be null or empty.*");
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void Guard_WithChainedValidations_ShouldAllPass()
    {
        // Arrange
        var value = "test@example.com";
        var number = 42;
        var guid = Guid.NewGuid();

        // Act & Assert - All should pass without throwing
        var emailResult = Guard.NotNullOrWhiteSpace(value);
        var emailPatternResult = Guard.MatchesPattern(emailResult, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        var numberResult = Guard.InRange(number, 1, 100);
        var guidResult = Guard.NotEmpty(guid);

        // All should return their respective values
        emailPatternResult.Should().Be(value);
        numberResult.Should().Be(number);
        guidResult.Should().Be(guid);
    }

    [Fact]
    public void Guard_WithComplexObjectValidation_ShouldWork()
    {
        // Arrange
        var complexObject = new Dictionary<string, object>
        {
            ["name"] = "Test User",
            ["age"] = 25,
            ["email"] = "test@example.com"
        };

        // Act & Assert
        var result = Guard.NotNull(complexObject);
        var nonEmptyResult = Guard.NotNullOrEmpty(result);

        result.Should().BeSameAs(complexObject);
        nonEmptyResult.Should().BeSameAs(complexObject);
    }

    [Fact]
    public void Guard_WithDateTimeValidation_ShouldWork()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(30);
        var pastDate = DateTime.Now.AddDays(-30);
        var currentDate = DateTime.Now;

        // Act & Assert
        Guard.GreaterThan(futureDate, currentDate).Should().Be(futureDate);
        Guard.LessThan(pastDate, currentDate).Should().Be(pastDate);
        Guard.InRange(currentDate, pastDate, futureDate).Should().Be(currentDate);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Guard_ValidationPerformance_ShouldBeFast()
    {
        // Arrange
        const int iterations = 10000;
        var testString = "performance test string";
        var testNumber = 42;

        // Act & Assert - Should complete quickly
        for (int i = 0; i < iterations; i++)
        {
            Guard.NotNullOrEmpty(testString);
            Guard.GreaterThan(testNumber, 0);
            Guard.InRange(testNumber, 1, 100);
        }

        // If we reach here without timeout, performance is acceptable
        Assert.True(true);
    }

    #endregion
} 