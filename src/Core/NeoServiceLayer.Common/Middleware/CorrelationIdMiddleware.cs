using Microsoft.AspNetCore.Http;
using NeoServiceLayer.Common.Services;

namespace NeoServiceLayer.Common.Middleware;

/// <summary>
/// Middleware for handling correlation IDs in HTTP requests
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    
    private readonly RequestDelegate _next;
    private readonly ICorrelationIdService _correlationIdService;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ICorrelationIdService correlationIdService)
    {
        _next = next;
        _correlationIdService = correlationIdService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        _correlationIdService.SetCorrelationId(correlationId);
        
        // Add correlation ID to response headers
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        }

        await _next(context);
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdValues))
        {
            var correlationId = correlationIdValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }
        }

        // Generate a new correlation ID if not found
        return _correlationIdService.GenerateCorrelationId();
    }
}