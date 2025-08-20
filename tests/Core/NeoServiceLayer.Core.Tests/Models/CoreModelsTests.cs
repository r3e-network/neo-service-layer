using NeoServiceLayer.Core.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Core.Tests.Models;

/// <summary>
/// Tests for Core model classes to verify property behavior and default values.
/// </summary>
public class CoreModelsTests
{
    #region PendingTransaction Tests

    [Fact]
    public void PendingTransaction_ShouldInitializeWithDefaults()
    {
        // Act
        var transaction = new PendingTransaction();

        // Assert
        transaction.TransactionId.Should().BeEmpty();
        transaction.Hash.Should().BeEmpty();
        transaction.From.Should().BeEmpty();
        transaction.To.Should().BeEmpty();
        transaction.Value.Should().Be(0);
        transaction.GasPrice.Should().Be(0);
        transaction.GasLimit.Should().Be(0);
        transaction.Data.Should().BeEmpty();
        transaction.Nonce.Should().Be(0);
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        transaction.Priority.Should().Be(TransactionPriority.Normal);
        transaction.FairnessLevel.Should().Be(FairnessLevel.Standard);
        transaction.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void PendingTransaction_Properties_ShouldBeSettable()
    {
        // Arrange
        var transaction = new PendingTransaction();
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        var createdAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        transaction.TransactionId = "tx-123";
        transaction.Hash = "0xabc123";
        transaction.From = "from-address";
        transaction.To = "to-address";
        transaction.Value = 100.5m;
        transaction.GasPrice = 20.0m;
        transaction.GasLimit = 21000;
        transaction.Data = data;
        transaction.Nonce = 42;
        transaction.CreatedAt = createdAt;
        transaction.Priority = TransactionPriority.High;
        transaction.FairnessLevel = FairnessLevel.High;
        transaction.Metadata = metadata;

        // Assert
        transaction.TransactionId.Should().Be("tx-123");
        transaction.Hash.Should().Be("0xabc123");
        transaction.From.Should().Be("from-address");
        transaction.To.Should().Be("to-address");
        transaction.Value.Should().Be(100.5m);
        transaction.GasPrice.Should().Be(20.0m);
        transaction.GasLimit.Should().Be(21000);
        transaction.Data.Should().BeEquivalentTo(data);
        transaction.Nonce.Should().Be(42);
        transaction.CreatedAt.Should().Be(createdAt);
        transaction.Priority.Should().Be(TransactionPriority.High);
        transaction.FairnessLevel.Should().Be(FairnessLevel.High);
        transaction.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Theory]
    [InlineData(TransactionPriority.Low)]
    [InlineData(TransactionPriority.Normal)]
    [InlineData(TransactionPriority.High)]
    [InlineData(TransactionPriority.Critical)]
    public void PendingTransaction_ShouldAcceptAllPriorityLevels(TransactionPriority priority)
    {
        // Arrange
        var transaction = new PendingTransaction();

        // Act
        transaction.Priority = priority;

        // Assert
        transaction.Priority.Should().Be(priority);
    }

    [Theory]
    [InlineData(FairnessLevel.Basic)]
    [InlineData(FairnessLevel.Standard)]
    [InlineData(FairnessLevel.High)]
    [InlineData(FairnessLevel.Maximum)]
    public void PendingTransaction_ShouldAcceptAllFairnessLevels(FairnessLevel fairnessLevel)
    {
        // Arrange
        var transaction = new PendingTransaction();

        // Act
        transaction.FairnessLevel = fairnessLevel;

        // Assert
        transaction.FairnessLevel.Should().Be(fairnessLevel);
    }

    #endregion

    #region OrderingPool Tests

    [Fact]
    public void OrderingPool_ShouldInitializeWithDefaults()
    {
        // Act
        var pool = new OrderingPool();

        // Assert
        pool.PoolId.Should().BeEmpty();
        pool.Name.Should().BeEmpty();
        pool.Algorithm.Should().Be(OrderingAlgorithm.FIFO);
        pool.PendingTransactions.Should().NotBeNull().And.BeEmpty();
        pool.MaxSize.Should().Be(1000);
        pool.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        pool.IsActive.Should().BeTrue();
        pool.Configuration.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void OrderingPool_Properties_ShouldBeSettable()
    {
        // Arrange
        var pool = new OrderingPool();
        var pendingTransactions = new List<PendingTransaction>
        {
            new() { TransactionId = "tx1" },
            new() { TransactionId = "tx2" }
        };
        var configuration = new Dictionary<string, object> { ["setting"] = "value" };
        var createdAt = DateTime.UtcNow.AddHours(-1);

        // Act
        pool.PoolId = "pool-456";
        pool.Name = "Test Pool";
        pool.Algorithm = OrderingAlgorithm.Priority;
        pool.PendingTransactions = pendingTransactions;
        pool.MaxSize = 500;
        pool.CreatedAt = createdAt;
        pool.IsActive = false;
        pool.Configuration = configuration;

        // Assert
        pool.PoolId.Should().Be("pool-456");
        pool.Name.Should().Be("Test Pool");
        pool.Algorithm.Should().Be(OrderingAlgorithm.Priority);
        pool.PendingTransactions.Should().BeEquivalentTo(pendingTransactions);
        pool.MaxSize.Should().Be(500);
        pool.CreatedAt.Should().Be(createdAt);
        pool.IsActive.Should().BeFalse();
        pool.Configuration.Should().BeEquivalentTo(configuration);
    }

    [Theory]
    [InlineData(OrderingAlgorithm.FIFO)]
    [InlineData(OrderingAlgorithm.Priority)]
    [InlineData(OrderingAlgorithm.GasPrice)]
    [InlineData(OrderingAlgorithm.Fair)]
    [InlineData(OrderingAlgorithm.TimeWeightedFair)]
    public void OrderingPool_ShouldAcceptAllOrderingAlgorithms(OrderingAlgorithm algorithm)
    {
        // Arrange
        var pool = new OrderingPool();

        // Act
        pool.Algorithm = algorithm;

        // Assert
        pool.Algorithm.Should().Be(algorithm);
    }

    [Fact]
    public void OrderingPool_PendingTransactions_ShouldBeModifiable()
    {
        // Arrange
        var pool = new OrderingPool();
        var transaction = new PendingTransaction { TransactionId = "tx-new" };

        // Act
        pool.PendingTransactions.Add(transaction);

        // Assert
        pool.PendingTransactions.Should().HaveCount(1);
        pool.PendingTransactions.First().TransactionId.Should().Be("tx-new");
    }

    [Fact]
    public void OrderingPool_Configuration_ShouldBeModifiable()
    {
        // Arrange
        var pool = new OrderingPool();

        // Act
        pool.Configuration["timeout"] = 30;
        pool.Configuration["retries"] = 3;

        // Assert
        pool.Configuration.Should().HaveCount(2);
        pool.Configuration["timeout"].Should().Be(30);
        pool.Configuration["retries"].Should().Be(3);
    }

    #endregion
}
