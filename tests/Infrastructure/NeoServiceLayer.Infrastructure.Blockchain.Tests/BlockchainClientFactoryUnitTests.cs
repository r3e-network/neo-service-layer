using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Infrastructure.Blockchain;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Blockchain.Tests;

public class BlockchainClientFactoryUnitTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<BlockchainClientFactory>> _mockLogger;
    private readonly BlockchainClientFactory _factory;

    public BlockchainClientFactoryUnitTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<BlockchainClientFactory>>();
        _factory = new BlockchainClientFactory(_mockServiceProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var factory = new BlockchainClientFactory(_mockServiceProvider.Object, _mockLogger.Object);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new BlockchainClientFactory(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithMessage("*serviceProvider*");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new BlockchainClientFactory(_mockServiceProvider.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [Fact]
    public void CreateClient_WithNeoN3Type_ReturnsNeoN3Client()
    {
        // Arrange
        var mockNeoN3Client = new Mock<IBlockchainClient>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(INeoN3Client)))
            .Returns(mockNeoN3Client.Object);

        // Act
        var result = _factory.CreateClient(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockNeoN3Client.Object);
        _mockServiceProvider.Verify(x => x.GetService(typeof(INeoN3Client)), Times.Once);
    }

    [Fact]
    public void CreateClient_WithNeoXType_ReturnsNeoXClient()
    {
        // Arrange
        var mockNeoXClient = new Mock<IBlockchainClient>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(INeoXClient)))
            .Returns(mockNeoXClient.Object);

        // Act
        var result = _factory.CreateClient(BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockNeoXClient.Object);
        _mockServiceProvider.Verify(x => x.GetService(typeof(INeoXClient)), Times.Once);
    }

    [Fact]
    public void CreateClient_WithEthereumType_ReturnsEthereumClient()
    {
        // Arrange
        var mockEthereumClient = new Mock<IBlockchainClient>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEthereumClient)))
            .Returns(mockEthereumClient.Object);

        // Act
        var result = _factory.CreateClient(BlockchainType.Ethereum);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockEthereumClient.Object);
        _mockServiceProvider.Verify(x => x.GetService(typeof(IEthereumClient)), Times.Once);
    }

    [Fact]
    public void CreateClient_WithUnsupportedType_ThrowsArgumentException()
    {
        // Arrange
        var unsupportedType = (BlockchainType)999;

        // Act & Assert
        Action act = () => _factory.CreateClient(unsupportedType);
        act.Should().Throw<ArgumentException>().WithMessage("*Unsupported blockchain type*");
    }

    [Fact]
    public void CreateClient_WhenServiceNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockServiceProvider.Setup(x => x.GetService(typeof(INeoN3Client)))
            .Returns(null); // Service not registered

        // Act & Assert
        Action act = () => _factory.CreateClient(BlockchainType.NeoN3);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BlockchainClient for type NeoN3 is not registered*");
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3, typeof(INeoN3Client))]
    [InlineData(BlockchainType.NeoX, typeof(INeoXClient))]
    [InlineData(BlockchainType.Ethereum, typeof(IEthereumClient))]
    public void CreateClient_WithValidTypes_RequestsCorrectService(BlockchainType blockchainType, Type expectedServiceType)
    {
        // Arrange
        var mockClient = new Mock<IBlockchainClient>();
        _mockServiceProvider.Setup(x => x.GetService(expectedServiceType))
            .Returns(mockClient.Object);

        // Act
        var result = _factory.CreateClient(blockchainType);

        // Assert
        result.Should().NotBeNull();
        _mockServiceProvider.Verify(x => x.GetService(expectedServiceType), Times.Once);
    }

    [Fact]
    public void IsSupported_WithSupportedTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        _factory.IsSupported(BlockchainType.NeoN3).Should().BeTrue();
        _factory.IsSupported(BlockchainType.NeoX).Should().BeTrue();
        _factory.IsSupported(BlockchainType.Ethereum).Should().BeTrue();
    }

    [Fact]
    public void IsSupported_WithUnsupportedType_ReturnsFalse()
    {
        // Arrange
        var unsupportedType = (BlockchainType)999;

        // Act & Assert
        _factory.IsSupported(unsupportedType).Should().BeFalse();
    }

    [Fact]
    public void GetSupportedTypes_ReturnsAllSupportedTypes()
    {
        // Act
        var supportedTypes = _factory.GetSupportedTypes();

        // Assert
        supportedTypes.Should().NotBeNull();
        supportedTypes.Should().Contain(BlockchainType.NeoN3);
        supportedTypes.Should().Contain(BlockchainType.NeoX);
        supportedTypes.Should().Contain(BlockchainType.Ethereum);
        supportedTypes.Count().Should().Be(3);
    }

    [Fact]
    public void CreateClient_CalledMultipleTimes_RequestsServiceEachTime()
    {
        // Arrange
        var mockClient = new Mock<IBlockchainClient>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(INeoN3Client)))
            .Returns(mockClient.Object);

        // Act
        var result1 = _factory.CreateClient(BlockchainType.NeoN3);
        var result2 = _factory.CreateClient(BlockchainType.NeoN3);
        var result3 = _factory.CreateClient(BlockchainType.NeoN3);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();
        _mockServiceProvider.Verify(x => x.GetService(typeof(INeoN3Client)), Times.Exactly(3));
    }
}

// Mock interfaces for testing
public interface INeoN3Client : IBlockchainClient { }
public interface INeoXClient : IBlockchainClient { }
public interface IEthereumClient : IBlockchainClient { }

public enum BlockchainType
{
    NeoN3,
    NeoX,
    Ethereum
}