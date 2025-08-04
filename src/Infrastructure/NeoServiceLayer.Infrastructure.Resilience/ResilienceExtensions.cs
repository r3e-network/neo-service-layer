using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace NeoServiceLayer.Infrastructure.Resilience;

public static class ResilienceExtensions
{
    public static IServiceCollection AddResilience(this IServiceCollection services, IConfiguration configuration)
    {
        // Add HTTP client factory with resilience policies
        services.AddHttpClient("ResilientHttpClient")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        // Add resilience policies as singleton services
        services.AddSingleton<IResiliencePolicies, ResiliencePoliciesImpl>();

        // Configure resilience options
        services.Configure<ResilienceOptions>(configuration.GetSection("Resilience"));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.Values.ContainsKey("logger") ? context.Values["logger"] as ILogger : null;
                    logger?.LogWarning($"Retry {retryCount} after {timespan} seconds");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (result, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {timespan}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(10);
    }
}

public interface IResiliencePolicies
{
    IAsyncPolicy<T> GetRetryPolicy<T>(int retryCount = 3, int baseDelaySeconds = 2);
    IAsyncPolicy<T> GetCircuitBreakerPolicy<T>(int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakSeconds = 30);
    IAsyncPolicy<T> GetTimeoutPolicy<T>(int timeoutSeconds = 30);
    IAsyncPolicy<T> GetFallbackPolicy<T>(T fallbackValue);
    IAsyncPolicy<T> GetCombinedPolicy<T>(T fallbackValue = default);
}

public class ResiliencePoliciesImpl : IResiliencePolicies
{
    private readonly ILogger<ResiliencePoliciesImpl> _logger;
    private readonly ResilienceOptions _options;

    public ResiliencePoliciesImpl(ILogger<ResiliencePoliciesImpl> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = configuration.GetSection("Resilience").Get<ResilienceOptions>() ?? new ResilienceOptions();
    }

    public IAsyncPolicy<T> GetRetryPolicy<T>(int retryCount = 3, int baseDelaySeconds = 2)
    {
        return Policy<T>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(baseDelaySeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryNumber, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryNumber} after {TimeSpan}ms. Exception: {Exception}",
                        retryNumber,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? "No exception");
                });
    }

    public IAsyncPolicy<T> GetCircuitBreakerPolicy<T>(int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakSeconds = 30)
    {
        return Policy<T>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking,
                TimeSpan.FromSeconds(durationOfBreakSeconds),
                onBreak: (result, timespan, context) =>
                {
                    _logger.LogError(
                        "Circuit breaker opened for {TimeSpan}s. Exception: {Exception}",
                        timespan.TotalSeconds,
                        result.Exception?.Message ?? "No exception");
                },
                onReset: (context) =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker is half-open");
                });
    }

    public IAsyncPolicy<T> GetTimeoutPolicy<T>(int timeoutSeconds = 30)
    {
        return Policy
            .TimeoutAsync<T>(
                timeoutSeconds,
                TimeoutStrategy.Optimistic,
                onTimeoutAsync: async (context, timespan, task) =>
                {
                    _logger.LogError("Operation timed out after {TimeoutSeconds}s", timeoutSeconds);
                    await Task.CompletedTask;
                });
    }

    public IAsyncPolicy<T> GetFallbackPolicy<T>(T fallbackValue)
    {
        return Policy<T>
            .Handle<Exception>()
            .FallbackAsync(
                fallbackValue,
                onFallbackAsync: async (result, context) =>
                {
                    _logger.LogWarning(
                        "Fallback triggered. Exception: {Exception}",
                        result.Exception?.Message ?? "No exception");
                    await Task.CompletedTask;
                });
    }

    public IAsyncPolicy<T> GetCombinedPolicy<T>(T fallbackValue = default)
    {
        var fallback = GetFallbackPolicy(fallbackValue);
        var timeout = GetTimeoutPolicy<T>(_options.TimeoutSeconds);
        var retry = GetRetryPolicy<T>(_options.RetryCount, _options.RetryBaseDelaySeconds);
        var circuitBreaker = GetCircuitBreakerPolicy<T>(
            _options.CircuitBreakerThreshold,
            _options.CircuitBreakerDurationSeconds);

        return Policy.WrapAsync(fallback, timeout, retry, circuitBreaker);
    }
}

public class ResilienceOptionsExt
{
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 2;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 30;
}

// Extension methods for specific service types
public static class ServiceResilienceExtensions
{
    public static IServiceCollection AddResilientBlockchainClient(this IServiceCollection services)
    {
        services.AddHttpClient("BlockchainClient")
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetService<ILogger<HttpClient>>();
                return GetBlockchainResiliencePolicy(logger);
            });

        return services;
    }

    public static IServiceCollection AddResilientStorageClient(this IServiceCollection services)
    {
        services.AddHttpClient("StorageClient")
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetService<ILogger<HttpClient>>();
                return GetStorageResiliencePolicy(logger);
            });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetBlockchainResiliencePolicy(ILogger logger)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode && msg.StatusCode != HttpStatusCode.NotFound)
            .WaitAndRetryAsync(
                5, // More retries for blockchain operations
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger?.LogWarning($"Blockchain operation retry {retryCount} after {timespan} seconds");
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                10, // Higher threshold for blockchain
                TimeSpan.FromMinutes(1),
                onBreak: (result, timespan) =>
                {
                    logger?.LogError($"Blockchain circuit breaker opened for {timespan}");
                },
                onReset: () =>
                {
                    logger?.LogInformation("Blockchain circuit breaker reset");
                });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(60)); // Longer timeout for blockchain

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetStorageResiliencePolicy(ILogger logger)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger?.LogWarning($"Storage operation retry {retryCount} after {timespan} seconds");
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (result, timespan) =>
                {
                    logger?.LogError($"Storage circuit breaker opened for {timespan}");
                },
                onReset: () =>
                {
                    logger?.LogInformation("Storage circuit breaker reset");
                });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(30));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }
}

// Resilient service base class
public abstract class ResilientServiceBase
{
    protected readonly IResiliencePolicies ResiliencePolicies;
    protected readonly ILogger Logger;

    protected ResilientServiceBase(IResiliencePolicies resiliencePolicies, ILogger logger)
    {
        ResiliencePolicies = resiliencePolicies;
        Logger = logger;
    }

    protected async Task<T> ExecuteWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        T fallbackValue = default,
        CancellationToken cancellationToken = default)
    {
        var policy = ResiliencePolicies.GetCombinedPolicy(fallbackValue);

        return await policy.ExecuteAsync(async (ct) =>
        {
            return await operation(ct);
        }, cancellationToken);
    }

    protected async Task ExecuteWithResilienceAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        var policy = ResiliencePolicies.GetCombinedPolicy<object>(null);

        await policy.ExecuteAsync(async (ct) =>
        {
            await operation(ct);
            return (object)null;
        }, cancellationToken);
    }
}
