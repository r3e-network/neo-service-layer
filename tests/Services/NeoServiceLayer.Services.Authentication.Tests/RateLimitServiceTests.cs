using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Services.Authentication;
using Xunit;

namespace NeoServiceLayer.Services.Authentication.Tests
{
    public class RateLimitServiceTests
    {
        private readonly Mock<ILogger<RateLimitService>> _loggerMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly IConfiguration _configuration;
        private readonly RateLimitService _rateLimitService;

        public RateLimitServiceTests()
        {
            _loggerMock = new Mock<ILogger<RateLimitService>>();
            _cacheMock = new Mock<IDistributedCache>();

            var configData = new Dictionary<string, string>
            {
                ["RateLimit:DefaultRequestsPerMinute"] = "60",
                ["RateLimit:DefaultRequestsPerHour"] = "1000",
                ["RateLimit:DefaultBurstSize"] = "10"
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _rateLimitService = new RateLimitService(
                _loggerMock.Object,
                _cacheMock.Object,
                _configuration);
        }

        [Fact]
        public async Task CheckRateLimitAsync_UnderLimit_ShouldAllowRequest()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/test";

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.True(result.IsAllowed);
            Assert.True(result.Remaining > 0);
            Assert.Equal(0, result.RetryAfter);
        }

        [Fact]
        public async Task CheckRateLimitAsync_ExceedsMinuteLimit_ShouldBlockRequest()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/authentication/login"; // Has custom limits
            var now = DateTimeOffset.UtcNow;
            var minuteKey = $"rate:{identifier}:{resource}:minute:{now.Minute}";

            // Mock cache to return count at limit (5 for login endpoint)
            _cacheMock.Setup(c => c.GetAsync(minuteKey, default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("5"));

            _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(":hour:")), default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("10"));

            _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(":burst")), default))
                .ReturnsAsync((byte[])null);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.False(result.IsAllowed);
            Assert.Equal(0, result.Remaining);
            Assert.True(result.RetryAfter > 0);
            Assert.True(result.ResetAt > now);
        }

        [Fact]
        public async Task CheckRateLimitAsync_ExceedsHourLimit_ShouldBlockRequest()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/authentication/register";
            var now = DateTimeOffset.UtcNow;
            var hourKey = $"rate:{identifier}:{resource}:hour:{now.Hour}";

            // Mock cache to return counts
            _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(":minute:")), default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("1"));

            // At hour limit (10 for register endpoint)
            _cacheMock.Setup(c => c.GetAsync(hourKey, default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("10"));

            _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(":burst")), default))
                .ReturnsAsync((byte[])null);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.False(result.IsAllowed);
            Assert.Equal(0, result.Remaining);
            Assert.True(result.RetryAfter > 0);
        }

        [Fact]
        public async Task CheckRateLimitAsync_ExceedsBurstLimit_ShouldBlockRequest()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/authentication/password/reset";
            var burstKey = $"rate:{identifier}:{resource}:burst";

            // Mock cache to return counts
            _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(":minute:")), default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("0"));

            _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(":hour:")), default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("0"));

            // At burst limit (1 for password reset endpoint)
            _cacheMock.Setup(c => c.GetAsync(burstKey, default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("1"));

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.False(result.IsAllowed);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(1, result.RetryAfter); // Burst window is 1 second
        }

        [Fact]
        public async Task RecordRequestAsync_ShouldIncrementAllCounters()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/test";
            var now = DateTimeOffset.UtcNow;

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            await _rateLimitService.RecordRequestAsync(identifier, resource);

            // Assert
            // Verify that all three counters are incremented
            _cacheMock.Verify(c => c.SetAsync(
                It.Is<string>(s => s.Contains(":minute:")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);

            _cacheMock.Verify(c => c.SetAsync(
                It.Is<string>(s => s.Contains(":hour:")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);

            _cacheMock.Verify(c => c.SetAsync(
                It.Is<string>(s => s.Contains(":burst")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task ResetLimitAsync_ShouldRemoveAllCounters()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/test";

            _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);

            // Act
            await _rateLimitService.ResetLimitAsync(identifier, resource);

            // Assert
            // Verify that all three counters are removed
            _cacheMock.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains(":minute:")),
                default), Times.Once);

            _cacheMock.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains(":hour:")),
                default), Times.Once);

            _cacheMock.Verify(c => c.RemoveAsync(
                It.Is<string>(s => s.Contains(":burst")),
                default), Times.Once);
        }

        [Fact]
        public async Task CheckRateLimitAsync_LoginEndpoint_ShouldUseCustomLimits()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/authentication/login";

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal(5, result.Limit); // Custom limit for login endpoint
            Assert.Equal(4, result.Remaining); // 5 - 1 = 4
        }

        [Fact]
        public async Task CheckRateLimitAsync_RegisterEndpoint_ShouldUseCustomLimits()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/authentication/register";

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal(2, result.Limit); // Custom limit for register endpoint
            Assert.Equal(1, result.Remaining); // 2 - 1 = 1
        }

        [Fact]
        public async Task CheckRateLimitAsync_PasswordResetEndpoint_ShouldUseCustomLimits()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/authentication/password/reset";

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal(2, result.Limit); // Custom limit for password reset endpoint
            Assert.Equal(1, result.Remaining); // 2 - 1 = 1
        }

        [Fact]
        public async Task CheckRateLimitAsync_UnknownEndpoint_ShouldUseDefaultLimits()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/v1/other/endpoint";

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal(60, result.Limit); // Default limit from configuration
            Assert.Equal(59, result.Remaining); // 60 - 1 = 59
        }

        [Fact]
        public async Task CheckRateLimitAsync_CacheError_ShouldAllowRequest()
        {
            // Arrange
            var identifier = "user-123";
            var resource = "/api/test";

            // Simulate cache error
            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ThrowsAsync(new Exception("Cache connection failed"));

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, resource);

            // Assert
            Assert.True(result.IsAllowed); // Should allow request on cache error to prevent service disruption
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }
    }
}