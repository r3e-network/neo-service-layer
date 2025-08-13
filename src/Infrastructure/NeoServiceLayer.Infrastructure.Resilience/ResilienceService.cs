using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure.Resilience;

/// <summary>
/// Resilience service providing retry policies, circuit breakers, and fault tolerance.
/// Addresses error handling and resilience issues identified in the code review.
/// </summary>
public class ResilienceService : ServiceBase, IResilienceService
{
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();
    private readonly ConcurrentDictionary<string, RetryState> _retryStates = new();
    private readonly Timer _maintenanceTimer;
    
    // Default configuration
    private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(1);
    private readonly int _circuitBreakerFailureThreshold = 5;
    private readonly TimeSpan _circuitBreakerResetTimeout = TimeSpan.FromMinutes(5);

    public ResilienceService(ILogger<ResilienceService> logger)
        : base("ResilienceService", "Resilience and fault tolerance service", "1.0.0", logger)
    {
        // Add resilience capability
        AddCapability<IResilienceService>();
        
        // Set metadata
        SetMetadata("CircuitBreakerThreshold", _circuitBreakerFailureThreshold);
        SetMetadata("DefaultRetryCount", 3);
        SetMetadata("DefaultBackoffMs", 1000);
        
        // Initialize maintenance timer
        _maintenanceTimer = new Timer(MaintenanceCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicy? retryPolicy = null)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var policy = retryPolicy ?? new RetryPolicy();
        var operationId = Guid.NewGuid().ToString();
        
        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                Logger.LogDebug("Executing operation attempt {Attempt}/{MaxRetries} for {OperationId}", 
                    attempt + 1, policy.MaxRetries + 1, operationId);
                
                var result = await operation();
                
                if (attempt > 0)
                {
                    Logger.LogInformation("Operation succeeded after {Attempts} attempts for {OperationId}", 
                        attempt + 1, operationId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (attempt == policy.MaxRetries)
                {
                    Logger.LogError(ex, "Operation failed after {MaxRetries} retries for {OperationId}", 
                        policy.MaxRetries + 1, operationId);
                    throw new ResilienceException($"Operation failed after {policy.MaxRetries + 1} attempts", ex);
                }

                if (!ShouldRetry(ex, policy))
                {
                    Logger.LogWarning("Operation will not be retried due to exception type: {ExceptionType} for {OperationId}", 
                        ex.GetType().Name, operationId);
                    throw;
                }

                var delay = CalculateDelay(attempt, policy);
                Logger.LogWarning(ex, "Operation failed (attempt {Attempt}), retrying in {Delay}ms for {OperationId}", 
                    attempt + 1, delay.TotalMilliseconds, operationId);
                
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException("This should never be reached");
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(string circuitName, Func<Task<T>> operation, CircuitBreakerPolicy? policy = null)
    {
        if (string.IsNullOrWhiteSpace(circuitName))
            throw new ArgumentException("Circuit name cannot be null or empty", nameof(circuitName));
        
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var circuitPolicy = policy ?? new CircuitBreakerPolicy();
        var circuitState = _circuitBreakers.GetOrAdd(circuitName, _ => new CircuitBreakerState
        {
            State = CircuitState.Closed,
            FailureCount = 0,
            LastFailureTime = DateTime.MinValue,
            Policy = circuitPolicy
        });

        // Check circuit state
        switch (circuitState.State)
        {
            case CircuitState.Open:
                if (DateTime.UtcNow - circuitState.LastFailureTime > circuitPolicy.OpenTimeout)
                {
                    // Transition to half-open
                    circuitState.State = CircuitState.HalfOpen;
                    Logger.LogInformation("Circuit breaker {CircuitName} transitioning to half-open state", circuitName);
                }
                else
                {
                    Logger.LogWarning("Circuit breaker {CircuitName} is open, rejecting request", circuitName);
                    throw new CircuitBreakerOpenException($"Circuit breaker {circuitName} is open");
                }
                break;

            case CircuitState.HalfOpen:
                Logger.LogDebug("Circuit breaker {CircuitName} is half-open, allowing test request", circuitName);
                break;

            case CircuitState.Closed:
                // Normal operation
                break;
        }

        try
        {
            var result = await operation();
            
            // Success - reset circuit if needed
            if (circuitState.State == CircuitState.HalfOpen)
            {
                circuitState.State = CircuitState.Closed;
                circuitState.FailureCount = 0;
                Logger.LogInformation("Circuit breaker {CircuitName} closed after successful test", circuitName);
            }
            else if (circuitState.FailureCount > 0)
            {
                circuitState.FailureCount = Math.Max(0, circuitState.FailureCount - 1);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            circuitState.FailureCount++;
            circuitState.LastFailureTime = DateTime.UtcNow;
            
            if (circuitState.State == CircuitState.HalfOpen)
            {
                // Failed test - reopen circuit
                circuitState.State = CircuitState.Open;
                Logger.LogWarning("Circuit breaker {CircuitName} reopened after failed test", circuitName);
            }
            else if (circuitState.FailureCount >= circuitPolicy.FailureThreshold)
            {
                // Open circuit
                circuitState.State = CircuitState.Open;
                Logger.LogWarning("Circuit breaker {CircuitName} opened after {FailureCount} failures", 
                    circuitName, circuitState.FailureCount);
            }
            
            Logger.LogError(ex, "Operation failed in circuit breaker {CircuitName}, failure count: {FailureCount}", 
                circuitName, circuitState.FailureCount);
            
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Logger.LogWarning("Operation timed out after {Timeout}", timeout);
            throw new TimeoutException($"Operation timed out after {timeout}");
        }
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithBulkheadAsync<T>(string bulkheadName, Func<Task<T>> operation, BulkheadPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(bulkheadName))
            throw new ArgumentException("Bulkhead name cannot be null or empty", nameof(bulkheadName));
        
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // Simple semaphore-based bulkhead implementation
        var semaphoreKey = $"bulkhead_{bulkheadName}";
        var semaphore = GetOrCreateSemaphore(semaphoreKey, policy.MaxConcurrency);

        var acquired = await semaphore.WaitAsync(policy.MaxWaitTime);
        if (!acquired)
        {
            Logger.LogWarning("Bulkhead {BulkheadName} rejected request - maximum concurrency reached", bulkheadName);
            throw new BulkheadRejectedException($"Bulkhead {bulkheadName} rejected request");
        }

        try
        {
            Logger.LogDebug("Executing operation in bulkhead {BulkheadName}", bulkheadName);
            return await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public CircuitBreakerStatus GetCircuitBreakerStatus(string circuitName)
    {
        if (!_circuitBreakers.TryGetValue(circuitName, out var state))
        {
            return new CircuitBreakerStatus
            {
                CircuitName = circuitName,
                State = CircuitState.Closed,
                FailureCount = 0,
                LastFailureTime = null
            };
        }

        return new CircuitBreakerStatus
        {
            CircuitName = circuitName,
            State = state.State,
            FailureCount = state.FailureCount,
            LastFailureTime = state.LastFailureTime == DateTime.MinValue ? null : state.LastFailureTime
        };
    }

    /// <inheritdoc/>
    public void ResetCircuitBreaker(string circuitName)
    {
        if (_circuitBreakers.TryGetValue(circuitName, out var state))
        {
            state.State = CircuitState.Closed;
            state.FailureCount = 0;
            state.LastFailureTime = DateTime.MinValue;
            Logger.LogInformation("Circuit breaker {CircuitName} manually reset", circuitName);
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing resilience service...");
            
            // Test basic functionality
            await ExecuteWithRetryAsync(async () =>
            {
                await Task.CompletedTask;
                return true;
            }, new RetryPolicy { MaxRetries = 1 });
            
            Logger.LogInformation("Resilience service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize resilience service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Resilience service started");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Resilience service stopping...");
        
        _maintenanceTimer?.Dispose();
        
        Logger.LogInformation("Resilience service stopped");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            var openCircuits = 0;
            foreach (var circuit in _circuitBreakers.Values)
            {
                if (circuit.State == CircuitState.Open)
                    openCircuits++;
            }

            if (openCircuits > _circuitBreakers.Count / 2)
            {
                Logger.LogWarning("More than half of circuit breakers are open: {OpenCircuits}/{TotalCircuits}", 
                    openCircuits, _circuitBreakers.Count);
                return ServiceHealth.Degraded;
            }

            await Task.CompletedTask;
            return ServiceHealth.Healthy;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
            return ServiceHealth.Unhealthy;
        }
    }

    #region Private Helper Methods

    private bool ShouldRetry(Exception exception, RetryPolicy policy)
    {
        // Don't retry for certain exception types
        if (exception is ArgumentException or ArgumentNullException or InvalidOperationException)
        {
            return false;
        }

        // Check custom retry conditions if provided
        return policy.ShouldRetryCondition?.Invoke(exception) ?? true;
    }

    private TimeSpan CalculateDelay(int attempt, RetryPolicy policy)
    {
        return policy.BackoffType switch
        {
            BackoffType.Fixed => TimeSpan.FromMilliseconds(policy.BaseDelayMs),
            BackoffType.Linear => TimeSpan.FromMilliseconds(policy.BaseDelayMs * (attempt + 1)),
            BackoffType.Exponential => TimeSpan.FromMilliseconds(policy.BaseDelayMs * Math.Pow(2, attempt)),
            BackoffType.ExponentialWithJitter => TimeSpan.FromMilliseconds(
                policy.BaseDelayMs * Math.Pow(2, attempt) * (0.5 + Random.Shared.NextDouble() * 0.5)),
            _ => TimeSpan.FromMilliseconds(policy.BaseDelayMs)
        };
    }

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    private SemaphoreSlim GetOrCreateSemaphore(string key, int maxCount)
    {
        return _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(maxCount, maxCount));
    }

    private void MaintenanceCallback(object? state)
    {
        try
        {
            // Clean up old retry states
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _retryStates)
            {
                if (kvp.Value.LastAttempt < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _retryStates.TryRemove(key, out _);
            }

            // Log circuit breaker states
            var openCircuits = 0;
            var halfOpenCircuits = 0;
            
            foreach (var kvp in _circuitBreakers)
            {
                switch (kvp.Value.State)
                {
                    case CircuitState.Open:
                        openCircuits++;
                        break;
                    case CircuitState.HalfOpen:
                        halfOpenCircuits++;
                        break;
                }
            }

            if (openCircuits > 0 || halfOpenCircuits > 0)
            {
                Logger.LogInformation("Circuit breaker status - Open: {Open}, Half-open: {HalfOpen}, Closed: {Closed}",
                    openCircuits, halfOpenCircuits, _circuitBreakers.Count - openCircuits - halfOpenCircuits);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during maintenance callback");
        }
    }

    #endregion

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _maintenanceTimer?.Dispose();
            
            foreach (var semaphore in _semaphores.Values)
            {
                semaphore?.Dispose();
            }
            _semaphores.Clear();
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// Internal circuit breaker state.
/// </summary>
internal class CircuitBreakerState
{
    public CircuitState State { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public CircuitBreakerPolicy Policy { get; set; } = new();
}

/// <summary>
/// Internal retry state.
/// </summary>
internal class RetryState
{
    public int AttemptCount { get; set; }
    public DateTime LastAttempt { get; set; }
}