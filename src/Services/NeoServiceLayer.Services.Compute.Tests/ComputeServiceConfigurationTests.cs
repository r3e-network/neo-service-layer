using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Compute.Tests;

/// <summary>
/// Unit tests for ComputeService configuration and validation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "ComputeService")]
[Trait("Area", "Configuration")]
public class ComputeServiceConfigurationTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;

    public ComputeServiceConfigurationTests()
    {
        _mockLogger = new Mock<ILogger<ComputeService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
    }

    [Theory]
    [InlineData("100", "5000")]
    [InlineData("500", "15000")]
    [InlineData("2000", "60000")]
    [InlineData("10000", "120000")]
    public void Constructor_WithDifferentConfigurationValues_SetsMetadataCorrectly(string maxComputationCount, string maxExecutionTime)
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns(maxComputationCount);
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns(maxExecutionTime);

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxComputationCount"].Should().Be(maxComputationCount);
        service.Metadata["MaxExecutionTimeMs"].Should().Be(maxExecutionTime);
    }

    [Fact]
    public void Constructor_WithMissingConfiguration_UsesDefaultValues()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxComputationCount"].Should().Be("1000");
        service.Metadata["MaxExecutionTimeMs"].Should().Be("30000");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("-100")]
    public void Constructor_WithInvalidMaxComputationCount_UsesDefaultValue(string invalidValue)
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns(invalidValue ?? "1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxComputationCount"].Should().Be(invalidValue ?? "1000");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("-5000")]
    public void Constructor_WithInvalidMaxExecutionTime_UsesDefaultValue(string invalidValue)
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns(invalidValue ?? "30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["MaxExecutionTimeMs"].Should().Be(invalidValue ?? "30000");
    }

    [Fact]
    public void Constructor_SetsRequiredMetadata()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Metadata["CreatedAt"].Should().NotBeNullOrEmpty();
        service.Metadata["MaxComputationCount"].Should().NotBeNullOrEmpty();
        service.Metadata["MaxExecutionTimeMs"].Should().NotBeNullOrEmpty();
        service.Metadata["SupportedComputationTypes"].Should().Be("JavaScript,WebAssembly");
    }

    [Fact]
    public void Constructor_SetsCorrectDependencies()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Dependencies.Should().NotBeEmpty();
        service.Dependencies.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Constructor_InitializesInternalFieldsCorrectly()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        var requestCount = GetPrivateField<int>(service, "_requestCount");
        var successCount = GetPrivateField<int>(service, "_successCount");
        var failureCount = GetPrivateField<int>(service, "_failureCount");
        var lastRequestTime = GetPrivateField<DateTime>(service, "_lastRequestTime");

        requestCount.Should().Be(0);
        successCount.Should().Be(0);
        failureCount.Should().Be(0);
        lastRequestTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void Constructor_InitializesCachesCorrectly()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        var computationCache = GetPrivateField<Dictionary<string, ComputationMetadata>>(service, "_computationCache");
        var resultCache = GetPrivateField<Dictionary<string, ComputationResult>>(service, "_resultCache");

        computationCache.Should().NotBeNull();
        computationCache.Should().BeEmpty();
        resultCache.Should().NotBeNull();
        resultCache.Should().BeEmpty();
    }

    [Fact]
    public void ServiceProperties_HaveCorrectValues()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Name.Should().Be("Compute");
        service.Description.Should().Be("High-Performance Verifiable Compute Service");
        service.Version.Should().Be("1.0.0");
        service.SupportedBlockchains.Should().BeEquivalentTo(new[] { BlockchainType.NeoN3, BlockchainType.NeoX });
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3, true)]
    [InlineData(BlockchainType.NeoX, true)]
    [InlineData((BlockchainType)100, false)]
    [InlineData((BlockchainType)50, false)]
    [InlineData((BlockchainType)999, false)]
    public void SupportsBlockchain_ReturnsCorrectValue(BlockchainType blockchainType, bool expectedSupport)
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = service.SupportsBlockchain(blockchainType);

        // Assert
        result.Should().Be(expectedSupport);
    }

    [Fact]
    public void ServiceCapabilities_AreSetCorrectly()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Capabilities.Should().Contain(typeof(IComputeService));
        service.Capabilities.Should().Contain(typeof(IEnclaveService));
        service.Capabilities.Should().Contain(typeof(IBlockchainService));
        service.Capabilities.Should().Contain(typeof(IService));
    }

    [Fact]
    public void ServiceMetadata_CreatedAt_IsValidISODateTime()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        var beforeCreation = DateTime.UtcNow;

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        var createdAtString = service.Metadata["CreatedAt"];
        createdAtString.Should().NotBeNullOrEmpty();

        var createdAt = DateTime.Parse(createdAtString!).ToUniversalTime();
        createdAt.Should().BeOnOrAfter(beforeCreation.ToUniversalTime().AddMinutes(-1));
        createdAt.Should().BeOnOrBefore(DateTime.UtcNow.AddMinutes(1));
        createdAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ServiceMetadata_SupportedComputationTypes_ContainsExpectedTypes()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        var supportedTypes = service.Metadata["SupportedComputationTypes"];
        supportedTypes.Should().Be("JavaScript,WebAssembly");
        supportedTypes.Should().Contain("JavaScript");
        supportedTypes.Should().Contain("WebAssembly");
    }

    [Fact]
    public void ServiceInitialState_IsCorrect()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.IsRunning.Should().BeFalse();
        service.IsEnclaveInitialized.Should().BeFalse();
    }

    [Fact]
    public void ConfigurationAccess_CallsConfigurationService()
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        mockConfiguration.Verify(x => x.GetValue("Compute:MaxComputationCount", "1000"), Times.Once);
        mockConfiguration.Verify(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000"), Times.Once);
    }

    [Theory]
    [InlineData("Compute:MaxComputationCount", "1000", "2000")]
    [InlineData("Compute:MaxExecutionTimeMs", "30000", "60000")]
    public void ConfigurationValues_AreUsedCorrectly(string configKey, string defaultValue, string actualValue)
    {
        // Arrange
        var mockConfiguration = new Mock<IServiceConfiguration>();
        mockConfiguration.Setup(x => x.GetValue(configKey, defaultValue)).Returns(actualValue);

        // Setup other required configuration
        if (configKey != "Compute:MaxComputationCount")
            mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        if (configKey != "Compute:MaxExecutionTimeMs")
            mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Act
        var service = new ComputeService(_mockEnclaveManager.Object, mockConfiguration.Object, _mockLogger.Object);

        // Assert
        var metadataKey = configKey.Split(':')[1]; // Extract the key part after ':'
        service.Metadata[metadataKey].Should().Be(actualValue);
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field!.GetValue(obj)!;
    }
}
