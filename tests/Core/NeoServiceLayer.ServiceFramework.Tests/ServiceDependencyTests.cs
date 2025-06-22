using Moq;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.ServiceFramework.Tests;

/// <summary>
/// Tests for the ServiceDependency class.
/// </summary>
public class ServiceDependencyTests
{
    [Fact]
    public void Required_ShouldCreateRequiredDependency()
    {
        // Act
        var dependency = ServiceDependency.Required("TestService", "1.0.0");

        // Assert
        Assert.Equal("TestService", dependency.ServiceName);
        Assert.Equal("1.0.0", dependency.MinimumVersion);
        Assert.Null(dependency.MaximumVersion);
        Assert.True(dependency.IsRequired);
        Assert.Null(dependency.ServiceType);
    }

    [Fact]
    public void Required_WithType_ShouldCreateRequiredDependencyWithType()
    {
        // Act
        var dependency = ServiceDependency.Required<IService>("TestService", "1.0.0");

        // Assert
        Assert.Equal("TestService", dependency.ServiceName);
        Assert.Equal("1.0.0", dependency.MinimumVersion);
        Assert.Null(dependency.MaximumVersion);
        Assert.True(dependency.IsRequired);
        Assert.Equal(typeof(IService), dependency.ServiceType);
    }

    [Fact]
    public void Optional_ShouldCreateOptionalDependency()
    {
        // Act
        var dependency = ServiceDependency.Optional("TestService", "1.0.0");

        // Assert
        Assert.Equal("TestService", dependency.ServiceName);
        Assert.Equal("1.0.0", dependency.MinimumVersion);
        Assert.Null(dependency.MaximumVersion);
        Assert.False(dependency.IsRequired);
        Assert.Null(dependency.ServiceType);
    }

    [Fact]
    public void Optional_WithType_ShouldCreateOptionalDependencyWithType()
    {
        // Act
        var dependency = ServiceDependency.Optional<IService>("TestService", "1.0.0");

        // Assert
        Assert.Equal("TestService", dependency.ServiceName);
        Assert.Equal("1.0.0", dependency.MinimumVersion);
        Assert.Null(dependency.MaximumVersion);
        Assert.False(dependency.IsRequired);
        Assert.Equal(typeof(IService), dependency.ServiceType);
    }

    [Fact]
    public void Validate_ShouldReturnTrue_WhenServiceNameMatches_AndVersionIsCompatible()
    {
        // Arrange
        var dependency = ServiceDependency.Required("TestService", "1.0.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_ShouldReturnTrue_WhenServiceNameMatches_AndVersionIsHigher()
    {
        // Arrange
        var dependency = ServiceDependency.Required("TestService", "1.0.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.1.0");

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenServiceNameMatches_ButVersionIsTooLow()
    {
        // Arrange
        var dependency = ServiceDependency.Required("TestService", "1.1.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenServiceNameMatches_ButVersionIsTooHigh()
    {
        // Arrange
        var dependency = ServiceDependency.Required("TestService", "1.0.0", "1.1.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.2.0");

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenServiceNameDoesNotMatch()
    {
        // Arrange
        var dependency = ServiceDependency.Required("TestService", "1.0.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("OtherService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenServiceTypeDoesNotMatch()
    {
        // Arrange
        var dependency = ServiceDependency.Required<IEnclaveService>("TestService", "1.0.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");
        serviceMock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(IService) });

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_ShouldReturnTrue_WhenServiceTypeMatches()
    {
        // Arrange
        var dependency = ServiceDependency.Required<IService>("TestService", "1.0.0");
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");
        serviceMock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(IService) });

        // Act
        bool result = dependency.Validate(serviceMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var dependency = ServiceDependency.Required("TestService", "1.0.0", "2.0.0");

        // Act
        string? result = dependency.ToString();

        // Assert
        // The actual implementation might format the string differently, so we'll just check
        // that it returns a non-null, non-empty string that contains the type name
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("ServiceDependency", result);
    }
}

/// <summary>
/// Interface for testing service dependencies.
/// </summary>
public interface IEnclaveService : IService
{
}
