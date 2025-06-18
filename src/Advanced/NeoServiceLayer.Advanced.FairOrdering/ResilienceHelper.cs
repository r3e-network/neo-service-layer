using Microsoft.Extensions.Logging;
using System.Net;

namespace NeoServiceLayer.Advanced.FairOrdering;

/// <summary>
/// Helper class providing retry and resilience patterns for the Fair Ordering Service.
/// </summary>
public static class ResilienceHelper
{
    /// <summary>
    /// Executes an operation with retry logic and exponential backoff.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay for exponential backoff.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        ILogger logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        string operationName = "operation")
    {
        var delay = baseDelay ?? TimeSpan.FromMilliseconds(100);
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsRetriableException(ex) && attempt < maxRetries)
            {
                lastException = ex;
                attempt++;
                
                var delayMs = (int)(delay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                var jitteredDelay = TimeSpan.FromMilliseconds(delayMs + Random.Shared.Next(0, delayMs / 4));
                
                logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed for {Operation}. Retrying in {Delay}ms", 
                    attempt, maxRetries, operationName, jitteredDelay.TotalMilliseconds);
                
                await Task.Delay(jitteredDelay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Non-retriable exception in {Operation} on attempt {Attempt}", 
                    operationName, attempt + 1);
                throw;
            }
        }

        logger.LogError(lastException, "All {MaxRetries} retry attempts failed for {Operation}", 
            maxRetries, operationName);
        throw lastException!;
    }

    /// <summary>
    /// Executes an operation with retry logic (void return).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay for exponential backoff.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        ILogger logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        string operationName = "operation")
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, logger, maxRetries, baseDelay, operationName);
    }

    /// <summary>
    /// Executes an operation with circuit breaker pattern.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="circuitBreaker">The circuit breaker instance.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T> ExecuteWithCircuitBreakerAsync<T>(
        Func<Task<T>> operation,
        CircuitBreaker circuitBreaker,
        ILogger logger,
        string operationName = "operation")
    {
        if (circuitBreaker.State == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow < circuitBreaker.NextAttemptTime)
            {
                logger.LogWarning("Circuit breaker is OPEN for {Operation}. Next attempt allowed at {NextAttempt}", 
                    operationName, circuitBreaker.NextAttemptTime);
                throw new InvalidOperationException($"Circuit breaker is open for {operationName}");
            }
            
            // Transition to Half-Open state
            circuitBreaker.State = CircuitBreakerState.HalfOpen;
            logger.LogInformation("Circuit breaker transitioning to HALF-OPEN for {Operation}", operationName);
        }

        try
        {
            var result = await operation();
            
            // Success - reset circuit breaker if it was half-open
            if (circuitBreaker.State == CircuitBreakerState.HalfOpen)
            {
                circuitBreaker.Reset();
                logger.LogInformation("Circuit breaker reset to CLOSED for {Operation}", operationName);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            // Record failure
            circuitBreaker.RecordFailure();
            
            logger.LogError(ex, "Operation {Operation} failed. Circuit breaker state: {State}, Failure count: {FailureCount}", 
                operationName, circuitBreaker.State, circuitBreaker.FailureCount);
            
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with both retry and circuit breaker patterns.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="circuitBreaker">The circuit breaker instance.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="baseDelay">Base delay for exponential backoff.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T> ExecuteWithRetryAndCircuitBreakerAsync<T>(
        Func<Task<T>> operation,
        CircuitBreaker circuitBreaker,
        ILogger logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        string operationName = "operation")
    {
        return await ExecuteWithCircuitBreakerAsync(
            () => ExecuteWithRetryAsync(operation, logger, maxRetries, baseDelay, operationName),
            circuitBreaker,
            logger,
            operationName);
    }

    /// <summary>
    /// Executes an operation with timeout.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<Task<T>> operation,
        TimeSpan timeout,
        ILogger logger,
        string operationName = "operation")
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            var operationTask = operation();
            var delayTask = Task.Delay(timeout, cts.Token);
            
            var completedTask = await Task.WhenAny(operationTask, delayTask);
            
            if (completedTask == operationTask)
            {
                cts.Cancel(); // Cancel the delay task
                return await operationTask;
            }
            else
            {
                logger.LogWarning("Operation {Operation} timed out after {Timeout}ms", 
                    operationName, timeout.TotalMilliseconds);
                throw new TimeoutException($"Operation {operationName} timed out after {timeout.TotalMilliseconds}ms");
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            logger.LogWarning("Operation {Operation} was cancelled due to timeout after {Timeout}ms", 
                operationName, timeout.TotalMilliseconds);
            throw new TimeoutException($"Operation {operationName} timed out");
        }
    }

    /// <summary>
    /// Determines if an exception is retriable.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is retriable.</returns>
    private static bool IsRetriableException(Exception exception)
    {
        return exception switch
        {
            // Network-related exceptions
            HttpRequestException httpEx when IsRetriableHttpStatusCode(httpEx) => true,
            TaskCanceledException => true,
            SocketException => true,
            
            // Temporary failures
            InvalidOperationException ex when ex.Message.Contains("temporary") => true,
            InvalidOperationException ex when ex.Message.Contains("busy") => true,
            InvalidOperationException ex when ex.Message.Contains("unavailable") => true,
            
            // Enclave-related temporary failures
            InvalidOperationException ex when ex.Message.Contains("enclave") && ex.Message.Contains("busy") => true,
            
            // Database/storage temporary failures
            InvalidOperationException ex when ex.Message.Contains("connection") => true,
            InvalidOperationException ex when ex.Message.Contains("timeout") => true,
            
            // Explicitly non-retriable exceptions
            ArgumentException => false,
            ArgumentNullException => false,
            NotSupportedException => false,
            UnauthorizedAccessException => false,
            
            _ => false
        };
    }

    /// <summary>
    /// Determines if an HTTP status code is retriable.
    /// </summary>
    /// <param name="httpException">The HTTP exception.</param>
    /// <returns>True if the status code is retriable.</returns>
    private static bool IsRetriableHttpStatusCode(HttpRequestException httpException)
    {
        // Extract status code from exception message or data
        var message = httpException.Message.ToLowerInvariant();
        
        return message.Contains("500") ||  // Internal Server Error
               message.Contains("502") ||  // Bad Gateway
               message.Contains("503") ||  // Service Unavailable
               message.Contains("504") ||  // Gateway Timeout
               message.Contains("408") ||  // Request Timeout
               message.Contains("429");    // Too Many Requests
    }
}

/// <summary>
/// Circuit breaker implementation for resilience patterns.
/// </summary>
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
    /// <param name="timeout">Time to wait before attempting to close the circuit.</param>
    public CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null)
    {
        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
        State = CircuitBreakerState.Closed;
        FailureCount = 0;
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State { get; set; }

    /// <summary>
    /// Gets the current failure count.
    /// </summary>
    public int FailureCount { get; private set; }

    /// <summary>
    /// Gets the next time an attempt is allowed when the circuit is open.
    /// </summary>
    public DateTime NextAttemptTime { get; private set; }

    /// <summary>
    /// Records a failure and potentially opens the circuit.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            FailureCount++;
            
            if (FailureCount >= _failureThreshold)
            {
                State = CircuitBreakerState.Open;
                NextAttemptTime = DateTime.UtcNow.Add(_timeout);
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker to the closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            FailureCount = 0;
            State = CircuitBreakerState.Closed;
            NextAttemptTime = DateTime.MinValue;
        }
    }
}

/// <summary>
/// States of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed - operations flow normally.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open - operations are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open - testing if operations can resume.
    /// </summary>
    HalfOpen
}