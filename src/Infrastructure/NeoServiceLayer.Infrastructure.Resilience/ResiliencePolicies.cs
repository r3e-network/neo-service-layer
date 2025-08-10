using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace NeoServiceLayer.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var resilienceOptions = configuration.GetSection("Resilience").Get<ResilienceOptions>()
                                ?? new ResilienceOptions();

        // Register Polly policies for different service types
        services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ResilienceOptions>>();
            return CreateHttpPolicy(resilienceOptions, logger);
        });

        // Add typed HTTP clients with resilience policies
        services.AddHttpClient("ResilientHttpClient")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        // Register service-specific policies
        RegisterServicePolicies(services, resilienceOptions);

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateHttpPolicy(ResilienceOptions options, ILogger logger)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                options.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? $"HTTP {outcome.Result?.StatusCode}";
                    logger.LogWarning(
                        "Retry {RetryCount} after {TimeSpan}ms due to: {Reason}",
                        retryCount, timespan.TotalMilliseconds, reason);
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                options.CircuitBreakerFailureThreshold,
                TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                onBreak: (result, timespan) =>
                {
                    logger.LogError(
                        "Circuit breaker opened for {TimeSpan}s",
                        timespan.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker is half-open");
                });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            options.TimeoutSeconds,
            TimeoutStrategy.Optimistic,
            onTimeoutAsync: async (context, timespan, task) =>
            {
                logger.LogWarning(
                    "Request timed out after {TimeSpan}s",
                    timespan.TotalSeconds);
            });

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
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
                    var logger = context.Values != null && context.Values is IDictionary<string, object> dict && dict.ContainsKey("logger")
                        ? dict["logger"] as ILogger
                        : null;

                    logger?.LogWarning(
                        "HTTP retry {RetryCount} after {Delay}ms",
                        retryCount, timespan.TotalMilliseconds);
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
                    // Logging should be handled by the caller with injected ILogger
                    // Circuit breaker opened for timespan.TotalSeconds
                },
                onReset: () =>
                {
                    // Logging should be handled by the caller with injected ILogger
                    // Circuit breaker reset
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(10);
    }

    private static void RegisterServicePolicies(IServiceCollection services, ResilienceOptions options)
    {
        // Blockchain service policies
        services.AddSingleton<IBlockchainResiliencePolicy>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<BlockchainResiliencePolicy>>();
            return new BlockchainResiliencePolicy(options, logger);
        });

        // Database service policies
        services.AddSingleton<IDatabaseResiliencePolicy>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DatabaseResiliencePolicy>>();
            return new DatabaseResiliencePolicy(options, logger);
        });

        // External service policies
        services.AddSingleton<IExternalServiceResiliencePolicy>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ExternalServiceResiliencePolicy>>();
            return new ExternalServiceResiliencePolicy(options, logger);
        });
    }
}

public interface IBlockchainResiliencePolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);
    Task ExecuteAsync(Func<Task> action);
}

public class BlockchainResiliencePolicy : IBlockchainResiliencePolicy
{
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<BlockchainResiliencePolicy> _logger;

    public BlockchainResiliencePolicy(ResilienceOptions options, ILogger<BlockchainResiliencePolicy> logger)
    {
        _logger = logger;

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                options.BlockchainRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Blockchain operation retry {RetryCount} after {Delay}ms",
                        retryCount, timespan.TotalMilliseconds);
                });

        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                options.BlockchainCircuitBreakerThreshold,
                TimeSpan.FromSeconds(options.BlockchainCircuitBreakerDuration),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(exception,
                        "Blockchain circuit breaker opened for {Duration}s",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Blockchain circuit breaker reset");
                });

        var timeoutPolicy = Policy
            .TimeoutAsync(
                options.BlockchainTimeoutSeconds,
                TimeoutStrategy.Optimistic,
                onTimeoutAsync: async (context, timespan, task) =>
                {
                    _logger.LogWarning(
                        "Blockchain operation timed out after {Timeout}s",
                        timespan.TotalSeconds);
                });

        _policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        return _policy.ExecuteAsync(action);
    }

    public Task ExecuteAsync(Func<Task> action)
    {
        return _policy.ExecuteAsync(action);
    }
}

public interface IDatabaseResiliencePolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);
    Task ExecuteAsync(Func<Task> action);
}

public class DatabaseResiliencePolicy : IDatabaseResiliencePolicy
{
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<DatabaseResiliencePolicy> _logger;

