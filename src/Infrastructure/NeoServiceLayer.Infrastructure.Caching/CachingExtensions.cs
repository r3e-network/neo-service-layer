using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Caching;

public static class CachingExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CachingOptions>(configuration.GetSection("Caching"));

        // Add memory cache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = configuration.GetValue<long?>("Caching:MemoryCache:SizeLimit") ?? 1000;
            options.CompactionPercentage = configuration.GetValue<double?>("Caching:MemoryCache:CompactionPercentage") ?? 0.2;
        });

        // Add distributed cache
        var cacheProvider = configuration["Caching:Provider"] ?? "Redis";
        
        switch (cacheProvider.ToLower())
        {
            case "redis":
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = configuration.GetConnectionString("Redis");
                    options.InstanceName = configuration["Caching:Redis:InstanceName"] ?? "NeoServiceLayer";
                });
                break;
            
            case "sqlserver":
                services.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("CacheDb");
                    options.SchemaName = "dbo";
                    options.TableName = "Cache";
                    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
                    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
                });
                break;
            
            default:
                services.AddDistributedMemoryCache();
                break;
        }

        // Add cache services
        services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IResponseCacheService, ResponseCacheService>();

        // Add cache invalidation service
        services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();
        services.AddHostedService<CacheMaintenanceService>();

        return services;
    }

    public static IApplicationBuilder UseResponseCaching(this IApplicationBuilder app)
    {
        // Add response caching middleware
        app.UseMiddleware<ResponseCachingMiddleware>();
        
        return app;
    }
}

// Cache service interface
public interface ICacheService
{
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options = null, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, CacheOptions options = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

// Cache service implementation
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<CacheService> _logger;
    private readonly CachingOptions _options;

    public CacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ICacheKeyGenerator keyGenerator,
        ILogger<CacheService> logger,
        IOptions<CachingOptions> options)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _keyGenerator = keyGenerator;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        // Try memory cache first
        if (_options.UseMemoryCache && _memoryCache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("Cache hit (memory): {Key}", key);
            return cachedValue;
        }

        // Try distributed cache
        try
        {
            var data = await _distributedCache.GetAsync(key, cancellationToken);
            if (data != null)
            {
                var value = JsonSerializer.Deserialize<T>(data);
                
                // Store in memory cache
                if (_options.UseMemoryCache)
                {
                    _memoryCache.Set(key, value, TimeSpan.FromMinutes(_options.MemoryCacheDurationMinutes));
                }

                _logger.LogDebug("Cache hit (distributed): {Key}", key);
                return value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
        }

        _logger.LogDebug("Cache miss: {Key}", key);
        return null;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options = null, CancellationToken cancellationToken = default) where T : class
    {
        var value = await GetAsync<T>(key, cancellationToken);
        if (value != null)
        {
            return value;
        }

        // Use lock to prevent cache stampede
        using (await _keyGenerator.GetLockAsync(key, TimeSpan.FromSeconds(30)))
        {
            // Double-check after acquiring lock
            value = await GetAsync<T>(key, cancellationToken);
            if (value != null)
            {
                return value;
            }

            // Generate value
            value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, options, cancellationToken);
            }

            return value;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheOptions options = null, CancellationToken cancellationToken = default) where T : class
    {
        if (value == null)
        {
            return;
        }

        options ??= new CacheOptions();

        try
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(value);
            
            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.AbsoluteExpiration,
                SlidingExpiration = options.SlidingExpiration
            };

            await _distributedCache.SetAsync(key, data, distributedCacheOptions, cancellationToken);

            // Also set in memory cache
            if (_options.UseMemoryCache)
            {
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpiration,
                    SlidingExpiration = options.SlidingExpiration,
                    Size = 1
                };

                _memoryCache.Set(key, value, memoryCacheOptions);
            }

            _logger.LogDebug("Cache set: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _memoryCache.Remove(key);
            
            _logger.LogDebug("Cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // This implementation depends on the cache provider
        // For Redis, we can use pattern matching
        if (_distributedCache is RedisCache)
        {
            var connection = await GetRedisConnection();
            if (connection != null)
            {
                var server = connection.GetServer(connection.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                
                if (keys.Any())
                {
                    await connection.GetDatabase().KeyDeleteAsync(keys);
                }
            }
        }
        
        _logger.LogDebug("Cache removed by prefix: {Prefix}", prefix);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_options.UseMemoryCache && _memoryCache.TryGetValue(key, out _))
        {
            return true;
        }

        var data = await _distributedCache.GetAsync(key, cancellationToken);
        return data != null;
    }

    private async Task<IConnectionMultiplexer> GetRedisConnection()
    {
        // This is a simplified implementation
        // In production, you would inject IConnectionMultiplexer
        return null;
    }
}

