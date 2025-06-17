using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Tee.Host.Services;
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;

namespace NeoServiceLayer.Services.Oracle.Tests;

/// <summary>
/// Unit tests for the OracleService class.
/// </summary>
public class OracleServiceTests
{
    private readonly Mock<ILogger<OracleService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainClientFactory;
    private readonly Mock<IBlockchainClient> _mockBlockchainClient;
    private readonly OracleService _oracleService;

    public OracleServiceTests()
    {
        _mockLogger = new Mock<ILogger<OracleService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockBlockchainClientFactory = new Mock<IBlockchainClientFactory>();
        _mockBlockchainClient = new Mock<IBlockchainClient>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x.GetValue("Oracle:MaxConcurrentRequests", "10")).Returns("10");
        _mockConfiguration.Setup(x => x.GetValue("Oracle:DefaultTimeout", "30000")).Returns("30000");
        _mockConfiguration.Setup(x => x.GetValue("Oracle:MaxRequestsPerBatch", "10")).Returns("10");

        // Setup blockchain client factory
        _mockBlockchainClientFactory.Setup(x => x.CreateClient(It.IsAny<BlockchainType>()))
            .Returns(_mockBlockchainClient.Object);

        // Setup blockchain client
        _mockBlockchainClient.Setup(x => x.GetBlockHeightAsync()).ReturnsAsync(1000000L);
        _mockBlockchainClient.Setup(x => x.GetBlockHashAsync(It.IsAny<long>()))
            .ReturnsAsync("0x1234567890abcdef");

