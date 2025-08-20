using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


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
        request.Message.Should().NotBeNull().And.BeEmpty();
        request.DestinationAddress.Should().BeEmpty();
        request.MessageType.Should().BeEmpty();
        request.Parameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CrossChainMessageRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new CrossChainMessageRequest();
        var data = new byte[] { 1, 2, 3, 4 };
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        request.Message = data;
        request.DestinationAddress = "destination-address";
        request.MessageType = "test-message";
        request.Parameters = metadata;

        // Assert
        request.Message.Should().BeEquivalentTo(data);
        request.DestinationAddress.Should().Be("destination-address");
        request.MessageType.Should().Be("test-message");
        request.Parameters.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region CrossChainTransferRequest Tests

    [Fact]
    public void CrossChainTransferRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new CrossChainTransferRequest();

        // Assert
        request.TokenAddress.Should().BeEmpty();
        request.Amount.Should().Be(0);
        request.DestinationAddress.Should().BeEmpty();
        request.Data.Should().NotBeNull().And.BeEmpty();
        request.Sender.Should().BeEmpty();
    }

    [Fact]
    public void CrossChainTransferRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new CrossChainTransferRequest();
        var metadata = new Dictionary<string, object> { ["gas"] = 21000 };

        // Act
        request.TokenAddress = "0xtoken123";
        request.Amount = 100.5m;
        request.DestinationAddress = "0xdestination";
        request.Data = new byte[] { 1, 2, 3 };
        request.Sender = "0xsender";

        // Assert
        request.TokenAddress.Should().Be("0xtoken123");
        request.Amount.Should().Be(100.5m);
        request.DestinationAddress.Should().Be("0xdestination");
        request.Data.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        request.Sender.Should().Be("0xsender");
    }

    #endregion

    #region RemoteCallRequest Tests

    [Fact]
    public void RemoteCallRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new RemoteCallRequest();

        // Assert
        request.ContractAddress.Should().BeEmpty();
        request.MethodName.Should().BeEmpty();
        request.Parameters.Should().BeEmpty();
        request.GasLimit.Should().Be(0);
    }

    [Fact]
    public void RemoteCallRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new RemoteCallRequest();
        var parameters = new object[] { "param1", 42, true };
        // Act
        request.ContractAddress = "0xcontract";
        request.MethodName = "transfer";
        request.Parameters = parameters;
        request.GasLimit = 21000;

        // Assert
        request.ContractAddress.Should().Be("0xcontract");
        request.MethodName.Should().Be("transfer");
        request.Parameters.Should().BeEquivalentTo(parameters);
        request.GasLimit.Should().Be(21000);
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
        message.Content.Should().BeEmpty();
        message.Status.Should().Be(CrossChainMessageStatus.Pending);
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
        message.Content = data;
        message.Status = CrossChainMessageStatus.Processing;
        message.CreatedAt = createdAt;

        // Assert
        message.MessageId.Should().Be("cross-msg-123");
        message.SourceChain.Should().Be(BlockchainType.NeoN3);
        message.DestinationChain.Should().Be(BlockchainType.NeoX);
        message.Content.Should().BeEquivalentTo(data);
        message.Status.Should().Be(CrossChainMessageStatus.Processing);
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
        route.RouteId.Should().BeEmpty();
        route.SourceChain.Should().Be(BlockchainType.NeoN3);
        route.DestinationChain.Should().Be(BlockchainType.NeoN3);
        route.IntermediateHops.Should().BeEmpty();
        route.EstimatedCost.Should().Be(0);
        route.EstimatedTimeSeconds.Should().Be(0);
    }

    [Fact]
    public void CrossChainRoute_Properties_ShouldBeSettable()
    {
        // Arrange
        var route = new CrossChainRoute();
        var intermediateChains = new List<BlockchainType> { BlockchainType.NeoN3, BlockchainType.NeoX };
        var estimatedTime = TimeSpan.FromMinutes(15);

        // Act
        route.RouteId = "route-123";
        route.SourceChain = BlockchainType.NeoN3;
        route.DestinationChain = BlockchainType.NeoX;
        route.IntermediateHops = intermediateChains;
        route.EstimatedCost = 0.001m;
        route.EstimatedTimeSeconds = 900; // 15 minutes

        // Assert
        route.RouteId.Should().Be("route-123");
        route.SourceChain.Should().Be(BlockchainType.NeoN3);
        route.DestinationChain.Should().Be(BlockchainType.NeoX);
        route.IntermediateHops.Should().BeEquivalentTo(intermediateChains);
        route.EstimatedCost.Should().Be(0.001m);
        route.EstimatedTimeSeconds.Should().Be(900);
    }

    #endregion


    #region Enum Tests

    [Fact]
    public void CrossChainMessageStatus_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<CrossChainMessageStatus>().Should().BeEquivalentTo([
            CrossChainMessageStatus.Pending,
            CrossChainMessageStatus.Processing,
            CrossChainMessageStatus.Delivered,
            CrossChainMessageStatus.Failed,
            CrossChainMessageStatus.Cancelled
        ]);
    }

    [Fact]
    public void CrossChainOperation_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<CrossChainOperation>().Should().BeEquivalentTo([
            CrossChainOperation.MessageTransfer,
            CrossChainOperation.TokenTransfer,
            CrossChainOperation.ContractCall,
            CrossChainOperation.DataSync
        ]);
    }

    [Fact]
    public void BlockchainType_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<BlockchainType>().Should().BeEquivalentTo([
            BlockchainType.NeoN3,
            BlockchainType.NeoX,
            BlockchainType.Ethereum,
            BlockchainType.Test,
            BlockchainType.Bitcoin
        ]);
    }

    #endregion
}
