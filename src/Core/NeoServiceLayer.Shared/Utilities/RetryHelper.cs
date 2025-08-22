using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;


namespace NeoServiceLayer.Shared.Utilities;

/// <summary>
/// Provides retry functionality with exponential backoff and circuit breaker patterns.
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes an action with retry logic.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay between retries.</param>
    /// <param name="maxDelay">Maximum delay between retries.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff.</param>
    /// <param name="retryCondition">Condition to determine if retry should occur.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="onRetry">Optional callback invoked on each retry.</param>
    /// <param name="retryDelay">Optional specific delay between retries (overrides baseDelay).</param>
    /// <param name="exceptionFilter">Optional filter for exceptions that should trigger retries.</param>
    /// <returns>A task representing the operation.</returns>
    public static async Task ExecuteAsync(
        Func<Task> action,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        Func<Exception, bool>? retryCondition = null,
        ILogger? logger = null,
        Action<Exception, int, TimeSpan>? onRetry = null,
        TimeSpan? retryDelay = null,
        Func<Exception, bool>? exceptionFilter = null)
    {
        Guard.NotNull(action);

        await ExecuteAsync(async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }, maxRetries, retryDelay ?? baseDelay, maxDelay, backoffMultiplier, exceptionFilter ?? retryCondition, logger, onRetry, retryDelay, exceptionFilter).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a function with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay between retries.</param>
    /// <param name="maxDelay">Maximum delay between retries.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff.</param>
    /// <param name="retryCondition">Condition to determine if retry should occur.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="onRetry">Optional callback invoked on each retry.</param>
    /// <param name="retryDelay">Optional specific delay between retries (overrides baseDelay).</param>
    /// <param name="exceptionFilter">Optional filter for exceptions that should trigger retries.</param>
    /// <returns>The result of the function.</returns>
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> func,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        Func<Exception, bool>? retryCondition = null,
        ILogger? logger = null,
        Action<Exception, int, TimeSpan>? onRetry = null,
        TimeSpan? retryDelay = null,
        Func<Exception, bool>? exceptionFilter = null)
    {
        Guard.NotNull(func);
        Guard.GreaterThanOrEqual(maxRetries, 0);
        Guard.GreaterThan(backoffMultiplier, 1.0);

        var effectiveDelay = retryDelay ?? baseDelay ?? TimeSpan.FromSeconds(1);
        maxDelay ??= TimeSpan.FromMinutes(5);
        var effectiveCondition = exceptionFilter ?? retryCondition ?? IsTransientException;

        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (Exception ex) when (effectiveCondition(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt > maxRetries)
                {
                    throw;
                }

                var delay = retryDelay ?? CalculateDelay(attempt, effectiveDelay, maxDelay.Value, backoffMultiplier);

                logger?.LogWarning(ex, "Attempt {Attempt} failed. Retrying in {Delay}ms. Error: {Error}",
                    attempt, delay.TotalMilliseconds, ex.Message);

                onRetry?.Invoke(ex, attempt, delay);

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        // If we get here, all retries have been exhausted
        throw lastException ?? new InvalidOperationException("Retry operation failed without exception.");
    }

    /// <summary>
    /// Executes a synchronous function with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay between retries.</param>
    /// <param name="maxDelay">Maximum delay between retries.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff.</param>
    /// <param name="retryCondition">Condition to determine if retry should occur.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <returns>The result of the function.</returns>
    public static T ExecuteWithRetry<T>(
        Func<T> func,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        Func<Exception, bool>? retryCondition = null,
        ILogger? logger = null)
    {
        Guard.NotNull(func);
        Guard.GreaterThanOrEqual(maxRetries, 0);
        Guard.GreaterThan(backoffMultiplier, 1.0);

        baseDelay ??= TimeSpan.FromSeconds(1);
        maxDelay ??= TimeSpan.FromMinutes(5);
        retryCondition ??= IsTransientException;

        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                return func();
            }
            catch (Exception ex) when (attempt < maxRetries && retryCondition(ex))
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(attempt, baseDelay.Value, maxDelay.Value, backoffMultiplier);

                logger?.LogWarning(ex, "Attempt {Attempt} failed. Retrying in {Delay}ms. Error: {Error}",
                    attempt, delay.TotalMilliseconds, ex.Message);

                Thread.Sleep(delay);
            }
        }

        // If we get here, all retries have been exhausted
        throw lastException ?? new InvalidOperationException("Retry operation failed without exception.");
    }

    /// <summary>
    /// Executes a synchronous action with retry logic.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay between retries.</param>
    /// <param name="maxDelay">Maximum delay between retries.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff.</param>
    /// <param name="retryCondition">Condition to determine if retry should occur.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    public static void ExecuteWithRetry(
        Action action,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        Func<Exception, bool>? retryCondition = null,
        ILogger? logger = null)
    {
        Guard.NotNull(action);

        ExecuteWithRetry(() =>
        {
            action();
            return true;
        }, maxRetries, baseDelay, maxDelay, backoffMultiplier, retryCondition, logger);
    }

    /// <summary>
    /// Executes an action with circuit breaker pattern.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="circuitBreaker">The circuit breaker instance.</param>
    /// <returns>A task representing the operation.</returns>
    public static async Task ExecuteWithCircuitBreakerAsync(
        Func<Task> action,
        CircuitBreaker circuitBreaker)
    {
        Guard.NotNull(action);
        Guard.NotNull(circuitBreaker);

        await ExecuteWithCircuitBreakerAsync(async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }, circuitBreaker).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a function with circuit breaker pattern.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="circuitBreaker">The circuit breaker instance.</param>
    /// <returns>The result of the function.</returns>
    public static async Task<T> ExecuteWithCircuitBreakerAsync<T>(
        Func<Task<T>> func,
        CircuitBreaker circuitBreaker)
    {
        Guard.NotNull(func);
        Guard.NotNull(circuitBreaker);

        if (circuitBreaker.IsOpen)
        {
            throw new CircuitBreakerOpenException("Circuit breaker is open");
        }

        try
        {
            var result = await func();
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Calculates the delay for exponential backoff.
    /// </summary>
    /// <param name="attempt">The current attempt number.</param>
    /// <param name="baseDelay">The base delay.</param>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <param name="backoffMultiplier">The backoff multiplier.</param>
    /// <returns>The calculated delay.</returns>
    private static TimeSpan CalculateDelay(int attempt, TimeSpan baseDelay, TimeSpan maxDelay, double backoffMultiplier)
    {
        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(backoffMultiplier, attempt - 1));
        return delay > maxDelay ? maxDelay : delay;
    }

    /// <summary>
    /// Determines if an exception is transient and should be retried.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is transient; otherwise, false.</returns>
    private static bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            SocketException => true,
            _ when exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}

/// <summary>
/// Implements a circuit breaker pattern for fault tolerance.
/// </summary>
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly ILogger? _logger;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitBreakerState _state;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="failureThreshold">The number of failures before opening the circuit.</param>
    /// <param name="timeout">The timeout before attempting to close the circuit.</param>
    /// <param name="logger">Optional logger for circuit breaker events.</param>
    public CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null, ILogger? logger = null)
    {
        _failureThreshold = Guard.GreaterThan(failureThreshold, 0);
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
        _logger = logger;
        _state = CircuitBreakerState.Closed;
    }

    /// <summary>
    /// Gets a value indicating whether the circuit breaker is open.
    /// </summary>
    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open && DateTime.UtcNow - _lastFailureTime > _timeout)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _logger?.LogInformation("Circuit breaker transitioning to half-open state");
                }

                return _state == CircuitBreakerState.Open;
            }
        }
    }

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
                _logger?.LogInformation("Circuit breaker closed after successful operation");
            }
        }
    }

    /// <summary>
    /// Records a failed operation.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _logger?.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
            }
        }
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }
}

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// The circuit is closed and operations are allowed.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit is open and operations are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit is half-open and testing if operations should resume.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when a circuit breaker is open.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
