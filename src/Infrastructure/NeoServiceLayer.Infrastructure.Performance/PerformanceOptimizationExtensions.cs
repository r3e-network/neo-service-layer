using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Performance;

public static class PerformanceOptimizationExtensions
{
    public static IServiceCollection AddPerformanceOptimizations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PerformanceOptions>(configuration.GetSection("Performance"));

        // Configure Kestrel for performance
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxConcurrentConnections = configuration.GetValue<int?>("Performance:MaxConcurrentConnections") ?? 100;
            options.Limits.MaxConcurrentUpgradedConnections = configuration.GetValue<int?>("Performance:MaxConcurrentUpgradedConnections") ?? 100;
            options.Limits.MaxRequestBodySize = configuration.GetValue<long?>("Performance:MaxRequestBodySize") ?? 30_000_000;
            options.Limits.MinRequestBodyDataRate = new MinDataRate(
                bytesPerSecond: configuration.GetValue<double?>("Performance:MinRequestBodyDataRate") ?? 240, 
                gracePeriod: TimeSpan.FromSeconds(5));
            options.Limits.MinResponseDataRate = new MinDataRate(
                bytesPerSecond: configuration.GetValue<double?>("Performance:MinResponseDataRate") ?? 240, 
                gracePeriod: TimeSpan.FromSeconds(5));
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
        });

        // Add response compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/xml",
                "text/xml",
                "text/json",
                "text/plain",
                "text/html",
                "text/css",
                "application/javascript"
            });
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // Add object pooling
        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.AddSingleton(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
            return provider.Create(new StringBuilderPooledObjectPolicy());
        });

        // Add RecyclableMemoryStreamManager for efficient memory usage
        services.AddSingleton<RecyclableMemoryStreamManager>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PerformanceOptions>>().Value;
            return new RecyclableMemoryStreamManager
            {
                BlockSize = options.MemoryStreamBlockSize,
                LargeBufferMultiple = options.MemoryStreamLargeBufferMultiple,
                MaximumBufferSize = options.MemoryStreamMaxBufferSize,
                GenerateCallStacks = false,
                AggressiveBufferReturn = true
            };
        });

        // Add array pool
        services.AddSingleton<ArrayPool<byte>>(ArrayPool<byte>.Shared);
        services.AddSingleton<ArrayPool<char>>(ArrayPool<char>.Shared);

        // Add performance monitoring
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.AddHostedService<PerformanceMonitoringService>();

        // Add request/response buffering optimization
        services.AddScoped<IRequestBufferingService, RequestBufferingService>();
        services.AddScoped<IResponseBufferingService, ResponseBufferingService>();

        // Add connection pooling for HTTP clients
        services.AddHttpClient("Optimized")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 50,
                EnableMultipleHttp2Connections = true,
                UseProxy = false
            });

        return services;
    }

    public static IApplicationBuilder UsePerformanceOptimizations(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<PerformanceOptions>>().Value;

        // Add response compression early in pipeline
        if (options.EnableResponseCompression)
        {
            app.UseResponseCompression();
        }

        // Add request buffering middleware
        if (options.EnableRequestBuffering)
        {
            app.UseMiddleware<RequestBufferingMiddleware>();
        }

        // Add response buffering middleware
        if (options.EnableResponseBuffering)
        {
            app.UseMiddleware<ResponseBufferingMiddleware>();
        }

        // Add performance monitoring middleware
        app.UseMiddleware<PerformanceMonitoringMiddleware>();

        return app;
    }
}

// Performance monitoring interface
public interface IPerformanceMonitor
{
    void RecordMetric(string name, double value, Dictionary<string, string> tags = null);
    void RecordDuration(string name, TimeSpan duration, Dictionary<string, string> tags = null);
    PerformanceMetrics GetMetrics();
}

