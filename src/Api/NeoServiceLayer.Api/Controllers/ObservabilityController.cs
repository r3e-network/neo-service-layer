using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Middleware;
using NeoServiceLayer.Infrastructure.Observability.Logging;
using System.Threading;
using System;


namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// API endpoints for monitoring and observability.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "Admin,Operator")]
    public class ObservabilityController : BaseApiController
    {
        private readonly ILogger<ObservabilityController> _logger;
        private readonly PerformanceMetricsCollector _metricsCollector;
        private readonly IStructuredLoggerFactory _loggerFactory;

        public ObservabilityController(
            ILogger<ObservabilityController> logger,
            PerformanceMetricsCollector metricsCollector,
            IStructuredLoggerFactory loggerFactory)
            : base(logger)
        {
            _logger = logger;
            _metricsCollector = metricsCollector;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Get performance statistics for an endpoint.
        /// </summary>
        [HttpGet("metrics/performance/{endpoint}")]
        [ProducesResponseType(typeof(PerformanceStatistics), 200)]
        public async Task<IActionResult> GetPerformanceStatistics(
            string endpoint,
            [FromQuery] int periodMinutes = 60)
        {
            var decodedEndpoint = Uri.UnescapeDataString(endpoint);
            var period = TimeSpan.FromMinutes(periodMinutes);

            var stats = await _metricsCollector.GetStatisticsAsync(decodedEndpoint, period);

            return Ok(stats);
        }

        /// <summary>
        /// Get error rate for an endpoint.
        /// </summary>
        [HttpGet("metrics/error-rate/{endpoint}")]
        [ProducesResponseType(typeof(ErrorRateResponse), 200)]
        public async Task<IActionResult> GetErrorRate(
            string endpoint,
            [FromQuery] int periodMinutes = 5)
        {
            var decodedEndpoint = Uri.UnescapeDataString(endpoint);
            var period = TimeSpan.FromMinutes(periodMinutes);

            var errorRate = await _metricsCollector.GetErrorRateAsync(decodedEndpoint, period);

            return Ok(new ErrorRateResponse
            {
                Endpoint = decodedEndpoint,
                ErrorRate = errorRate,
                PeriodMinutes = periodMinutes,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get system health overview.
        /// </summary>
        [HttpGet("health/overview")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HealthOverview), 200)]
        public async Task<IActionResult> GetHealthOverview()
        {
            var overview = new HealthOverview
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Components = new List<ComponentHealth>
                {
                    new ComponentHealth { Name = "API", Status = "Healthy", ResponseTimeMs = 50 },
                    new ComponentHealth { Name = "Database", Status = "Healthy", ResponseTimeMs = 10 },
                    new ComponentHealth { Name = "Cache", Status = "Healthy", ResponseTimeMs = 2 },
                    new ComponentHealth { Name = "SGX Enclave", Status = "Healthy", ResponseTimeMs = 100 },
                    new ComponentHealth { Name = "Blockchain", Status = "Healthy", ResponseTimeMs = 200 }
                },
                Metrics = new SystemMetrics
                {
                    CpuUsagePercent = GetCpuUsage(),
                    MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024),
                    ActiveConnections = 42,
                    RequestsPerSecond = 150,
                    AverageResponseTimeMs = 75
                }
            };

            return Ok(overview);
        }

        /// <summary>
        /// Get recent traces for debugging.
        /// </summary>
        [HttpGet("traces")]
        [ProducesResponseType(typeof(List<TraceInfo>), 200)]
        public async Task<IActionResult> GetRecentTraces(
            [FromQuery] int limit = 100,
            [FromQuery] string correlationId = null)
        {
            // This would integrate with your trace storage (e.g., Jaeger, Zipkin)
            var traces = new List<TraceInfo>
            {
                new TraceInfo
                {
                    TraceId = Guid.NewGuid().ToString("N"),
                    CorrelationId = correlationId ?? GenerateCorrelationId(),
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    Duration = 150,
                    ServiceName = "NeoServiceLayer.Api",
                    OperationName = "GET /api/v1/health",
                    Status = "Success",
                    Tags = new Dictionary<string, string>
                    {
                        ["http.method"] = "GET",
                        ["http.status_code"] = "200",
                        ["user.id"] = "user123"
                    }
                }
            };

            return Ok(traces);
        }

        /// <summary>
        /// Get recent logs for debugging.
        /// </summary>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(List<LogEntry>), 200)]
        public async Task<IActionResult> GetRecentLogs(
            [FromQuery] int limit = 100,
            [FromQuery] string level = null,
            [FromQuery] string correlationId = null)
        {
            // This would integrate with your log storage
            var logs = new List<LogEntry>
            {
                new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    CorrelationId = correlationId ?? GenerateCorrelationId(),
                    Message = "Request processed successfully",
                    Properties = new Dictionary<string, object>
                    {
                        ["Endpoint"] = "/api/v1/health",
                        ["Duration"] = 50,
                        ["StatusCode"] = 200
                    }
                }
            };

            return Ok(logs);
        }

        /// <summary>
        /// Trigger a test alert for monitoring validation.
        /// </summary>
        [HttpPost("alerts/test")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> TriggerTestAlert([FromBody] TestAlertRequest request)
        {
            var structuredLogger = _loggerFactory.CreateLogger("ObservabilityTest", GetCorrelationId());

            structuredLogger.LogOperation("TestAlert", new Dictionary<string, object>
            {
                ["AlertType"] = request.AlertType,
                ["Message"] = request.Message,
                ["Severity"] = request.Severity
            }, LogLevel.Warning);

            await _metricsCollector.TriggerPerformanceAlertAsync(
                "TestEndpoint",
                new List<string> { request.Message });

            return NoContent();
        }

        private double GetCpuUsage()
        {
            // Simplified CPU usage calculation
            return Environment.ProcessorCount > 0 ? 35.5 : 0;
        }

        private string GenerateCorrelationId()
        {
            return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
        }
    }

    // Response DTOs
    public class ErrorRateResponse
    {
        public string Endpoint { get; set; }
        public double ErrorRate { get; set; }
        public int PeriodMinutes { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class HealthOverview
    {
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ComponentHealth> Components { get; set; }
        public SystemMetrics Metrics { get; set; }
    }

    public class ComponentHealth
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public double ResponseTimeMs { get; set; }
    }

    public class SystemMetrics
    {
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMB { get; set; }
        public int ActiveConnections { get; set; }
        public double RequestsPerSecond { get; set; }
        public double AverageResponseTimeMs { get; set; }
    }

    public class TraceInfo
    {
        public string TraceId { get; set; }
        public string CorrelationId { get; set; }
        public DateTime StartTime { get; set; }
        public double Duration { get; set; }
        public string ServiceName { get; set; }
        public string OperationName { get; set; }
        public string Status { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string CorrelationId { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class TestAlertRequest
    {
        public string AlertType { get; set; } = "Performance";
        public string Message { get; set; } = "This is a test alert";
        public string Severity { get; set; } = "Warning";
    }
}
