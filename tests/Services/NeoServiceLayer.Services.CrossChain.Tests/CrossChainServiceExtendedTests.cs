using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.Services.CrossChain.Models;
using NeoServiceLayer.TestInfrastructure;
using Xunit;
using CoreModels = NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.CrossChain.Tests;

/// <summary>
/// Comprehensive tests for CrossChainService to improve code coverage.
/// </summary>
public class CrossChainServiceExtendedTests : TestBase, IDisposable
{
    private readonly Mock<ILogger<CrossChainService>> _loggerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly CrossChainService _service;

    public CrossChainServiceExtendedTests()
    {
        _loggerMock = new Mock<ILogger<CrossChainService>>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _service = new CrossChainService(_loggerMock.Object, _configurationMock.Object);
    }

    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Name.Should().Be("CrossChainService");
        _service.Description.Should().Be("Cross-chain interoperability and messaging service");
        _service.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CrossChainService(null!, _configurationMock.Object);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task OnInitializeAsync_ShouldInitializeSuccessfully()
    {
        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task OnStartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.StartAsync();

        // Assert
        result.Should().BeTrue();
        _service.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task OnStopAsync_ShouldStopSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.StopAsync();

        // Assert
        result.Should().BeTrue();
        _service.IsRunning.Should().BeFalse();
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task GetHealthAsync_WhenNotRunning_ShouldReturnNotRunning()
    {
        // Act
        var health = await _service.GetHealthAsync();

        // Assert
        health.Should().Be(ServiceHealth.NotRunning);
    }

    [Fact]
    public async Task GetHealthAsync_WhenRunning_ShouldReturnHealthy()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var health = await _service.GetHealthAsync();

        // Assert
        health.Should().Be(ServiceHealth.Healthy);
    }

    #endregion

    #region SendMessage Tests

    [Fact]
    public async Task SendMessageAsync_WithValidRequest_ShouldReturnMessageId()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainMessageRequest
        {
            Sender = "TestSender",
            Receiver = "TestReceiver",
            Data = System.Text.Encoding.UTF8.GetBytes("Test data")
        };

