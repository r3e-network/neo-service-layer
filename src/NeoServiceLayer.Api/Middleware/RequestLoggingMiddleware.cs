using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for logging requests.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the RequestLoggingMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            // Add request ID to response headers
            context.Response.Headers.Add("X-Request-ID", requestId);

            // Log request
            _logger.LogInformation(
                "Request {RequestId} {Method} {Path} started",
                requestId,
                context.Request.Method,
                context.Request.Path);

            // Call the next middleware
            await _next(context);

            // Log response
            stopwatch.Stop();
            _logger.LogInformation(
                "Request {RequestId} {Method} {Path} completed with status code {StatusCode} in {ElapsedMilliseconds}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
