using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Services.EventSubscription.Tests;

public class EventSubscriptionServiceTests
{
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly Mock<ILogger<EventSubscriptionService>> _loggerMock;
    private readonly EventSubscriptionService _service;

    public EventSubscriptionServiceTests()
    {
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _loggerMock = new Mock<ILogger<EventSubscriptionService>>();

        _configurationMock
            .Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string defaultValue) => defaultValue);

        _enclaveManagerMock
            .Setup(e => e.InitializeAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _enclaveManagerMock
            .Setup(e => e.InitializeEnclaveAsync())
            .ReturnsAsync(true);

        var subscriptionStore = new Dictionary<string, EventSubscription>();

        _enclaveManagerMock
            .Setup(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string script, CancellationToken token) =>
            {
                if (script.Contains("createSubscription"))
                {
                    // Generate a unique subscription ID
                    var subscriptionId = $"subscription-{Guid.NewGuid():N}";

                    // Extract subscription data from the script to preserve original data
                    try
                    {
                        var startIndex = script.IndexOf("createSubscription(") + "createSubscription(".Length;
                        var endIndex = script.LastIndexOf(", '");
                        var subscriptionJson = script.Substring(startIndex, endIndex - startIndex);
                        var originalSubscription = JsonSerializer.Deserialize<EventSubscription>(subscriptionJson);

                        // Update the subscription ID in the original subscription
                        if (originalSubscription != null)
                        {
                            originalSubscription.SubscriptionId = subscriptionId;
                            originalSubscription.CreatedAt = DateTime.UtcNow;
                            originalSubscription.LastModifiedAt = DateTime.UtcNow;
                            subscriptionStore[subscriptionId] = originalSubscription;
                        }
                    }
                    catch
                    {
                        // Fallback to default subscription if parsing fails
                        var subscription = new EventSubscription
                        {
                            SubscriptionId = subscriptionId,
                            Name = "Test Subscription",
                            Description = "Test subscription description",
                            EventType = "Block",
                            EventFilter = "",
                            CallbackUrl = "https://example.com/callback",
                            CallbackAuthHeader = "Bearer token123",
                            Enabled = true,
                            CreatedAt = DateTime.UtcNow,
                            LastModifiedAt = DateTime.UtcNow,
                            RetryPolicy = new RetryPolicy
                            {
                                MaxRetries = 3,
                                InitialRetryDelaySeconds = 5,
                                RetryBackoffFactor = 2.0,
                                MaxRetryDelaySeconds = 60
                            }
                        };
                        subscriptionStore[subscriptionId] = subscription;
                    }

                    return JsonSerializer.Serialize(subscriptionId);
                }
                else if (script.Contains("getSubscription"))
                {
                    // Extract subscription ID from script
                    var startIndex = script.IndexOf("('") + 2;
                    var endIndex = script.IndexOf("',", startIndex);
                    if (endIndex == -1) endIndex = script.IndexOf("')", startIndex);
                    var subscriptionId = script.Substring(startIndex, endIndex - startIndex);

                    if (subscriptionStore.TryGetValue(subscriptionId, out var subscription))
                    {
                        return JsonSerializer.Serialize(subscription);
                    }

                    // Return default subscription for "subscription-1" for compatibility
                    return JsonSerializer.Serialize(new EventSubscription
                    {
                        SubscriptionId = subscriptionId,
                        Name = "Test Subscription",
                        Description = "Test subscription description",
                        EventType = "Block",
                        EventFilter = "",
                        CallbackUrl = "https://example.com/callback",
                        CallbackAuthHeader = "Bearer token123",
                        Enabled = true,
                        CreatedAt = DateTime.UtcNow,
                        LastModifiedAt = DateTime.UtcNow,
                        RetryPolicy = new RetryPolicy
                        {
                            MaxRetries = 3,
                            InitialRetryDelaySeconds = 5,
                            RetryBackoffFactor = 2.0,
                            MaxRetryDelaySeconds = 60
                        }
                    });
                }
                else if (script.Contains("updateSubscription"))
                {
                    return "true";
                }
                else if (script.Contains("deleteSubscription"))
                {
                    return "true";
                }
                else if (script.Contains("listSubscriptions"))
                {
                    var subscriptions = subscriptionStore.Values.ToList();
                    if (subscriptions.Count == 0)
                    {
                        // Return a default subscription for compatibility
                        subscriptions.Add(new EventSubscription
                        {
                            SubscriptionId = "subscription-1",
                            Name = "Test Subscription",
                            Description = "Test subscription description",
                            EventType = "Block",
                            EventFilter = "",
                            CallbackUrl = "https://example.com/callback",
                            CallbackAuthHeader = "Bearer token123",
                            Enabled = true,
                            CreatedAt = DateTime.UtcNow,
                            LastModifiedAt = DateTime.UtcNow,
                            RetryPolicy = new RetryPolicy
                            {
                                MaxRetries = 3,
                                InitialRetryDelaySeconds = 5,
                                RetryBackoffFactor = 2.0,
                                MaxRetryDelaySeconds = 60
                            }
                        });
                    }
                    return JsonSerializer.Serialize(subscriptions);
                }
                else if (script.Contains("getEvents"))
                {
                    return JsonSerializer.Serialize(new List<EventData>
                    {
                        new EventData
                        {
                            EventId = "event-1",
                            SubscriptionId = "subscription-1",
                            EventType = "Block",
                            Data = "Block data",
                            Timestamp = DateTime.UtcNow,
                            Acknowledged = false,
                            DeliveryAttempts = 0,
                            DeliveryStatus = "Pending"
                        }
                    });
                }
                else if (script.Contains("acknowledgeEvent"))
                {
                    return "true";
                }
                else if (script.Contains("triggerTestEvent"))
                {
                    return JsonSerializer.Serialize("event-2");
                }
                else if (script.Contains("startEventProcessor"))
                {
                    return "true";
                }

                return string.Empty;
            });

        _service = new EventSubscriptionService(_enclaveManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeEnclave()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        _enclaveManagerMock.Verify(e => e.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(_service.IsEnclaveInitialized);
    }

    [Fact]
    public async Task StartAsync_ShouldStartService()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAsync_ShouldStopService()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ShouldCreateSubscription()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var subscription = new EventSubscription
        {
            Name = "Test Subscription",
            Description = "Test subscription description",
            EventType = "Block",
            EventFilter = "",
            CallbackUrl = "https://example.com/callback",
            CallbackAuthHeader = "Bearer token123",
            Enabled = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                InitialRetryDelaySeconds = 5,
                RetryBackoffFactor = 2.0,
                MaxRetryDelaySeconds = 60
            }
        };

        // Act
        var result = await _service.CreateSubscriptionAsync(
            subscription,
            BlockchainType.NeoN3);

        // Assert
        Assert.NotEmpty(result);
        Assert.StartsWith("subscription-", result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldGetSubscription()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetSubscriptionAsync(
            "subscription-1",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("subscription-1", result.SubscriptionId);
        Assert.Equal("Test Subscription", result.Name);
        Assert.Equal("Test subscription description", result.Description);
        Assert.Equal("Block", result.EventType);
        Assert.Equal("", result.EventFilter);
        Assert.Equal("https://example.com/callback", result.CallbackUrl);
        Assert.Equal("Bearer token123", result.CallbackAuthHeader);
        Assert.True(result.Enabled);
        Assert.Equal(3, result.RetryPolicy.MaxRetries);
        Assert.Equal(5, result.RetryPolicy.InitialRetryDelaySeconds);
        Assert.Equal(2.0, result.RetryPolicy.RetryBackoffFactor);
        Assert.Equal(60, result.RetryPolicy.MaxRetryDelaySeconds);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_ShouldUpdateSubscription()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // First create a subscription
        var createSubscription = new EventSubscription
        {
            Name = "Test Subscription",
            Description = "Test subscription description",
            EventType = "Block",
            EventFilter = "",
            CallbackUrl = "https://example.com/callback",
            CallbackAuthHeader = "Bearer token123",
            Enabled = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                InitialRetryDelaySeconds = 5,
                RetryBackoffFactor = 2.0,
                MaxRetryDelaySeconds = 60
            }
        };
        var subscriptionId = await _service.CreateSubscriptionAsync(createSubscription, BlockchainType.NeoN3);

        // Now prepare the update
        var subscription = new EventSubscription
        {
            SubscriptionId = subscriptionId,
            Name = "Updated Subscription",
            Description = "Updated subscription description",
            EventType = "Block",
            EventFilter = "",
            CallbackUrl = "https://example.com/callback",
            CallbackAuthHeader = "Bearer token123",
            Enabled = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 5,
                InitialRetryDelaySeconds = 10,
                RetryBackoffFactor = 2.0,
                MaxRetryDelaySeconds = 120
            }
        };

        // Act
        var result = await _service.UpdateSubscriptionAsync(
            subscription,
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_ShouldDeleteSubscription()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // First create a subscription to delete
        var createSubscription = new EventSubscription
        {
            Name = "Test Subscription",
            Description = "Test subscription description",
            EventType = "Block",
            EventFilter = "",
            CallbackUrl = "https://example.com/callback",
            CallbackAuthHeader = "Bearer token123",
            Enabled = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                InitialRetryDelaySeconds = 5,
                RetryBackoffFactor = 2.0,
                MaxRetryDelaySeconds = 60
            }
        };
        var subscriptionId = await _service.CreateSubscriptionAsync(createSubscription, BlockchainType.NeoN3);

        // Act
        var result = await _service.DeleteSubscriptionAsync(
            subscriptionId,
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ListSubscriptionsAsync_ShouldListSubscriptions()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.ListSubscriptionsAsync(
            0,
            10,
            BlockchainType.NeoN3);

        // Assert
        Assert.Single(result);
        Assert.Equal("subscription-1", result.First().SubscriptionId);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ListEventsAsync_ShouldGetEvents()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // First create a subscription
        var createSubscription = new EventSubscription
        {
            Name = "Test Subscription",
            Description = "Test subscription description",
            EventType = "Block",
            EventFilter = "",
            CallbackUrl = "https://example.com/callback",
            CallbackAuthHeader = "Bearer token123",
            Enabled = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                InitialRetryDelaySeconds = 5,
                RetryBackoffFactor = 2.0,
                MaxRetryDelaySeconds = 60
            }
        };
        var subscriptionId = await _service.CreateSubscriptionAsync(createSubscription, BlockchainType.NeoN3);

        // Act
        var result = await _service.ListEventsAsync(
            subscriptionId,
            0,
            10,
            BlockchainType.NeoN3);

        // Assert
        Assert.Single(result);
        Assert.Equal("event-1", result.First().EventId);
        Assert.Equal("subscription-1", result.First().SubscriptionId);
        Assert.Equal("Block", result.First().EventType);
        Assert.Equal("Block data", result.First().Data);
        Assert.False(result.First().Acknowledged);
        Assert.Equal(0, result.First().DeliveryAttempts);
        Assert.Equal("Pending", result.First().DeliveryStatus);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AcknowledgeEventAsync_ShouldAcknowledgeEvent()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.AcknowledgeEventAsync(
            "subscription-1",
            "event-1",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task TriggerTestEventAsync_ShouldTriggerTestEvent()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var eventData = new EventData
        {
            EventType = "Test",
            Data = "Test event data"
        };

        // Act
        var result = await _service.TriggerTestEventAsync(
            "subscription-1",
            eventData,
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("event-2", result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
