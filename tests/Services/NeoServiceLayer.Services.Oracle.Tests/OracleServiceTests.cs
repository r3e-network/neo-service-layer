using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;
// Use Infrastructure namespace for IBlockchainClientFactory and IBlockchainClient
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;

namespace NeoServiceLayer.Services.Oracle.Tests;

public class OracleServiceTests
{
    private readonly Mock<ILogger<OracleService>> _loggerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IBlockchainClientFactory> _blockchainClientFactoryMock;
    private readonly Mock<IBlockchainClient> _blockchainClientMock;
    private readonly Mock<IHttpClientService> _httpClientServiceMock;
    private readonly OracleService _service;

    public OracleServiceTests()
    {
        _loggerMock = new Mock<ILogger<OracleService>>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _blockchainClientFactoryMock = new Mock<IBlockchainClientFactory>();
        _blockchainClientMock = new Mock<IBlockchainClient>();
        _httpClientServiceMock = new Mock<IHttpClientService>();

        // Setup mocks
        _enclaveManagerMock.Setup(e => e.InitializeEnclaveAsync()).ReturnsAsync(true);
        _enclaveManagerMock.Setup(e => e.InitializeAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _enclaveManagerMock.Setup(e => e.GetDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string dataSource, string dataPath) =>
                $"{{\"value\": 42, \"source\": \"{dataSource}\", \"path\": \"{dataPath}\", \"timestamp\": \"{DateTime.UtcNow}\"}}");

        // Setup KMS encryption mock - all overloads
        _enclaveManagerMock.Setup(e => e.KmsEncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string keyId, string dataHex, string algorithm, CancellationToken ct) =>
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"encrypted_{dataHex}")));

        // Also setup the version without cancellation token
        _enclaveManagerMock.Setup(e => e.KmsEncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string keyId, string dataHex, string algorithm) =>
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"encrypted_{dataHex}")));

        // Setup Oracle fetch method
        _enclaveManagerMock.Setup(e => e.OracleFetchAndProcessDataAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"value\": 42, \"source\": \"test\", \"timestamp\": \"2024-01-01\"}");

        _blockchainClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<BlockchainType>()))
            .Returns(_blockchainClientMock.Object);
        _blockchainClientMock.Setup(c => c.GetBlockHeightAsync()).ReturnsAsync(1000L);
        _blockchainClientMock.Setup(c => c.GetBlockAsync(It.IsAny<long>()))
            .ReturnsAsync(new Block
            {
                Hash = "0x123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef01",
                Height = 1000L,
                Timestamp = DateTime.UtcNow,
                PreviousHash = "0x0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef00",
                Transactions = new List<Transaction>()
            });

        _configurationMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, defaultValue) => defaultValue);

        // Setup HTTP client service mock to return successful responses
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"test\": \"data\"}")
        };

        // Setup GetAsync overloads (only setup the actual interface methods)
        _httpClientServiceMock.Setup(h => h.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\": \"success\"}")
            });

        _httpClientServiceMock.Setup(h => h.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\": \"success\"}")
            });

        _service = new OracleService(
            _configurationMock.Object,
            _enclaveManagerMock.Object,
            _blockchainClientFactoryMock.Object,
            _httpClientServiceMock.Object,
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
    public async Task RegisterDataSourceAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.RegisterDataSourceAsync("https://example.com/api", "Example API", BlockchainType.NeoX);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RegisterDataSourceAsync_ShouldReturnFalse_WhenDataSourceAlreadyExists()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.RegisterDataSourceAsync("https://example.com/api", "Example API", BlockchainType.NeoX);

        // Act
        var result = await _service.RegisterDataSourceAsync("https://example.com/api", "Example API", BlockchainType.NeoX);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDataSourcesAsync_ShouldReturnDataSources()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.RegisterDataSourceAsync("https://example.com/api", "Example API", BlockchainType.NeoX);

        // Act
        var result = await _service.GetDataSourcesAsync(BlockchainType.NeoX);

        // Assert
        Assert.Single(result);
        Assert.Equal("https://example.com/api", result.First().Url);
        Assert.Equal("Example API", result.First().Description);
        Assert.Equal(BlockchainType.NeoX, result.First().BlockchainType);
    }

    [Fact]
    public async Task RemoveDataSourceAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.RegisterDataSourceAsync("https://example.com/api", "Example API", BlockchainType.NeoX);

        // Act
        var result = await _service.RemoveDataSourceAsync("https://example.com/api", BlockchainType.NeoX);

        // Assert
        Assert.True(result);
        var dataSources = await _service.GetDataSourcesAsync(BlockchainType.NeoX);
        Assert.Empty(dataSources);
    }

    [Fact]
    public async Task RemoveDataSourceAsync_ShouldReturnFalse_WhenDataSourceDoesNotExist()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.RemoveDataSourceAsync("https://example.com/api", BlockchainType.NeoX);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDataAsync_ShouldReturnData()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetDataAsync("https://example.com/api", "data.value", BlockchainType.NeoX);

        // Assert
        Assert.NotNull(result);
        _enclaveManagerMock.Verify(e => e.GetDataAsync("https://example.com/api", "data.value"), Times.Once);
        _blockchainClientFactoryMock.Verify(f => f.CreateClient(BlockchainType.NeoX), Times.Once);
        _blockchainClientMock.Verify(c => c.GetBlockHeightAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDataAsync_ShouldThrowNotSupportedException_WhenBlockchainTypeIsNotSupported()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => _service.GetDataAsync("https://example.com/api", "data.value", (BlockchainType)999));
    }

    [Fact]
    public async Task GetDataAsync_WithParameters_ShouldReturnData()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://example.com/api",
            ["dataPath"] = "data.value",
            ["blockchain"] = "NeoX"
        };

        // Act
        var result = await _service.GetDataAsync("feed1", parameters);

        // Assert
        Assert.NotNull(result);
        _enclaveManagerMock.Verify(e => e.GetDataAsync("https://example.com/api", "data.value"), Times.Once);
    }

    [Fact]
    public async Task SubscribeToFeedAsync_ShouldReturnSubscriptionId()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://example.com/api",
            ["dataPath"] = "data.value",
            ["blockchain"] = "NeoX",
            ["interval"] = "60000"
        };

        // Create a callback that will be called when data is received
        Func<string, Task> callback = data =>
        {
            // Just log the data and return a completed task
            _loggerMock.Object.LogInformation("Received data: {Data}", data);
            return Task.CompletedTask;
        };

        // Act
        var subscriptionId = await _service.SubscribeToFeedAsync("feed1", parameters, callback);

        // Assert
        Assert.NotNull(subscriptionId);
        Assert.NotEmpty(subscriptionId);

        // Wait a bit to allow the subscription to run at least once
        await Task.Delay(200);

        // Unsubscribe to clean up
        await _service.UnsubscribeFromFeedAsync(subscriptionId);
    }

    [Fact]
    public async Task UnsubscribeFromFeedAsync_ShouldReturnTrue_WhenSubscriptionExists()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://example.com/api",
            ["dataPath"] = "data.value",
            ["blockchain"] = "NeoX",
            ["interval"] = "60000"
        };
        var subscriptionId = await _service.SubscribeToFeedAsync("feed1", parameters, _ => Task.CompletedTask);

        // Act
        var result = await _service.UnsubscribeFromFeedAsync(subscriptionId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UnsubscribeFromFeedAsync_ShouldReturnFalse_WhenSubscriptionDoesNotExist()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.UnsubscribeFromFeedAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.False(result);
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
        Assert.Contains("SubscriptionCount", metrics.Keys);
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
