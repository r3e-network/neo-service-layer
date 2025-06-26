using FluentAssertions;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.Core.Tests;

/// <summary>
/// Tests for Cross-Chain model classes to verify property behavior and default values.
/// </summary>
public class CrossChainModelsTests
{
    #region CrossChainMessageRequest Tests

    [Fact]
    public void CrossChainMessageRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new CrossChainMessageRequest();

        // Assert
        request.MessageId.Should().NotBeEmpty();
        Guid.TryParse(request.MessageId, out _).Should().BeTrue();
        request.Sender.Should().BeEmpty();
        request.Receiver.Should().BeEmpty();
        request.Recipient.Should().BeEmpty();
        request.Data.Should().BeEmpty();
        request.Nonce.Should().Be(0);
        request.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CrossChainMessageRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new CrossChainMessageRequest();
        var data = new byte[] { 1, 2, 3, 4 };
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        request.MessageId = "msg-123";
        request.Sender = "sender-address";
        request.Receiver = "receiver-address";
        request.Recipient = "recipient-address";
        request.Data = data;
        request.Nonce = 42;
        request.Metadata = metadata;

        // Assert
        request.MessageId.Should().Be("msg-123");
        request.Sender.Should().Be("sender-address");
        request.Receiver.Should().Be("receiver-address");
        request.Recipient.Should().Be("recipient-address");
        request.Data.Should().BeEquivalentTo(data);
        request.Nonce.Should().Be(42);
        request.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region CrossChainTransferRequest Tests

    [Fact]
    public void CrossChainTransferRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new CrossChainTransferRequest();

        // Assert
        request.TransferId.Should().NotBeEmpty();
        Guid.TryParse(request.TransferId, out _).Should().BeTrue();
        request.TokenAddress.Should().BeEmpty();
        request.Amount.Should().Be(0);
        request.Sender.Should().BeEmpty();
        request.Receiver.Should().BeEmpty();
        request.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CrossChainTransferRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new CrossChainTransferRequest();
        var metadata = new Dictionary<string, object> { ["gas"] = 21000 };

        // Act
        request.TransferId = "transfer-456";
        request.TokenAddress = "0xtoken123";
        request.Amount = 100.5m;
        request.Sender = "0xsender";
        request.Receiver = "0xreceiver";
        request.Metadata = metadata;

        // Assert
        request.TransferId.Should().Be("transfer-456");
        request.TokenAddress.Should().Be("0xtoken123");
        request.Amount.Should().Be(100.5m);
        request.Sender.Should().Be("0xsender");
        request.Receiver.Should().Be("0xreceiver");
        request.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region RemoteCallRequest Tests

    [Fact]
    public void RemoteCallRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new RemoteCallRequest();

        // Assert
        request.CallId.Should().NotBeEmpty();
        Guid.TryParse(request.CallId, out _).Should().BeTrue();
        request.ContractAddress.Should().BeEmpty();
        request.FunctionName.Should().BeEmpty();
        request.Parameters.Should().BeEmpty();
        request.Caller.Should().BeEmpty();
        request.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void RemoteCallRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new RemoteCallRequest();
        var parameters = new object[] { "param1", 42, true };
        var metadata = new Dictionary<string, object> { ["timeout"] = 30 };

        // Act
        request.CallId = "call-789";
        request.ContractAddress = "0xcontract";
        request.FunctionName = "transfer";
        request.Parameters = parameters;
        request.Caller = "0xcaller";
        request.Metadata = metadata;

        // Assert
        request.CallId.Should().Be("call-789");
        request.ContractAddress.Should().Be("0xcontract");
        request.FunctionName.Should().Be("transfer");
        request.Parameters.Should().BeEquivalentTo(parameters);
        request.Caller.Should().Be("0xcaller");
        request.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region CrossChainMessage Tests

