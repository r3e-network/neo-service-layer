using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class EventServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<ILogger<EventService>> _mockLogger;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockLogger = new Mock<ILogger<EventService>>();

            // Create an adapter that implements Core.Interfaces.ITeeHostService
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);

            _eventService = new EventService(teeHostServiceAdapter, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateSubscriptionAsync_ValidSubscription_ReturnsCreatedSubscription()
        {
            // Arrange
            var subscription = new Subscription
            {
                UserId = "user123",
                EventType = EventType.BlockchainEvent,
                EventFilter = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "event", "Transfer" }
                },
                CallbackUrl = "https://example.com/callback",
                Status = SubscriptionStatus.Active
            };

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "subscription_id", Guid.NewGuid().ToString() }
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _eventService.CreateSubscriptionAsync(subscription);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(subscription.UserId, result.UserId);
            Assert.Equal(subscription.EventType, result.EventType);
            Assert.Equal(subscription.EventFilter, result.EventFilter);
            Assert.Equal(subscription.CallbackUrl, result.CallbackUrl);
            Assert.Equal(subscription.Status, result.Status);
            Assert.NotNull(result.Id);
            Assert.NotEqual(Guid.Empty.ToString(), result.Id);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Event &&
                m.Data.Contains("create_subscription") &&
                m.Data.Contains(subscription.EventType.ToString()) &&
                m.Data.Contains(subscription.CallbackUrl))),
                Times.Once);
        }

        [Fact]
        public async Task CreateSubscriptionAsync_NullSubscription_ThrowsArgumentNullException()
        {
            // Arrange
            Subscription subscription = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _eventService.CreateSubscriptionAsync(subscription));
        }

        [Fact]
        public async Task GetSubscriptionAsync_ExistingSubscriptionId_ReturnsSubscription()
        {
            // Arrange
            string subscriptionId = Guid.NewGuid().ToString();
            var expectedSubscription = new Subscription
            {
                Id = subscriptionId,
                UserId = "user123",
                EventType = EventType.BlockchainEvent,
                EventFilter = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "event", "Transfer" }
                },
                CallbackUrl = "https://example.com/callback",
                Status = SubscriptionStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            // First, create the subscription to store it in the service
            var createSubscriptionResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "subscription_id", subscriptionId }
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Event &&
                m.Data.Contains("create_subscription"))))
                .ReturnsAsync(createSubscriptionResponse);

            // Create the subscription first
            await _eventService.CreateSubscriptionAsync(expectedSubscription);

            // No need to set up the mock for GetSubscriptionAsync since we're using the in-memory storage

            // Act
            var result = await _eventService.GetSubscriptionAsync(subscriptionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSubscription.Id, result.Id);
            Assert.Equal(expectedSubscription.UserId, result.UserId);
            Assert.Equal(expectedSubscription.EventType, result.EventType);
            Assert.Equal(expectedSubscription.CallbackUrl, result.CallbackUrl);
            Assert.Equal(expectedSubscription.Status, result.Status);

            // No need to verify TEE host service call since we're using in-memory storage
        }

        [Fact]
        public async Task GetSubscriptionAsync_NonExistingSubscriptionId_ReturnsNull()
        {
            // Arrange
            string subscriptionId = Guid.NewGuid().ToString();

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = "null",
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _eventService.GetSubscriptionAsync(subscriptionId);

            // Assert
            Assert.Null(result);

            // No need to verify TEE host service call since we're using in-memory storage
        }

        [Fact]
        public async Task GetSubscriptionAsync_NullSubscriptionId_ThrowsArgumentException()
        {
            // Arrange
            string subscriptionId = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _eventService.GetSubscriptionAsync(subscriptionId));
        }

        [Fact]
        public async Task GetSubscriptionsAsync_ReturnsSubscriptions()
        {
            // Arrange
            string userId = "user123";
            var subscription1 = new Subscription
            {
                UserId = userId,
                EventType = EventType.BlockchainEvent,
                EventFilter = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "event", "Transfer" }
                },
                CallbackUrl = "https://example.com/callback1",
                Status = SubscriptionStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var subscription2 = new Subscription
            {
                UserId = userId,
                EventType = EventType.SystemEvent,
                EventFilter = new Dictionary<string, object>
                {
                    { "type", "maintenance" }
                },
                CallbackUrl = "https://example.com/callback2",
                Status = SubscriptionStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            // Setup responses for creating subscriptions
            var createResponse1 = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "subscription_id", Guid.NewGuid().ToString() }
                }),
                CreatedAt = DateTime.UtcNow
            };

            var createResponse2 = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "subscription_id", Guid.NewGuid().ToString() }
                }),
                CreatedAt = DateTime.UtcNow
            };

            // Setup mock for creating subscriptions
            _mockTeeHostService.SetupSequence(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Event &&
                m.Data.Contains("create_subscription"))))
                .ReturnsAsync(createResponse1)
                .ReturnsAsync(createResponse2);

            // Create the subscriptions first
            await _eventService.CreateSubscriptionAsync(subscription1);
            await _eventService.CreateSubscriptionAsync(subscription2);

            var expectedSubscriptions = new List<Subscription> { subscription1, subscription2 };

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = JsonSerializer.Serialize(expectedSubscriptions),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _eventService.GetSubscriptionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, s => s.EventType == EventType.BlockchainEvent);
            Assert.Contains(result, s => s.EventType == EventType.SystemEvent);

            // No need to verify TEE host service call since we're using in-memory storage
        }

        [Fact]
        public async Task PublishEventAsync_ValidEvent_ReturnsPublishedEvent()
        {
            // Arrange
            var @event = new Event
            {
                Type = EventType.BlockchainEvent,
                Source = "neo-blockchain",
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "event", "Transfer" },
                    { "from", "address1" },
                    { "to", "address2" },
                    { "amount", 100 }
                },
                OccurredAt = DateTime.UtcNow
            };

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Event,
                Data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "event_id", Guid.NewGuid().ToString() }
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _eventService.PublishEventAsync(@event);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Type, result.Type);
            Assert.Equal(@event.Source, result.Source);
            Assert.Equal(@event.Data, result.Data);
            Assert.Equal(@event.OccurredAt, result.OccurredAt);
            Assert.NotNull(result.Id);
            Assert.NotEqual(Guid.Empty.ToString(), result.Id);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Event &&
                m.Data.Contains("publish_event"))),
                Times.Once);
        }
    }
}
