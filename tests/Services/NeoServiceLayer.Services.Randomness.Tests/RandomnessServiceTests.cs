using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;

// Use Infrastructure namespace for IBlockchainClientFactory and IBlockchainClient
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;

namespace NeoServiceLayer.Services.Randomness.Tests;

public class RandomnessServiceTests
{
    private readonly Mock<ILogger<RandomnessService>> _loggerMock;
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IBlockchainClientFactory> _blockchainClientFactoryMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly Mock<IBlockchainClient> _blockchainClientMock;
    private readonly RandomnessService _service;

    public RandomnessServiceTests()
    {
        _loggerMock = new Mock<ILogger<RandomnessService>>();
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _blockchainClientFactoryMock = new Mock<IBlockchainClientFactory>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _blockchainClientMock = new Mock<IBlockchainClient>();

        // Setup configuration mock
        _configurationMock
            .Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, defaultValue) => defaultValue);

        // Setup blockchain client factory mock
        _blockchainClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<BlockchainType>()))
            .Returns(_blockchainClientMock.Object);

        // Setup blockchain client mock
        _blockchainClientMock
            .Setup(c => c.GetBlockHeightAsync())
            .ReturnsAsync(1000L);

        _blockchainClientMock
            .Setup(c => c.GetBlockHashAsync(It.IsAny<long>()))
            .ReturnsAsync("0x1234567890abcdef");

        // Setup enclave manager mock
        _enclaveManagerMock
            .Setup(e => e.InitializeEnclaveAsync())
            .ReturnsAsync(true);

        _enclaveManagerMock
            .Setup(e => e.GenerateRandomAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(42);

        _enclaveManagerMock
            .Setup(e => e.GenerateRandomBytesAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((int length, string seed) => Enumerable.Range(0, length).Select(i => (byte)(42 + i)).ToArray());

        _enclaveManagerMock
            .Setup(e => e.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("0102030405");

        _enclaveManagerMock
            .Setup(e => e.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _service = new RandomnessService(
            _enclaveManagerMock.Object,
            _blockchainClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _service.InitializeAsync();

        // Assert
        Assert.True(result);
        Assert.True(_service.IsEnclaveInitialized);
        _enclaveManagerMock.Verify(e => e.InitializeEnclaveAsync(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.StartAsync();

        // Assert
        Assert.True(result);
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.StopAsync();

        // Assert
        Assert.True(result);
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthy()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetHealthAsync();

        // Assert
        Assert.Equal(ServiceHealth.Healthy, result);
    }

    [Fact]
    public async Task GenerateRandomNumberAsync_ShouldReturnRandomNumber()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GenerateRandomNumberAsync(1, 100, BlockchainType.NeoN3);

        // Assert
        Assert.Equal(42, result);
        _enclaveManagerMock.Verify(e => e.GenerateRandomAsync(1, 100), Times.Once);
    }

    [Fact]
    public async Task GenerateRandomNumberAsync_ShouldThrowNotSupportedException_WhenBlockchainTypeIsNotSupported()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => _service.GenerateRandomNumberAsync(1, 100, (BlockchainType)999));
    }

    [Fact]
    public async Task GenerateRandomNumberAsync_ShouldThrowArgumentException_WhenMinIsGreaterThanMax()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomNumberAsync(100, 1, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GenerateRandomNumberAsync_ShouldThrowArgumentException_WhenRangeExceedsMaximum()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        _configurationMock
            .Setup(c => c.GetValue("Randomness:MaxRandomNumberRange", It.IsAny<string>()))
            .Returns("100");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomNumberAsync(1, 1000, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GenerateRandomBytesAsync_ShouldReturnRandomBytes()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GenerateRandomBytesAsync(5, BlockchainType.NeoN3);

        // Assert
        Assert.Equal(5, result.Length);
        _enclaveManagerMock.Verify(e => e.GenerateRandomBytesAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateRandomBytesAsync_ShouldThrowArgumentException_WhenLengthIsZeroOrNegative()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomBytesAsync(0, BlockchainType.NeoN3));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomBytesAsync(-1, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GenerateRandomBytesAsync_ShouldThrowArgumentException_WhenLengthExceedsMaximum()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        _configurationMock
            .Setup(c => c.GetValue("Randomness:MaxRandomBytesLength", It.IsAny<string>()))
            .Returns("100");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomBytesAsync(1000, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GenerateRandomStringAsync_ShouldReturnRandomString()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GenerateRandomStringAsync(10, null, BlockchainType.NeoN3);

        // Assert
        Assert.Equal(10, result.Length);
        // Verify that the enclave manager was called to generate random bytes
        _enclaveManagerMock.Verify(e => e.GenerateRandomBytesAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateRandomStringAsync_ShouldUseProvidedCharset()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var charset = "ABC";

        // Act
        var result = await _service.GenerateRandomStringAsync(5, charset, BlockchainType.NeoN3);

        // Assert
        Assert.Equal(5, result.Length);
        // All characters should be from the provided charset
        Assert.All(result, c => Assert.Contains(c, charset));
        // Verify that the enclave manager was called to generate random bytes
        _enclaveManagerMock.Verify(e => e.GenerateRandomBytesAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateRandomStringAsync_ShouldThrowArgumentException_WhenLengthIsZeroOrNegative()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomStringAsync(0, null, BlockchainType.NeoN3));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomStringAsync(-1, null, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GenerateRandomStringAsync_ShouldThrowArgumentException_WhenLengthExceedsMaximum()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        _configurationMock
            .Setup(c => c.GetValue("Randomness:MaxRandomStringLength", It.IsAny<string>()))
            .Returns("100");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateRandomStringAsync(1000, null, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GenerateVerifiableRandomNumberAsync_ShouldReturnVerifiableRandomResult()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GenerateVerifiableRandomNumberAsync(1, 100, "test-seed", BlockchainType.NeoN3);

        // Assert
        Assert.Equal(42, result.Value);
        Assert.Equal("test-seed", result.Seed);
        Assert.NotEmpty(result.Proof);
        Assert.Equal(BlockchainType.NeoN3, result.BlockchainType);
        Assert.Equal(1000L, result.BlockHeight);
        Assert.Equal("0x1234567890abcdef", result.BlockHash);
        Assert.NotEmpty(result.RequestId);
        _enclaveManagerMock.Verify(e => e.GenerateRandomAsync(1, 100), Times.Once);
        _blockchainClientFactoryMock.Verify(f => f.CreateClient(BlockchainType.NeoN3), Times.Once);
        _blockchainClientMock.Verify(c => c.GetBlockHeightAsync(), Times.Once);
        _blockchainClientMock.Verify(c => c.GetBlockHashAsync(1000L), Times.Once);
        _enclaveManagerMock.Verify(e => e.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task VerifyRandomNumberAsync_ShouldReturnTrue_WhenResultIsValid()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var result = new VerifiableRandomResult
        {
            Value = 42,
            Seed = "test-seed",
            Proof = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 }),
            BlockchainType = BlockchainType.NeoN3,
            BlockHeight = 1000,
            BlockHash = "0x1234567890abcdef"
        };

        // Act
        var isValid = await _service.VerifyRandomNumberAsync(result);

        // Assert
        Assert.True(isValid);
        _enclaveManagerMock.Verify(e => e.VerifySignatureAsync(
            It.Is<string>(s => s == "test-seed:0x1234567890abcdef:42"),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyRandomNumberAsync_ShouldThrowNotSupportedException_WhenBlockchainTypeIsNotSupported()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var result = new VerifiableRandomResult
        {
            Value = 42,
            Seed = "test-seed",
            Proof = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 }),
            BlockchainType = (BlockchainType)999,
            BlockHeight = 1000,
            BlockHash = "0x1234567890abcdef"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => _service.VerifyRandomNumberAsync(result));
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var metrics = await _service.GetMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.Contains("RequestCount", metrics.Keys);
        Assert.Contains("SuccessCount", metrics.Keys);
        Assert.Contains("FailureCount", metrics.Keys);
        Assert.Contains("StoredResultCount", metrics.Keys);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Create a mock IEnclaveService with the correct name and version
        var enclaveServiceMock = new Mock<IEnclaveService>();
        enclaveServiceMock.Setup(s => s.Name).Returns("EnclaveManager");
        enclaveServiceMock.Setup(s => s.Version).Returns("1.0.0");
        enclaveServiceMock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(IEnclaveService) });

        var services = new List<IService> { enclaveServiceMock.Object };

        // Act
        var result = await _service.ValidateDependenciesAsync(services);

        // Assert
        Assert.True(result);
    }
}
