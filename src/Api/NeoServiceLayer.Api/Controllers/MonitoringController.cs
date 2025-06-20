using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for monitoring services.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/monitoring")]
[ApiVersion("1.0")]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MonitoringController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringController"/> class.
    /// </summary>
    /// <param name="monitoringService">The monitoring service.</param>
    /// <param name="logger">The logger.</param>
    public MonitoringController(IMonitoringService monitoringService, ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets system health information.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>System health information.</returns>
    [HttpGet("health/{blockchainType}")]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemHealthResponse>> GetSystemHealthAsync(
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.GetSystemHealthAsync(blockchainType, cancellationToken);

            var response = new SystemHealthResponse
            {
                Status = result.Status,
                Timestamp = result.Timestamp,
                Services = result.Services.Select(s => new ServiceHealthInfo
                {
                    Name = s.Name,
                    Status = s.Status,
                    ResponseTime = s.ResponseTime,
                    LastChecked = s.LastChecked,
                    Error = s.Error
                }).ToList(),
                SystemMetrics = new SystemMetrics
                {
                    CpuUsage = result.SystemMetrics.CpuUsage,
                    MemoryUsage = result.SystemMetrics.MemoryUsage,
                    DiskUsage = result.SystemMetrics.DiskUsage,
                    NetworkLatency = result.SystemMetrics.NetworkLatency
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting system health");
        }
    }

    /// <summary>
    /// Gets service metrics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="serviceName">The service name (optional).</param>
    /// <param name="timeRange">The time range in hours (default: 1).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Service metrics.</returns>
    [HttpGet("metrics/{blockchainType}")]
    [ProducesResponseType(typeof(ServiceMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceMetricsResponse>> GetServiceMetricsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? serviceName = null,
        [FromQuery] int timeRange = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.GetServiceMetricsAsync(blockchainType, serviceName, timeRange, cancellationToken);

            var response = new ServiceMetricsResponse
            {
                ServiceName = serviceName ?? "All Services",
                TimeRange = timeRange,
                Timestamp = DateTime.UtcNow,
                Metrics = result.Metrics.Select(m => new MetricData
                {
                    Name = m.Name,
                    Value = m.Value,
                    Unit = m.Unit,
                    Timestamp = m.Timestamp,
                    Tags = m.Tags
                }).ToList(),
                Summary = new MetricsSummary
                {
                    RequestCount = result.Summary.RequestCount,
                    SuccessRate = result.Summary.SuccessRate,
                    AverageResponseTime = result.Summary.AverageResponseTime,
                    ErrorCount = result.Summary.ErrorCount
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service metrics for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting service metrics");
        }
    }

    /// <summary>
    /// Records a custom metric.
    /// </summary>
    /// <param name="request">The metric recording request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The recording result.</returns>
    [HttpPost("metrics/{blockchainType}")]
    [ProducesResponseType(typeof(RecordMetricResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RecordMetricResponse>> RecordMetricAsync(
        [FromBody] RecordMetricRequest request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _monitoringService.RecordMetricAsync(
                request.Name,
                request.Value,
                request.Unit,
                request.Tags,
                blockchainType,
                cancellationToken);

            var response = new RecordMetricResponse
            {
                Success = result.Success,
                MetricId = result.MetricId,
                Timestamp = result.Timestamp,
                Message = result.Message
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for recording metric");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while recording the metric");
        }
    }

    /// <summary>
    /// Gets performance statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="serviceName">The service name (optional).</param>
    /// <param name="timeRange">The time range in hours (default: 24).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Performance statistics.</returns>
    [HttpGet("performance/{blockchainType}")]
    [ProducesResponseType(typeof(PerformanceStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PerformanceStatisticsResponse>> GetPerformanceStatisticsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? serviceName = null,
        [FromQuery] int timeRange = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.GetPerformanceStatisticsAsync(blockchainType, serviceName, timeRange, cancellationToken);

            var response = new PerformanceStatisticsResponse
            {
                ServiceName = serviceName ?? "All Services",
                TimeRange = timeRange,
                Timestamp = DateTime.UtcNow,
                TotalRequests = result.TotalRequests,
                SuccessfulRequests = result.SuccessfulRequests,
                FailedRequests = result.FailedRequests,
                AverageResponseTime = result.AverageResponseTime,
                MedianResponseTime = result.MedianResponseTime,
                P95ResponseTime = result.P95ResponseTime,
                P99ResponseTime = result.P99ResponseTime,
                ThroughputPerSecond = result.ThroughputPerSecond,
                ErrorRate = result.ErrorRate,
                UptimePercentage = result.UptimePercentage
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance statistics for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting performance statistics");
        }
    }

    /// <summary>
    /// Creates an alert rule.
    /// </summary>
    /// <param name="request">The alert rule creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created alert rule.</returns>
    [HttpPost("alerts/rules/{blockchainType}")]
    [ProducesResponseType(typeof(AlertRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertRuleResponse>> CreateAlertRuleAsync(
        [FromBody] CreateAlertRuleRequest request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _monitoringService.CreateAlertRuleAsync(
                request.Name,
                request.Description,
                request.MetricName,
                request.Condition,
                request.Threshold,
                request.NotificationChannels,
                blockchainType,
                cancellationToken);

            var response = new AlertRuleResponse
            {
                RuleId = result.RuleId,
                Name = result.Name,
                Status = result.Status,
                CreatedAt = result.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetActiveAlertsAsync),
                new { blockchainType },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating alert rule");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert rule for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the alert rule");
        }
    }

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="severity">The alert severity filter (optional).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Active alerts.</returns>
    [HttpGet("alerts/{blockchainType}")]
    [ProducesResponseType(typeof(IEnumerable<ActiveAlert>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ActiveAlert>>> GetActiveAlertsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? severity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.GetActiveAlertsAsync(blockchainType, severity, cancellationToken);

            var response = result.Select(alert => new ActiveAlert
            {
                AlertId = alert.AlertId,
                RuleName = alert.RuleName,
                Severity = alert.Severity,
                Message = alert.Message,
                TriggeredAt = alert.TriggeredAt,
                Status = alert.Status,
                AcknowledgedBy = alert.AcknowledgedBy,
                AcknowledgedAt = alert.AcknowledgedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting active alerts");
        }
    }

    /// <summary>
    /// Gets system logs.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="level">The log level filter (optional).</param>
    /// <param name="limit">The maximum number of logs to return (default: 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>System logs.</returns>
    [HttpGet("logs/{blockchainType}")]
    [ProducesResponseType(typeof(SystemLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemLogsResponse>> GetSystemLogsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? level = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.GetSystemLogsAsync(blockchainType, level, limit, cancellationToken);

            var response = new SystemLogsResponse
            {
                Logs = result.Logs.Select(log => new LogEntry
                {
                    Timestamp = log.Timestamp,
                    Level = log.Level,
                    Message = log.Message,
                    Source = log.Source,
                    Exception = log.Exception,
                    Properties = log.Properties
                }).ToList(),
                TotalCount = result.TotalCount,
                RetrievedCount = result.Logs.Count()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system logs for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting system logs");
        }
    }

    /// <summary>
    /// Starts monitoring for a specific service.
    /// </summary>
    /// <param name="request">The monitoring start request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The monitoring start result.</returns>
    [HttpPost("start/{blockchainType}")]
    [ProducesResponseType(typeof(MonitoringActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MonitoringActionResponse>> StartMonitoringAsync(
        [FromBody] StartMonitoringRequest request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _monitoringService.StartMonitoringAsync(
                request.ServiceName,
                request.Configuration,
                blockchainType,
                cancellationToken);

            var response = new MonitoringActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                ServiceName = request.ServiceName,
                Action = "Start",
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for starting monitoring");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting monitoring for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while starting monitoring");
        }
    }

    /// <summary>
    /// Stops monitoring for a specific service.
    /// </summary>
    /// <param name="request">The monitoring stop request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The monitoring stop result.</returns>
    [HttpPost("stop/{blockchainType}")]
    [ProducesResponseType(typeof(MonitoringActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MonitoringActionResponse>> StopMonitoringAsync(
        [FromBody] StopMonitoringRequest request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _monitoringService.StopMonitoringAsync(
                request.ServiceName,
                blockchainType,
                cancellationToken);

            var response = new MonitoringActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                ServiceName = request.ServiceName,
                Action = "Stop",
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for stopping monitoring");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping monitoring for blockchain {BlockchainType}", blockchainType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while stopping monitoring");
        }
    }
}

// Request/Response Models

public class SystemHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<ServiceHealthInfo> Services { get; set; } = new();
    public SystemMetrics SystemMetrics { get; set; } = new();
}

public class ServiceHealthInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
    public string? Error { get; set; }
}

public class SystemMetrics
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public TimeSpan NetworkLatency { get; set; }
}

public class ServiceMetricsResponse
{
    public string ServiceName { get; set; } = string.Empty;
    public int TimeRange { get; set; }
    public DateTime Timestamp { get; set; }
    public List<MetricData> Metrics { get; set; } = new();
    public MetricsSummary Summary { get; set; } = new();
}

public class MetricData
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class MetricsSummary
{
    public long RequestCount { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public long ErrorCount { get; set; }
}

public class RecordMetricRequest
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class RecordMetricResponse
{
    public bool Success { get; set; }
    public string MetricId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PerformanceStatisticsResponse
{
    public string ServiceName { get; set; } = string.Empty;
    public int TimeRange { get; set; }
    public DateTime Timestamp { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public double UptimePercentage { get; set; }
}

public class CreateAlertRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public List<string> NotificationChannels { get; set; } = new();
}

public class AlertRuleResponse
{
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ActiveAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public class SystemLogsResponse
{
    public List<LogEntry> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int RetrievedCount { get; set; }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class StartMonitoringRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class StopMonitoringRequest
{
    public string ServiceName { get; set; } = string.Empty;
}

public class MonitoringActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}