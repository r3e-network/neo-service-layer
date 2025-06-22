using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Monitoring.Models;

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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetSystemHealthAsync(
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.GetSystemHealthAsync(blockchainType);

            var response = new
            {
                Status = result.OverallStatus.ToString(),
                Timestamp = result.LastHealthCheck,
                Services = result.ServiceStatuses.Select(s => new
                {
                    Name = s.ServiceName,
                    Status = s.Status.ToString(),
                    ResponseTime = TimeSpan.FromMilliseconds(s.ResponseTimeMs),
                    LastChecked = s.LastCheck,
                    Error = s.ErrorMessage
                }).ToList(),
                SystemMetrics = new
                {
                    CpuUsage = 0.0, // SystemMetrics not available in current SystemHealthResult
                    MemoryUsage = 0.0, // SystemMetrics not available in current SystemHealthResult
                    DiskUsage = 0.0, // SystemMetrics not available in current SystemHealthResult
                    NetworkLatency = TimeSpan.Zero // SystemMetrics not available in current SystemHealthResult
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetServiceMetricsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? serviceName = null,
        [FromQuery] int timeRange = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ServiceMetricsRequest { ServiceName = serviceName ?? "All Services" };
            var result = await _monitoringService.GetServiceMetricsAsync(request, blockchainType);

            var response = new
            {
                ServiceName = serviceName ?? "All Services",
                TimeRange = timeRange,
                Timestamp = DateTime.UtcNow,
                Metrics = result.Metrics.Select(m => new
                {
                    Name = m.Name,
                    Value = m.Value,
                    Unit = m.Unit,
                    Timestamp = m.Timestamp,
                    Tags = m.Metadata
                }).ToList(),
                Summary = result.Metadata
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> RecordMetricAsync(
        [FromBody] dynamic request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var serviceRequest = new Services.Monitoring.RecordMetricRequest
            {
                MetricName = request.Name,
                Value = request.Value,
                Unit = request.Unit,
                ServiceName = "MonitoringController",
                Tags = request.Tags ?? new Dictionary<string, string>()
            };
            var result = await _monitoringService.RecordMetricAsync(serviceRequest, blockchainType);

            var response = new
            {
                Success = result.Success,
                MetricId = result.MetricId,
                Timestamp = result.RecordedAt,
                Message = result.ErrorMessage ?? "Metric recorded successfully"
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetPerformanceStatisticsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? serviceName = null,
        [FromQuery] int timeRange = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new Services.Monitoring.Models.PerformanceStatisticsRequest
            {
                // PerformanceStatisticsRequest doesn't have ServiceName/TimeRange properties
                // Just use empty request
            };
            var result = await _monitoringService.GetPerformanceStatisticsAsync(request, blockchainType);

            var response = new
            {
                ServiceName = serviceName ?? "All Services",
                TimeRange = timeRange,
                Timestamp = DateTime.UtcNow,
                // Return the service result directly as it contains the statistics
                Statistics = result
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
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> CreateAlertRuleAsync(
        [FromBody] dynamic request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var alertRequest = new Services.Monitoring.Models.CreateAlertRuleRequest
            {
                RuleName = request.Name,
                ServiceName = "All Services",
                MetricName = request.MetricName,
                Condition = Services.Monitoring.Models.AlertCondition.GreaterThan, // Default condition
                Threshold = request.Threshold,
                Severity = Services.Monitoring.Models.AlertSeverity.Warning,
                NotificationChannels = request.NotificationChannels?.ToObject<string[]>() ?? Array.Empty<string>()
            };
            var result = await _monitoringService.CreateAlertRuleAsync(alertRequest, blockchainType);

            var response = new
            {
                RuleId = result.RuleId,
                Name = request.Name,
                Status = result.Success ? "Active" : "Failed",
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
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<object>>> GetActiveAlertsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? severity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new Services.Monitoring.Models.GetAlertsRequest
            {
                Severity = string.IsNullOrEmpty(severity) ? null : Enum.Parse<Services.Monitoring.Models.AlertSeverity>(severity, true)
            };
            var alertsResult = await _monitoringService.GetActiveAlertsAsync(request, blockchainType);

            var response = alertsResult.Alerts?.Select(alert => new
            {
                AlertId = alert.AlertId,
                RuleName = alert.ServiceName + "_" + alert.MetricName,
                Severity = alert.Severity.ToString(),
                Message = alert.Message,
                TriggeredAt = alert.TriggeredAt,
                Status = "Active",
                AcknowledgedBy = (string?)null,
                AcknowledgedAt = (DateTime?)null
            }) ?? Enumerable.Empty<object>();

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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetSystemLogsAsync(
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] string? level = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new Services.Monitoring.Models.GetLogsRequest
            {
                ServiceName = "All",
                Limit = limit
            };
            var logsResult = await _monitoringService.GetLogsAsync(request, blockchainType);

            var response = new
            {
                Logs = (logsResult.LogEntries ?? Array.Empty<Services.Monitoring.Models.LogEntry>()).Select(log => new
                {
                    Timestamp = log.Timestamp,
                    Level = log.Level.ToString(),
                    Message = log.Message,
                    Source = log.ServiceName,
                    Exception = log.Exception,
                    Properties = log.Metadata
                }).ToList(),
                TotalCount = logsResult.TotalCount,
                RetrievedCount = logsResult.LogEntries?.Length ?? 0
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> StartMonitoringAsync(
        [FromBody] dynamic request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var monitoringRequest = new Services.Monitoring.Models.StartMonitoringRequest
            {
                ServiceName = request.ServiceName
            };
            var result = await _monitoringService.StartMonitoringAsync(monitoringRequest, blockchainType);

            var response = new
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "Monitoring started successfully",
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> StopMonitoringAsync(
        [FromBody] dynamic request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var stopRequest = new Services.Monitoring.Models.StopMonitoringRequest
            {
                ServiceName = request.ServiceName
            };
            var result = await _monitoringService.StopMonitoringAsync(stopRequest, blockchainType);

            var response = new
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "Monitoring stopped successfully",
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
