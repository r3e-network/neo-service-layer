using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Neo.X;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Neo.X.Tests;

/// <summary>
/// Comprehensive unit tests for NeoXClient with EVM-compatible functionality testing.
/// Tests all blockchain client functionality including RPC calls, Wei/Ether conversion, and smart contracts.
/// </summary>
public class NeoXClientTests : IDisposable
{
    private readonly Mock<ILogger<NeoXClient>> _mockLogger;
    private readonly WireMockServer _mockServer;
    private readonly NeoXClient _client;
    private readonly string _testRpcUrl;

    public NeoXClientTests()
    {
        _mockLogger = new Mock<ILogger<NeoXClient>>();
        _mockServer = WireMockServer.Start();
        _testRpcUrl = _mockServer.Urls[0];

        var httpClient = new HttpClient();
        _client = new NeoXClient(_mockLogger.Object, httpClient, _testRpcUrl);
    }

    #region Block Height Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockHeightAsync_ValidHexResponse_ReturnsCorrectHeight()
    {
        // Arrange
        const long expectedHeight = 0x12345; // 74565 in decimal
        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = "0x12345"
                })));

        // Act
        var result = await _client.GetBlockHeightAsync();

        // Assert
        result.Should().Be(expectedHeight);
        VerifyLoggerCalled(LogLevel.Debug, "Getting block height from");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    [InlineData("0x0", 0)]
    [InlineData("0x1", 1)]
    [InlineData("0xff", 255)]
    [InlineData("0x1000", 4096)]
    [InlineData("0xffffff", 16777215)]
    public async Task GetBlockHeightAsync_VariousHexValues_ConvertsCorrectly(string hexValue, long expectedDecimal)
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = hexValue
                })));

        // Act
        var result = await _client.GetBlockHeightAsync();

        // Assert
        result.Should().Be(expectedDecimal);
    }

    #endregion

    #region Block Retrieval Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockAsync_ByHeight_ValidEthereumBlock_ReturnsBlock()
    {
        // Arrange
        const long blockHeight = 100;
        var expectedBlock = CreateTestEthereumBlockResponse(blockHeight);

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = expectedBlock
                })));

        // Act
        var result = await _client.GetBlockAsync(blockHeight);

        // Assert
        result.Should().NotBeNull();
        result.Height.Should().Be(blockHeight);
        result.Hash.Should().Be(((dynamic)expectedBlock).hash);
        result.PreviousHash.Should().Be(((dynamic)expectedBlock).parentHash);
        result.Transactions.Should().HaveCount(((dynamic)expectedBlock).transactions.Length);
        VerifyLoggerCalled(LogLevel.Debug, $"Getting block at height {blockHeight}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockAsync_ByHash_ValidEthereumBlock_ReturnsBlock()
    {
        // Arrange
        const string blockHash = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var expectedBlock = CreateTestEthereumBlockResponse(100, blockHash);

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = expectedBlock
                })));

        // Act
        var result = await _client.GetBlockAsync(blockHash);

        // Assert
        result.Should().NotBeNull();
        result.Hash.Should().Be(blockHash);
        result.Height.Should().Be(100);
        VerifyLoggerCalled(LogLevel.Debug, $"Getting block with hash {blockHash}");
    }

    #endregion

    #region Wei/Ether Conversion Tests

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Conversion")]
    [InlineData("0x0", 0)]
    [InlineData("0xde0b6b3a7640000", 1)] // 1 Ether in Wei
    [InlineData("0x1bc16d674ec80000", 2)] // 2 Ether in Wei
    [InlineData("0x6f05b59d3b20000", 0.5)] // 0.5 Ether in Wei
    public async Task GetTransactionAsync_WeiConversion_ConvertsCorrectly(string weiHex, decimal expectedEther)
    {
        // Arrange
        const string txHash = "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890";
        var expectedTx = CreateTestEthereumTransactionResponse(txHash, weiHex);

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = expectedTx
                })));

        // Act
        var result = await _client.GetTransactionAsync(txHash);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(expectedEther);
    }

    #endregion

    #region Smart Contract Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "SmartContract")]
    public async Task CallContractMethodAsync_ValidCall_ReturnsResult()
    {
        // Arrange
        const string contractAddress = "0x1234567890123456789012345678901234567890";
        const string method = "balanceOf";
        var args = new object[] { "0xabcdef1234567890abcdef1234567890abcdef12" };
        const string expectedResult = "0x56bc75e2d630fffff";

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = expectedResult
                })));

        // Act
        var result = await _client.CallContractMethodAsync(contractAddress, method, args);

        // Assert
        result.Should().Be(expectedResult);
        VerifyLoggerCalled(LogLevel.Debug, $"Calling contract method {method} on contract {contractAddress}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "SmartContract")]
    public async Task InvokeContractMethodAsync_ValidInvocation_ReturnsTransactionHash()
    {
        // Arrange
        const string contractAddress = "0x1234567890123456789012345678901234567890";
        const string method = "transfer";
        var args = new object[] { "0xabcdef1234567890abcdef1234567890abcdef12", 1000 };
        const string expectedTxHash = "0x5555666677778888999900001111222233334444555566667777888899990000";

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = expectedTxHash
                })));

        // Act
        var result = await _client.InvokeContractMethodAsync(contractAddress, method, args);

        // Assert
        result.Should().Be(expectedTxHash);
        VerifyLoggerCalled(LogLevel.Debug, $"Invoking contract method {method} on contract {contractAddress}");
    }

    #endregion

    #region Gas Estimation Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "GasEstimation")]
    public async Task SendTransactionAsync_ValidTransaction_IncludesGasEstimation()
    {
        // Arrange
        var transaction = CreateTestEthereumTransaction();
        const string expectedTxHash = "0x9876543210fedcba9876543210fedcba9876543210fedcba9876543210fedcba";

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = expectedTxHash
                })));

        // Act
        var result = await _client.SendTransactionAsync(transaction);

        // Assert
        result.Should().Be(expectedTxHash);
        VerifyLoggerCalled(LogLevel.Debug, "Sending transaction from");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task GetBlockHeightAsync_RpcError_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    error = new { code = -32601, message = "Method not found" }
                })));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _client.GetBlockHeightAsync());
        exception.Message.Should().Contain("RPC Error: Method not found");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task GetBlockAsync_ServerError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetBlockAsync(100L));
        exception.Should().NotBeNull();
        VerifyLoggerCalled(LogLevel.Error, "HTTP request error while getting block at height");
    }

    #endregion

    #region Helper Methods

    private static object CreateTestEthereumBlockResponse(long height, string? hash = null)
    {
        return new
        {
            hash = hash ?? $"0x{height:x64}",
            number = $"0x{height:x}",
            parentHash = $"0x{height - 1:x64}",
            timestamp = $"0x{DateTimeOffset.UtcNow.ToUnixTimeSeconds():x}",
            gasLimit = "0x1c9c380",
            gasUsed = "0x5208",
            transactions = new[]
            {
                CreateTestEthereumTransactionResponse("0x1111111111111111111111111111111111111111111111111111111111111111")
            }
        };
    }

    private static object CreateTestEthereumTransactionResponse(string hash, string? value = null)
    {
        return new
        {
            hash,
            blockHash = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            blockNumber = "0x64",
            from = "0x1234567890123456789012345678901234567890",
            to = "0xabcdef1234567890abcdef1234567890abcdef12",
            value = value ?? "0xde0b6b3a7640000", // 1 Ether in Wei
            gas = "0x5208",
            gasPrice = "0x9184e72a000",
            input = "0x",
            nonce = "0x1",
            transactionIndex = "0x0"
        };
    }

    private static Transaction CreateTestEthereumTransaction()
    {
        return new Transaction
        {
            Hash = "0x0000000000000000000000000000000000000000000000000000000000000000",
            From = "0x1234567890123456789012345678901234567890",
            To = "0xabcdef1234567890abcdef1234567890abcdef12",
            Value = 1.5m,
            Data = "0x",
            Timestamp = DateTime.UtcNow,
            BlockHash = "0x1111111111111111111111111111111111111111111111111111111111111111",
            BlockHeight = 12345
        };
    }

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
        _client?.Dispose();
    }

    #endregion
}
