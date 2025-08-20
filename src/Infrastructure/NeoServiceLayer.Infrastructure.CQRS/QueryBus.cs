using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace NeoServiceLayer.Infrastructure.CQRS
{
    /// <summary>
    /// Query bus implementation for routing and executing queries with caching support
    /// </summary>
    public class QueryBus : IQueryBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueryBus> _logger;
        private readonly IDistributedCache _cache;
        private readonly QueryBusMetrics _metrics;
        private readonly ConcurrentDictionary<Type, Type> _handlerRegistry;
        private readonly JsonSerializerOptions _jsonOptions;

        public QueryBus(
            IServiceProvider serviceProvider,
            ILogger<QueryBus> logger,
            IDistributedCache cache,
            QueryBusMetrics metrics)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _handlerRegistry = new ConcurrentDictionary<Type, Type>();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<TResult> QueryAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var stopwatch = Stopwatch.StartNew();
            var queryType = query.GetType();

            try
            {
                _logger.LogInformation(
                    "Executing query {QueryType} with ID {QueryId}",
                    queryType.Name, query.QueryId);

                // Check cache if allowed
                if (query.AllowCached)
                {
                    var cachedResult = await GetFromCacheAsync<TResult>(query, cancellationToken);
                    if (cachedResult != null)
                    {
                        _metrics.RecordCacheHit(queryType.Name);
                        _logger.LogDebug(
                            "Query {QueryType} with ID {QueryId} served from cache",
                            queryType.Name, query.QueryId);
                        return cachedResult;
                    }
                    _metrics.RecordCacheMiss(queryType.Name);
                }

                // Get handler type
                var handlerType = GetHandlerType(queryType, typeof(TResult));
                if (handlerType == null)
                {
                    throw new HandlerNotFoundException(
                        $"No handler registered for query type {queryType.Name}");
                }

                // Create timeout cancellation token if specified
                using var timeoutCts = query.TimeoutSeconds.HasValue
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(query.TimeoutSeconds.Value))
                    : null;

                using var linkedCts = timeoutCts != null
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                    : null;

                var effectiveCancellationToken = linkedCts?.Token ?? cancellationToken;

                // Execute query
                TResult result;
                using var scope = _serviceProvider.CreateScope();
                try
                {
                    var handler = scope.ServiceProvider.GetService(handlerType);

                    if (handler == null)
                    {
                        throw new HandlerNotFoundException(
                            $"Handler {handlerType.Name} could not be resolved from DI container");
                    }

                    // Validate query if validator exists
                    await ValidateQueryAsync(query, scope.ServiceProvider, effectiveCancellationToken);

                    // Execute handler
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod == null)
                    {
                        throw new InvalidOperationException(
                            $"Handler {handlerType.Name} does not have HandleAsync method");
                    }

                    var task = handleMethod.Invoke(handler, new object[] { query, effectiveCancellationToken });
                    if (task == null)
                    {
                        throw new InvalidOperationException("Handler did not return a task");
                    }

                    // Get result from task
                    var taskType = task.GetType();
                    var resultProperty = taskType.GetProperty("Result");

                    // Await the task
                    await (Task)task;

                    result = (TResult)resultProperty?.GetValue(task)!;
                }
                catch (OperationCanceledException) when (timeoutCts?.Token.IsCancellationRequested == true)
                {
                    throw new TimeoutException(
                        $"Query {queryType.Name} timed out after {query.TimeoutSeconds} seconds");
                }

                // Cache result if allowed
                if (query.AllowCached && result != null)
                {
                    await SetCacheAsync(query, result, cancellationToken);
                }

                _metrics.RecordQuerySuccess(queryType.Name, stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(
                    "Query {QueryType} with ID {QueryId} executed successfully in {ElapsedMs}ms",
                    queryType.Name, query.QueryId, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordQueryFailure(queryType.Name, stopwatch.ElapsedMilliseconds);

                _logger.LogError(ex,
                    "Failed to execute query {QueryType} with ID {QueryId}",
                    queryType.Name, query.QueryId);

                throw;
            }
        }

        public void RegisterHandler(Type queryType, Type resultType, Type handlerType)
        {
            if (!typeof(IQuery<>).MakeGenericType(resultType).IsAssignableFrom(queryType))
            {
                throw new ArgumentException(
                    $"Type {queryType.Name} does not implement IQuery<{resultType.Name}>",
                    nameof(queryType));
            }

            _handlerRegistry[queryType] = handlerType;

            _logger.LogDebug(
                "Registered handler {HandlerType} for query {QueryType}",
                handlerType.Name, queryType.Name);
        }

        private Type? GetHandlerType(Type queryType, Type resultType)
        {
            if (_handlerRegistry.TryGetValue(queryType, out var handlerType))
            {
                return handlerType;
            }

            // Try to find handler through DI container
            var genericHandlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, resultType);
            return genericHandlerType;
        }

        private async Task ValidateQueryAsync<TResult>(
            IQuery<TResult> query,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var validatorType = typeof(IQueryValidator<>).MakeGenericType(query.GetType());
            var validator = serviceProvider.GetService(validatorType);

            if (validator != null)
            {
                var validateMethod = validatorType.GetMethod("ValidateAsync");
                if (validateMethod != null)
                {
                    var validationTask = (Task<ValidationResult>?)validateMethod.Invoke(
                        validator, new object[] { query, cancellationToken });

                    if (validationTask != null)
                    {
                        var validationResult = await validationTask;
                        if (!validationResult.IsValid)
                        {
                            throw new ValidationException(validationResult.Errors);
                        }
                    }
                }
            }
        }

        private async Task<TResult?> GetFromCacheAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GenerateCacheKey(query);
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonSerializer.Deserialize<TResult>(cachedData, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve query result from cache");
            }

            return default;
        }

        private async Task SetCacheAsync<TResult>(
            IQuery<TResult> query,
            TResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GenerateCacheKey(query);
                var serializedData = JsonSerializer.Serialize(result, _jsonOptions);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };

                await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache query result");
            }
        }

        private string GenerateCacheKey<TResult>(IQuery<TResult> query)
        {
            var queryType = query.GetType();
            var queryData = JsonSerializer.Serialize(query, _jsonOptions);
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(queryData));
            var hashString = Convert.ToBase64String(hash);

            return $"query:{queryType.Name}:{hashString}";
        }
    }

    /// <summary>
    /// Interface for query bus
    /// </summary>
    public interface IQueryBus
    {
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
        void RegisterHandler(Type queryType, Type resultType, Type handlerType);
    }
}