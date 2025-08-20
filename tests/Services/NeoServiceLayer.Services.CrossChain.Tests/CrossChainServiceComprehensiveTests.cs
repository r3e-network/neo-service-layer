using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Services.CrossChain;
using CrossChainModels = NeoServiceLayer.Services.CrossChain.Models;
using NeoServiceLayer.TestInfrastructure;
using System.Text.Json;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Services.CrossChain.Tests;

/// <summary>
/// Comprehensive test suite for CrossChainService covering all major functionality
/// </summary>
public class CrossChainServiceComprehensiveTests : TestBase
{
    private readonly Mock<ILogger<CrossChainService>> _loggerMock;
    private readonly Mock<IBlockchainClientFactory> _blockchainClientFactoryMock;
    private readonly Mock<NeoServiceLayer.Core.Configuration.IServiceConfiguration> _configurationMock;
    private readonly Mock<IBlockchainClient> _neoN3ClientMock;
    private readonly Mock<IBlockchainClient> _neoXClientMock;
    private readonly Mock<NeoServiceLayer.Tee.Host.Services.IEnclaveManager> _enclaveManagerMock;
    private readonly CrossChainService _service;

    public CrossChainServiceComprehensiveTests()
    {
        _loggerMock = new Mock<ILogger<CrossChainService>>();
        _blockchainClientFactoryMock = new Mock<IBlockchainClientFactory>();
        _configurationMock = new Mock<NeoServiceLayer.Core.Configuration.IServiceConfiguration>();
        _neoN3ClientMock = new Mock<IBlockchainClient>();
        _neoXClientMock = new Mock<IBlockchainClient>();
        _enclaveManagerMock = new Mock<NeoServiceLayer.Tee.Host.Services.IEnclaveManager>();

        // Setup enclave manager
        SetupEnclaveManager();

        // Setup blockchain client factory
        _blockchainClientFactoryMock.Setup(x => x.CreateClient(BlockchainType.NeoN3))
            .Returns(_neoN3ClientMock.Object);
        _blockchainClientFactoryMock.Setup(x => x.CreateClient(BlockchainType.NeoX))
            .Returns(_neoXClientMock.Object);
            
        // Setup blockchain client mock to return true for message verification
        _neoN3ClientMock.Setup(x => x.CallContractMethodAsync(
                It.IsAny<string>(), 
                "verifyMessage", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync("true");
            
        _neoXClientMock.Setup(x => x.CallContractMethodAsync(
                It.IsAny<string>(), 
                "verifyMessage", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync("true");

        _service = new CrossChainService(
            _loggerMock.Object, 
            _configurationMock.Object,
            _blockchainClientFactoryMock.Object,
            _enclaveManagerMock.Object);
        
        // Initialize the service
        _service.InitializeAsync().GetAwaiter().GetResult();
    }
    
    private void SetupEnclaveManager()
    {
        // Setup enclave initialization
        _enclaveManagerMock
            .Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        _enclaveManagerMock
            .Setup(x => x.InitializeAsync(null, default))
            .Returns(Task.CompletedTask);
        
        _enclaveManagerMock
            .Setup(x => x.InitializeEnclaveAsync())
            .ReturnsAsync(true);
            
        _enclaveManagerMock
            .Setup(x => x.IsInitialized)
            .Returns(true);
        
        // Setup enclave operations for cross-chain functionality
        _enclaveManagerMock
            .Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string script, CancellationToken ct) => 
            {
                // Return success for verification operations
                if (script.Contains("verify")) return "{\"valid\": true}";
                if (script.Contains("sign")) return "{\"signature\": \"test-signature\"}";
                if (script.Contains("hash")) return "{\"hash\": \"test-hash\"}";
                return "{}";
            });
    }

    #region Service Initialization Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act & Assert
        _service.Should().NotBeNull();
        _service.Name.Should().Be("CrossChainService");
        _service.Description.Should().Contain("Cross-chain");
        _service.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new CrossChainService(
            null!, 
            _configurationMock.Object,
            _blockchainClientFactoryMock.Object);
            
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Cross-Chain Transfer Tests

    [Fact]
    public async Task TransferTokensAsync_WithValidRequest_ShouldInitiateTransfer()
    {
        // Arrange
        var transferRequest = new CoreModels.CrossChainTransferRequest
        {
            TokenAddress = "0x1234567890abcdef",
            Amount = 100.0m,
            DestinationAddress = GenerateTestAddress(BlockchainType.NeoX),
            Sender = GenerateTestAddress(BlockchainType.NeoN3),
            Receiver = GenerateTestAddress(BlockchainType.NeoX)
        };

        // Act
        var result = await _service.TransferTokensAsync(transferRequest, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TransferTokensAsync_WithNullRequest_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.TransferTokensAsync(null!, BlockchainType.NeoN3, BlockchainType.NeoX));
    }

    [Fact]
    public async Task TransferTokensAsync_WithUnsupportedChain_ShouldThrow()
    {
        // Arrange
        var transferRequest = new CoreModels.CrossChainTransferRequest
        {
            TokenAddress = "0x1234567890abcdef",
            Amount = 100.0m,
            DestinationAddress = GenerateTestAddress(BlockchainType.NeoX),
            Sender = GenerateTestAddress(BlockchainType.NeoN3),
            Receiver = GenerateTestAddress(BlockchainType.NeoX)
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.TransferTokensAsync(transferRequest, (BlockchainType)999, BlockchainType.NeoX));
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithValidAddress_ShouldReturnHistory()
    {
        // Arrange
        var address = GenerateTestAddress(BlockchainType.NeoN3);
        
        // Act
        var history = await _service.GetTransactionHistoryAsync(address, BlockchainType.NeoN3);

        // Assert
        history.Should().NotBeNull();
    }

    #endregion

    #region Cross-Chain Message Passing Tests

    [Fact]
    public async Task SendMessageAsync_WithValidMessage_ShouldSendMessage()
    {
        // Arrange
        var messageRequest = new CrossChainModels.CrossChainMessageRequest
        {
            Id = Guid.NewGuid().ToString(),
            Content = "test message",
            Sender = GenerateTestAddress(BlockchainType.NeoN3),
            Receiver = GenerateTestAddress(BlockchainType.NeoX)
        };

        // Act
        var result = await _service.SendMessageAsync(messageRequest, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendMessageAsync_WithNullRequest_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SendMessageAsync(null!, BlockchainType.NeoN3, BlockchainType.NeoX));
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithValidMessageId_ShouldReturnStatus()
    {
        // Arrange
        var messageRequest = new CrossChainModels.CrossChainMessageRequest
        {
            Id = Guid.NewGuid().ToString(),
            Content = "test message"
        };
        
        var messageId = await _service.SendMessageAsync(messageRequest, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Act
        var status = await _service.GetMessageStatusAsync(messageId, BlockchainType.NeoN3);

        // Assert
        status.Should().NotBeNull();
        status.MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldReturnPendingMessages()
    {
        // Act
        var pendingMessages = await _service.GetPendingMessagesAsync(BlockchainType.NeoX);

        // Assert
        pendingMessages.Should().NotBeNull();
    }

    #endregion

    #region Contract Call Tests

    [Fact]
    public async Task ExecuteContractCallAsync_WithValidRequest_ShouldExecuteCall()
    {
        // Arrange
        var request = new CrossChainModels.CrossChainContractCallRequest
        {
            TargetContract = "0xcontract",
            Method = "transfer",
            Parameters = new[] { "0xrecipient", "100" },
            GasLimit = 100000
        };

        // Act
        var result = await _service.ExecuteContractCallAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNull();
        result.ExecutionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteRemoteCallAsync_WithValidRequest_ShouldExecuteCall()
    {
        // Arrange
        var request = new CoreModels.RemoteCallRequest
        {
            ContractAddress = "0xcontract",
            MethodName = "getValue",
            Parameters = new object[] { "key" }
        };

        // Act
        var result = await _service.ExecuteRemoteCallAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Route and Fee Tests

    [Fact]
    public async Task GetOptimalRouteAsync_WithSupportedChains_ShouldReturnRoute()
    {
        // Act
        var route = await _service.GetOptimalRouteAsync(BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        route.Should().NotBeNull();
        route.Source.Should().Be(BlockchainType.NeoN3);
        route.Destination.Should().Be(BlockchainType.NeoX);
        route.EstimatedFee.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EstimateFeesAsync_WithValidOperation_ShouldReturnFee()
    {
        // Arrange
        var operation = new CrossChainModels.CrossChainOperation
        {
            OperationType = "TokenTransfer",
            SourceChain = BlockchainType.NeoN3,
            TargetChain = BlockchainType.NeoX,
            Priority = "Normal"
        };

        // Act
        var fee = await _service.EstimateFeesAsync(operation, BlockchainType.NeoN3);

        // Assert
        fee.Should().BeGreaterThan(0);
    }

    #endregion

    #region Verification Tests

    [Fact]
    public async Task VerifyMessageAsync_WithValidProof_ShouldReturnTrue()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var proof = $@"{{""MessageId"":""{messageId}"",""MessageHash"":""hash123"",""Signature"":""sig123""}}";

        // Act
        var result = await _service.VerifyMessageAsync(messageId, proof, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMessageProofAsync_WithValidProof_ShouldReturnTrue()
    {
        // Arrange
        var proof = new CrossChainModels.CrossChainMessageProof
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageHash = "hash123",
            Signature = "sig123"
        };

        // Act
        var result = await _service.VerifyMessageProofAsync(proof, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Token Mapping Tests

    [Fact]
    public async Task RegisterTokenMappingAsync_WithValidMapping_ShouldRegister()
    {
        // Arrange
        var mapping = new CoreModels.TokenMapping
        {
            SourceTokenAddress = "0xsource",
            DestinationTokenAddress = "0xtarget",
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX
        };

        // Act
        var result = await _service.RegisterTokenMappingAsync(mapping, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Chain Support Tests

    [Fact]
    public async Task GetSupportedChainsAsync_ShouldReturnChains()
    {
        // Act
        var chains = await _service.GetSupportedChainsAsync();

        // Assert
        chains.Should().NotBeEmpty();
        chains.Should().HaveCountGreaterOrEqualTo(2); // At least NeoN3 and NeoX
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SendMessageAsync_WithUnsupportedChain_ShouldThrow()
    {
        // Arrange
        var request = new CrossChainModels.CrossChainMessageRequest
        {
            Id = Guid.NewGuid().ToString(),
            Content = "test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.SendMessageAsync(request, (BlockchainType)999, BlockchainType.NeoX));
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithEmptyMessageId_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMessageStatusAsync("", BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithEmptyAddress_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetTransactionHistoryAsync("", BlockchainType.NeoN3));
    }

    #endregion

    #region Helper Methods

    // Use the GenerateTestAddress method from TestBase instead of defining our own

    #endregion
}