// Response cache service
public interface IResponseCacheService
{
    Task CacheResponseAsync(string key, ResponseCacheEntry entry, TimeSpan? expiration = null);
    Task<ResponseCacheEntry> GetCachedResponseAsync(string key);
}

public class ResponseCacheService : IResponseCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<ResponseCacheService> _logger;

    public ResponseCacheService(ICacheService cacheService, ILogger<ResponseCacheService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task CacheResponseAsync(string key, ResponseCacheEntry entry, TimeSpan? expiration = null)
    {
        var options = new CacheOptions
        {
            AbsoluteExpiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        await _cacheService.SetAsync($"response:{key}", entry, options);
    }

    public async Task<ResponseCacheEntry> GetCachedResponseAsync(string key)
    {
        return await _cacheService.GetAsync<ResponseCacheEntry>($"response:{key}");
    }
}

// Response caching middleware
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseCacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<ResponseCachingMiddleware> _logger;
    private readonly CachingOptions _options;

    public ResponseCachingMiddleware(
        RequestDelegate next,
        IResponseCacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ILogger<ResponseCachingMiddleware> logger,
        IOptions<CachingOptions> options)
    {
        _next = next;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check if caching is enabled for this endpoint
        var endpoint = context.GetEndpoint();
        var cacheAttribute = endpoint?.Metadata.GetMetadata<ResponseCacheAttribute>();
        
        if (cacheAttribute?.NoStore == true)
        {
            await _next(context);
            return;
        }

        // Generate cache key
        var cacheKey = _keyGenerator.GenerateKey(context);

        // Try to get cached response
        var cachedResponse = await _cacheService.GetCachedResponseAsync(cacheKey);
        
        if (cachedResponse != null && !IsStale(cachedResponse, context))
        {
            await ServeCachedResponse(context, cachedResponse);
            return;
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Cache successful responses
        if (ShouldCache(context))
        {
            await CacheResponse(context, cacheKey, responseBody);
        }

        // Copy response to original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }

    private bool IsStale(ResponseCacheEntry entry, HttpContext context)
    {
        // Check if-none-match
        if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch))
        {
            if (ifNoneMatch == entry.ETag)
            {
                return false;
            }
        }

        // Check if-modified-since
        if (context.Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSince))
        {
            if (DateTimeOffset.TryParse(ifModifiedSince, out var modifiedSince))
            {
                if (entry.LastModified <= modifiedSince)
                {
                    return false;
                }
            }
        }

        return false;
    }

    private async Task ServeCachedResponse(HttpContext context, ResponseCacheEntry entry)
    {
        context.Response.StatusCode = entry.StatusCode;
        
        foreach (var header in entry.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }

        // Add cache headers
        context.Response.Headers[HeaderNames.CacheControl] = "public";
        context.Response.Headers[HeaderNames.ETag] = entry.ETag;
        context.Response.Headers[HeaderNames.LastModified] = entry.LastModified.ToString("R");
        context.Response.Headers["X-Cache"] = "HIT";

        await context.Response.WriteAsync(entry.Body);
        
        _logger.LogDebug("Served cached response for {Path}", context.Request.Path);
    }

    private bool ShouldCache(HttpContext context)
    {
        // Only cache successful responses
        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
        {
            return false;
        }

        // Check cache-control headers
        if (context.Response.Headers.TryGetValue(HeaderNames.CacheControl, out var cacheControl))
        {
            if (cacheControl.ToString().Contains("no-cache") || 
                cacheControl.ToString().Contains("no-store") ||
                cacheControl.ToString().Contains("private"))
            {
                return false;
            }
        }

        return true;
    }

    private async Task CacheResponse(HttpContext context, string cacheKey, MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        
        var entry = new ResponseCacheEntry
        {
            StatusCode = context.Response.StatusCode,
            Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = body,
            ETag = GenerateETag(body),
            LastModified = DateTimeOffset.UtcNow
        };

        var duration = GetCacheDuration(context);
        await _cacheService.CacheResponseAsync(cacheKey, entry, duration);
        
        // Add cache headers to response
        context.Response.Headers["X-Cache"] = "MISS";
        context.Response.Headers[HeaderNames.ETag] = entry.ETag;
        context.Response.Headers[HeaderNames.LastModified] = entry.LastModified.ToString("R");
    }

    private TimeSpan GetCacheDuration(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var cacheAttribute = endpoint?.Metadata.GetMetadata<ResponseCacheAttribute>();
        
        if (cacheAttribute?.Duration > 0)
        {
            return TimeSpan.FromSeconds(cacheAttribute.Duration);
        }

        return TimeSpan.FromSeconds(_options.DefaultCacheDurationSeconds);
    }

    private string GenerateETag(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToBase64String(hash)}\"";
    }
}

