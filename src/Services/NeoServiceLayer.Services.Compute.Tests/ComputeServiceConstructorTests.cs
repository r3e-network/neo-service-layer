using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Compute.Tests;

/// <summary>
/// Unit tests for ComputeService constructor and initialization.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "ComputeService")]
[Trait("Area", "Constructor")]
public class ComputeServiceConstructorTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;

    public ComputeServiceConstructorTests()
    {
        _mockLogger = new Mock<ILogger<ComputeService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("2000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("60000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Name.Should().Be("Compute");
        service.Description.Should().Be("High-Performance Verifiable Compute Service");
        service.Version.Should().Be("1.0.0");
        service.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        service.SupportedBlockchains.Should().Contain(BlockchainType.NeoX);
        service.SupportedBlockchains.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithNullEnclaveManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ComputeService(null!, _mockConfiguration.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ComputeService(_mockEnclaveManager.Object, null!, _mockLogger.Object));
    }

    // Note: Logger validation is handled by the dependency injection framework
    // and the base class, so we don't test for null logger scenarios

    [Fact]
    public void Constructor_SetsDefaultConfigurationValues()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        _mockConfiguration.Verify(x => x.GetValue("Compute:MaxComputationCount", "1000"), Times.Once);
        _mockConfiguration.Verify(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000"), Times.Once);
    }

    [Fact]
    public void Constructor_SetsMetadataCorrectly()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1500");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("45000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxComputationCount"].Should().Be("1500");
        service.Metadata["MaxExecutionTimeMs"].Should().Be("45000");
        service.Metadata["SupportedComputationTypes"].Should().Be("JavaScript,WebAssembly");
        service.Metadata["CreatedAt"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_AddsCapabilitiesCorrectly()
    {
        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Capabilities.Should().Contain(typeof(IComputeService));
        service.Capabilities.Should().Contain(typeof(IEnclaveService));
        service.Capabilities.Should().Contain(typeof(IBlockchainService));
    }

    [Fact]
    public void Constructor_AddsDependenciesCorrectly()
    {
        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Dependencies.Should().NotBeEmpty();
        service.Dependencies.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Constructor_InitializesInternalStateCorrectly()
    {
        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.IsRunning.Should().BeFalse();
        service.IsEnclaveInitialized.Should().BeFalse();
    }

    [Theory]
    [InlineData("500")]
    [InlineData("2000")]
    [InlineData("10000")]
    public void Constructor_WithDifferentMaxComputationCounts_SetsMetadataCorrectly(string maxCount)
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns(maxCount);
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxComputationCount"].Should().Be(maxCount);
    }

    [Theory]
    [InlineData("15000")]
    [InlineData("60000")]
    [InlineData("120000")]
    public void Constructor_WithDifferentMaxExecutionTimes_SetsMetadataCorrectly(string maxTime)
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns(maxTime);

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxExecutionTimeMs"].Should().Be(maxTime);
    }

    [Fact]
    public void Constructor_CreatedAtMetadata_IsValidDateTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);
        var afterCreation = DateTime.UtcNow;

        // Assert
        var createdAtString = service.Metadata["CreatedAt"];
        createdAtString.Should().NotBeNullOrEmpty();

        var createdAt = DateTime.Parse(createdAtString!).ToUniversalTime();
        createdAt.Should().BeOnOrAfter(beforeCreation.ToUniversalTime().AddMinutes(-1));
        createdAt.Should().BeOnOrBefore(afterCreation.ToUniversalTime().AddMinutes(1));
    }

    [Fact]
    public void Constructor_InheritsFromCorrectBaseClass()
    {
        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Should().BeAssignableTo<EnclaveBlockchainServiceBase>();
        service.Should().BeAssignableTo<IComputeService>();
        service.Should().BeAssignableTo<IEnclaveService>();
        service.Should().BeAssignableTo<IBlockchainService>();
    }

    [Fact]
    public void Constructor_SetsCorrectServiceType()
    {
        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.GetType().Name.Should().Be("ComputeService");
        service.GetType().Namespace.Should().Be("NeoServiceLayer.Services.Compute");
    }
}
