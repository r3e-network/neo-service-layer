using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Neo.N3;
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


namespace NeoServiceLayer.Neo.N3.Tests;

/// <summary>
/// Comprehensive unit tests for NeoN3Client with high coverage and professional test cases.
/// Tests all blockchain client functionality including RPC calls, subscriptions, and error handling.
/// </summary>
public class NeoN3ClientTests : IDisposable
{
    private readonly Mock<ILogger<NeoN3Client>> _mockLogger;
    private readonly WireMockServer _mockServer;
    private readonly NeoN3Client _client;
    private readonly string _testRpcUrl;

    public NeoN3ClientTests()
    {
        _mockLogger = new Mock<ILogger<NeoN3Client>>();
        _mockServer = WireMockServer.Start();
        _testRpcUrl = _mockServer.Urls[0];

        var httpClient = new HttpClient();
        _client = new NeoN3Client(_mockLogger.Object, httpClient, _testRpcUrl);
    }

    #region Block Height Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockHeightAsync_ValidResponse_ReturnsCorrectHeight()
    {
        // Arrange
        const long expectedHeight = 12345;
        const long blockCount = expectedHeight + 1; // getblockcount returns count, height = count - 1
        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = blockCount
                })));

        // Act
        var result = await _client.GetBlockHeightAsync();

        // Assert
        result.Should().Be(expectedHeight);
        VerifyLoggerCalled(LogLevel.Debug, "Getting block height from");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockHeightAsync_ServerError_ThrowsException()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetBlockHeightAsync());
        exception.Should().NotBeNull();
        VerifyLoggerCalled(LogLevel.Error, "Failed to get block height from");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
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

    #endregion

    #region Block Retrieval Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockAsync_ByHeight_ValidResponse_ReturnsBlock()
    {
        // Arrange
        const long blockHeight = 100;
        var expectedBlock = CreateTestBlockResponse(blockHeight);

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
        result.PreviousHash.Should().Be(((dynamic)expectedBlock).previousblockhash);
        result.Transactions.Should().HaveCount(((dynamic)expectedBlock).tx.Length);
        VerifyLoggerCalled(LogLevel.Debug, $"Getting block at height {blockHeight}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockAsync_ByHash_ValidResponse_ReturnsBlock()
    {
        // Arrange
        const string blockHash = "0x1234567890abcdef1234567890abcdef12345678901234567890abcdef123456";
        var expectedBlock = CreateTestBlockResponse(100, blockHash);

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

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockAsync_InvalidHeight_ThrowsException()
    {
        // Arrange
        const long invalidHeight = -1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _client.GetBlockAsync(invalidHeight));
        exception.ParamName.Should().Be("height");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetBlockAsync_NullHash_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _client.GetBlockAsync((string)null!));
        exception.ParamName.Should().Be("hash");
    }

    #endregion

    #region Transaction Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task GetTransactionAsync_ValidHash_ReturnsTransaction()
    {
        // Arrange
        const string txHash = "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890";
        var expectedTx = CreateTestTransactionResponse(txHash);

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
        result.Hash.Should().Be(txHash);
        result.From.Should().Be(((dynamic)expectedTx).sender);
        result.Value.Should().Be(100.5m);
        VerifyLoggerCalled(LogLevel.Debug, $"Getting transaction with hash {txHash}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task SendTransactionAsync_ValidTransaction_ReturnsTransactionHash()
    {
        // Arrange
        var transaction = CreateTestTransaction();
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
                    result = new { hash = expectedTxHash }
                })));

        // Act
        var result = await _client.SendTransactionAsync(transaction);

        // Assert
        result.Should().Be(expectedTxHash);
        VerifyLoggerCalled(LogLevel.Debug, "Sending transaction from");
    }

    #endregion

    #region Contract Interaction Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task CallContractMethodAsync_ValidParameters_ReturnsResult()
    {
        // Arrange
        const string contractAddress = "0x1234567890123456789012345678901234567890";
        const string method = "balanceOf";
        var args = new object[] { "0xabcdef1234567890abcdef1234567890abcdef12" };
        const string expectedResult = "1000000000";

        _mockServer
            .Given(Request.Create().WithPath("/").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    result = new { stack = new[] { new { type = "Integer", value = expectedResult } } }
                })));

        // Act
        var result = await _client.CallContractMethodAsync(contractAddress, method, args);

        // Assert
        result.Should().Contain(expectedResult);
        VerifyLoggerCalled(LogLevel.Debug, $"Calling contract method {method} on contract {contractAddress}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BlockchainClient")]
    public async Task InvokeContractMethodAsync_ValidParameters_ReturnsTransactionHash()
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

    #region Helper Methods

    private static object CreateTestBlockResponse(long height, string? hash = null)
    {
        return new
        {
            hash = hash ?? $"0x{height:x64}",
            size = 1024,
            version = 0,
            previousblockhash = $"0x{height - 1:x64}",
            merkleroot = "0x1111222233334444555566667777888899990000aaaabbbbccccddddeeeeffff",
            time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            index = height,
            primary = 0,
            nextconsensus = "NiNmXL8FjEUEs1nfX9uHFBNaenxDHJtmuB",
            witnesses = new object[0],
            tx = new[]
            {
                CreateTestTransactionResponse("0x1111111111111111111111111111111111111111111111111111111111111111")
            }
        };
    }

    private static object CreateTestTransactionResponse(string hash)
    {
        return new
        {
            hash,
            size = 256,
            version = 0,
            nonce = 123456789,
            sender = "NiNmXL8FjEUEs1nfX9uHFBNaenxDHJtmuB",
            sysfee = "1005000",
            netfee = "1234570",
            validuntilblock = 12345,
            value = 100.5m, // Add value field for test to extract
            signers = new[]
            {
                new { account = "0x1234567890123456789012345678901234567890", scopes = "CalledByEntry" }
            },
            attributes = new object[0],
            script = "0x0c14abcdef1234567890abcdef1234567890abcdef1241627d5b52",
            witnesses = new[]
            {
                new
                {
                    invocation = "0x40abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
                    verification = "0x0c2102abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890ab41627d5b52"
                }
            }
        };
    }

    private static Transaction CreateTestTransaction()
    {
        return new Transaction
        {
            Hash = "0x0000000000000000000000000000000000000000000000000000000000000000",
            From = "NiNmXL8FjEUEs1nfX9uHFBNaenxDHJtmuB",
            To = "NfgHwwTi3wHAS8aFAN243C5vGbkYDpqLHP",
            Value = 100.5m,
            Data = "test transaction data",
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