// Performance monitor implementation
public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, MetricData> _metrics = new();
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceOptions _options;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger, IOptions<PerformanceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void RecordMetric(string name, double value, Dictionary<string, string> tags = null)
    {
        var key = GenerateKey(name, tags);
        _metrics.AddOrUpdate(key,
            new MetricData { Name = name, Count = 1, Total = value, Min = value, Max = value },
            (k, existing) =>
            {
                existing.Count++;
                existing.Total += value;
                existing.Min = Math.Min(existing.Min, value);
                existing.Max = Math.Max(existing.Max, value);
                return existing;
            });
    }

    public void RecordDuration(string name, TimeSpan duration, Dictionary<string, string> tags = null)
    {
        RecordMetric(name, duration.TotalMilliseconds, tags);
    }

    public PerformanceMetrics GetMetrics()
    {
        var snapshot = _metrics.ToArray();
        var metrics = new PerformanceMetrics
        {
            Timestamp = DateTimeOffset.UtcNow,
            Metrics = snapshot.Select(kvp => new PerformanceMetric
            {
                Name = kvp.Value.Name,
                Count = kvp.Value.Count,
                Average = kvp.Value.Total / kvp.Value.Count,
                Min = kvp.Value.Min,
                Max = kvp.Value.Max,
                Total = kvp.Value.Total
            }).ToList()
        };

        return metrics;
    }

    private string GenerateKey(string name, Dictionary<string, string> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return name;
        }

        var tagString = string.Join(",", tags.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}:{tagString}";
    }

    private class MetricData
    {
        public string Name { get; set; }
        public long Count { get; set; }
        public double Total { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }
}

// Performance monitoring middleware
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPerformanceMonitor _monitor;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly PerformanceOptions _options;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        IPerformanceMonitor monitor,
        ILogger<PerformanceMonitoringMiddleware> logger,
        IOptions<PerformanceOptions> options)
    {
        _next = next;
        _monitor = monitor;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.EnablePerformanceMonitoring)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var tags = new Dictionary<string, string>
            {
                ["method"] = context.Request.Method,
                ["path"] = path,
                ["status"] = context.Response.StatusCode.ToString()
            };

            _monitor.RecordDuration("http.request.duration", stopwatch.Elapsed, tags);

            if (stopwatch.Elapsed > _options.SlowRequestThreshold)
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {Duration}ms",
                    context.Request.Method, path, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}

// Request buffering service
public interface IRequestBufferingService
{
    Task<Stream> BufferRequestBodyAsync(HttpRequest request);
}

public class RequestBufferingService : IRequestBufferingService
{
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly ILogger<RequestBufferingService> _logger;
    private readonly PerformanceOptions _options;

    public RequestBufferingService(
        RecyclableMemoryStreamManager streamManager,
        ILogger<RequestBufferingService> logger,
        IOptions<PerformanceOptions> options)
    {
        _streamManager = streamManager;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Stream> BufferRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            var buffer = _streamManager.GetStream("RequestBuffer");
            await request.Body.CopyToAsync(buffer);
            buffer.Position = 0;
            request.Body = buffer;
        }

        return request.Body;
    }
}

// Response buffering service
public interface IResponseBufferingService
{
    Task<Stream> BufferResponseBodyAsync(HttpResponse response);
}

public class ResponseBufferingService : IResponseBufferingService
{
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly ILogger<ResponseBufferingService> _logger;

    public ResponseBufferingService(
        RecyclableMemoryStreamManager streamManager,
        ILogger<ResponseBufferingService> logger)
    {
        _streamManager = streamManager;
        _logger = logger;
    }

    public Task<Stream> BufferResponseBodyAsync(HttpResponse response)
    {
        var buffer = _streamManager.GetStream("ResponseBuffer");
        return Task.FromResult<Stream>(buffer);
    }
}

// Request buffering middleware
public class RequestBufferingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestBufferingService _bufferingService;
    private readonly ILogger<RequestBufferingMiddleware> _logger;
    private readonly PerformanceOptions _options;

    public RequestBufferingMiddleware(
        RequestDelegate next,
        IRequestBufferingService bufferingService,
        ILogger<RequestBufferingMiddleware> logger,
        IOptions<PerformanceOptions> options)
    {
        _next = next;
        _bufferingService = bufferingService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only buffer if content length is below threshold
        if (context.Request.ContentLength.HasValue && 
            context.Request.ContentLength.Value > _options.MaxBufferSize)
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();
        await _next(context);
    }
}

// Response buffering middleware
public class ResponseBufferingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseBufferingService _bufferingService;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly ILogger<ResponseBufferingMiddleware> _logger;
    private readonly PerformanceOptions _options;

    public ResponseBufferingMiddleware(
        RequestDelegate next,
        IResponseBufferingService bufferingService,
        RecyclableMemoryStreamManager streamManager,
        ILogger<ResponseBufferingMiddleware> logger,
        IOptions<PerformanceOptions> options)
    {
        _next = next;
        _bufferingService = bufferingService;
        _streamManager = streamManager;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip buffering for certain content types
        if (ShouldSkipBuffering(context))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;

        using var responseBody = _streamManager.GetStream("ResponseBuffer");
        context.Response.Body = responseBody;

        await _next(context);

        // Copy buffered response to original stream
        context.Response.Body = originalBodyStream;
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private bool ShouldSkipBuffering(HttpContext context)
    {
        // Skip for streaming endpoints
        if (context.Request.Path.StartsWithSegments("/stream"))
        {
            return true;
        }

        // Skip for large file downloads
        if (context.Response.ContentLength.HasValue && 
            context.Response.ContentLength.Value > _options.MaxBufferSize)
        {
            return true;
        }

        return false;
    }
}

