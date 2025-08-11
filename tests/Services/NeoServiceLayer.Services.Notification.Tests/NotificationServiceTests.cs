using Microsoft.Extensions.Configuration;
﻿using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.Notification.Models;
using static NeoServiceLayer.Services.Notification.Models.NotificationChannel;
using NeoServiceLayer.TestInfrastructure;
using Xunit;

namespace NeoServiceLayer.Services.Notification.Tests;

/// <summary>
/// Comprehensive unit tests for NotificationService covering all notification operations.
/// Tests notification sending, subscriptions, channels, error handling, and performance.
/// </summary>
public class NotificationServiceTests : TestBase, IDisposable
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly Mock<IOptions<NotificationOptions>> _optionsMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _optionsMock = new Mock<IOptions<NotificationOptions>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        SetupConfiguration();
        SetupHttpClient();

        // Create service with mock enclave manager for testing
        _service = new NotificationService(_optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object, null);
    }

    #region Service Lifecycle Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Name.Should().Be("Notification");
        _service.Description.Should().Be("Secure Notification Service");
        _service.Version.Should().Be("1.0.0");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task InitializeAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Notification Service initialized successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StartAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.StartAsync();

        // Assert
        result.Should().BeTrue();
        _service.IsRunning.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Notification Service started successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StopAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.StopAsync();

        // Assert
        result.Should().BeTrue();
        _service.IsRunning.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Information, "Notification Service stopped successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task GetHealthAsync_ShouldReturnHealthy()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetHealthAsync();

        // Assert
        result.Should().Be(ServiceHealth.Healthy);
    }

    #endregion

    #region Notification Sending Tests

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task SendNotificationAsync_ValidEmailRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SendNotificationRequest
        {
            Recipient = "test@example.com",
            Subject = "Test Email",
            Message = "This is a test email notification",
            Channel = NotificationChannel.Email,
            Priority = Services.Notification.Models.NotificationPriority.Normal,
            Metadata = new Dictionary<string, object> { ["source"] = "unit_test" }
        };

        // Act
        var result = await _service.SendNotificationAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be(Services.Notification.Models.DeliveryStatus.Delivered);
        result.NotificationId.Should().NotBeNullOrEmpty();
        result.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(LogLevel.Information, "Notification sent successfully");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task SendNotificationAsync_ValidWebhookRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SendNotificationRequest
        {
            Recipient = "https://api.example.com/webhook",
            Subject = "Test Webhook",
            Message = "This is a test webhook notification",
            Channel = NotificationChannel.Webhook,
            Priority = Services.Notification.Models.NotificationPriority.High,
            Metadata = new Dictionary<string, object> { ["source"] = "unit_test" }
        };

        // Act
        var result = await _service.SendNotificationAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be(Services.Notification.Models.DeliveryStatus.Delivered);
        result.NotificationId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(LogLevel.Information, "Notification sent successfully");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task SendNotificationAsync_ValidSmsRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        SetupSmsConfiguration();
        var request = new SendNotificationRequest
        {
            Recipient = "+1234567890",
            Subject = "Test SMS",
            Message = "This is a test SMS notification",
            Channel = NotificationChannel.SMS,
            Priority = Services.Notification.Models.NotificationPriority.Critical,
            Metadata = new Dictionary<string, object> { ["source"] = "unit_test" }
        };

        // Act
        var result = await _service.SendNotificationAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be(Services.Notification.Models.DeliveryStatus.Delivered);
        result.NotificationId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(LogLevel.Information, "Notification sent successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    public async Task SendNotificationAsync_UnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SendNotificationRequest
        {
            Recipient = "test@example.com",
            Subject = "Test",
            Message = "Test message",
            Channel = NotificationChannel.Email
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.SendNotificationAsync(request, (BlockchainType)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    public async Task SendNotificationAsync_ServiceNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new NotificationService(_optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object, null);
        var request = new SendNotificationRequest
        {
            Recipient = "test@example.com",
            Subject = "Test",
            Message = "Test message",
            Channel = NotificationChannel.Email
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendNotificationAsync(request, BlockchainType.NeoN3));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    public async Task SendNotificationAsync_EmptyRecipient_ShouldReturnFailedResult()
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SendNotificationRequest
        {
            Recipient = "",
            Subject = "Test",
            Message = "Test message",
            Channel = NotificationChannel.Email
        };

        // Act
        var result = await _service.SendNotificationAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Status.Should().Be(Services.Notification.Models.DeliveryStatus.Failed);
        result.ErrorMessage.Should().Contain("Recipient is required");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "NotificationSending")]
    public async Task SendNotificationAsync_UnsupportedChannel_ShouldReturnFailedResult()
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SendNotificationRequest
        {
            Recipient = "test@example.com",
            Subject = "Test",
            Message = "Test message",
            Channel = (NotificationChannel)999
        };

        // Act
        var result = await _service.SendNotificationAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Status.Should().Be(Services.Notification.Models.DeliveryStatus.Failed);
        result.ErrorMessage.Should().Contain("is not enabled");
    }

    #endregion

    #region Bulk Notification Tests

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "BulkNotifications")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task SendBatchNotificationsAsync_ValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        dynamic request = new
        {
            Recipients = new List<string> { "user1@example.com", "user2@example.com", "user3@example.com" },
            Subject = "Bulk Test Email",
            Message = "This is a bulk email notification",
            Channel = NotificationChannel.Email,
            Priority = Services.Notification.Models.NotificationPriority.Normal,
            Metadata = new Dictionary<string, object> { ["batch_type"] = "promotional" }
        };

        // Act
        var result = await _service.SendBatchNotificationsAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be(Services.Notification.Models.DeliveryStatus.Delivered);
        result.NotificationId.Should().NotBeNullOrEmpty();
        result.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(LogLevel.Information, "Bulk notification completed");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BulkNotifications")]
    public async Task SendBatchNotificationsAsync_UnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await InitializeServiceAsync();
        dynamic request = new
        {
            Recipients = new List<string> { "user1@example.com" },
            Subject = "Test",
            Message = "Test message",
            Channel = NotificationChannel.Email
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.SendBatchNotificationsAsync(request, (BlockchainType)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BulkNotifications")]
    public async Task SendBatchNotificationsAsync_ServiceNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new NotificationService(_optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object, null);
        dynamic request = new
        {
            Recipients = new List<string> { "user1@example.com" },
            Subject = "Test",
            Message = "Test message",
            Channel = NotificationChannel.Email
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendBatchNotificationsAsync(request, BlockchainType.NeoN3));
    }

    #endregion

    #region Subscription Management Tests

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Subscriptions")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task SubscribeAsync_ValidSubscription_ShouldReturnSubscriptionId(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SubscribeRequest
        {
            Recipient = "user@example.com",
            Channel = NotificationChannel.Email,
            EventTypes = new[] { "transaction", "block" }
        };

        // Act
        var result = await _service.SubscribeAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SubscriptionId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(LogLevel.Information, "Created notification subscription");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Subscriptions")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task UnsubscribeAsync_ExistingSubscription_ShouldReturnTrue(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SubscribeRequest
        {
            Recipient = "user@example.com",
            Channel = NotificationChannel.Email,
            EventTypes = new[] { "transaction" }
        };
        var subscribeResult = await _service.SubscribeAsync(request, blockchainType);

        // Act
        var unsubscribeRequest = new UnsubscribeRequest { SubscriptionId = subscribeResult.SubscriptionId };
        var result = await _service.UnsubscribeAsync(unsubscribeRequest, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Removed notification subscription");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Subscriptions")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task UnsubscribeAsync_NonExistentSubscription_ShouldReturnFalse(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var unsubscribeRequest = new UnsubscribeRequest { SubscriptionId = nonExistentId };
        var result = await _service.UnsubscribeAsync(unsubscribeRequest, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Warning, "Subscription");
        VerifyLoggerCalled(LogLevel.Warning, "not found for removal");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Subscriptions")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetSubscriptionsAsync_WithSubscriptions_ShouldReturnSubscriptions(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var recipient = "user@example.com";
        var request1 = new SubscribeRequest
        {
            Recipient = recipient,
            Channel = NotificationChannel.Email,
            EventTypes = new[] { "transaction" }
        };
        var request2 = new SubscribeRequest
        {
            Recipient = recipient,
            Channel = NotificationChannel.Webhook,
            EventTypes = new[] { "block" }
        };

        await _service.SubscribeAsync(request1, blockchainType);
        await _service.SubscribeAsync(request2, blockchainType);

        // Act
        var subscriptions = await _service.GetSubscriptionsAsync(recipient, blockchainType);

        // Assert
        subscriptions.Should().NotBeNull();
        subscriptions.Should().HaveCount(2);
        subscriptions.Should().AllSatisfy(s => s.Recipient.Should().Be(recipient));
        subscriptions.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
        VerifyLoggerCalled(LogLevel.Debug, "Found");
        VerifyLoggerCalled(LogLevel.Debug, "active subscriptions");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Subscriptions")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetSubscriptionsAsync_NoSubscriptions_ShouldReturnEmpty(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();
        var recipient = "nonexistent@example.com";

        // Act
        var subscriptions = await _service.GetSubscriptionsAsync(recipient, blockchainType);

        // Assert
        subscriptions.Should().NotBeNull();
        subscriptions.Should().BeEmpty();
    }

    #endregion

    #region Channel Management Tests

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Channels")]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetAvailableChannelsAsync_ShouldReturnEnabledChannels(BlockchainType blockchainType)
    {
        // Arrange
        await InitializeServiceAsync();

        // Act
        var result = await _service.GetAvailableChannelsAsync(blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Channels.Should().NotBeNull();
        result.Channels.Should().HaveCount(2); // Email and Webhook from default config
        result.Channels.Should().AllSatisfy(c => c.IsEnabled.Should().BeTrue());
        result.Metadata.Should().ContainKey("total_channels");
        result.Metadata.Should().ContainKey("blockchain_type");
        VerifyLoggerCalled(LogLevel.Information, "Retrieved");
        VerifyLoggerCalled(LogLevel.Information, "available notification channels");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Channels")]
    public async Task GetAvailableChannelsAsync_UnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await InitializeServiceAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.GetAvailableChannelsAsync((BlockchainType)999));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task SubscribeAsync_UnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await InitializeServiceAsync();
        var request = new SubscribeRequest
        {
            Recipient = "user@example.com",
            Channel = NotificationChannel.Email,
            EventTypes = new[] { "transaction" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.SubscribeAsync(request, (BlockchainType)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task SubscribeAsync_ServiceNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new NotificationService(_optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object, null);
        var request = new SubscribeRequest
        {
            Recipient = "user@example.com",
            Channel = NotificationChannel.Email,
            EventTypes = new[] { "transaction" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubscribeAsync(request, BlockchainType.NeoN3));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task UnsubscribeAsync_UnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await InitializeServiceAsync();

        // Act & Assert
        var unsubscribeRequest = new UnsubscribeRequest { SubscriptionId = "test-id" };
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.UnsubscribeAsync(unsubscribeRequest, (BlockchainType)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task GetSubscriptionsAsync_UnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        await InitializeServiceAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.GetSubscriptionsAsync("user@example.com", (BlockchainType)999));
    }

    #endregion

    #region Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "NotificationSending")]
    public async Task SendNotificationAsync_HighVolumeOperations_PerformsEfficiently()
    {
        // Arrange
        await InitializeServiceAsync();
        const int notificationCount = 50;
        var tasks = new List<Task<Services.Notification.Models.NotificationResult>>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < notificationCount; i++)
        {
            var request = new SendNotificationRequest
            {
                Recipient = $"user{i}@example.com",
                Subject = $"Test Email {i}",
                Message = $"This is test email notification {i}",
                Channel = NotificationChannel.Email,
                Priority = Services.Notification.Models.NotificationPriority.Normal
            };
            tasks.Add(_service.SendNotificationAsync(request, BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(notificationCount);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "Subscriptions")]
    public async Task SubscribeAsync_HighVolumeOperations_PerformsEfficiently()
    {
        // Arrange
        await InitializeServiceAsync();
        const int subscriptionCount = 100;
        var tasks = new List<Task<Models.SubscriptionResult>>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < subscriptionCount; i++)
        {
            var request = new SubscribeRequest
            {
                Recipient = $"user{i}@example.com",
                Channel = NotificationChannel.Email,
                EventTypes = new[] { "transaction" }
            };
            tasks.Add(_service.SubscribeAsync(request, BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(subscriptionCount);
        results.Should().AllSatisfy(result => 
        {
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.SubscriptionId.Should().NotBeNullOrEmpty();
        });
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    #endregion

    #region Service Properties Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceProperties")]
    public void Service_ShouldSupportCorrectBlockchainTypes()
    {
        // Act & Assert
        _service.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
        _service.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
        _service.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceProperties")]
    public void Service_ShouldHaveCorrectCapabilities()
    {
        // Act & Assert
        _service.Capabilities.Should().Contain(typeof(INotificationService));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceProperties")]
    public void Service_ShouldHaveCorrectProperties()
    {
        // Act & Assert
        _service.Name.Should().Be("Notification");
        _service.Description.Should().Be("Secure Notification Service");
        _service.Version.Should().Be("1.0.0");
        _service.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        _service.SupportedBlockchains.Should().Contain(BlockchainType.NeoX);
    }

    #endregion

    #region Helper Methods

    private async Task InitializeServiceAsync()
    {
        await _service.InitializeAsync();
        await _service.StartAsync();
    }

    private void SetupConfiguration()
    {
        var options = new NotificationOptions
        {
            EnabledChannels = new[] { "Email", "Webhook" },
            RetryAttempts = 3,
            BatchSize = 100
        };
        _optionsMock.Setup(x => x.Value).Returns(options);
    }

    private void SetupSmsConfiguration()
    {
        var options = new NotificationOptions
        {
            EnabledChannels = new[] { "Email", "Webhook", "SMS" },
            RetryAttempts = 3,
            BatchSize = 100
        };
        _optionsMock.Setup(x => x.Value).Returns(options);
    }

    private void SetupHttpClient()
    {
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Setup successful HTTP response for webhook notifications
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _loggerMock.Verify(
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
        _service?.Dispose();
    }

    #endregion
}