    [Fact]
    public void CrossChainMessage_ShouldInitializeWithDefaults()
    {
        // Act
        var message = new CrossChainMessage();

        // Assert
        message.MessageId.Should().BeEmpty();
        message.SourceChain.Should().Be(BlockchainType.NeoN3);
        message.DestinationChain.Should().Be(BlockchainType.NeoN3);
        message.Sender.Should().BeEmpty();
        message.Receiver.Should().BeEmpty();
        message.Data.Should().BeEmpty();
        message.Status.Should().Be(MessageStatus.Pending);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CrossChainMessage_Properties_ShouldBeSettable()
    {
        // Arrange
        var message = new CrossChainMessage();
        var data = new byte[] { 0xAB, 0xCD };
        var createdAt = DateTime.UtcNow.AddMinutes(-10);

        // Act
        message.MessageId = "cross-msg-123";
        message.SourceChain = BlockchainType.NeoN3;
        message.DestinationChain = BlockchainType.NeoX;
        message.Sender = "neo-sender";
        message.Receiver = "neox-receiver";
        message.Data = data;
        message.Status = MessageStatus.Processing;
        message.CreatedAt = createdAt;

        // Assert
        message.MessageId.Should().Be("cross-msg-123");
        message.SourceChain.Should().Be(BlockchainType.NeoN3);
        message.DestinationChain.Should().Be(BlockchainType.NeoX);
        message.Sender.Should().Be("neo-sender");
        message.Receiver.Should().Be("neox-receiver");
        message.Data.Should().BeEquivalentTo(data);
        message.Status.Should().Be(MessageStatus.Processing);
        message.CreatedAt.Should().Be(createdAt);
    }

    #endregion

    #region CrossChainRoute Tests

    [Fact]
    public void CrossChainRoute_ShouldInitializeWithDefaults()
    {
        // Act
        var route = new CrossChainRoute();

        // Assert
        route.Source.Should().Be(BlockchainType.NeoN3);
        route.Destination.Should().Be(BlockchainType.NeoN3);
        route.IntermediateChains.Should().BeEmpty();
        route.EstimatedFee.Should().Be(0);
        route.EstimatedTime.Should().Be(TimeSpan.Zero);
        route.ReliabilityScore.Should().Be(0);
    }

    [Fact]
    public void CrossChainRoute_Properties_ShouldBeSettable()
    {
        // Arrange
        var route = new CrossChainRoute();
        var intermediateChains = new[] { "polygon", "arbitrum" };
        var estimatedTime = TimeSpan.FromMinutes(15);

        // Act
        route.Source = BlockchainType.NeoN3;
        route.Destination = BlockchainType.NeoX;
        route.IntermediateChains = intermediateChains;
        route.EstimatedFee = 0.001m;
        route.EstimatedTime = estimatedTime;
        route.ReliabilityScore = 0.95;

        // Assert
        route.Source.Should().Be(BlockchainType.NeoN3);
        route.Destination.Should().Be(BlockchainType.NeoX);
        route.IntermediateChains.Should().BeEquivalentTo(intermediateChains);
        route.EstimatedFee.Should().Be(0.001m);
        route.EstimatedTime.Should().Be(estimatedTime);
        route.ReliabilityScore.Should().Be(0.95);
    }

    #endregion

    #region CrossChainOperation Tests

    [Fact]
    public void CrossChainOperation_ShouldInitializeWithDefaults()
    {
        // Act
        var operation = new CrossChainOperation();

        // Assert
        operation.OperationId.Should().NotBeEmpty();
        Guid.TryParse(operation.OperationId, out _).Should().BeTrue();
        operation.Type.Should().Be(OperationType.Message);
        operation.SourceChain.Should().Be(BlockchainType.NeoN3);
        operation.DestinationChain.Should().Be(BlockchainType.NeoN3);
        operation.Amount.Should().Be(0);
        operation.Parameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CrossChainOperation_Properties_ShouldBeSettable()
    {
        // Arrange
        var operation = new CrossChainOperation();
        var parameters = new Dictionary<string, object> { ["gasPrice"] = "20000000000" };

        // Act
        operation.OperationId = "op-456";
        operation.Type = OperationType.Transfer;
        operation.SourceChain = BlockchainType.NeoN3;
        operation.DestinationChain = BlockchainType.NeoX;
        operation.Amount = 50.0m;
        operation.Parameters = parameters;

        // Assert
        operation.OperationId.Should().Be("op-456");
        operation.Type.Should().Be(OperationType.Transfer);
        operation.SourceChain.Should().Be(BlockchainType.NeoN3);
        operation.DestinationChain.Should().Be(BlockchainType.NeoX);
        operation.Amount.Should().Be(50.0m);
        operation.Parameters.Should().BeEquivalentTo(parameters);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void MessageStatus_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<MessageStatus>().Should().BeEquivalentTo([
            MessageStatus.Pending,
            MessageStatus.Processing,
            MessageStatus.Completed,
            MessageStatus.Failed,
            MessageStatus.Cancelled
        ]);
    }

    [Fact]
    public void OperationType_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<OperationType>().Should().BeEquivalentTo([
            OperationType.Message,
            OperationType.Transfer,
            OperationType.RemoteCall
        ]);
    }

    [Fact]
    public void BlockchainType_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<BlockchainType>().Should().BeEquivalentTo([
            BlockchainType.NeoN3,
            BlockchainType.NeoX
        ]);
    }

    #endregion
}