// Background performance monitoring service
public class PerformanceMonitoringService : BackgroundService
{
    private readonly IPerformanceMonitor _monitor;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly PerformanceOptions _options;

    public PerformanceMonitoringService(
        IPerformanceMonitor monitor,
        ILogger<PerformanceMonitoringService> logger,
        IOptions<PerformanceOptions> options)
    {
        _monitor = monitor;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectSystemMetricsAsync();
                await Task.Delay(TimeSpan.FromSeconds(_options.MetricsCollectionIntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting system metrics");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task CollectSystemMetricsAsync()
    {
        // Collect GC metrics
        var gcInfo = GC.GetMemoryInfo();
        _monitor.RecordMetric("gc.heap_size", gcInfo.HeapSizeBytes);
        _monitor.RecordMetric("gc.memory_load", gcInfo.MemoryLoadBytes);
        _monitor.RecordMetric("gc.total_available_memory", gcInfo.TotalAvailableMemoryBytes);
        _monitor.RecordMetric("gc.high_memory_load_threshold", gcInfo.HighMemoryLoadThresholdBytes);

        for (int i = 0; i <= GC.MaxGeneration; i++)
        {
            _monitor.RecordMetric($"gc.collection_count.gen{i}", GC.CollectionCount(i));
        }

        // Collect thread pool metrics
        ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        
        _monitor.RecordMetric("threadpool.available_worker_threads", workerThreads);
        _monitor.RecordMetric("threadpool.available_io_threads", completionPortThreads);
        _monitor.RecordMetric("threadpool.max_worker_threads", maxWorkerThreads);
        _monitor.RecordMetric("threadpool.max_io_threads", maxCompletionPortThreads);

        // Collect process metrics
        using var process = Process.GetCurrentProcess();
        _monitor.RecordMetric("process.working_set", process.WorkingSet64);
        _monitor.RecordMetric("process.private_memory", process.PrivateMemorySize64);
        _monitor.RecordMetric("process.virtual_memory", process.VirtualMemorySize64);
        _monitor.RecordMetric("process.thread_count", process.Threads.Count);
        _monitor.RecordMetric("process.handle_count", process.HandleCount);

        _logger.LogDebug("System metrics collected");
    }
}

// String builder pooled object policy
public class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
{
    public int InitialCapacity { get; set; } = 256;
    public int MaximumRetainedCapacity { get; set; } = 4096;

    public override StringBuilder Create()
    {
        return new StringBuilder(InitialCapacity);
    }

    public override bool Return(StringBuilder obj)
    {
        if (obj.Capacity > MaximumRetainedCapacity)
        {
            return false;
        }

        obj.Clear();
        return true;
    }
}

// Models
public class PerformanceMetrics
{
    public DateTimeOffset Timestamp { get; set; }
    public List<PerformanceMetric> Metrics { get; set; }
}

public class PerformanceMetric
{
    public string Name { get; set; }
    public long Count { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Total { get; set; }
}

// Configuration
public class PerformanceOptions
{
    // Response compression
    public bool EnableResponseCompression { get; set; } = true;
    
    // Request/Response buffering
    public bool EnableRequestBuffering { get; set; } = true;
    public bool EnableResponseBuffering { get; set; } = true;
    public long MaxBufferSize { get; set; } = 1_048_576; // 1MB
    
    // Memory stream settings
    public int MemoryStreamBlockSize { get; set; } = 16384;
    public int MemoryStreamLargeBufferMultiple { get; set; } = 1_048_576;
    public int MemoryStreamMaxBufferSize { get; set; } = 16_777_216; // 16MB
    
    // Performance monitoring
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public TimeSpan SlowRequestThreshold { get; set; } = TimeSpan.FromSeconds(3);
    public int MetricsCollectionIntervalSeconds { get; set; } = 60;
    
    // Connection limits
    public int MaxConcurrentConnections { get; set; } = 100;
    public int MaxConcurrentUpgradedConnections { get; set; } = 100;
    public long MaxRequestBodySize { get; set; } = 30_000_000; // 30MB
    public double MinRequestBodyDataRate { get; set; } = 240; // bytes/second
    public double MinResponseDataRate { get; set; } = 240; // bytes/second
}