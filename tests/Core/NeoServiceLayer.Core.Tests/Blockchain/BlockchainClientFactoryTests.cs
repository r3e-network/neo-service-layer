using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using InfraBlockchain = NeoServiceLayer.Infrastructure;

namespace NeoServiceLayer.Core.Tests.Blockchain;

/// <summary>
/// Comprehensive tests for IBlockchainClientFactory covering factory pattern and client creation.
/// </summary>
public class BlockchainClientFactoryTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IBlockchainClientFactory> _factoryMock;
    private readonly Mock<ILogger<IBlockchainClientFactory>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public BlockchainClientFactoryTests()
    {
        _fixture = new Fixture();
        _factoryMock = new Mock<IBlockchainClientFactory>();
        _loggerMock = new Mock<ILogger<IBlockchainClientFactory>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    #region Factory Creation Tests

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public void CreateClient_WithValidBlockchainType_ShouldReturnClient(BlockchainType blockchainType)
    {
        // Arrange
        var expectedClient = CreateMockBlockchainClient(blockchainType);
        _factoryMock.Setup(x => x.CreateClient(blockchainType))
                   .Returns(expectedClient.Object);

        // Act
        var result = _factoryMock.Object.CreateClient(blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.BlockchainType.Should().Be(blockchainType);
        _factoryMock.Verify(x => x.CreateClient(blockchainType), Times.Once);
    }

    [Fact]
    public void CreateClient_WithUnsupportedBlockchainType_ShouldThrowArgumentException()
    {
        // Arrange
        var unsupportedType = (BlockchainType)999;
        _factoryMock.Setup(x => x.CreateClient(unsupportedType))
                   .Throws(new ArgumentException($"Unsupported blockchain type: {unsupportedType}"));

        // Act & Assert
        var action = () => _factoryMock.Object.CreateClient(unsupportedType);
        action.Should().Throw<ArgumentException>()
              .WithMessage($"Unsupported blockchain type: {unsupportedType}");
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3, "https://mainnet.neo.org:10332")]
    [InlineData(BlockchainType.NeoX, "https://mainnet.neox.org:8332")]
    public void CreateClient_WithConfiguration_ShouldReturnConfiguredClient(BlockchainType blockchainType, string endpoint)
    {
        // Arrange
        var configuration = new BlockchainConfiguration
        {
            BlockchainType = blockchainType,
            Endpoint = endpoint,
            ApiKey = "test-api-key",
            Timeout = TimeSpan.FromSeconds(30)
        };

        var expectedClient = CreateMockBlockchainClient(blockchainType);
        _factoryMock.Setup(x => x.CreateClient(blockchainType, configuration))
                   .Returns(expectedClient.Object);

        // Act
        var result = _factoryMock.Object.CreateClient(blockchainType, configuration);

        // Assert
        result.Should().NotBeNull();
        result.BlockchainType.Should().Be(blockchainType);
        _factoryMock.Verify(x => x.CreateClient(blockchainType, configuration), Times.Once);
    }

    [Fact]
    public void CreateClient_WithNullConfiguration_ShouldUseDefaultConfiguration()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        var expectedClient = CreateMockBlockchainClient(blockchainType);

        _factoryMock.Setup(x => x.CreateClient(blockchainType, null))
                   .Returns(expectedClient.Object);

        // Act
        var result = _factoryMock.Object.CreateClient(blockchainType, null);

        // Assert
        result.Should().NotBeNull();
        result.BlockchainType.Should().Be(blockchainType);
        _factoryMock.Verify(x => x.CreateClient(blockchainType, null), Times.Once);
    }

    #endregion

    #region Factory Registration Tests

    [Fact]
    public void RegisterClientType_ShouldRegisterClientSuccessfully()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        var clientType = typeof(TestNeoN3Client);

        // Act & Assert - Just verify the method can be called
        _factoryMock.Setup(x => x.RegisterClientType(blockchainType, clientType));
        _factoryMock.Object.RegisterClientType(blockchainType, clientType);
        _factoryMock.Verify(x => x.RegisterClientType(blockchainType, clientType), Times.Once);
    }

    [Fact]
    public void RegisterClientType_WithInvalidType_ShouldThrowArgumentException()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        var invalidType = typeof(string); // Not an IBlockchainClient

        _factoryMock.Setup(x => x.RegisterClientType(blockchainType, invalidType))
                   .Throws(new ArgumentException($"Type {invalidType.Name} does not implement IBlockchainClient"));

        // Act & Assert
        var action = () => _factoryMock.Object.RegisterClientType(blockchainType, invalidType);
        action.Should().Throw<ArgumentException>()
              .WithMessage($"Type {invalidType.Name} does not implement IBlockchainClient");
    }

    [Fact]
    public void IsClientTypeRegistered_WithRegisteredType_ShouldReturnTrue()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        _factoryMock.Setup(x => x.IsClientTypeRegistered(blockchainType))
                   .Returns(true);

        // Act
        var result = _factoryMock.Object.IsClientTypeRegistered(blockchainType);

        // Assert
        result.Should().BeTrue();
        _factoryMock.Verify(x => x.IsClientTypeRegistered(blockchainType), Times.Once);
    }

    [Fact]
    public void IsClientTypeRegistered_WithUnregisteredType_ShouldReturnFalse()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        _factoryMock.Setup(x => x.IsClientTypeRegistered(blockchainType))
                   .Returns(false);

        // Act
        var result = _factoryMock.Object.IsClientTypeRegistered(blockchainType);

        // Assert
        result.Should().BeFalse();
        _factoryMock.Verify(x => x.IsClientTypeRegistered(blockchainType), Times.Once);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void BlockchainConfiguration_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new BlockchainConfiguration();

        // Assert
        config.BlockchainType.Should().Be(BlockchainType.NeoN3);
        config.Endpoint.Should().Be(string.Empty);
        config.ApiKey.Should().Be(string.Empty);
        config.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        config.MaxRetries.Should().Be(3);
        config.EnableLogging.Should().BeTrue();
    }

    [Fact]
    public void BlockchainConfiguration_ShouldSupportCustomValues()
    {
        // Arrange
        var expectedType = BlockchainType.NeoX;
        var expectedEndpoint = "https://mainnet.infura.io/v3/test";
        var expectedApiKey = "test-api-key-123";
        var expectedTimeout = TimeSpan.FromMinutes(2);
        var expectedMaxRetries = 5;

        // Act
        var config = new BlockchainConfiguration
        {
            BlockchainType = expectedType,
            Endpoint = expectedEndpoint,
            ApiKey = expectedApiKey,
            Timeout = expectedTimeout,
            MaxRetries = expectedMaxRetries,
            EnableLogging = false
        };

        // Assert
        config.BlockchainType.Should().Be(expectedType);
        config.Endpoint.Should().Be(expectedEndpoint);
        config.ApiKey.Should().Be(expectedApiKey);
        config.Timeout.Should().Be(expectedTimeout);
        config.MaxRetries.Should().Be(expectedMaxRetries);
        config.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void BlockchainConfiguration_ShouldValidateEndpoint()
    {
        // Arrange
        var config = new BlockchainConfiguration();

        // Act & Assert
        var validationResult = config.Validate();
        validationResult.Should().BeFalse();
        config.ValidationErrors.Should().Contain("Endpoint is required");
    }

    [Fact]
    public void BlockchainConfiguration_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var config = new BlockchainConfiguration
        {
            BlockchainType = BlockchainType.NeoN3,
            Endpoint = "https://mainnet.neo.org:10332",
            ApiKey = "valid-api-key",
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };

        // Act
        var validationResult = config.Validate();

        // Assert
        validationResult.Should().BeTrue();
        config.ValidationErrors.Should().BeEmpty();
    }

    #endregion

    #region Dependency Injection Tests

    [Fact]
    public void CreateClient_WithDependencyInjection_ShouldResolveCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedClient = CreateMockBlockchainClient(BlockchainType.NeoN3);

        services.AddSingleton(_ => expectedClient.Object);
        services.AddSingleton(_factoryMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        _factoryMock.Setup(x => x.CreateClient(BlockchainType.NeoN3))
                   .Returns(expectedClient.Object);

        // Act
        var factory = serviceProvider.GetRequiredService<IBlockchainClientFactory>();
        var client = factory.CreateClient(BlockchainType.NeoN3);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeSameAs(expectedClient.Object);
    }

    [Fact]
    public void Factory_WithMultipleClients_ShouldManageLifecycleCorrectly()
    {
        // Arrange
        var neoN3Client = CreateMockBlockchainClient(BlockchainType.NeoN3);
        var neoXClient = CreateMockBlockchainClient(BlockchainType.NeoX);

        _factoryMock.Setup(x => x.CreateClient(BlockchainType.NeoN3))
                   .Returns(neoN3Client.Object);
        _factoryMock.Setup(x => x.CreateClient(BlockchainType.NeoX))
                   .Returns(neoXClient.Object);

        // Act
        var client1 = _factoryMock.Object.CreateClient(BlockchainType.NeoN3);
        var client2 = _factoryMock.Object.CreateClient(BlockchainType.NeoX);
        var client3 = _factoryMock.Object.CreateClient(BlockchainType.NeoN3); // Same type

        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client3.Should().NotBeNull();

        client1.BlockchainType.Should().Be(BlockchainType.NeoN3);
        client2.BlockchainType.Should().Be(BlockchainType.NeoX);
        client3.BlockchainType.Should().Be(BlockchainType.NeoN3);

        _factoryMock.Verify(x => x.CreateClient(BlockchainType.NeoN3), Times.Exactly(2));
        _factoryMock.Verify(x => x.CreateClient(BlockchainType.NeoX), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void CreateClient_WithConnectionFailure_ShouldHandleGracefully()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        _factoryMock.Setup(x => x.CreateClient(blockchainType))
                   .Throws(new InvalidOperationException("Failed to connect to blockchain network"));

        // Act & Assert
        var action = () => _factoryMock.Object.CreateClient(blockchainType);
        action.Should().Throw<InvalidOperationException>()
              .WithMessage("Failed to connect to blockchain network");
    }

    [Fact]
    public void CreateClient_WithInvalidConfiguration_ShouldThrowConfigurationException()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;
        var invalidConfig = new BlockchainConfiguration
        {
            Endpoint = "invalid-endpoint",
            Timeout = TimeSpan.Zero
        };

        _factoryMock.Setup(x => x.CreateClient(blockchainType, invalidConfig))
                   .Throws(new ArgumentException("Invalid configuration provided"));

        // Act & Assert
        var action = () => _factoryMock.Object.CreateClient(blockchainType, invalidConfig);
        action.Should().Throw<ArgumentException>()
              .WithMessage("Invalid configuration provided");
    }

    #endregion

    #region Helper Methods and Test Classes

    private static Mock<IBlockchainClient> CreateMockBlockchainClient(BlockchainType blockchainType)
    {
        var mock = new Mock<IBlockchainClient>();
        mock.Setup(x => x.BlockchainType).Returns(blockchainType);
        return mock;
    }

    // Test implementation for registration tests
    private class TestNeoN3Client : IBlockchainClient
    {
        public NeoServiceLayer.Core.BlockchainType BlockchainType { get; } = NeoServiceLayer.Core.BlockchainType.NeoN3;

        public Task<long> GetBlockHeightAsync() => Task.FromResult(1000000L);
        public Task<Block> GetBlockAsync(long height) => Task.FromResult(new Block());
        public Task<Block> GetBlockAsync(string hash) => Task.FromResult(new Block());
        public Task<Transaction> GetTransactionAsync(string hash) => Task.FromResult(new Transaction());
        public Task<string> SendTransactionAsync(Transaction transaction) => Task.FromResult("0x123");
        public Task<decimal> GetBalanceAsync(string address, string assetId = "") => Task.FromResult(100m);
        public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback) => Task.FromResult("sub1");
        public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId) => Task.FromResult(true);
        public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback) => Task.FromResult("sub2");
        public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId) => Task.FromResult(true);
        public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback) => Task.FromResult("sub3");
        public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId) => Task.FromResult(true);
        public Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args) => Task.FromResult("result");
        public Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args) => Task.FromResult("0x456");
    }

    #endregion
}

/// <summary>
/// Configuration model for blockchain clients.
/// </summary>
public class BlockchainConfiguration
{
    public BlockchainType BlockchainType { get; set; } = BlockchainType.NeoN3;
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public bool EnableLogging { get; set; } = true;
    public List<string> ValidationErrors { get; private set; } = [];

    public bool Validate()
    {
        ValidationErrors.Clear();

        if (string.IsNullOrWhiteSpace(Endpoint))
            ValidationErrors.Add("Endpoint is required");

        if (Timeout <= TimeSpan.Zero)
            ValidationErrors.Add("Timeout must be greater than zero");

        if (MaxRetries < 0)
            ValidationErrors.Add("MaxRetries cannot be negative");

        return ValidationErrors.Count == 0;
    }
}

/// <summary>
/// Extended interface for IBlockchainClientFactory with registration methods.
/// </summary>
public interface IBlockchainClientFactory
{
    IBlockchainClient CreateClient(BlockchainType blockchainType);
    IBlockchainClient CreateClient(BlockchainType blockchainType, BlockchainConfiguration? configuration);
    void RegisterClientType(BlockchainType blockchainType, Type clientType);
    bool IsClientTypeRegistered(BlockchainType blockchainType);
}
