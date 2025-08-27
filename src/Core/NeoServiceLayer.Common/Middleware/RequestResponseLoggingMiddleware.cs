using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Common.Services;
using System.Diagnostics;
using System.Text;

namespace NeoServiceLayer.Common.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly IPerformanceMonitor _performanceMonitor;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        ICorrelationIdService correlationIdService,
        IPerformanceMonitor performanceMonitor)
    {
        _next = next;
        _logger = logger;
        _correlationIdService = correlationIdService;
        _performanceMonitor = performanceMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = _correlationIdService.CorrelationId;
        var stopwatch = Stopwatch.StartNew();
        
        // Log request
        await LogRequestAsync(context, correlationId);
        
        // Capture original response stream
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
            
            // Record performance metrics
            var operationName = $"{context.Request.Method} {context.Request.Path}";
            _performanceMonitor.RecordValue(operationName, stopwatch.Elapsed.TotalMilliseconds);
            
            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        if (!ShouldLog(context.Request.Path))
            return;

        var request = context.Request;
        var requestBody = await ReadRequestBodyAsync(request);
        
        _logger.LogInformation(
            "HTTP Request - {Method} {Path} | CorrelationId: {CorrelationId} | Content-Length: {ContentLength} | User-Agent: {UserAgent} | IP: {RemoteIpAddress}",
            request.Method,
            request.Path + request.QueryString,
            correlationId,
            request.ContentLength ?? 0,
            request.Headers.UserAgent.ToString(),
            context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        
        if (!string.IsNullOrEmpty(requestBody) && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "HTTP Request Body - CorrelationId: {CorrelationId} | Body: {RequestBody}",
                correlationId,
                requestBody);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMs)
    {
        if (!ShouldLog(context.Request.Path))
            return;

        var response = context.Response;
        var responseBody = await ReadResponseBodyAsync(response);
        
        var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        
        _logger.Log(logLevel,
            "HTTP Response - {Method} {Path} | CorrelationId: {CorrelationId} | Status: {StatusCode} | Duration: {ElapsedMs}ms | Content-Length: {ContentLength}",
            context.Request.Method,
            context.Request.Path + context.Request.QueryString,
            correlationId,
            response.StatusCode,
            elapsedMs,
            response.ContentLength ?? 0);
        
        if (!string.IsNullOrEmpty(responseBody) && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "HTTP Response Body - CorrelationId: {CorrelationId} | Body: {ResponseBody}",
                correlationId,
                responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        
        return body;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        
        return body;
    }

    private static bool ShouldLog(PathString path)
    {
        // Skip logging for health check and metrics endpoints
        var pathValue = path.Value?.ToLowerInvariant();
        return pathValue != null && 
               !pathValue.Contains("/health") && 
               !pathValue.Contains("/metrics") &&
               !pathValue.Contains("/favicon.ico");
    }
}