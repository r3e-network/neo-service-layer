using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Shared.Tests.Utilities
{
    /// <summary>
    /// Comprehensive unit tests for RetryHelper utility class.
    /// Tests all retry scenarios, configurations, and exception handling.
    /// </summary>
    public class RetryHelperComprehensiveTests
    {
        [Fact]
        public async Task ExecuteWithRetryAsync_WithSuccessfulOperation_ShouldReturnResult()
        {
            // Arrange
            var expectedResult = "success";
            var operation = () => Task.FromResult(expectedResult);

            // Act
            var result = await RetryHelper.ExecuteAsync(operation);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithFailingOperation_ShouldRetryAndEventuallySucceed()
        {
            // Arrange
            int attemptCount = 0;
            var expectedResult = "success";
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException("Temporary failure");
                return Task.FromResult(expectedResult);
            };

            // Act
            var result = await RetryHelper.ExecuteAsync(operation, maxRetries: 3, retryCondition: ex => ex is InvalidOperationException);

            // Assert
            result.Should().Be(expectedResult);
            attemptCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithAlwaysFailingOperation_ShouldThrowAfterMaxRetries()
        {
            // Arrange
            int attemptCount = 0;
            var operation = async () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Always fails");
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => RetryHelper.ExecuteAsync(operation, maxRetries: 3, retryCondition: ex => ex is InvalidOperationException));
            exception.Message.Should().Be("Always fails");
            attemptCount.Should().Be(4); // Original attempt + 3 retries
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithCustomRetryDelay_ShouldRespectDelay()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException("Temporary failure");
                return Task.FromResult("success");
            };
            var retryDelay = TimeSpan.FromMilliseconds(100);

            // Act
            await RetryHelper.ExecuteAsync(operation, maxRetries: 3, baseDelay: retryDelay);
            stopwatch.Stop();

            // Assert
            attemptCount.Should().Be(3);
            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(150); // At least 2 delays
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithZeroMaxRetries_ShouldNotRetry()
        {
            // Arrange
            int attemptCount = 0;
            var operation = async () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Always fails");
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => RetryHelper.ExecuteAsync(operation, maxRetries: 0, retryCondition: ex => ex is InvalidOperationException));
            attemptCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithExceptionFilter_ShouldOnlyRetrySpecificExceptions()
        {
            // Arrange
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount == 1)
                    throw new InvalidOperationException("Retryable");
                if (attemptCount == 2)
                    throw new ArgumentException("Non-retryable");
                return Task.FromResult("success");
            };

            bool exceptionFilter(Exception ex) => ex is InvalidOperationException;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => RetryHelper.ExecuteAsync(operation, maxRetries: 3, retryCondition: exceptionFilter));
            attemptCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithExponentialBackoff_ShouldIncreaseDelayExponentially()
        {
            // Arrange
            var delays = new List<TimeSpan>();
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 4)
                    throw new InvalidOperationException("Temporary failure");
                return Task.FromResult("success");
            };

            var onRetry = (Exception ex, int retryCount, TimeSpan delay) =>
            {
                delays.Add(delay);
            };

            // Act
            await RetryHelper.ExecuteAsync(
                operation,
                maxRetries: 3,
                baseDelay: TimeSpan.FromMilliseconds(100),
                retryCondition: ex => ex is InvalidOperationException,
                onRetry: onRetry);

            // Assert
            attemptCount.Should().Be(4);
            delays.Should().HaveCount(3);
            delays[1].Should().BeGreaterThan(delays[0]);
            delays[2].Should().BeGreaterThan(delays[1]);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithOnRetryCallback_ShouldCallCallbackOnRetry()
        {
            // Arrange
            var retryCallbacks = new List<(Exception exception, int retryCount, TimeSpan delay)>();
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException($"Attempt {attemptCount}");
                return Task.FromResult("success");
            };

            var onRetry = (Exception ex, int retryCount, TimeSpan delay) =>
            {
                retryCallbacks.Add((ex, retryCount, delay));
            };

            // Act
            var result = await RetryHelper.ExecuteAsync(operation, maxRetries: 3, onRetry: onRetry);

            // Assert
            result.Should().Be("success");
            retryCallbacks.Should().HaveCount(2);
            retryCallbacks[0].retryCount.Should().Be(1);
            retryCallbacks[1].retryCount.Should().Be(2);
            attemptCount.Should().Be(3); // Initial + 2 retries
            retryCallbacks[0].exception.Message.Should().Be("Attempt 1");
            retryCallbacks[1].exception.Message.Should().Be("Attempt 2");
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithMaxDelayLimit_ShouldNotExceedMaxDelay()
        {
            // Arrange
            var delays = new List<TimeSpan>();
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 5)
                    throw new InvalidOperationException("Temporary failure");
                return Task.FromResult("success");
            };

            var onRetry = (Exception ex, int retryCount, TimeSpan delay) =>
            {
                delays.Add(delay);
            };

            var maxDelay = TimeSpan.FromMilliseconds(200);

            // Act
            await RetryHelper.ExecuteAsync(
                operation,
                maxRetries: 4,
                baseDelay: TimeSpan.FromMilliseconds(50),
                maxDelay: maxDelay,
                onRetry: onRetry);

            // Assert
            delays.Should().OnlyContain(delay => delay <= maxDelay);
        }

        [Fact]
        public void ExecuteWithRetry_Synchronous_WithSuccessfulOperation_ShouldReturnResult()
        {
            // Arrange
            var expectedResult = "success";
            var operation = () => expectedResult;

            // Act
            var result = RetryHelper.ExecuteWithRetry(operation);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void ExecuteWithRetry_Synchronous_WithFailingOperation_ShouldRetryAndEventuallySucceed()
        {
            // Arrange
            int attemptCount = 0;
            var expectedResult = "success";
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException("Temporary failure");
                return expectedResult;
            };

            // Act
            var result = RetryHelper.ExecuteWithRetry(operation, maxRetries: 3, retryCondition: ex => ex is InvalidOperationException);

            // Assert
            result.Should().Be(expectedResult);
            attemptCount.Should().Be(3);
        }

        [Fact]
        public void ExecuteWithRetry_Synchronous_WithAlwaysFailingOperation_ShouldThrowAfterMaxRetries()
        {
            // Arrange
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Always fails");
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => RetryHelper.ExecuteWithRetry(operation, maxRetries: 3, retryCondition: ex => ex is InvalidOperationException));
            exception.Message.Should().Be("Always fails");
            attemptCount.Should().Be(4); // Original attempt + 3 retries
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ExecuteWithRetryAsync_WithDifferentMaxRetries_ShouldRespectMaxRetries(int maxRetries)
        {
            // Arrange
            int attemptCount = 0;
            var operation = async () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Always fails");
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => RetryHelper.ExecuteAsync(operation, maxRetries: maxRetries, retryCondition: ex => ex is InvalidOperationException));
            attemptCount.Should().Be(maxRetries + 1); // Original attempt + retries
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithVoidOperation_ShouldRetryAndEventuallySucceed()
        {
            // Arrange
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException("Temporary failure");
                return Task.CompletedTask;
            };

            // Act
            await RetryHelper.ExecuteAsync(operation, maxRetries: 3, retryCondition: ex => ex is InvalidOperationException);

            // Assert
            attemptCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            int attemptCount = 0;
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            var operation = async () =>
            {
                attemptCount++;
                await Task.Delay(1000, cts.Token); // This should be cancelled
                throw new InvalidOperationException("Always fails");
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => RetryHelper.ExecuteAsync(
                    operation,
                    maxRetries: 10,
                    baseDelay: TimeSpan.FromMilliseconds(100)));
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithJitter_ShouldAddRandomnessToDelay()
        {
            // Arrange
            var delays = new List<TimeSpan>();
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                if (attemptCount < 4)
                    throw new InvalidOperationException("Temporary failure");
                return Task.FromResult("success");
            };

            var onRetry = (Exception ex, int retryCount, TimeSpan delay) =>
            {
                delays.Add(delay);
            };

            var baseDelay = TimeSpan.FromMilliseconds(100);

            // Act
            await RetryHelper.ExecuteAsync(
                operation,
                maxRetries: 3,
                baseDelay: baseDelay,
                backoffMultiplier: 1.0, // No exponential backoff to test jitter more clearly
                onRetry: onRetry);

            // Assert
            delays.Should().HaveCount(3);
            // With jitter, delays should vary (not all exactly the same)
            var minDelay = TimeSpan.FromMilliseconds(50);
            var maxDelay = TimeSpan.FromMilliseconds(150);
            delays.Should().OnlyContain(delay => 
                delay >= minDelay && 
                delay <= maxDelay);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithComplexExceptionFilter_ShouldFilterCorrectly()
        {
            // Arrange
            int attemptCount = 0;
            var operation = () =>
            {
                attemptCount++;
                return attemptCount switch
                {
                    1 => throw new HttpRequestException("Network error"),
                    2 => throw new TimeoutException("Request timeout"),
                    3 => throw new ArgumentException("Invalid argument"),
                    _ => Task.FromResult("success")
                };
            };

            bool exceptionFilter(Exception ex) => 
                ex is HttpRequestException or TimeoutException;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => RetryHelper.ExecuteAsync(operation, maxRetries: 5, exceptionFilter: exceptionFilter));
            attemptCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_WithCircuitBreakerPattern_ShouldStopRetryingAfterThreshold()
        {
            // Arrange
            int failureCount = 0;
            int attemptCount = 0;
            var operation = async () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Always fails");
            };

            bool circuitBreakerFilter(Exception ex)
            {
                failureCount++;
                return failureCount <= 3; // Stop retrying after 3 consecutive failures
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => RetryHelper.ExecuteAsync(
                    operation, 
                    maxRetries: 10, 
                    exceptionFilter: circuitBreakerFilter));
            attemptCount.Should().Be(4); // Original + 3 retries before circuit breaker opens
        }
    }
}