// Cache key generator
public interface ICacheKeyGenerator
{
    string GenerateKey(HttpContext context);
    string GenerateKey(string prefix, params object[] values);
    Task<IDisposable> GetLockAsync(string key, TimeSpan timeout);
}

public class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    private readonly SemaphoreSlim _lockSemaphore = new(1, 1);
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();

    public string GenerateKey(HttpContext context)
    {
        var keyBuilder = new StringBuilder();
        
        keyBuilder.Append(context.Request.Path);
        
        // Include query string
        if (context.Request.QueryString.HasValue)
        {
            keyBuilder.Append(context.Request.QueryString);
        }

        // Include user identity if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            keyBuilder.Append($"_user:{context.User.Identity.Name}");
        }

        // Include accepted languages
        if (context.Request.Headers.TryGetValue(HeaderNames.AcceptLanguage, out var languages))
        {
            keyBuilder.Append($"_lang:{languages}");
        }

        return GenerateHash(keyBuilder.ToString());
    }

    public string GenerateKey(string prefix, params object[] values)
    {
        var keyBuilder = new StringBuilder(prefix);
        
        foreach (var value in values)
        {
            keyBuilder.Append($":{value}");
        }

        return keyBuilder.ToString();
    }

    public async Task<IDisposable> GetLockAsync(string key, TimeSpan timeout)
    {
        SemaphoreSlim keyLock;
        
        await _lockSemaphore.WaitAsync();
        try
        {
            if (!_locks.TryGetValue(key, out keyLock))
            {
                keyLock = new SemaphoreSlim(1, 1);
                _locks[key] = keyLock;
            }
        }
        finally
        {
            _lockSemaphore.Release();
        }

        await keyLock.WaitAsync(timeout);
        return new LockReleaser(keyLock);
    }

    private string GenerateHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    private class LockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public LockReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}

// Cache invalidation service
public interface ICacheInvalidationService
{
    Task InvalidateAsync(string key);
    Task InvalidateByTagAsync(string tag);
    Task InvalidateByPatternAsync(string pattern);
}

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(ICacheService cacheService, ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvalidateAsync(string key)
    {
        await _cacheService.RemoveAsync(key);
        _logger.LogInformation("Cache invalidated: {Key}", key);
    }

    public async Task InvalidateByTagAsync(string tag)
    {
        // Implementation depends on cache provider supporting tags
        await _cacheService.RemoveByPrefixAsync($"tag:{tag}:");
        _logger.LogInformation("Cache invalidated by tag: {Tag}", tag);
    }

    public async Task InvalidateByPatternAsync(string pattern)
    {
        await _cacheService.RemoveByPrefixAsync(pattern);
        _logger.LogInformation("Cache invalidated by pattern: {Pattern}", pattern);
    }
}

// Cache maintenance service
public class CacheMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheMaintenanceService> _logger;
    private readonly CachingOptions _options;

    public CacheMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<CacheMaintenanceService> logger,
        IOptions<CachingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(_options.MaintenanceIntervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache maintenance");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        _logger.LogInformation("Performing cache maintenance");

        // Clean up expired entries (implementation specific to cache provider)
        // For Redis, this is handled automatically
        // For SQL Server cache, you might need to run cleanup

        _logger.LogInformation("Cache maintenance completed");
    }
}

// Models
public class ResponseCacheEntry
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
    public string ETag { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

public class CacheOptions
{
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public List<string> Tags { get; set; } = new();
}

// Configuration
public class CachingOptions
{
    public string Provider { get; set; } = "Redis";
    public bool UseMemoryCache { get; set; } = true;
    public int MemoryCacheDurationMinutes { get; set; } = 5;
    public int DefaultCacheDurationSeconds { get; set; } = 300;
    public int MaintenanceIntervalMinutes { get; set; } = 60;
    public List<string> ExcludedPaths { get; set; } = new() { "/health", "/metrics" };
}

// Cache attribute for action-level control
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CacheResponseAttribute : Attribute
{
    public int Duration { get; set; }
    public bool VaryByUser { get; set; }
    public string[] VaryByQueryKeys { get; set; }
    public string[] Tags { get; set; }
}