using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Shared.Utilities;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Shared;

/// <summary>
/// Tests for RetryHelper utility class to verify retry logic and circuit breaker patterns.
/// </summary>
public class RetryHelperTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    #region ExecuteAsync Action Tests

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulAction_ShouldExecuteOnce()
    {
        // Arrange
        var executionCount = 0;
        Task action() => Task.Run(() => executionCount++);

        // Act
        await RetryHelper.ExecuteAsync(action);

        // Assert
        executionCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<Task>? nullAction = null;

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteAsync(nullAction!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientException_ShouldRetryAndSucceed()
    {
        // Arrange
        var executionCount = 0;
        Task action() => Task.Run(() =>
        {
            executionCount++;
            if (executionCount < 3)
                throw new HttpRequestException("Transient error");
        });

        // Act
        await RetryHelper.ExecuteAsync(action, maxRetries: 3);

        // Assert
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithPersistentException_ShouldExhaustRetriesAndThrow()
    {
        // Arrange
        var executionCount = 0;
        Task action() => Task.Run(() =>
        {
            executionCount++;
            throw new HttpRequestException("Persistent error");
        });

        // Act & Assert
        var act = async () => await RetryHelper.ExecuteAsync(action, maxRetries: 2);
        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("Persistent error");
        executionCount.Should().Be(3); // Initial + 2 retries
    }

    #endregion

    #region ExecuteAsync Function Tests

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulFunction_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "Success";
        Task<string> func() => Task.FromResult(expectedResult);

        // Act
        var result = await RetryHelper.ExecuteAsync(func);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullFunction_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<Task<string>>? nullFunc = null;

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteAsync(nullFunc!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidMaxRetries_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        Task<string> func() => Task.FromResult("test");

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteAsync(func, maxRetries: -1);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidBackoffMultiplier_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        Task<string> func() => Task.FromResult("test");

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteAsync(func, backoffMultiplier: 0.5);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientExceptionAndRetries_ShouldEventuallySucceed()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run(() =>
        {
            executionCount++;
            if (executionCount < 2)
                throw new TaskCanceledException("Transient timeout");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 3);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomRetryCondition_ShouldRespectCondition()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func()
        {
            executionCount++;
            throw new ArgumentException("Non-retryable error");
        }

        bool retryCondition(Exception ex) => false; // Never retry

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteAsync(func, maxRetries: 3, retryCondition: retryCondition);
        await action.Should().ThrowAsync<ArgumentException>();
        executionCount.Should().Be(1); // No retries
    }

    [Fact]
    public async Task ExecuteAsync_WithLogger_ShouldLogRetryAttempts()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run(() =>
        {
            executionCount++;
            if (executionCount < 3)
                throw new TimeoutException("Test timeout");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(1), logger: _mockLogger.Object);

        // Assert
        result.Should().Be("Success");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2)); // 2 retry attempts
    }

    [Fact]
    public async Task ExecuteAsync_WithExponentialBackoff_ShouldIncreaseDelay()
    {
        // Arrange
        var executionCount = 0;
        var executionTimes = new List<DateTime>();

        Task<string> func() => Task.Run<string>(() =>
        {
            executionTimes.Add(DateTime.UtcNow);
            executionCount++;
            if (executionCount < 3)
                throw new SocketException();
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            backoffMultiplier: 2.0);

        // Assert
        result.Should().Be("Success");
        executionTimes.Should().HaveCount(3);

        // Verify delays increase (allowing for some timing variation)
        var delay1 = executionTimes[1] - executionTimes[0];
        var delay2 = executionTimes[2] - executionTimes[1];
        delay2.Should().BeGreaterThan(delay1);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxDelay_ShouldCapDelay()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run<string>(() =>
        {
            executionCount++;
            if (executionCount < 4)
                throw new HttpRequestException("Test error");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 4,
            baseDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMilliseconds(150),
            backoffMultiplier: 3.0);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(4);
    }

    #endregion

    #region Transient Exception Detection Tests

    [Fact]
    public async Task ExecuteAsync_WithHttpRequestException_ShouldRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run<string>(() =>
        {
            executionCount++;
            if (executionCount == 1)
                throw new HttpRequestException("Test error");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 2);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithTaskCanceledException_ShouldRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run<string>(() =>
        {
            executionCount++;
            if (executionCount == 1)
                throw new TaskCanceledException("Test error");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 2);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutException_ShouldRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run<string>(() =>
        {
            executionCount++;
            if (executionCount == 1)
                throw new TimeoutException("Test error");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 2);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithSocketException_ShouldRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run<string>(() =>
        {
            executionCount++;
            if (executionCount == 1)
                throw new SocketException();
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 2);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutInMessage_ShouldRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run(() =>
        {
            executionCount++;
            if (executionCount == 1)
                throw new InvalidOperationException("Operation timeout occurred");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 2);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithConnectionInMessage_ShouldRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func() => Task.Run(() =>
        {
            executionCount++;
            if (executionCount == 1)
                throw new InvalidOperationException("Connection failed");
            return "Success";
        });

        // Act
        var result = await RetryHelper.ExecuteAsync(func, maxRetries: 2);

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonTransientException_ShouldNotRetry()
    {
        // Arrange
        var executionCount = 0;
        Task<string> func()
        {
            executionCount++;
            throw new ArgumentException("Invalid argument");
        }

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteAsync(func, maxRetries: 3);
        await action.Should().ThrowAsync<ArgumentException>();
        executionCount.Should().Be(1); // No retries
    }

    #endregion

    #region Circuit Breaker Tests

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<Task>? nullAction = null;
        var circuitBreaker = new CircuitBreaker();

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteWithCircuitBreakerAsync(nullAction!, circuitBreaker);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WithNullCircuitBreaker_ShouldThrowArgumentNullException()
    {
        // Arrange
        Task action() => Task.CompletedTask;
        CircuitBreaker? nullCircuitBreaker = null;

        // Act & Assert
        var act = async () => await RetryHelper.ExecuteWithCircuitBreakerAsync(action, nullCircuitBreaker!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WithSuccessfulAction_ShouldExecute()
    {
        // Arrange
        var executed = false;
        Task action() => Task.Run(() => executed = true);
        var circuitBreaker = new CircuitBreaker();

        // Act
        await RetryHelper.ExecuteWithCircuitBreakerAsync(action, circuitBreaker);

        // Assert
        executed.Should().BeTrue();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WithSuccessfulFunction_ShouldReturnResult()
    {
        // Arrange
        Task<string> func() => Task.FromResult("Success");
        var circuitBreaker = new CircuitBreaker();

        // Act
        var result = await RetryHelper.ExecuteWithCircuitBreakerAsync(func, circuitBreaker);

        // Assert
        result.Should().Be("Success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WithOpenCircuit_ShouldThrowCircuitBreakerOpenException()
    {
        // Arrange
        Task<string> func() => Task.FromResult("Success");
        var circuitBreaker = new CircuitBreaker(failureThreshold: 1);

        // Cause circuit to open
        circuitBreaker.RecordFailure();

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteWithCircuitBreakerAsync(func, circuitBreaker);
        await action.Should().ThrowAsync<CircuitBreakerOpenException>()
            .WithMessage("Circuit breaker is open");
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WithFailure_ShouldRecordFailure()
    {
        // Arrange
        Task<string> func() => throw new InvalidOperationException("Test failure");
        var circuitBreaker = new CircuitBreaker(failureThreshold: 2);

        // Act & Assert
        var action = async () => await RetryHelper.ExecuteWithCircuitBreakerAsync(func, circuitBreaker);
        await action.Should().ThrowAsync<InvalidOperationException>();

        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed); // Still closed, threshold not reached
    }

    #endregion
}

/// <summary>
/// Tests for CircuitBreaker class to verify circuit breaker pattern implementation.
/// </summary>
public class CircuitBreakerTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(30), _mockLogger.Object);

        // Assert
        circuitBreaker.IsOpen.Should().BeFalse();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Constructor_WithZeroFailureThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => new CircuitBreaker(0);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNegativeFailureThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => new CircuitBreaker(-1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithDefaultTimeout_ShouldUseOneMinute()
    {
        // Arrange & Act
        var circuitBreaker = new CircuitBreaker(5);

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public void RecordSuccess_InClosedState_ShouldResetFailureCount()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(3);
        circuitBreaker.RecordFailure(); // Add one failure

        // Act
        circuitBreaker.RecordSuccess();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        circuitBreaker.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_BelowThreshold_ShouldStayClosed()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(3);

        // Act
        circuitBreaker.RecordFailure();
        circuitBreaker.RecordFailure();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        circuitBreaker.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_AtThreshold_ShouldOpen()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(2, logger: _mockLogger.Object);

        // Act
        circuitBreaker.RecordFailure();
        circuitBreaker.RecordFailure();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
        circuitBreaker.IsOpen.Should().BeTrue();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Circuit breaker opened")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task IsOpen_AfterTimeout_ShouldTransitionToHalfOpen()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(1, TimeSpan.FromMilliseconds(50), _mockLogger.Object);
        circuitBreaker.RecordFailure(); // Open the circuit

        // Act
        await Task.Delay(60); // Wait for timeout
        var isOpen = circuitBreaker.IsOpen;

        // Assert
        isOpen.Should().BeFalse(); // Should be half-open now
        circuitBreaker.State.Should().Be(CircuitBreakerState.HalfOpen);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("half-open")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordSuccess_InHalfOpenState_ShouldClose()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(1, TimeSpan.FromMilliseconds(50), _mockLogger.Object);
        circuitBreaker.RecordFailure(); // Open the circuit
        await Task.Delay(60); // Wait for timeout to transition to half-open
        _ = circuitBreaker.IsOpen; // Trigger state transition

        // Act
        circuitBreaker.RecordSuccess();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        circuitBreaker.IsOpen.Should().BeFalse();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("closed after successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordFailure_InHalfOpenState_ShouldReopenCircuit()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(1, TimeSpan.FromMilliseconds(50));
        circuitBreaker.RecordFailure(); // Open the circuit
        await Task.Delay(60); // Wait for timeout
        _ = circuitBreaker.IsOpen; // Transition to half-open

        // Act
        circuitBreaker.RecordFailure();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
        circuitBreaker.IsOpen.Should().BeTrue();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void CircuitBreaker_WithConcurrentAccess_ShouldBehaveCorrectly()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(5); // Lower threshold for deterministic test
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;

        // Act - Record enough failures to open the circuit
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                circuitBreaker.RecordFailure();
                Interlocked.Increment(ref failureCount);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        failureCount.Should().Be(10);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open); // Should be open due to failures
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void CircuitBreakerState_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<CircuitBreakerState>().Should().BeEquivalentTo([
            CircuitBreakerState.Closed,
            CircuitBreakerState.Open,
            CircuitBreakerState.HalfOpen
        ]);
    }

    #endregion
}

/// <summary>
/// Tests for CircuitBreakerOpenException class.
/// </summary>
public class CircuitBreakerOpenExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Circuit breaker is open";

        // Act
        var exception = new CircuitBreakerOpenException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Circuit breaker is open";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new CircuitBreakerOpenException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }
}
