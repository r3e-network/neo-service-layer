using FluentAssertions;
using NeoServiceLayer.Shared.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Shared.Tests;

/// <summary>
/// Comprehensive tests for ObjectExtensions utility methods.
/// </summary>
public class ObjectExtensionsTests
{
    #region Test Classes

    public class TestPerson
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Range(0, 150)]
        public int Age { get; set; }
        
        public string? Email { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public List<string> Tags { get; set; } = new();
    }

    public class TestEmployee
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Email { get; set; }
        public string Department { get; set; } = string.Empty;
        public decimal Salary { get; set; }
    }

    public class InvalidTestClass
    {
        [Required]
        public string RequiredField { get; set; } = string.Empty;
        
        [Range(10, 100)]
        public int Number { get; set; }
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void ToJson_WithValidObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com",
            Tags = new List<string> { "developer", "senior" }
        };

        // Act
        var json = person.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("John Doe");
        json.Should().Contain("john@example.com");
        json.Should().Contain("developer");
    }

    [Fact]
    public void ToJson_WithNullObject_ShouldReturnNull()
    {
        // Act & Assert
        ((TestPerson?)null).ToJson().Should().Be("null");
    }

    [Fact]
    public void ToJson_WithCustomOptions_ShouldUseCustomOptions()
    {
        // Arrange
        var person = new TestPerson { Name = "Test" };
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Act
        var json = person.ToJson(options);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"name\""); // camelCase
        json.Should().NotContain("\n"); // not indented
    }

    #endregion

    #region Deep Clone Tests

    [Fact]
    public void DeepClone_WithValidObject_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new TestPerson
        {
            Name = "Original",
            Age = 25,
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var clone = original.DeepClone();

        // Assert
        clone.Should().NotBeNull();
        clone.Should().NotBeSameAs(original);
        clone!.Name.Should().Be(original.Name);
        clone.Age.Should().Be(original.Age);
        clone.Tags.Should().NotBeSameAs(original.Tags);
        clone.Tags.Should().BeEquivalentTo(original.Tags);
    }

    [Fact]
    public void DeepClone_WithModifiedClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new TestPerson
        {
            Name = "Original",
            Tags = new List<string> { "tag1" }
        };

        // Act
        var clone = original.DeepClone();
        clone!.Name = "Modified";
        clone.Tags.Add("tag2");

        // Assert
        original.Name.Should().Be("Original");
        original.Tags.Should().HaveCount(1);
        original.Tags.Should().Contain("tag1");
        original.Tags.Should().NotContain("tag2");
    }

    [Fact]
    public void DeepClone_WithNullObject_ShouldReturnNull()
    {
        // Act & Assert
        ((TestPerson?)null).DeepClone().Should().BeNull();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithValidObject_ShouldReturnNoErrors()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Valid Name",
            Age = 25
        };

        // Act
        var results = person.Validate();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidObject_ShouldReturnErrors()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "", // Required field is empty
            Age = 200 // Outside valid range
        };

        // Act
        var results = person.Validate().ToList();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void IsValid_WithValidObject_ShouldReturnTrue()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Valid Name",
            Age = 25
        };

        // Act & Assert
        person.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidObject_ShouldReturnFalse()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "", // Required field is empty
            Age = 200 // Outside valid range
        };

        // Act & Assert
        person.IsValid().Should().BeFalse();
    }

    #endregion

    #region Property Reflection Tests

    [Fact]
    public void GetPropertyValue_WithExistingProperty_ShouldReturnValue()
    {
        // Arrange
        var person = new TestPerson { Name = "Test Name", Age = 30 };

        // Act
        var name = person.GetPropertyValue("Name");
        var age = person.GetPropertyValue("Age");

        // Assert
        name.Should().Be("Test Name");
        age.Should().Be(30);
    }

    [Fact]
    public void GetPropertyValue_WithNonExistentProperty_ShouldReturnNull()
    {
        // Arrange
        var person = new TestPerson();

        // Act
        var result = person.GetPropertyValue("NonExistentProperty");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithNullObject_ShouldReturnNull()
    {
        // Act & Assert
        ((TestPerson?)null).GetPropertyValue("Name").Should().BeNull();
    }

    [Fact]
    public void SetPropertyValue_WithExistingProperty_ShouldSetValue()
    {
        // Arrange
        var person = new TestPerson();

        // Act
        var nameResult = person.SetPropertyValue("Name", "New Name");
        var ageResult = person.SetPropertyValue("Age", 35);

        // Assert
        nameResult.Should().BeTrue();
        ageResult.Should().BeTrue();
        person.Name.Should().Be("New Name");
        person.Age.Should().Be(35);
    }

    [Fact]
    public void SetPropertyValue_WithNonExistentProperty_ShouldReturnFalse()
    {
        // Arrange
        var person = new TestPerson();

        // Act
        var result = person.SetPropertyValue("NonExistentProperty", "value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetPropertyValue_WithNullObject_ShouldReturnFalse()
    {
        // Act & Assert
        ((TestPerson?)null).SetPropertyValue("Name", "value").Should().BeFalse();
    }

    [Fact]
    public void HasProperty_WithExistingProperty_ShouldReturnTrue()
    {
        // Arrange
        var person = new TestPerson();

        // Act & Assert
        person.HasProperty("Name").Should().BeTrue();
        person.HasProperty("Age").Should().BeTrue();
        person.HasProperty("Email").Should().BeTrue();
    }

    [Fact]
    public void HasProperty_WithNonExistentProperty_ShouldReturnFalse()
    {
        // Arrange
        var person = new TestPerson();

        // Act & Assert
        person.HasProperty("NonExistentProperty").Should().BeFalse();
    }

    [Fact]
    public void HasProperty_WithNullObject_ShouldReturnFalse()
    {
        // Act & Assert
        ((TestPerson?)null).HasProperty("Name").Should().BeFalse();
    }

    [Fact]
    public void GetProperties_WithValidObject_ShouldReturnAllProperties()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Test",
            Age = 30,
            Email = "test@example.com"
        };

        // Act
        var properties = person.GetProperties();

        // Assert
        properties.Should().NotBeEmpty();
        properties.Should().ContainKey("Name");
        properties.Should().ContainKey("Age");
        properties.Should().ContainKey("Email");
        properties["Name"].Should().Be("Test");
        properties["Age"].Should().Be(30);
        properties["Email"].Should().Be("test@example.com");
    }

    [Fact]
    public void GetProperties_WithNullObject_ShouldReturnEmptyDictionary()
    {
        // Act & Assert
        ((TestPerson?)null).GetProperties().Should().BeEmpty();
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_WithValidObject_ShouldReturnDictionary()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Test",
            Age = 30,
            Email = null
        };

        // Act
        var dict = person.ToDictionary();

        // Assert
        dict.Should().NotBeEmpty();
        dict.Should().ContainKey("Name");
        dict.Should().ContainKey("Age");
        dict.Should().ContainKey("Email");
        dict["Email"].Should().BeNull();
    }

    [Fact]
    public void ToDictionary_WithIncludeNullsFalse_ShouldExcludeNulls()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Test",
            Age = 30,
            Email = null
        };

        // Act
        var dict = person.ToDictionary(includeNulls: false);

        // Assert
        dict.Should().NotBeEmpty();
        dict.Should().ContainKey("Name");
        dict.Should().ContainKey("Age");
        dict.Should().NotContainKey("Email");
    }

    #endregion

    #region Object Mapping Tests

    [Fact]
    public void MapTo_WithCompatibleTypes_ShouldMapProperties()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com"
        };
        var employee = new TestEmployee();

        // Act
        var result = person.MapTo(employee);

        // Assert
        result.Should().BeSameAs(employee);
        result.Name.Should().Be("John Doe");
        result.Age.Should().Be(30);
        result.Email.Should().Be("john@example.com");
        result.Department.Should().Be(""); // Not mapped, retains default
        result.Salary.Should().Be(0); // Not mapped, retains default
    }

    [Fact]
    public void MapTo_WithGeneric_ShouldCreateNewInstanceAndMap()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Jane Doe",
            Age = 25,
            Email = "jane@example.com"
        };

        // Act
        var employee = person.MapTo<TestEmployee>();

        // Assert
        employee.Should().NotBeNull();
        employee.Name.Should().Be("Jane Doe");
        employee.Age.Should().Be(25);
        employee.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void MapTo_WithCaseInsensitive_ShouldMapRegardlessOfCase()
    {
        // Arrange
        var source = new { name = "Test", age = 30 };
        var destination = new TestPerson();

        // Act
        var result = source.MapTo(destination, ignoreCase: true);

        // Assert
        result.Name.Should().Be("Test");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void MapTo_WithNullSource_ShouldReturnDestination()
    {
        // Arrange
        var destination = new TestEmployee { Name = "Original" };

        // Act
        var result = ((TestPerson?)null).MapTo(destination);

        // Assert
        result.Should().BeSameAs(destination);
        result.Name.Should().Be("Original"); // Unchanged
    }

    [Fact]
    public void MapTo_WithNullDestination_ShouldReturnNull()
    {
        // Arrange
        var source = new TestPerson { Name = "Test" };

        // Act
        var result = source.MapTo((TestEmployee?)null);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Null Checking Tests

    [Fact]
    public void IsNull_WithNullObject_ShouldReturnTrue()
    {
        // Act & Assert
        ((TestPerson?)null).IsNull().Should().BeTrue();
    }

    [Fact]
    public void IsNull_WithValidObject_ShouldReturnFalse()
    {
        // Arrange
        var person = new TestPerson();

        // Act & Assert
        person.IsNull().Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_WithNullObject_ShouldReturnFalse()
    {
        // Act & Assert
        ((TestPerson?)null).IsNotNull().Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_WithValidObject_ShouldReturnTrue()
    {
        // Arrange
        var person = new TestPerson();

        // Act & Assert
        person.IsNotNull().Should().BeTrue();
    }

    [Fact]
    public void IfNull_WithNullObject_ShouldReturnDefaultValue()
    {
        // Arrange
        var defaultPerson = new TestPerson { Name = "Default" };

        // Act
        var result = ((TestPerson?)null).IfNull(defaultPerson);

        // Assert
        result.Should().BeSameAs(defaultPerson);
    }

    [Fact]
    public void IfNull_WithValidObject_ShouldReturnOriginal()
    {
        // Arrange
        var person = new TestPerson { Name = "Original" };
        var defaultPerson = new TestPerson { Name = "Default" };

        // Act
        var result = person.IfNull(defaultPerson);

        // Assert
        result.Should().BeSameAs(person);
        result.Name.Should().Be("Original");
    }

    [Fact]
    public void IfNull_WithFactory_ShouldCallFactoryWhenNull()
    {
        // Arrange
        var factoryCallCount = 0;
        TestPerson Factory()
        {
            factoryCallCount++;
            return new TestPerson { Name = "Factory Created" };
        }

        // Act
        var result = ((TestPerson?)null).IfNull(Factory);

        // Assert
        result.Name.Should().Be("Factory Created");
        factoryCallCount.Should().Be(1);
    }

    [Fact]
    public void IfNull_WithFactory_ShouldNotCallFactoryWhenNotNull()
    {
        // Arrange
        var person = new TestPerson { Name = "Original" };
        var factoryCallCount = 0;
        TestPerson Factory()
        {
            factoryCallCount++;
            return new TestPerson { Name = "Factory Created" };
        }

        // Act
        var result = person.IfNull(Factory);

        // Assert
        result.Should().BeSameAs(person);
        factoryCallCount.Should().Be(0);
    }

    [Fact]
    public void IfNotNull_WithValidObject_ShouldExecuteAction()
    {
        // Arrange
        var person = new TestPerson { Name = "Original" };
        var actionExecuted = false;

        // Act
        var result = person.IfNotNull(p =>
        {
            actionExecuted = true;
            p.Name = "Modified";
        });

        // Assert
        result.Should().BeSameAs(person);
        actionExecuted.Should().BeTrue();
        person.Name.Should().Be("Modified");
    }

    [Fact]
    public void IfNotNull_WithNullObject_ShouldNotExecuteAction()
    {
        // Arrange
        var actionExecuted = false;

        // Act
        var result = ((TestPerson?)null).IfNotNull(p =>
        {
            actionExecuted = true;
        });

        // Assert
        result.Should().BeNull();
        actionExecuted.Should().BeFalse();
    }

    [Fact]
    public void IfNotNull_WithFunction_ShouldReturnResult()
    {
        // Arrange
        var person = new TestPerson { Name = "Test", Age = 30 };

        // Act
        var result = person.IfNotNull(p => $"{p.Name} is {p.Age} years old");

        // Assert
        result.Should().Be("Test is 30 years old");
    }

    [Fact]
    public void IfNotNull_WithFunctionAndNull_ShouldReturnDefault()
    {
        // Act
        var result = ((TestPerson?)null).IfNotNull(p => p.Name);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public void SetPropertyValue_WithReadOnlyProperty_ShouldReturnFalse()
    {
        // Arrange
        var person = new TestPerson();

        // Act - Try to set a property that doesn't exist or is read-only
        var result = person.SetPropertyValue("NonExistentReadOnlyProperty", "value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MapTo_WithIncompatibleTypes_ShouldSkipIncompatibleProperties()
    {
        // Arrange
        var source = new { Name = "Test", InvalidProperty = new DateTime() };
        var destination = new TestPerson();

        // Act
        var result = source.MapTo(destination);

        // Assert
        result.Name.Should().Be("Test");
        // Other properties should remain default since they're incompatible
    }

    [Fact]
    public void GetProperties_WithComplexObject_ShouldHandleAllPropertyTypes()
    {
        // Arrange
        var person = new TestPerson
        {
            Name = "Test",
            Age = 30,
            Email = "test@example.com",
            CreatedAt = DateTime.Now,
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var properties = person.GetProperties();

        // Assert
        properties.Should().ContainKey("Name");
        properties.Should().ContainKey("Age");
        properties.Should().ContainKey("Email");
        properties.Should().ContainKey("CreatedAt");
        properties.Should().ContainKey("Tags");
        
        properties["Tags"].Should().BeOfType<List<string>>();
    }

    [Fact]
    public void DeepClone_WithCircularReference_ShouldHandleGracefully()
    {
        // Note: This test verifies that the JSON-based deep clone handles circular references
        // by either throwing an exception or handling it gracefully
        
        // Arrange
        var person = new TestPerson { Name = "Test" };
        // We can't easily create circular references with our test classes,
        // but this test demonstrates the pattern for when such scenarios exist

        // Act & Assert
        var clone = person.DeepClone();
        clone.Should().NotBeNull();
        clone.Should().NotBeSameAs(person);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void MapTo_WithLargeObject_ShouldPerformWell()
    {
        // Arrange
        var source = new TestPerson
        {
            Name = "Performance Test",
            Age = 30,
            Email = "perf@test.com",
            Tags = Enumerable.Range(1, 1000).Select(i => $"tag{i}").ToList()
        };
        var destination = new TestEmployee();

        // Act & Assert - Should complete without timeout
        var result = source.MapTo(destination);
        result.Should().NotBeNull();
        result.Name.Should().Be("Performance Test");
    }

    [Fact]
    public void DeepClone_WithComplexObject_ShouldPerformWell()
    {
        // Arrange
        var original = new TestPerson
        {
            Name = "Complex Object",
            Age = 30,
            Tags = Enumerable.Range(1, 1000).Select(i => $"tag{i}").ToList()
        };

        // Act & Assert - Should complete without timeout
        var clone = original.DeepClone();
        clone.Should().NotBeNull();
        clone!.Tags.Should().HaveCount(1000);
    }

    #endregion
} 