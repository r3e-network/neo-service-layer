using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace NeoServiceLayer.Infrastructure.Resilience;

/// <summary>
/// Provides circuit breaker pattern implementation for resilient external service calls.
/// </summary>
public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _httpPolicy;
    private readonly IAsyncPolicy _genericPolicy;

    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create HTTP-specific policy with retry, circuit breaker, and timeout
        _httpPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message ?? "Unknown";
                    _logger.LogWarning("Retry {RetryCount} after {Delay}ms. Reason: {Reason}", 
                        retryCount, timespan.TotalMilliseconds, reason);
                })
            .WrapAsync(Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, duration) =>
                    {
                        _logger.LogError("Circuit breaker opened for {Duration}s. Last error: {Error}",
                            duration.TotalSeconds, result.Result?.StatusCode ?? result.Exception?.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset. Service recovered.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker is half-open. Testing service availability.");
                    }))
            .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(10)); // 10 second timeout

        // Create generic policy for non-HTTP operations
        _genericPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Retry {RetryCount} after {Delay}ms", 
                        retryCount, timespan.TotalMilliseconds);
                })
            .WrapAsync(Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError(exception, "Circuit breaker opened for {Duration}s", 
                            duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset. Service recovered.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker is half-open. Testing service availability.");
                    }))
            .WrapAsync(Policy.TimeoutAsync(10)); // 10 second timeout
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await _genericPolicy.ExecuteAsync(
            async (ct) => await operation(ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> ExecuteHttpAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken = default)
    {
        return await _httpPolicy.ExecuteAsync(
            async (ct) => await operation(ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await _genericPolicy.ExecuteAsync(
            async (ct) => await operation(ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public CircuitState GetCircuitState()
    {
        // This would need to be enhanced to track actual circuit state
        // For now, return a default state
        return CircuitState.Closed;
    }

    /// <inheritdoc/>
    public CircuitBreakerStatistics GetStatistics()
    {
        // This would need to be enhanced to track actual statistics
        return new CircuitBreakerStatistics
        {
            TotalRequests = 0,
            SuccessfulRequests = 0,
            FailedRequests = 0,
            CircuitOpenCount = 0,
            LastFailureTime = null,
            CurrentState = GetCircuitState()
        };
    }
}

/// <summary>
/// Interface for circuit breaker service.
/// </summary>
public interface ICircuitBreakerService
{
    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an HTTP operation with circuit breaker protection.
    /// </summary>
    Task<HttpResponseMessage> ExecuteHttpAsync(Func<CancellationToken, Task<HttpResponseMessage>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation with circuit breaker protection (void return).
    /// </summary>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    CircuitState GetCircuitState();

    /// <summary>
    /// Gets statistics about the circuit breaker.
    /// </summary>
    CircuitBreakerStatistics GetStatistics();
}

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Statistics for circuit breaker operations.
/// </summary>
public class CircuitBreakerStatistics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public int CircuitOpenCount { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public CircuitState CurrentState { get; set; }
}