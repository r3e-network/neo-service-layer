using System.Text.Json;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Core.Tests.Blockchain;

/// <summary>
/// Comprehensive tests for IBlockchainClient interface and related blockchain functionality.
/// </summary>
public class BlockchainClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IBlockchainClient> _blockchainClientMock;

    public BlockchainClientTests()
    {
        _fixture = new Fixture();
        _blockchainClientMock = new Mock<IBlockchainClient>();
    }

    #region Block Operations Tests

    [Fact]
    public async Task GetBlockHeightAsync_ShouldReturnCurrentHeight()
    {
        // Arrange
        var expectedHeight = 1000000L;
        _blockchainClientMock.Setup(x => x.GetBlockHeightAsync())
                            .ReturnsAsync(expectedHeight);

        // Act
        var result = await _blockchainClientMock.Object.GetBlockHeightAsync();

        // Assert
        result.Should().Be(expectedHeight);
        _blockchainClientMock.Verify(x => x.GetBlockHeightAsync(), Times.Once);
    }

    [Fact]
    public async Task GetBlockAsync_ByHeight_ShouldReturnBlock()
    {
        // Arrange
        var blockHeight = 999999L;
        var expectedBlock = CreateTestBlock(blockHeight);

        _blockchainClientMock.Setup(x => x.GetBlockAsync(blockHeight))
                            .ReturnsAsync(expectedBlock);

        // Act
        var result = await _blockchainClientMock.Object.GetBlockAsync(blockHeight);

        // Assert
        result.Should().NotBeNull();
        result.Height.Should().Be(blockHeight);
        result.Hash.Should().Be(expectedBlock.Hash);
        result.Transactions.Should().HaveCount(expectedBlock.Transactions.Count);
        _blockchainClientMock.Verify(x => x.GetBlockAsync(blockHeight), Times.Once);
    }

    [Fact]
    public async Task GetBlockAsync_ByHash_ShouldReturnBlock()
    {
        // Arrange
        var blockHash = "0x1234567890abcdef1234567890abcdef12345678";
        var expectedBlock = CreateTestBlock(999999L, blockHash);

        _blockchainClientMock.Setup(x => x.GetBlockAsync(blockHash))
                            .ReturnsAsync(expectedBlock);

        // Act
        var result = await _blockchainClientMock.Object.GetBlockAsync(blockHash);

        // Assert
        result.Should().NotBeNull();
        result.Hash.Should().Be(blockHash);
        result.Height.Should().Be(expectedBlock.Height);
        _blockchainClientMock.Verify(x => x.GetBlockAsync(blockHash), Times.Once);
    }

    [Fact]
    public async Task GetBlockAsync_WithInvalidHeight_ShouldHandleGracefully()
    {
        // Arrange
        var invalidHeight = -1L;
        _blockchainClientMock.Setup(x => x.GetBlockAsync(invalidHeight))
                            .ThrowsAsync(new ArgumentException("Invalid block height"));

        // Act & Assert
        var action = () => _blockchainClientMock.Object.GetBlockAsync(invalidHeight);
        await action.Should().ThrowAsync<ArgumentException>()
                   .WithMessage("Invalid block height");
    }

    [Fact]
    public async Task GetBlockAsync_WithInvalidHash_ShouldHandleGracefully()
    {
        // Arrange
        var invalidHash = "invalid_hash";
        _blockchainClientMock.Setup(x => x.GetBlockAsync(invalidHash))
                            .ThrowsAsync(new ArgumentException("Invalid block hash"));

        // Act & Assert
        var action = () => _blockchainClientMock.Object.GetBlockAsync(invalidHash);
        await action.Should().ThrowAsync<ArgumentException>()
                   .WithMessage("Invalid block hash");
    }

    #endregion

    #region Transaction Operations Tests

    [Fact]
    public async Task GetTransactionAsync_ShouldReturnTransaction()
    {
        // Arrange
        var txHash = "0xabcdef1234567890abcdef1234567890abcdef12";
        var expectedTransaction = CreateTestTransaction(txHash);

        _blockchainClientMock.Setup(x => x.GetTransactionAsync(txHash))
                            .ReturnsAsync(expectedTransaction);

        // Act
        var result = await _blockchainClientMock.Object.GetTransactionAsync(txHash);

        // Assert
        result.Should().NotBeNull();
        result.Hash.Should().Be(txHash);
        result.Sender.Should().Be(expectedTransaction.Sender);
        result.Recipient.Should().Be(expectedTransaction.Recipient);
        result.Value.Should().Be(expectedTransaction.Value);
        _blockchainClientMock.Verify(x => x.GetTransactionAsync(txHash), Times.Once);
    }

    [Fact]
    public async Task SendTransactionAsync_ShouldReturnTransactionHash()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var expectedTxHash = "0x" + new string('a', 64);

        _blockchainClientMock.Setup(x => x.SendTransactionAsync(transaction))
                            .ReturnsAsync(expectedTxHash);

        // Act
        var result = await _blockchainClientMock.Object.SendTransactionAsync(transaction);

        // Assert
        result.Should().Be(expectedTxHash);
        result.Should().StartWith("0x");
        result.Length.Should().Be(66); // 0x + 64 hex characters
        _blockchainClientMock.Verify(x => x.SendTransactionAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task SendTransactionAsync_WithNullTransaction_ShouldThrowArgumentNullException()
    {
        // Arrange
        _blockchainClientMock.Setup(x => x.SendTransactionAsync(null!))
                            .ThrowsAsync(new ArgumentNullException("transaction"));

        // Act & Assert
        var action = () => _blockchainClientMock.Object.SendTransactionAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetTransactionAsync_WithInvalidHash_ShouldHandleGracefully()
    {
        // Arrange
        var invalidHash = "invalid_tx_hash";
        _blockchainClientMock.Setup(x => x.GetTransactionAsync(invalidHash))
                            .ThrowsAsync(new ArgumentException("Invalid transaction hash"));

        // Act & Assert
        var action = () => _blockchainClientMock.Object.GetTransactionAsync(invalidHash);
        await action.Should().ThrowAsync<ArgumentException>()
                   .WithMessage("Invalid transaction hash");
    }

    #endregion

    #region Subscription Tests

    [Fact]
    public async Task SubscribeToBlocksAsync_ShouldReturnSubscriptionId()
    {
        // Arrange
        var expectedSubscriptionId = Guid.NewGuid().ToString();
        var callbackInvoked = false;

        Func<Block, Task> callback = async (block) =>
        {
            callbackInvoked = true;
            await Task.CompletedTask;
        };

        _blockchainClientMock.Setup(x => x.SubscribeToBlocksAsync(callback))
                            .ReturnsAsync(expectedSubscriptionId);

        // Act
        var result = await _blockchainClientMock.Object.SubscribeToBlocksAsync(callback);

        // Assert
        result.Should().Be(expectedSubscriptionId);
        _blockchainClientMock.Verify(x => x.SubscribeToBlocksAsync(callback), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromBlocksAsync_ShouldReturnTrue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        _blockchainClientMock.Setup(x => x.UnsubscribeFromBlocksAsync(subscriptionId))
                            .ReturnsAsync(true);

        // Act
        var result = await _blockchainClientMock.Object.UnsubscribeFromBlocksAsync(subscriptionId);

        // Assert
        result.Should().BeTrue();
        _blockchainClientMock.Verify(x => x.UnsubscribeFromBlocksAsync(subscriptionId), Times.Once);
    }

    [Fact]
    public async Task SubscribeToTransactionsAsync_ShouldReturnSubscriptionId()
    {
        // Arrange
        var expectedSubscriptionId = Guid.NewGuid().ToString();

        Func<Transaction, Task> callback = async (tx) => await Task.CompletedTask;

        _blockchainClientMock.Setup(x => x.SubscribeToTransactionsAsync(callback))
                            .ReturnsAsync(expectedSubscriptionId);

        // Act
        var result = await _blockchainClientMock.Object.SubscribeToTransactionsAsync(callback);

        // Assert
        result.Should().Be(expectedSubscriptionId);
        _blockchainClientMock.Verify(x => x.SubscribeToTransactionsAsync(callback), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromTransactionsAsync_ShouldReturnTrue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        _blockchainClientMock.Setup(x => x.UnsubscribeFromTransactionsAsync(subscriptionId))
                            .ReturnsAsync(true);

        // Act
        var result = await _blockchainClientMock.Object.UnsubscribeFromTransactionsAsync(subscriptionId);

        // Assert
        result.Should().BeTrue();
        _blockchainClientMock.Verify(x => x.UnsubscribeFromTransactionsAsync(subscriptionId), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromBlocksAsync_WithInvalidSubscriptionId_ShouldReturnFalse()
    {
        // Arrange
        var invalidSubscriptionId = "invalid_id";
        _blockchainClientMock.Setup(x => x.UnsubscribeFromBlocksAsync(invalidSubscriptionId))
                            .ReturnsAsync(false);

        // Act
        var result = await _blockchainClientMock.Object.UnsubscribeFromBlocksAsync(invalidSubscriptionId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Smart Contract Tests

    [Fact]
    public async Task SubscribeToContractEventsAsync_ShouldReturnSubscriptionId()
    {
        // Arrange
        var contractAddress = "0x1234567890abcdef1234567890abcdef12345678";
        var eventName = "Transfer";
        var expectedSubscriptionId = Guid.NewGuid().ToString();

        Func<ContractEvent, Task> callback = async (evt) => await Task.CompletedTask;

        _blockchainClientMock.Setup(x => x.SubscribeToContractEventsAsync(contractAddress, eventName, callback))
                            .ReturnsAsync(expectedSubscriptionId);

        // Act
        var result = await _blockchainClientMock.Object.SubscribeToContractEventsAsync(contractAddress, eventName, callback);

        // Assert
        result.Should().Be(expectedSubscriptionId);
        _blockchainClientMock.Verify(x => x.SubscribeToContractEventsAsync(contractAddress, eventName, callback), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromContractEventsAsync_ShouldReturnTrue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        _blockchainClientMock.Setup(x => x.UnsubscribeFromContractEventsAsync(subscriptionId))
                            .ReturnsAsync(true);

        // Act
        var result = await _blockchainClientMock.Object.UnsubscribeFromContractEventsAsync(subscriptionId);

        // Assert
        result.Should().BeTrue();
        _blockchainClientMock.Verify(x => x.UnsubscribeFromContractEventsAsync(subscriptionId), Times.Once);
    }

    [Fact]
    public async Task CallContractMethodAsync_ShouldReturnResult()
    {
        // Arrange
        var contractAddress = "0x1234567890abcdef1234567890abcdef12345678";
        var methodName = "balanceOf";
        var args = new object[] { "NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx" };
        var expectedResult = "1000000000";

        _blockchainClientMock.Setup(x => x.CallContractMethodAsync(contractAddress, methodName, args))
                            .ReturnsAsync(expectedResult);

        // Act
        var result = await _blockchainClientMock.Object.CallContractMethodAsync(contractAddress, methodName, args);

        // Assert
        result.Should().Be(expectedResult);
        _blockchainClientMock.Verify(x => x.CallContractMethodAsync(contractAddress, methodName, args), Times.Once);
    }

    [Fact]
    public async Task InvokeContractMethodAsync_ShouldReturnTransactionHash()
    {
        // Arrange
        var contractAddress = "0x1234567890abcdef1234567890abcdef12345678";
        var methodName = "transfer";
        var args = new object[] { "NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx", "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y", 1000000 };
        var expectedTxHash = "0x" + new string('b', 64);

        _blockchainClientMock.Setup(x => x.InvokeContractMethodAsync(contractAddress, methodName, args))
                            .ReturnsAsync(expectedTxHash);

        // Act
        var result = await _blockchainClientMock.Object.InvokeContractMethodAsync(contractAddress, methodName, args);

        // Assert
        result.Should().Be(expectedTxHash);
        result.Should().StartWith("0x");
        result.Length.Should().Be(66);
        _blockchainClientMock.Verify(x => x.InvokeContractMethodAsync(contractAddress, methodName, args), Times.Once);
    }

    [Fact]
    public async Task CallContractMethodAsync_WithInvalidContract_ShouldThrowException()
    {
        // Arrange
        var invalidContractAddress = "invalid_address";
        var methodName = "balanceOf";
        var args = new object[] { "address" };

        _blockchainClientMock.Setup(x => x.CallContractMethodAsync(invalidContractAddress, methodName, args))
                            .ThrowsAsync(new ArgumentException("Invalid contract address"));

        // Act & Assert
        var action = () => _blockchainClientMock.Object.CallContractMethodAsync(invalidContractAddress, methodName, args);
        await action.Should().ThrowAsync<ArgumentException>()
                   .WithMessage("Invalid contract address");
    }

    #endregion

    #region BlockchainType Property Tests

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public void BlockchainType_ShouldReturnExpectedType(BlockchainType expectedType)
    {
        // Arrange
        _blockchainClientMock.Setup(x => x.BlockchainType).Returns(expectedType);

        // Act
        var result = _blockchainClientMock.Object.BlockchainType;

        // Assert
        result.Should().Be(expectedType);
    }

    #endregion

    #region Model Validation Tests

    [Fact]
    public void Block_ShouldInitializeWithDefaults()
    {
        // Act
        var block = new Block();

        // Assert
        block.Hash.Should().Be(string.Empty);
        block.Height.Should().Be(0);
        block.PreviousHash.Should().Be(string.Empty);
        block.Transactions.Should().NotBeNull().And.BeEmpty();
        block.Timestamp.Should().Be(default);
    }

    [Fact]
    public void Transaction_ShouldInitializeWithDefaults()
    {
        // Act
        var transaction = new Transaction();

        // Assert
        transaction.Hash.Should().Be(string.Empty);
        transaction.Sender.Should().Be(string.Empty);
        transaction.Recipient.Should().Be(string.Empty);
        transaction.Value.Should().Be(0);
        transaction.Data.Should().Be(string.Empty);
        transaction.BlockHash.Should().Be(string.Empty);
        transaction.BlockHeight.Should().Be(0);
        transaction.Timestamp.Should().Be(default);
    }

    [Fact]
    public void ContractEvent_ShouldInitializeWithDefaults()
    {
        // Act
        var contractEvent = new ContractEvent();

        // Assert
        contractEvent.ContractAddress.Should().Be(string.Empty);
        contractEvent.EventName.Should().Be(string.Empty);
        contractEvent.EventData.Should().Be(string.Empty);
        contractEvent.Parameters.Should().NotBeNull().And.BeEmpty();
        contractEvent.TransactionHash.Should().Be(string.Empty);
        contractEvent.BlockHash.Should().Be(string.Empty);
        contractEvent.BlockHeight.Should().Be(0);
        contractEvent.Timestamp.Should().Be(default);
    }

    [Fact]
    public void Block_ShouldSupportSerialization()
    {
        // Arrange
        var originalBlock = CreateTestBlock(123456L);

        // Act
        var json = JsonSerializer.Serialize(originalBlock);
        var deserializedBlock = JsonSerializer.Deserialize<Block>(json);

        // Assert
        deserializedBlock.Should().NotBeNull();
        deserializedBlock!.Hash.Should().Be(originalBlock.Hash);
        deserializedBlock.Height.Should().Be(originalBlock.Height);
        deserializedBlock.PreviousHash.Should().Be(originalBlock.PreviousHash);
        deserializedBlock.Transactions.Should().HaveCount(originalBlock.Transactions.Count);
    }

    [Fact]
    public void Transaction_ShouldSupportSerialization()
    {
        // Arrange
        var originalTransaction = CreateTestTransaction();

        // Act
        var json = JsonSerializer.Serialize(originalTransaction);
        var deserializedTransaction = JsonSerializer.Deserialize<Transaction>(json);

        // Assert
        deserializedTransaction.Should().NotBeNull();
        deserializedTransaction!.Hash.Should().Be(originalTransaction.Hash);
        deserializedTransaction.Sender.Should().Be(originalTransaction.Sender);
        deserializedTransaction.Recipient.Should().Be(originalTransaction.Recipient);
        deserializedTransaction.Value.Should().Be(originalTransaction.Value);
    }

    [Fact]
    public void ContractEvent_ShouldSupportSerialization()
    {
        // Arrange
        var originalEvent = CreateTestContractEvent();

        // Act
        var json = JsonSerializer.Serialize(originalEvent);
        var deserializedEvent = JsonSerializer.Deserialize<ContractEvent>(json);

        // Assert
        deserializedEvent.Should().NotBeNull();
        deserializedEvent!.ContractAddress.Should().Be(originalEvent.ContractAddress);
        deserializedEvent.EventName.Should().Be(originalEvent.EventName);
        deserializedEvent.Parameters.Should().HaveCount(originalEvent.Parameters.Count);
    }

    #endregion

    #region Helper Methods

    private Block CreateTestBlock(long height = 123456L, string? hash = null)
    {
        return new Block
        {
            Hash = hash ?? $"0x{new string('1', 64)}",
            Height = height,
            Timestamp = DateTime.UtcNow,
            PreviousHash = $"0x{new string('0', 64)}",
            Transactions =
            [
                CreateTestTransaction("0x" + new string('a', 64)),
                CreateTestTransaction("0x" + new string('b', 64))
            ]
        };
    }

    private static Transaction CreateTestTransaction(string? hash = null)
    {
        return new Transaction
        {
            Hash = hash ?? $"0x{new string('c', 64)}",
            Sender = "NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx",
            Recipient = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y",
            Value = 1000000m,
            Data = "transfer test",
            Timestamp = DateTime.UtcNow,
            BlockHash = $"0x{new string('d', 64)}",
            BlockHeight = 123456L
        };
    }

    private static ContractEvent CreateTestContractEvent()
    {
        return new ContractEvent
        {
            ContractAddress = "0x1234567890abcdef1234567890abcdef12345678",
            EventName = "Transfer",
            EventData = "transfer data",
            Parameters = new Dictionary<string, object>
            {
                { "from", "NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx" },
                { "to", "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y" },
                { "amount", 1000000 }
            },
            TransactionHash = $"0x{new string('e', 64)}",
            BlockHash = $"0x{new string('f', 64)}",
            BlockHeight = 123456L,
            Timestamp = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// Enumeration for blockchain types used in testing.
/// </summary>
public enum TestBlockchainType
{
    NeoN3,
    NeoX,
    Ethereum,
    Bitcoin
}
