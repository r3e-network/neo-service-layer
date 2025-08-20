using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.Oracle.Models;
using NeoServiceLayer.Shared.Configuration;
using Xunit;

namespace NeoServiceLayer.Services.Oracle.Tests;

public class OracleServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<OracleService>> _mockLogger;
    private readonly Mock<IPersistentStorageProvider> _mockStorageProvider;
    private readonly Mock<IServiceConfiguration> _mockConfig;
    private readonly Mock<IHealthCheckService> _mockHealthCheck;
    private readonly Mock<ITelemetryCollector> _mockTelemetry;
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainFactory;
    private readonly Mock<ISecretsManager> _mockSecretsManager;
    private readonly OracleService _oracleService;

    public OracleServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<OracleService>>();
        _mockStorageProvider = new Mock<IPersistentStorageProvider>();
        _mockConfig = new Mock<IServiceConfiguration>();
        _mockHealthCheck = new Mock<IHealthCheckService>();
        _mockTelemetry = new Mock<ITelemetryCollector>();
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockBlockchainFactory = new Mock<IBlockchainClientFactory>();
        _mockSecretsManager = new Mock<ISecretsManager>();

        _mockConfig.Setup(x => x.GetSetting("Oracle:MaxDataSources", "10"))
               .Returns("10");
        _mockConfig.Setup(x => x.GetSetting("Oracle:UpdateInterval", "300"))
               .Returns("300");

        _oracleService = new OracleService(
            _mockLogger.Object,
            _mockStorageProvider.Object,
            _mockConfig.Object,
            _mockHealthCheck.Object,
            _mockTelemetry.Object,
            _mockHttpClient.Object,
            _mockBlockchainFactory.Object,
            _mockSecretsManager.Object);
    }

    [Fact]
    public async Task InitializeAsync_InitializesSuccessfully()
    {
        var result = await _oracleService.InitializeAsync();

        result.Should().BeTrue();
        _oracleService.Name.Should().Be("OracleService");
        _oracleService.ServiceType.Should().Be("OracleService");
    }

    [Fact]
    public async Task AddDataSourceAsync_WithValidSource_AddsDataSource()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var dataSource = new DataSource
        {
            Id = "btc-usd-price",
            Name = "Bitcoin USD Price",
            Url = "https://api.coinbase.com/v2/exchange-rates?currency=BTC",
            DataType = "price",
            UpdateInterval = TimeSpan.FromMinutes(5),
            IsActive = true
        };

        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _oracleService.AddDataSourceAsync(dataSource);

        // Assert
        result.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.StoreAsync(
            $"oracle/sources/{dataSource.Id}", 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetDataAsync_WithValidSourceId_ReturnsData()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var sourceId = "btc-usd-price";
        var expectedData = new Dictionary<string, object>
        {
            ["price"] = 45000.50m,
            ["timestamp"] = DateTime.UtcNow.ToString(),
            ["source"] = "coinbase"
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"oracle/sources/{sourceId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"oracle/data/{sourceId}/latest"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(expectedData));

        // Act
        var result = await _oracleService.GetDataAsync(sourceId);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("price");
        result.Should().ContainKey("timestamp");
        result["price"].Should().Be(45000.50m);
    }

    [Fact]
    public async Task GetDataAsync_WithNonExistingSource_ReturnsNull()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var sourceId = "non-existent-source";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"oracle/sources/{sourceId}"))
            .ReturnsAsync(false);

        // Act
        var result = await _oracleService.GetDataAsync(sourceId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDataSourceAsync_FetchesAndStoresData()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var sourceId = "btc-usd-price";
        var dataSource = new DataSource
        {
            Id = sourceId,
            Name = "Bitcoin USD Price",
            Url = "https://api.coinbase.com/v2/exchange-rates?currency=BTC",
            DataType = "price",
            IsActive = true
        };

        var httpResponseData = "{\"data\":{\"rates\":{\"USD\":\"45000.50\"}}}";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"oracle/sources/{sourceId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"oracle/sources/{sourceId}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(dataSource));

        _mockHttpClient.Setup(x => x.GetStringAsync(dataSource.Url))
            .ReturnsAsync(httpResponseData);

        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _oracleService.UpdateDataSourceAsync(sourceId);

        // Assert
        result.Should().BeTrue();

        _mockHttpClient.Verify(x => x.GetStringAsync(dataSource.Url), Times.Once);
        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.Is<string>(s => s.Contains($"oracle/data/{sourceId}")), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SubscribeToDataSourceAsync_WithValidRequest_CreatesSubscription()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var subscription = new OracleSubscription
        {
            Id = "sub-001",
            SourceId = "btc-usd-price",
            CallbackUrl = "https://example.com/webhook",
            MinChangeThreshold = 100m,
            IsActive = true
        };

        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _oracleService.SubscribeToDataSourceAsync(subscription);

        // Assert
        result.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.StoreAsync(
            $"oracle/subscriptions/{subscription.Id}", 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromDataSourceAsync_WithExistingSubscription_RemovesSubscription()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var subscriptionId = "sub-001";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"oracle/subscriptions/{subscriptionId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.DeleteAsync($"oracle/subscriptions/{subscriptionId}"))
            .ReturnsAsync(true);

        // Act
        var result = await _oracleService.UnsubscribeFromDataSourceAsync(subscriptionId);

        // Assert
        result.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.DeleteAsync($"oracle/subscriptions/{subscriptionId}"), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalDataAsync_WithValidParameters_ReturnsHistoricalData()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var sourceId = "btc-usd-price";
        var startTime = DateTime.UtcNow.AddDays(-7);
        var endTime = DateTime.UtcNow;

        var historicalEntries = new[]
        {
            "oracle/data/btc-usd-price/2023-01-01",
            "oracle/data/btc-usd-price/2023-01-02",
            "oracle/data/btc-usd-price/2023-01-03"
        };

        var sampleData1 = new Dictionary<string, object> { ["price"] = 44000m, ["timestamp"] = "2023-01-01T00:00:00Z" };
        var sampleData2 = new Dictionary<string, object> { ["price"] = 44500m, ["timestamp"] = "2023-01-02T00:00:00Z" };
        var sampleData3 = new Dictionary<string, object> { ["price"] = 45000m, ["timestamp"] = "2023-01-03T00:00:00Z" };

        _mockStorageProvider.Setup(x => x.ListKeysAsync($"oracle/data/{sourceId}/", It.IsAny<string>()))
            .ReturnsAsync(historicalEntries);

        _mockStorageProvider.Setup(x => x.GetAsync(historicalEntries[0]))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(sampleData1));
        _mockStorageProvider.Setup(x => x.GetAsync(historicalEntries[1]))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(sampleData2));
        _mockStorageProvider.Setup(x => x.GetAsync(historicalEntries[2]))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(sampleData3));

        // Act
        var result = await _oracleService.GetHistoricalDataAsync(sourceId, startTime, endTime);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(3);
        result.First().Should().ContainKey("price");
        result.First().Should().ContainKey("timestamp");
    }

    [Fact]
    public async Task ValidateDataSourceAsync_WithValidUrl_ReturnsTrue()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var url = "https://api.coinbase.com/v2/exchange-rates?currency=BTC";
        var validResponseData = "{\"data\":{\"rates\":{\"USD\":\"45000.50\"}}}";

        _mockHttpClient.Setup(x => x.GetStringAsync(url))
            .ReturnsAsync(validResponseData);

        // Act
        var result = await _oracleService.ValidateDataSourceAsync(url);

        // Assert
        result.Should().BeTrue();

        _mockHttpClient.Verify(x => x.GetStringAsync(url), Times.Once);
    }

    [Fact]
    public async Task ValidateDataSourceAsync_WithInvalidUrl_ReturnsFalse()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var url = "https://invalid-url.com/api";

        _mockHttpClient.Setup(x => x.GetStringAsync(url))
            .ThrowsAsync(new HttpRequestException("Not found"));

        // Act
        var result = await _oracleService.ValidateDataSourceAsync(url);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListDataSourcesAsync_ReturnsAllDataSources()
    {
        // Arrange
        await _oracleService.InitializeAsync();
        await _oracleService.StartAsync();

        var sourceIds = new[] { "btc-usd", "eth-usd", "ada-usd" };
        var sourcePaths = sourceIds.Select(id => $"oracle/sources/{id}").ToArray();

        var source1 = new DataSource { Id = "btc-usd", Name = "Bitcoin USD", IsActive = true };
        var source2 = new DataSource { Id = "eth-usd", Name = "Ethereum USD", IsActive = true };
        var source3 = new DataSource { Id = "ada-usd", Name = "Cardano USD", IsActive = false };

        _mockStorageProvider.Setup(x => x.ListKeysAsync("oracle/sources/", It.IsAny<string>()))
            .ReturnsAsync(sourcePaths);

        _mockStorageProvider.Setup(x => x.GetAsync(sourcePaths[0]))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(source1));
        _mockStorageProvider.Setup(x => x.GetAsync(sourcePaths[1]))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(source2));
        _mockStorageProvider.Setup(x => x.GetAsync(sourcePaths[2]))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(source3));

        // Act
        var result = await _oracleService.ListDataSourcesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(3);
        result.Should().Contain(s => s.Id == "btc-usd");
        result.Should().Contain(s => s.Id == "eth-usd");
        result.Should().Contain(s => s.Id == "ada-usd");
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        _oracleService.Dispose();
        _oracleService.Status.Should().Be("Disposed");
    }

    public void Dispose()
    {
        _oracleService?.Dispose();
    }
}