    public DatabaseResiliencePolicy(ResilienceOptions options, ILogger<DatabaseResiliencePolicy> logger)
    {
        _logger = logger;

        var retryPolicy = Policy
            .Handle<Exception>(ex => IsTransientDatabaseException(ex))
            .WaitAndRetryAsync(
                options.DatabaseRetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Database operation retry {RetryCount} after {Delay}ms",
                        retryCount, timespan.TotalMilliseconds);
                });

        var circuitBreakerPolicy = Policy
            .Handle<Exception>(ex => IsTransientDatabaseException(ex))
            .CircuitBreakerAsync(
                options.DatabaseCircuitBreakerThreshold,
                TimeSpan.FromSeconds(options.DatabaseCircuitBreakerDuration),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(exception,
                        "Database circuit breaker opened for {Duration}s",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Database circuit breaker reset");
                });

        _policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        return _policy.ExecuteAsync(action);
    }

    public Task ExecuteAsync(Func<Task> action)
    {
        return _policy.ExecuteAsync(action);
    }

    private bool IsTransientDatabaseException(Exception ex)
    {
        // Add database-specific transient error detection
        if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

public interface IExternalServiceResiliencePolicy
{
    Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> action);
    Task ExecuteAsync(string serviceName, Func<Task> action);
}

public class ExternalServiceResiliencePolicy : IExternalServiceResiliencePolicy
{
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<ExternalServiceResiliencePolicy> _logger;

    public ExternalServiceResiliencePolicy(ResilienceOptions options, ILogger<ExternalServiceResiliencePolicy> logger)
    {
        _logger = logger;

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                options.ExternalServiceRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    var serviceName = context.Values != null && context.Values is IDictionary<string, object> dict && dict.ContainsKey("ServiceName")
                        ? dict["ServiceName"]
                        : "Unknown";

                    _logger.LogWarning(exception,
                        "External service {ServiceName} retry {RetryCount} after {Delay}ms",
                        serviceName, retryCount, timespan.TotalMilliseconds);
                });

        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                options.ExternalServiceCircuitBreakerThreshold,
                TimeSpan.FromSeconds(options.ExternalServiceCircuitBreakerDuration),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(exception,
                        "External service circuit breaker opened for {Duration}s",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("External service circuit breaker reset");
                });

        var bulkheadPolicy = Policy
            .BulkheadAsync(
                options.BulkheadMaxParallelization,
                options.BulkheadMaxQueuingActions,
                onBulkheadRejectedAsync: async context =>
                {
                    var serviceName = context.Values != null && context.Values is IDictionary<string, object> dict && dict.TryGetValue("ServiceName", out var value) 
                        ? value as string ?? "Unknown" 
                        : "Unknown";
                    _logger.LogWarning(
                        "Bulkhead rejected request for service {ServiceName}",
                        serviceName);
                });

        _policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, bulkheadPolicy);
    }

    public Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> action)
    {
        var context = new Context { ["ServiceName"] = serviceName };
        return _policy.ExecuteAsync(async (ctx) => await action(), context);
    }

    public Task ExecuteAsync(string serviceName, Func<Task> action)
    {
        var context = new Context { ["ServiceName"] = serviceName };
        return _policy.ExecuteAsync(async (ctx) => await action(), context);
    }
}

public class ResilienceOptions
{
    // General HTTP policies
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 2;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerThreshold { get; set; } = 5; // Alias for compatibility
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 30;

    // Blockchain-specific policies
    public int BlockchainRetryCount { get; set; } = 5;
    public int BlockchainCircuitBreakerThreshold { get; set; } = 3;
    public int BlockchainCircuitBreakerDuration { get; set; } = 60;
    public int BlockchainTimeoutSeconds { get; set; } = 60;

    // Database-specific policies
    public int DatabaseRetryCount { get; set; } = 3;
    public int DatabaseCircuitBreakerThreshold { get; set; } = 10;
    public int DatabaseCircuitBreakerDuration { get; set; } = 30;

    // External service policies
    public int ExternalServiceRetryCount { get; set; } = 3;
    public int ExternalServiceCircuitBreakerThreshold { get; set; } = 5;
    public int ExternalServiceCircuitBreakerDuration { get; set; } = 30;
    public int BulkheadMaxParallelization { get; set; } = 10;
    public int BulkheadMaxQueuingActions { get; set; } = 20;
}
