using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class NeoN3EventListenerServiceTests
    {
        private readonly Mock<ILogger<NeoN3EventListenerService>> _loggerMock;
        private readonly Mock<INeoN3BlockchainService> _blockchainServiceMock;
        private readonly Mock<IEventService> _eventServiceMock;
        private readonly NeoN3EventListenerService _service;

        public NeoN3EventListenerServiceTests()
        {
            _loggerMock = new Mock<ILogger<NeoN3EventListenerService>>();
            _blockchainServiceMock = new Mock<INeoN3BlockchainService>();
            _eventServiceMock = new Mock<IEventService>();

            _service = new NeoN3EventListenerService(
                _loggerMock.Object,
                _blockchainServiceMock.Object,
                _eventServiceMock.Object
            );
        }

        [Fact]
        public void AddSubscription_ShouldReturnSubscriptionId()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";
            var eventName = "Transfer";
            var callbackUrl = "https://example.com/callback";
            var startBlock = 10000u;

            // Act
            var result = _service.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(Guid.TryParse(result, out _));
        }

        [Fact]
        public void AddSubscription_ShouldUpdateLastProcessedBlock()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";
            var eventName = "Transfer";
            var callbackUrl = "https://example.com/callback";
            var startBlock = 10000u;

            // Act
            _service.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);

            // Add another subscription with a lower start block
            var lowerStartBlock = 5000u;
            _service.AddSubscription(scriptHash, "AnotherEvent", callbackUrl, lowerStartBlock);

            // Add another subscription with a higher start block
            var higherStartBlock = 15000u;
            _service.AddSubscription(scriptHash, "YetAnotherEvent", callbackUrl, higherStartBlock);

            // Assert - Use reflection to check the private _lastProcessedBlocks dictionary
            var lastProcessedBlocksField = typeof(NeoN3EventListenerService).GetField("_lastProcessedBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lastProcessedBlocks = (Dictionary<string, uint>)lastProcessedBlocksField.GetValue(_service);

            Assert.True(lastProcessedBlocks.ContainsKey(scriptHash));
            Assert.Equal(lowerStartBlock, lastProcessedBlocks[scriptHash]);
        }

        [Fact]
        public void RemoveSubscription_WhenSubscriptionExists_ShouldReturnTrue()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";
            var eventName = "Transfer";
            var callbackUrl = "https://example.com/callback";
            var startBlock = 10000u;

            var subscriptionId = _service.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);

            // Act
            var result = _service.RemoveSubscription(subscriptionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RemoveSubscription_WhenSubscriptionDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentSubscriptionId = Guid.NewGuid().ToString();

            // Act
            var result = _service.RemoveSubscription(nonExistentSubscriptionId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldPollEvents()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";
            var eventName = "Transfer";
            var callbackUrl = "https://example.com/callback";
            var startBlock = 10000u;

            _service.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);

            var currentHeight = "10100";
            _blockchainServiceMock.Setup(b => b.GetBlockchainHeightAsync())
                .ReturnsAsync(currentHeight);

            var events = new BlockchainEvent[]
            {
                new BlockchainEvent
                {
                    TxHash = "0xabcdef1234567890",
                    BlockIndex = 10050,
                    EventName = eventName,
                    State = new object[] { "address1", "address2", 100 }
                }
            };

            _blockchainServiceMock.Setup(b => b.GetContractEventsAsync(scriptHash, (int)startBlock, 100))
                .ReturnsAsync(events);

            _eventServiceMock.Setup(e => e.ProcessEventAsync(It.IsAny<Event>(), callbackUrl))
                .Returns(Task.FromResult(true));

            // Act - Start the service and let it run for a short time
            var cancellationTokenSource = new CancellationTokenSource();
            var task = _service.StartAsync(cancellationTokenSource.Token);

            // Wait a bit to allow the service to process events
            await Task.Delay(100);

            // Stop the service
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            _blockchainServiceMock.Verify(b => b.GetBlockchainHeightAsync(), Times.AtLeastOnce);
            _blockchainServiceMock.Verify(b => b.GetContractEventsAsync(scriptHash, (int)startBlock, 100), Times.AtLeastOnce);
            _eventServiceMock.Verify(e => e.ProcessEventAsync(It.IsAny<Event>(), callbackUrl), Times.AtLeastOnce);

            // Verify that the last processed block was updated
            var lastProcessedBlocksField = typeof(NeoN3EventListenerService).GetField("_lastProcessedBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lastProcessedBlocks = (Dictionary<string, uint>)lastProcessedBlocksField.GetValue(_service);

            Assert.True(lastProcessedBlocks.ContainsKey(scriptHash));
            Assert.Equal(10050u, lastProcessedBlocks[scriptHash]);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoNewBlocks_ShouldNotProcessEvents()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";
            var eventName = "Transfer";
            var callbackUrl = "https://example.com/callback";
            var startBlock = 10000u;

            _service.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);

            var currentHeight = "10000"; // Same as startBlock
            _blockchainServiceMock.Setup(b => b.GetBlockchainHeightAsync())
                .ReturnsAsync(currentHeight);

            // Act - Start the service and let it run for a short time
            var cancellationTokenSource = new CancellationTokenSource();
            var task = _service.StartAsync(cancellationTokenSource.Token);

            // Wait a bit to allow the service to process events
            await Task.Delay(100);

            // Stop the service
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            _blockchainServiceMock.Verify(b => b.GetBlockchainHeightAsync(), Times.AtLeastOnce);
            _blockchainServiceMock.Verify(b => b.GetContractEventsAsync(scriptHash, (int)startBlock, 100), Times.Never);
            _eventServiceMock.Verify(e => e.ProcessEventAsync(It.IsAny<Event>(), callbackUrl), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoEvents_ShouldUpdateLastProcessedBlock()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";
            var eventName = "Transfer";
            var callbackUrl = "https://example.com/callback";
            var startBlock = 10000u;

            _service.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);

            var currentHeight = "10100";
            _blockchainServiceMock.Setup(b => b.GetBlockchainHeightAsync())
                .ReturnsAsync(currentHeight);

            var events = Array.Empty<BlockchainEvent>();
            _blockchainServiceMock.Setup(b => b.GetContractEventsAsync(scriptHash, (int)startBlock, 100))
                .ReturnsAsync(events);

            // Act - Start the service and let it run for a short time
            var cancellationTokenSource = new CancellationTokenSource();
            var task = _service.StartAsync(cancellationTokenSource.Token);

            // Wait a bit to allow the service to process events
            await Task.Delay(100);

            // Stop the service
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            _blockchainServiceMock.Verify(b => b.GetBlockchainHeightAsync(), Times.AtLeastOnce);
            _blockchainServiceMock.Verify(b => b.GetContractEventsAsync(scriptHash, (int)startBlock, 100), Times.AtLeastOnce);
            _eventServiceMock.Verify(e => e.ProcessEventAsync(It.IsAny<Event>(), callbackUrl), Times.Never);

            // Verify that the last processed block was updated to the current height
            var lastProcessedBlocksField = typeof(NeoN3EventListenerService).GetField("_lastProcessedBlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lastProcessedBlocks = (Dictionary<string, uint>)lastProcessedBlocksField.GetValue(_service);

            Assert.True(lastProcessedBlocks.ContainsKey(scriptHash));
            Assert.Equal(10100u, lastProcessedBlocks[scriptHash]);
        }
    }
}
