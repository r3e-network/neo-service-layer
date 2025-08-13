using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NeoServiceLayer.Infrastructure.Observability.Logging;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for managing correlation IDs across HTTP requests.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly IStructuredLoggerFactory _loggerFactory;
        private const string CorrelationIdHeaderName = "X-Correlation-Id";
        private const string TraceIdHeaderName = "X-Trace-Id";

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger,
            IStructuredLoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrGenerateCorrelationId(context);
            var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

            // Add correlation ID to response headers
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers.Add(CorrelationIdHeaderName, correlationId);
                }

                if (!context.Response.Headers.ContainsKey(TraceIdHeaderName))
                {
                    context.Response.Headers.Add(TraceIdHeaderName, traceId);
                }

                return Task.CompletedTask;
            });

            // Store correlation ID in HttpContext for access throughout request
            context.Items["CorrelationId"] = correlationId;
            context.Items["TraceId"] = traceId;

            // Create structured logger for this request
            var structuredLogger = _loggerFactory.CreateLogger("API.Request", correlationId);
            context.Items["StructuredLogger"] = structuredLogger;

            // Log request start
            structuredLogger.LogOperation("RequestStart", new Dictionary<string, object>
            {
                ["Method"] = context.Request.Method,
                ["Path"] = context.Request.Path.Value,
                ["QueryString"] = context.Request.QueryString.Value,
                ["RemoteIP"] = context.Connection.RemoteIpAddress?.ToString(),
                ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
                ["TraceId"] = traceId
            });

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                structuredLogger.LogException(ex, "RequestProcessing", new Dictionary<string, object>
                {
                    ["StatusCode"] = context.Response.StatusCode,
                    ["ElapsedMs"] = stopwatch.ElapsedMilliseconds
                });
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Log request completion
                structuredLogger.LogOperation("RequestComplete", new Dictionary<string, object>
                {
                    ["StatusCode"] = context.Response.StatusCode,
                    ["ElapsedMs"] = stopwatch.ElapsedMilliseconds,
                    ["ResponseContentType"] = context.Response.ContentType
                }, GetLogLevel(context.Response.StatusCode));

                // Log performance metric
                structuredLogger.LogMetric("request.duration", stopwatch.ElapsedMilliseconds, new Dictionary<string, object>
                {
                    ["endpoint"] = context.Request.Path.Value,
                    ["method"] = context.Request.Method,
                    ["status_code"] = context.Response.StatusCode
                });
            }
        }

        private string GetOrGenerateCorrelationId(HttpContext context)
        {
            // Try to get correlation ID from request header
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues correlationId))
            {
                var id = correlationId.ToString();
                if (!string.IsNullOrWhiteSpace(id) && IsValidCorrelationId(id))
                {
                    return id;
                }
            }

            // Try to get from trace context (W3C Trace Context)
            if (context.Request.Headers.TryGetValue("traceparent", out StringValues traceParent))
            {
                var parts = traceParent.ToString().Split('-');
                if (parts.Length >= 3)
                {
                    return parts[1]; // Use trace ID as correlation ID
                }
            }

            // Generate new correlation ID
            return GenerateCorrelationId();
        }

        private bool IsValidCorrelationId(string correlationId)
        {
            // Validate correlation ID format and length
            if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
            {
                return false;
            }

            // Basic validation to prevent injection attacks
            foreach (char c in correlationId)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private string GenerateCorrelationId()
        {
            return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
        }

        private LogLevel GetLogLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }
    }

    /// <summary>
    /// Extension methods for adding correlation ID middleware.
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
