using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Shared.Tests.Extensions;

/// <summary>
/// Comprehensive tests for ObjectExtensions covering JSON, validation, reflection, and mapping.
/// </summary>
public class ObjectExtensionsTests
{
    #region JSON Serialization Tests

    [Fact]
    public void ToJson_WithValidObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42, IsActive = true };

        // Act
        var result = testObj.ToJson();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"name\":");
        result.Should().Contain("\"Test\"");
        result.Should().Contain("\"value\":");
        result.Should().Contain("42");
    }

    [Fact]
    public void ToJson_WithNullObject_ShouldReturnNull()
    {
        // Arrange
        TestModel? testObj = null;

        // Act
        var result = testObj.ToJson();

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void ToJson_WithCustomOptions_ShouldRespectOptions()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42 };
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Act
        var result = testObj.ToJson(options);

        // Assert
        result.Should().Contain("\"name\":");
        result.Should().NotContain("\n"); // Should not be indented
    }

    #endregion

    #region Deep Clone Tests

    [Fact]
    public void DeepClone_WithValidObject_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new TestModel 
        { 
            Name = "Original", 
            Value = 42, 
            NestedObject = new NestedModel { Id = 1, Description = "Nested" } 
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.Should().NotBeNull();
        clone.Should().NotBeSameAs(original);
        clone!.Name.Should().Be(original.Name);
        clone.Value.Should().Be(original.Value);
        clone.NestedObject.Should().NotBeSameAs(original.NestedObject);
        clone.NestedObject!.Id.Should().Be(original.NestedObject.Id);
    }

    [Fact]
    public void DeepClone_WithNullObject_ShouldReturnNull()
    {
        // Arrange
        TestModel? original = null;

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.Should().BeNull();
    }

    [Fact]
    public void DeepClone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new TestModel 
        { 
            Name = "Original", 
            NestedObject = new NestedModel { Id = 1, Description = "Nested" } 
        };

        // Act
        var clone = original.DeepClone();
        clone!.Name = "Modified";
        clone.NestedObject!.Description = "Modified Nested";

        // Assert
        original.Name.Should().Be("Original");
        original.NestedObject.Description.Should().Be("Nested");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithValidObject_ShouldReturnEmptyResults()
    {
        // Arrange
        var validObj = new ValidatedModel { Name = "Valid", Email = "test@example.com", Age = 25 };

        // Act
        var results = validObj.Validate();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidObject_ShouldReturnValidationErrors()
    {
        // Arrange
        var invalidObj = new ValidatedModel { Name = "", Email = "invalid-email", Age = -5 };

        // Act
        var results = invalidObj.Validate().ToList();

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.Should().Contain(r => r.MemberNames.Contains("Name"));
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
        results.Should().Contain(r => r.MemberNames.Contains("Age"));
    }

    [Fact]
    public void IsValid_WithValidObject_ShouldReturnTrue()
    {
        // Arrange
        var validObj = new ValidatedModel { Name = "Valid", Email = "test@example.com", Age = 25 };

        // Act
        var isValid = validObj.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidObject_ShouldReturnFalse()
    {
        // Arrange
        var invalidObj = new ValidatedModel { Name = "", Email = "invalid-email", Age = -5 };

        // Act
        var isValid = invalidObj.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Property Reflection Tests

    [Fact]
    public void GetPropertyValue_WithExistingProperty_ShouldReturnValue()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42 };

        // Act
        var nameValue = testObj.GetPropertyValue("Name");
        var valueValue = testObj.GetPropertyValue("Value");

        // Assert
        nameValue.Should().Be("Test");
        valueValue.Should().Be(42);
    }

    [Fact]
    public void GetPropertyValue_WithNonExistentProperty_ShouldReturnNull()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test" };

        // Act
        var result = testObj.GetPropertyValue("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithNullObject_ShouldReturnNull()
    {
        // Arrange
        TestModel? testObj = null;

        // Act
        var result = testObj.GetPropertyValue("Name");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SetPropertyValue_WithExistingProperty_ShouldSetValue()
    {
        // Arrange
        var testObj = new TestModel { Name = "Original", Value = 0 };

        // Act
        var nameSet = testObj.SetPropertyValue("Name", "Modified");
        var valueSet = testObj.SetPropertyValue("Value", 99);

        // Assert
        nameSet.Should().BeTrue();
        valueSet.Should().BeTrue();
        testObj.Name.Should().Be("Modified");
        testObj.Value.Should().Be(99);
    }

    [Fact]
    public void SetPropertyValue_WithNonExistentProperty_ShouldReturnFalse()
    {
        // Arrange
        var testObj = new TestModel();

        // Act
        var result = testObj.SetPropertyValue("NonExistent", "value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetPropertyValue_WithReadOnlyProperty_ShouldReturnFalse()
    {
        // Arrange
        var testObj = new TestModel();

        // Act
        var result = testObj.SetPropertyValue("ReadOnlyProperty", "value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasProperty_WithExistingProperty_ShouldReturnTrue()
    {
        // Arrange
        var testObj = new TestModel();

        // Act
        var hasName = testObj.HasProperty("Name");
        var hasValue = testObj.HasProperty("Value");

        // Assert
        hasName.Should().BeTrue();
        hasValue.Should().BeTrue();
    }

    [Fact]
    public void HasProperty_WithNonExistentProperty_ShouldReturnFalse()
    {
        // Arrange
        var testObj = new TestModel();

        // Act
        var result = testObj.HasProperty("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Property Dictionary Tests

    [Fact]
    public void GetProperties_ShouldReturnAllPublicProperties()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42, IsActive = true };

        // Act
        var properties = testObj.GetProperties();

        // Assert
        properties.Should().ContainKeys("Name", "Value", "IsActive", "ReadOnlyProperty");
        properties["Name"].Should().Be("Test");
        properties["Value"].Should().Be(42);
        properties["IsActive"].Should().Be(true);
    }

    [Fact]
    public void ToDictionary_WithIncludeNulls_ShouldIncludeNullValues()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42, NestedObject = null };

        // Act
        var dictionary = testObj.ToDictionary(includeNulls: true);

        // Assert
        dictionary.Should().ContainKey("NestedObject");
        dictionary["NestedObject"].Should().BeNull();
    }

    [Fact]
    public void ToDictionary_WithoutIncludeNulls_ShouldExcludeNullValues()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42, NestedObject = null };

        // Act
        var dictionary = testObj.ToDictionary(includeNulls: false);

        // Assert
        dictionary.Should().NotContainKey("NestedObject");
        dictionary.Should().ContainKeys("Name", "Value");
    }

    #endregion

    #region Object Mapping Tests

    [Fact]
    public void MapTo_WithCompatibleTypes_ShouldMapProperties()
    {
        // Arrange
        var source = new TestModel { Name = "Source", Value = 42, IsActive = true };
        var destination = new CompatibleModel();

        // Act
        var result = source.MapTo(destination);

        // Assert
        result.Should().BeSameAs(destination);
        result.Name.Should().Be("Source");
        result.Value.Should().Be(42);
        result.IsActive.Should().Be(true);
    }

    [Fact]
    public void MapTo_WithIgnoreCase_ShouldMapCaseInsensitive()
    {
        // Arrange
        var source = new TestModel { Name = "Source", Value = 42 };
        var destination = new CaseDifferentModel();

        // Act
        var result = source.MapTo(destination, ignoreCase: true);

        // Assert
        result.name.Should().Be("Source");
        result.VALUE.Should().Be(42);
    }

    [Fact]
    public void MapTo_WithCaseSensitive_ShouldNotMapDifferentCase()
    {
        // Arrange
        var source = new TestModel { Name = "Source", Value = 42 };
        var destination = new CaseDifferentModel();

        // Act
        var result = source.MapTo(destination, ignoreCase: false);

        // Assert
        result.name.Should().BeNull();
        result.VALUE.Should().Be(0);
    }

    [Fact]
    public void MapTo_Generic_ShouldCreateNewInstanceAndMap()
    {
        // Arrange
        var source = new TestModel { Name = "Source", Value = 42, IsActive = true };

        // Act
        var result = source.MapTo<CompatibleModel>();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Source");
        result.Value.Should().Be(42);
        result.IsActive.Should().Be(true);
    }

    [Fact]
    public void MapTo_WithIncompatibleTypes_ShouldIgnoreIncompatibleProperties()
    {
        // Arrange
        var source = new TestModel { Name = "Source", Value = 42 };
        var destination = new IncompatibleModel();

        // Act
        var result = source.MapTo(destination);

        // Assert
        result.Name.Should().Be("Source"); // String to string should work
        result.Value.Should().Be(""); // int to string should be ignored
    }

    #endregion

    #region Null Check Extensions Tests

    [Fact]
    public void IsNull_WithNullObject_ShouldReturnTrue()
    {
        // Arrange
        TestModel? testObj = null;

        // Act
        var result = testObj.IsNull();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNull_WithNonNullObject_ShouldReturnFalse()
    {
        // Arrange
        var testObj = new TestModel();

        // Act
        var result = testObj.IsNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_WithNonNullObject_ShouldReturnTrue()
    {
        // Arrange
        var testObj = new TestModel();

        // Act
        var result = testObj.IsNotNull();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNotNull_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        TestModel? testObj = null;

        // Act
        var result = testObj.IsNotNull();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IfNull Extensions Tests

    [Fact]
    public void IfNull_WithNullObject_ShouldReturnDefault()
    {
        // Arrange
        TestModel? testObj = null;
        var defaultObj = new TestModel { Name = "Default" };

        // Act
        var result = testObj.IfNull(defaultObj);

        // Assert
        result.Should().BeSameAs(defaultObj);
    }

    [Fact]
    public void IfNull_WithNonNullObject_ShouldReturnOriginal()
    {
        // Arrange
        var testObj = new TestModel { Name = "Original" };
        var defaultObj = new TestModel { Name = "Default" };

        // Act
        var result = testObj.IfNull(defaultObj);

        // Assert
        result.Should().BeSameAs(testObj);
    }

    [Fact]
    public void IfNull_WithFactory_ShouldCallFactoryOnlyWhenNull()
    {
        // Arrange
        TestModel? nullObj = null;
        var nonNullObj = new TestModel { Name = "Original" };
        var factoryCalled = false;
        Func<TestModel> factory = () =>
        {
            factoryCalled = true;
            return new TestModel { Name = "Factory" };
        };

        // Act
        var resultNull = nullObj.IfNull(factory);
        var resultNonNull = nonNullObj.IfNull(factory);

        // Assert
        resultNull.Name.Should().Be("Factory");
        resultNonNull.Should().BeSameAs(nonNullObj);
        factoryCalled.Should().BeTrue();
    }

    #endregion

    #region IfNotNull Extensions Tests

    [Fact]
    public void IfNotNull_WithNonNullObject_ShouldExecuteAction()
    {
        // Arrange
        var testObj = new TestModel { Name = "Original" };
        var actionExecuted = false;

        // Act
        var result = testObj.IfNotNull(obj => 
        {
            actionExecuted = true;
            obj.Name = "Modified";
        });

        // Assert
        result.Should().BeSameAs(testObj);
        actionExecuted.Should().BeTrue();
        testObj.Name.Should().Be("Modified");
    }

    [Fact]
    public void IfNotNull_WithNullObject_ShouldNotExecuteAction()
    {
        // Arrange
        TestModel? testObj = null;
        var actionExecuted = false;

        // Act
        var result = testObj.IfNotNull(obj => 
        {
            actionExecuted = true;
            obj.Name = "Modified";
        });

        // Assert
        result.Should().BeNull();
        actionExecuted.Should().BeFalse();
    }

    [Fact]
    public void IfNotNull_WithFunction_ShouldReturnFunctionResult()
    {
        // Arrange
        var testObj = new TestModel { Name = "Test", Value = 42 };

        // Act
        var result = testObj.IfNotNull(obj => obj.Name.ToUpper());

        // Assert
        result.Should().Be("TEST");
    }

    [Fact]
    public void IfNotNull_WithNullObject_AndFunction_ShouldReturnDefault()
    {
        // Arrange
        TestModel? testObj = null;

        // Act
        var result = testObj.IfNotNull(obj => obj.Name.ToUpper());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Test Model Classes

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
        public NestedModel? NestedObject { get; set; }
        public string ReadOnlyProperty { get; } = "ReadOnly";
    }

    public class NestedModel
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ValidatedModel
    {
        [Required]
        [MinLength(1)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Range(0, 150)]
        public int Age { get; set; }
    }

    public class CompatibleModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }

    public class CaseDifferentModel
    {
        public string? name { get; set; }
        public int VALUE { get; set; }
    }

    public class IncompatibleModel
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty; // int -> string incompatible
    }

    #endregion
} 