        // Setup enclave manager
        _mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);
        _mockEnclaveManager.Setup(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"value\": 42, \"timestamp\": \"2024-01-01T00:00:00Z\"}");
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("deadbeef");
        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _oracleService = new OracleService(
            _mockConfiguration.Object,
            _mockEnclaveManager.Object,
            _mockBlockchainClientFactory.Object,
            _mockLogger.Object);

        // Initialize the enclave and start the service for testing
        _oracleService.InitializeEnclaveAsync().Wait();
        _oracleService.StartAsync().Wait();
    }

    [Fact]
    public async Task FetchDataAsync_ValidRequest_ReturnsSuccessfulResponse()
    {
        // Arrange
        var request = new OracleRequest
        {
            RequestId = "test-request-1",
            Url = "https://api.example.com",
            Path = "data.price",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act
        var result = await _oracleService.FetchDataAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().Be("test-request-1");
        result.StatusCode.Should().Be(200);
        result.Data.Should().NotBeEmpty();
        result.BlockchainType.Should().Be(BlockchainType.NeoN3);
        result.SourceUrl.Should().Be("https://api.example.com");
        result.SourcePath.Should().Be("data.price");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task FetchDataAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _oracleService.FetchDataAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task FetchDataAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var request = new OracleRequest
        {
            RequestId = "test-request-1",
            Url = "https://api.example.com",
            Path = "data.price"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.FetchDataAsync(request, (BlockchainType)999));
    }

    [Fact]
    public async Task FetchDataBatchAsync_ValidRequests_ReturnsAllResponses()
    {
        // Arrange
        var requests = new List<OracleRequest>
        {
            new OracleRequest
            {
                RequestId = "test-request-1",
                Url = "https://api.example.com",
                Path = "data.price1"
            },
            new OracleRequest
            {
                RequestId = "test-request-2",
                Url = "https://api.example.com",
                Path = "data.price2"
            }
        };

        // Act
        var result = await _oracleService.FetchDataBatchAsync(requests, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().RequestId.Should().Be("test-request-1");
        result.Last().RequestId.Should().Be("test-request-2");
    }

    [Fact]
    public async Task FetchDataBatchAsync_NullRequests_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _oracleService.FetchDataBatchAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task VerifyDataAsync_ValidResponse_ReturnsTrue()
    {
        // Arrange
        var response = new OracleResponse
        {
            RequestId = "test-request-1",
            Data = "{\"value\": 42}",
            Signature = "valid-signature",
            BlockHash = "0x1234567890abcdef",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act
        var result = await _oracleService.VerifyDataAsync(response, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyDataAsync_NullResponse_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _oracleService.VerifyDataAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetSupportedDataSourcesAsync_ValidBlockchain_ReturnsEmptyCollection()
    {
        // Act
        var result = await _oracleService.GetSupportedDataSourcesAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No data sources registered yet
    }

    [Fact]
    public async Task GetDataAsync_ValidParameters_ReturnsData()
    {
        // Act
        var result = await _oracleService.GetDataAsync("https://api.example.com", "data.price", BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDataAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.GetDataAsync("https://api.example.com", "data.price", (BlockchainType)999));
    }

    [Fact]
    public void ServiceInfo_HasCorrectProperties()
    {
        // Assert
        _oracleService.Name.Should().Be("Oracle");
        _oracleService.Description.Should().Be("Confidential Oracle Service");
        _oracleService.Version.Should().Be("1.0.0");
        _oracleService.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        _oracleService.SupportedBlockchains.Should().Contain(BlockchainType.NeoX);
    }

    [Fact]
    public void SupportsBlockchain_NeoN3_ReturnsTrue()
    {
        // Act & Assert
        _oracleService.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_NeoX_ReturnsTrue()
    {
        // Act & Assert
        _oracleService.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_UnsupportedBlockchain_ReturnsFalse()
    {
        // Act & Assert
        _oracleService.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
    }

    [Fact]
    public async Task BatchRequestAsync_ValidRequests_ReturnsSuccessfulBatchResponse()
    {
        // Arrange
        var requests = new List<OracleRequest>
        {
            new OracleRequest
            {
                RequestId = "batch-request-1",
                Url = "https://api.example.com",
                Path = "data.price1"
            },
            new OracleRequest
            {
                RequestId = "batch-request-2",
                Url = "https://api.example.com",
                Path = "data.price2"
            }
        };

        // Act
        var result = await _oracleService.BatchRequestAsync(requests, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BatchId.Should().NotBeEmpty();
        result.Responses.Should().HaveCount(2);
        result.BlockchainType.Should().Be(BlockchainType.NeoN3);
        result.Proof.Should().NotBeEmpty();
        result.Signature.Should().NotBeEmpty();
        result.BlockHeight.Should().Be(1000000L);
        result.BlockHash.Should().Be("0x1234567890abcdef");
    }

    [Fact]
    public async Task BatchRequestAsync_EmptyRequests_ThrowsArgumentException()
    {
        // Arrange
        var emptyRequests = new List<OracleRequest>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _oracleService.BatchRequestAsync(emptyRequests, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task BatchRequestAsync_TooManyRequests_ThrowsArgumentException()
    {
        // Arrange
        var tooManyRequests = new List<OracleRequest>();
        for (int i = 0; i < 15; i++) // Exceeds the default limit of 10
        {
            tooManyRequests.Add(new OracleRequest
            {
                RequestId = $"request-{i}",
                Url = "https://api.example.com",
                Path = $"data.price{i}"
            });
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _oracleService.BatchRequestAsync(tooManyRequests, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task BatchRequestAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var requests = new List<OracleRequest>
        {
            new OracleRequest
            {
                RequestId = "test-request-1",
                Url = "https://api.example.com",
                Path = "data.price"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.BatchRequestAsync(requests, (BlockchainType)999));
    }

    [Fact]
    public async Task VerifyResponseAsync_ValidResponse_ReturnsTrue()
    {
        // Arrange
        var response = new OracleResponse
        {
            RequestId = "test-request-1",
            Data = "{\"value\": 42}",
            Signature = "valid-signature",
            BlockHash = "0x1234567890abcdef",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act
        var result = await _oracleService.VerifyResponseAsync(response);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyResponseAsync_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var response = new OracleResponse
        {
            RequestId = "test-request-1",
            Data = "{\"value\": 42}",
            Signature = "invalid-signature",
            BlockHash = "0x1234567890abcdef",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act
        var result = await _oracleService.VerifyResponseAsync(response);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetDataAsync_WithFeedIdAndParameters_ReturnsData()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://api.example.com",
            ["dataPath"] = "data.price",
            ["blockchain"] = "NeoN3"
        };

        // Act
        var result = await _oracleService.GetDataAsync("test-feed", parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDataAsync_WithInvalidBlockchainParameter_UsesDefaultBlockchain()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://api.example.com",
            ["dataPath"] = "data.price",
            ["blockchain"] = "InvalidBlockchain"
        };

        // Act
        var result = await _oracleService.GetDataAsync("test-feed", parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}
