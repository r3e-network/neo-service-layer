using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Common.Models;
using System.Diagnostics;

namespace NeoServiceLayer.Common.HealthChecks;

/// <summary>
/// Common health check implementation for all Neo Service Layer microservices
/// </summary>
public class CommonHealthCheck : IHealthCheck
{
    private readonly CommonServiceOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public CommonHealthCheck(
        IOptionsSnapshot<CommonServiceOptions> options,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            { "service", _options.ServiceName },
            { "version", _options.ServiceVersion },
            { "environment", _options.Environment },
            { "timestamp", DateTimeOffset.UtcNow },
            { "uptime", GetUptime() }
        };

        try
        {
            // Check memory usage
            var memoryUsage = GetMemoryUsageMB();
            data["memory_usage_mb"] = memoryUsage;

            if (memoryUsage > _options.MaxMemoryUsageMB)
            {
                return HealthCheckResult.Degraded(
                    $"High memory usage: {memoryUsage}MB (max: {_options.MaxMemoryUsageMB}MB)",
                    data: data);
            }

            // Check if service is responsive
            var responseTime = await CheckServiceResponsiveness(cancellationToken);
            data["response_time_ms"] = responseTime;

            if (responseTime > _options.RequestTimeoutSeconds * 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Slow response time: {responseTime}ms",
                    data: data);
            }

            data["status"] = "healthy";
            return HealthCheckResult.Healthy(
                "Service is running normally",
                data: data);
        }
        catch (Exception ex)
        {
            data["error"] = ex.Message;
            return HealthCheckResult.Unhealthy(
                "Service health check failed",
                ex,
                data: data);
        }
    }

    private static TimeSpan GetUptime()
    {
        using var process = Process.GetCurrentProcess();
        return DateTime.UtcNow - process.StartTime.ToUniversalTime();
    }

    private static long GetMemoryUsageMB()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64 / (1024 * 1024);
    }

    private static async Task<long> CheckServiceResponsiveness(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate a simple operation to check responsiveness
        await Task.Delay(1, cancellationToken);
        
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}