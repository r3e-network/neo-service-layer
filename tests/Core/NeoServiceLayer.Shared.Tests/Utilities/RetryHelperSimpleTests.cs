using FluentAssertions;
using NeoServiceLayer.Shared.Utilities;
using Xunit;
using System.Threading.Tasks;
using System;

namespace NeoServiceLayer.Shared.Tests.Utilities
{
    /// <summary>
    /// Simple unit tests for RetryHelper utility class.
    /// Tests basic retry scenarios with the actual method signatures.
    /// </summary>
    public class RetryHelperSimpleTests
    {
        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult()
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
        public async Task ExecuteAsync_WithFailingOperation_ShouldRetryAndEventuallySucceed()
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
        public async Task ExecuteAsync_WithAlwaysFailingOperation_ShouldThrowAfterMaxRetries()
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
    }
}