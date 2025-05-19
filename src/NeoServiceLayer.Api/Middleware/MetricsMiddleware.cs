using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Monitoring;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for collecting metrics about HTTP requests.
    /// </summary>
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MetricsMiddleware> _logger;
        private readonly IMetricsService _metricsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="metricsService">The metrics service.</param>
        public MetricsMiddleware(
            RequestDelegate next,
            ILogger<MetricsMiddleware> logger,
            IMetricsService metricsService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var method = context.Request.Method;
            
            // Skip metrics endpoint to avoid circular references
            if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Capture the original body stream
                var originalBodyStream = context.Response.Body;
                
                // Create a new memory stream
                using var responseBody = new System.IO.MemoryStream();
                context.Response.Body = responseBody;
                
                // Continue down the middleware pipeline
                await _next(context);
                
                // Record metrics
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode.ToString();
                var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                
                // Increment request counter
                _metricsService.IncrementCounter("nsl_api_requests_total", method, path, statusCode);
                
                // Observe request duration
                _metricsService.ObserveHistogram("nsl_request_duration_seconds", elapsedSeconds, method, path);
                
                // Observe response size
                var responseSize = context.Response.Body.Length;
                _metricsService.ObserveSummary("nsl_response_size_bytes", responseSize, method, path);
                
                // Copy the response body to the original stream
                responseBody.Seek(0, System.IO.SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                // Record error
                _metricsService.IncrementCounter("nsl_errors_total", "api", ex.GetType().Name);
                
                // Re-throw the exception
                throw;
            }
        }
    }
}