        // Act
        var messageId = await _service.SendMessageAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        messageId.Should().NotBeNullOrEmpty();
        Guid.TryParse(messageId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.SendMessageAsync(null!, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendMessageAsync_WithUnsupportedSourceChain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainMessageRequest
        {
            Sender = "TestSender",
            Receiver = "TestReceiver",
            Data = System.Text.Encoding.UTF8.GetBytes("Test data")
        };

        // Act & Assert
        var action = async () => await _service.SendMessageAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Ethereum*not supported");
    }

    [Fact]
    public async Task SendMessageAsync_WithUnsupportedTargetChain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainMessageRequest
        {
            Sender = "TestSender",
            Receiver = "TestReceiver",
            Data = System.Text.Encoding.UTF8.GetBytes("Test data")
        };

        // Act & Assert
        var action = async () => await _service.SendMessageAsync(request, BlockchainType.NeoN3, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Ethereum*not supported");
    }

    #endregion

    #region GetMessageStatus Tests

    [Fact]
    public async Task GetMessageStatusAsync_WithValidMessageId_ShouldReturnStatus()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainMessageRequest
        {
            Sender = "TestSender",
            Receiver = "TestReceiver",
            Data = System.Text.Encoding.UTF8.GetBytes("Test data")
        };

        var messageId = await _service.SendMessageAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Act
        var status = await _service.GetMessageStatusAsync(messageId, BlockchainType.NeoN3);

        // Assert
        status.Should().NotBeNull();
        status.MessageId.Should().Be(messageId);
        status.Status.Should().Be(CoreModels.MessageStatus.Pending);
        status.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithInvalidMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.GetMessageStatusAsync("invalid-id", BlockchainType.NeoN3);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Message invalid-id not found*");
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithNullOrEmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var nullAction = async () => await _service.GetMessageStatusAsync(null!, BlockchainType.NeoN3);
        await nullAction.Should().ThrowAsync<ArgumentException>();

        var emptyAction = async () => await _service.GetMessageStatusAsync("", BlockchainType.NeoN3);
        await emptyAction.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetMessageStatusAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.GetMessageStatusAsync("test-id", BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Ethereum*not supported");
    }

    #endregion

    #region TransferTokens Tests

    [Fact]
    public async Task TransferTokensAsync_WithValidRequest_ShouldReturnTransferId()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainTransferRequest
        {
            Sender = "NTestAddress1",
            Receiver = "XTestAddress1",
            Amount = 10.5m,
            TokenAddress = "GAS"
        };

        // Act
        var transferId = await _service.TransferTokensAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        transferId.Should().NotBeNullOrEmpty();
        Guid.TryParse(transferId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task TransferTokensAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.TransferTokensAsync(null!, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TransferTokensAsync_WithAmountTooSmall_ShouldThrowArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainTransferRequest
        {
            Sender = "NTestAddress1",
            Receiver = "XTestAddress1",
            Amount = 0.0001m, // Below minimum
            TokenAddress = "GAS"
        };

        // Act & Assert
        var action = async () => await _service.TransferTokensAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*outside allowed range*");
    }

    [Fact]
    public async Task TransferTokensAsync_WithAmountTooLarge_ShouldThrowArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainTransferRequest
        {
            Sender = "NTestAddress1",
            Receiver = "XTestAddress1",
            Amount = 2000000m, // Above maximum
            TokenAddress = "GAS"
        };

        // Act & Assert
        var action = async () => await _service.TransferTokensAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*outside allowed range*");
    }

    [Fact]
    public async Task TransferTokensAsync_WithUnsupportedChains_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainTransferRequest
        {
            Sender = "TestAddress1",
            Receiver = "TestAddress2",
            Amount = 10m,
            TokenAddress = "GAS"
        };

        // Act & Assert
        var action = async () => await _service.TransferTokensAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region ExecuteContractCall Tests

    [Fact]
    public async Task ExecuteContractCallAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CrossChainContractCallRequest
        {
            CallerAddress = "TestCaller",
            TargetContract = "TestContract",
            Method = "TestMethod",
            Parameters = new[] { "param1", "param2" }
        };

        // Act
        var result = await _service.ExecuteContractCallAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExecutionId.Should().NotBeNullOrEmpty();
        result.Result.Should().Contain("executed successfully");
        result.TransactionHash.Should().NotBeNullOrEmpty();
        result.GasUsed.Should().BeGreaterThan(0);
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ExecuteContractCallAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.ExecuteContractCallAsync(null!, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteContractCallAsync_WithUnsupportedChains_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CrossChainContractCallRequest
        {
            CallerAddress = "TestCaller",
            TargetContract = "TestContract",
            Method = "TestMethod",
            Parameters = new[] { "param1" }
        };

        // Act & Assert
        var action = async () => await _service.ExecuteContractCallAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region VerifyMessageProof Tests

    [Fact]
    public async Task VerifyMessageProofAsync_WithValidProof_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var proof = new CoreModels.CrossChainMessageProof
        {
            MessageId = Guid.NewGuid().ToString(),
            ProofData = System.Text.Encoding.UTF8.GetBytes("valid_proof_data"),
            MessageHash = "message_hash",
            Signature = "signature_data"
        };

        // Act
        var isValid = await _service.VerifyMessageProofAsync(proof, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMessageProofAsync_WithNullProof_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.VerifyMessageProofAsync(null!, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task VerifyMessageProofAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var proof = new CoreModels.CrossChainMessageProof
        {
            MessageId = Guid.NewGuid().ToString(),
            ProofData = System.Text.Encoding.UTF8.GetBytes("valid_proof_data")
        };

        // Act & Assert
        var action = async () => await _service.VerifyMessageProofAsync(proof, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region GetSupportedChains Tests

    [Fact]
    public async Task GetSupportedChainsAsync_ShouldReturnSupportedChains()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var chains = await _service.GetSupportedChainsAsync();

        // Assert
        chains.Should().NotBeNull();
        chains.Should().HaveCount(2); // NeoN3 and NeoX
        chains.Should().Contain(c => c.ChainType == BlockchainType.NeoN3);
        chains.Should().Contain(c => c.ChainType == BlockchainType.NeoX);
        chains.All(c => c.SupportedTokens.Contains("GAS")).Should().BeTrue();
        chains.All(c => c.SupportedTokens.Contains("NEO")).Should().BeTrue();
        chains.All(c => c.SupportedTokens.Contains("USDT")).Should().BeTrue();
    }

    #endregion

    #region GetTransactionHistory Tests

    [Fact]
    public async Task GetTransactionHistoryAsync_WithExistingTransactions_ShouldReturnHistory()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        const string testAddress = "NTestAddress123";
        var request = new CoreModels.CrossChainTransferRequest
        {
            Sender = testAddress,
            Receiver = "XTestAddress456",
            Amount = 5.5m,
            TokenAddress = "GAS"
        };

        // Create a transaction first
        await _service.TransferTokensAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Act
        var history = await _service.GetTransactionHistoryAsync(testAddress, BlockchainType.NeoN3);

        // Assert
        history.Should().NotBeNull();
        history.Should().HaveCount(1);
        var transaction = history.First();
        transaction.FromAddress.Should().Be(testAddress);
        transaction.ToAddress.Should().Be("XTestAddress456");
        transaction.Amount.Should().Be(5.5m);
        transaction.TokenContract.Should().Be("GAS");
        transaction.SourceChain.Should().Be(BlockchainType.NeoN3);
        transaction.TargetChain.Should().Be(BlockchainType.NeoX);
        transaction.Type.Should().Be(CrossChainTransactionType.TokenTransfer);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithNonExistentAddress_ShouldReturnEmpty()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var history = await _service.GetTransactionHistoryAsync("NonExistentAddress", BlockchainType.NeoN3);

        // Assert
        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithNullOrEmptyAddress_ShouldThrowArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var nullAction = async () => await _service.GetTransactionHistoryAsync(null!, BlockchainType.NeoN3);
        await nullAction.Should().ThrowAsync<ArgumentException>();

        var emptyAction = async () => await _service.GetTransactionHistoryAsync("", BlockchainType.NeoN3);
        await emptyAction.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.GetTransactionHistoryAsync("TestAddress", BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region ExecuteRemoteCall Tests

    [Fact]
    public async Task ExecuteRemoteCallAsync_WithValidRequest_ShouldReturnCallId()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.RemoteCallRequest
        {
            Caller = "TestCaller",
            ContractAddress = "TestContract",
            FunctionName = "TestMethod"
        };

        // Act
        var callId = await _service.ExecuteRemoteCallAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        callId.Should().NotBeNullOrEmpty();
        Guid.TryParse(callId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteRemoteCallAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.ExecuteRemoteCallAsync(null!, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteRemoteCallAsync_WithUnsupportedChains_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.RemoteCallRequest
        {
            Caller = "TestCaller",
            ContractAddress = "TestContract",
            FunctionName = "TestMethod"
        };

        // Act & Assert
        var action = async () => await _service.ExecuteRemoteCallAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region GetPendingMessages Tests

    [Fact]
    public async Task GetPendingMessagesAsync_WithPendingMessages_ShouldReturnMessages()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var request = new CoreModels.CrossChainMessageRequest
        {
            Sender = "TestSender",
            Receiver = "TestReceiver",
            Data = System.Text.Encoding.UTF8.GetBytes("Test data")
        };

        // Create a pending message
        await _service.SendMessageAsync(request, BlockchainType.NeoN3, BlockchainType.NeoX);

        // Act
        var pendingMessages = await _service.GetPendingMessagesAsync(BlockchainType.NeoX);

        // Assert
        pendingMessages.Should().NotBeNull();
        pendingMessages.Should().HaveCountGreaterThan(0);
        pendingMessages.All(m => m.Status == CoreModels.MessageStatus.Pending).Should().BeTrue();
        pendingMessages.All(m => m.DestinationChain == BlockchainType.NeoX).Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.GetPendingMessagesAsync(BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region VerifyMessage Tests

    [Fact]
    public async Task VerifyMessageAsync_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var isValid = await _service.VerifyMessageAsync("test-message-id", "valid-proof", BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMessageAsync_WithNullOrEmptyParameters_ShouldThrowArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var nullMessageAction = async () => await _service.VerifyMessageAsync(null!, "proof", BlockchainType.NeoN3);
        await nullMessageAction.Should().ThrowAsync<ArgumentException>();

        var emptyMessageAction = async () => await _service.VerifyMessageAsync("", "proof", BlockchainType.NeoN3);
        await emptyMessageAction.Should().ThrowAsync<ArgumentException>();

        var nullProofAction = async () => await _service.VerifyMessageAsync("message-id", null!, BlockchainType.NeoN3);
        await nullProofAction.Should().ThrowAsync<ArgumentException>();

        var emptyProofAction = async () => await _service.VerifyMessageAsync("message-id", "", BlockchainType.NeoN3);
        await emptyProofAction.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task VerifyMessageAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.VerifyMessageAsync("message-id", "proof", BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region GetOptimalRoute Tests

    [Fact]
    public async Task GetOptimalRouteAsync_WithSupportedChains_ShouldReturnRoute()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var route = await _service.GetOptimalRouteAsync(BlockchainType.NeoN3, BlockchainType.NeoX);

        // Assert
        route.Should().NotBeNull();
        route.Source.Should().Be(BlockchainType.NeoN3);
        route.Destination.Should().Be(BlockchainType.NeoX);
        route.IntermediateChains.Should().BeEmpty();
        route.EstimatedFee.Should().BeGreaterThan(0);
        route.EstimatedTime.Should().BeGreaterThan(TimeSpan.Zero);
        route.ReliabilityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOptimalRouteAsync_WithUnsupportedSourceChain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.GetOptimalRouteAsync(BlockchainType.NeoN3, BlockchainType.NeoX);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task GetOptimalRouteAsync_WithUnsupportedDestinationChain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.GetOptimalRouteAsync(BlockchainType.NeoN3, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region EstimateFees Tests

    [Fact]
    public async Task EstimateFeesAsync_WithValidOperation_ShouldReturnFee()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var operation = new CoreModels.CrossChainOperation
        {
            Type = CoreModels.OperationType.Transfer,
            Amount = 10m,
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX
        };

        // Act
        var fee = await _service.EstimateFeesAsync(operation, BlockchainType.NeoN3);

        // Assert
        fee.Should().BeGreaterThan(0);
        fee.Should().Be(0.001m);
    }

    [Fact]
    public async Task EstimateFeesAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.EstimateFeesAsync(null!, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EstimateFeesAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var operation = new CoreModels.CrossChainOperation
        {
            Type = CoreModels.OperationType.Transfer,
            Amount = 10m
        };

        // Act & Assert
        var action = async () => await _service.EstimateFeesAsync(operation, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region RegisterTokenMapping Tests

    [Fact]
    public async Task RegisterTokenMappingAsync_WithValidMapping_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var mapping = new CoreModels.TokenMapping
        {
            SourceToken = "0x123456",
            DestinationToken = "0x789abc",
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX
        };

        // Act
        var result = await _service.RegisterTokenMappingAsync(mapping, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterTokenMappingAsync_WithNullMapping_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        var action = async () => await _service.RegisterTokenMappingAsync(null!, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterTokenMappingAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        var mapping = new CoreModels.TokenMapping
        {
            SourceToken = "0x123456",
            DestinationToken = "0x789abc"
        };

        // Act & Assert
        var action = async () => await _service.RegisterTokenMappingAsync(mapping, BlockchainType.NeoN3);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}
